using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WordLengthBucket { Short, Medium, Long }

public enum TrainingPattern
{
    Balanced,
    FocusWeakest25,
    FocusWeakest40,
    FocusTop2,
    AntiStreak
}

public class BanditWordTrainer : MonoBehaviour
{
    public static BanditWordTrainer Instance;

    [Header("Learning Settings")]
    [Range(0f, 0.5f)] public float epsilon = 0.2f;
    public int applyFromWave = 5;
    public bool silentLearningBeforeApply = true;

    [Header("Early Exploration")]
    [Range(0f, 0.9f)] public float earlyEpsilon = 0.45f;
    public int earlyEpsilonWaves = 4;

    [Header("Try Each Policy Once")]
    public bool forceTryAllPoliciesOnce = true;

    [Header("Word Length Mix (avoid length bias)")]
    public float shortRatio = 0.50f;
    public float mediumRatio = 0.30f;
    public float longRatio = 0.20f;

    // Upper Confidence Bound Exploit Settings
    [Header("Upper Confidence Bound Exploit (avg + confidence bonus)")]
    [Tooltip("Use UCB when exploiting (instead of avgReward only).")]
    public bool useUCBExploit = true;

    [Tooltip("If Auto C is ON, this is just a fallback.")]
    [Range(0.05f, 2.0f)] public float baseC = 0.30f;

    [Tooltip("Automatically adapt C from reward noise (std dev).")]
    public bool autoC = true;

    [Tooltip("Clamp for auto C (min).")]
    [Range(0.05f, 2.0f)] public float autoCMin = 0.15f;

    [Tooltip("Clamp for auto C (max).")]
    [Range(0.05f, 4.0f)] public float autoCMax = 1.50f;

    [Tooltip("How quickly auto C reacts (higher = faster).")]
    [Range(0.01f, 0.5f)] public float rewardStatAlpha = 0.15f;

    // bandit score
    private readonly Dictionary<TrainingPattern, float> avgReward = new();
    private readonly Dictionary<TrainingPattern, int> trials = new();

    private TrainingPattern lastChosenPattern = TrainingPattern.Balanced;
    private FingerZone lastWeakestZone;
    private FingerZone lastSecondZone;
    private bool hasLastDecision = false;

    private bool lastDecisionForcedByStress = false;

    private TrainingPattern currentPattern = TrainingPattern.Balanced;
    private FingerZone currentWeakestZone;
    private FingerZone currentSecondZone;

    private bool rewardStatInit = false;
    private float rewardMean = 0f;
    private float rewardVar = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        foreach (TrainingPattern p in Enum.GetValues(typeof(TrainingPattern)))
        {
            avgReward[p] = 0f;
            trials[p] = 0;
        }
    }

    public void OnWaveEnded(
        int waveNumber,
        float accuracyPrevWave,
        float accuracyThisWave,
        Dictionary<FingerZone, int> mistakesThisWave,
        Dictionary<FingerZone, int> mistakesPrevWave,
        bool stressHigh
    )
    {
        // Update reward for the pattern used in This wave
        if (hasLastDecision)
        {
            bool canLearn = silentLearningBeforeApply || waveNumber >= applyFromWave;

            if (canLearn && !lastDecisionForcedByStress)
            {
                float reward = ComputeReward(
                    lastChosenPattern,
                    accuracyPrevWave, accuracyThisWave,
                    mistakesPrevWave, mistakesThisWave,
                    lastWeakestZone
                );

                // update reward noise stats for auto C
                UpdateRewardStats(reward);

                UpdateBandit(lastChosenPattern, reward);
                DebugPrintPolicyScores($"after Wave {waveNumber} | C={GetCurrentC():F2}");
            }
        }

        // Choose pattern for Next wave
        ChoosePolicyForNextWave(waveNumber, mistakesThisWave, stressHigh);
    }

    public bool ShouldApplyPolicy(int waveNumber) => waveNumber >= applyFromWave;
    public bool IsWarmupPhase(int wave) => wave < applyFromWave;

    public (TrainingPattern pattern, FingerZone weakest, FingerZone second, float[] zoneWeights, (float s, float m, float l) lenMix)
        GetCurrentPolicy()
    {
        float[] w = BuildZoneWeights(currentPattern, currentWeakestZone, currentSecondZone);
        return (currentPattern, currentWeakestZone, currentSecondZone, w, (shortRatio, mediumRatio, longRatio));
    }

    void UpdateBandit(TrainingPattern pattern, float reward)
    {
        trials[pattern] += 1;
        int n = trials[pattern];
        avgReward[pattern] = avgReward[pattern] + (reward - avgReward[pattern]) / n;
    }

    void ChoosePolicyForNextWave(int waveNumber, Dictionary<FingerZone, int> mistakesThisWave, bool stressHigh)
    {
        (FingerZone z1, FingerZone z2) = GetTop2Zones(mistakesThisWave);

        TrainingPattern chosen;

        if (stressHigh)
        {
            chosen = TrainingPattern.FocusWeakest25;
            ApplyNextPolicy(chosen, z1, z2, true);
            return;
        }

        // A) Force try each policy at least once
        if (forceTryAllPoliciesOnce)
        {
            var untried = trials.Where(kv => kv.Value == 0).Select(kv => kv.Key).ToList();
            if (untried.Count > 0)
            {
                chosen = untried[UnityEngine.Random.Range(0, untried.Count)];
                ApplyNextPolicy(chosen, z1, z2, false);
                return;
            }
        }

        // B) epsilon greedy explore/exploit
        float eps = GetEpsilonForWave(waveNumber);
        bool explore = UnityEngine.Random.value < eps;

        if (explore)
        {
            chosen = RandomPattern();
        }
        else
        {
            // exploit with Upper Confidence Bound if enabled
            chosen = useUCBExploit ? SelectBestByUCB() : avgReward.OrderByDescending(kv => kv.Value).First().Key;
        }

        ApplyNextPolicy(chosen, z1, z2, false);
    }

    float GetEpsilonForWave(int waveNumber)
    {
        if (waveNumber <= earlyEpsilonWaves) return Mathf.Max(epsilon, earlyEpsilon);
        return epsilon;
    }

    void ApplyNextPolicy(TrainingPattern chosen, FingerZone z1, FingerZone z2, bool forcedByStress)
    {
        currentPattern = chosen;
        currentWeakestZone = z1;
        currentSecondZone = z2;

        lastChosenPattern = chosen;
        lastWeakestZone = z1;
        lastSecondZone = z2;

        lastDecisionForcedByStress = forcedByStress;
        hasLastDecision = true;

        var best = avgReward.OrderByDescending(kv => kv.Value).First();
        Debug.Log(
            $"[Bandit] NextWavePolicy\n" +
            $" - chosen : {chosen}\n" +
            $" - zones  : Z1={z1}, Z2={z2}\n" +
            $" - eps    : {GetEpsilonForWave(GameManager.Instance != null ? GameManager.Instance.currentWave : 0):F2}\n" +
            $" - forced : {forcedByStress}\n" +
            $" - bestAvgOnly : {best.Key} ({best.Value:F2})\n" +
            $" - exploitMode : {(useUCBExploit ? "UCB" : "AVG")}\n" +
            $" - C(auto) : {GetCurrentC():F2}"
        );
    }

    // Upper Confidence Bound selection
    TrainingPattern SelectBestByUCB()
    {
        float C = GetCurrentC();
        int total = trials.Values.Sum();

        TrainingPattern bestPolicy = TrainingPattern.Balanced;
        float bestScore = float.NegativeInfinity;

        foreach (TrainingPattern p in Enum.GetValues(typeof(TrainingPattern)))
        {
            int n = trials[p];
            float avg = avgReward[p];

            // UCB bonus (avoid div by zero)
            float bonus = C * Mathf.Sqrt(Mathf.Log(total + 1) / (n + 1));
            float score = avg + bonus;

            if (score > bestScore)
            {
                bestScore = score;
                bestPolicy = p;
            }
        }

        Debug.Log($"[Bandit-UCB] totalTrials={total} | C={C:F2} | best={bestPolicy} | score={bestScore:F2}");
        return bestPolicy;
    }
    void UpdateRewardStats(float r)
    {
        if (!rewardStatInit)
        {
            rewardStatInit = true;
            rewardMean = r;
            rewardVar = 0f;
            return;
        }

        // exponential moving average
        float delta = r - rewardMean;
        rewardMean += rewardStatAlpha * delta;

        // exponential moving variance (approx)
        rewardVar = (1f - rewardStatAlpha) * (rewardVar + rewardStatAlpha * delta * delta);
    }

    float GetCurrentC()
    {
        if (!autoC) return baseC;
        // std dev from var
        float std = Mathf.Sqrt(Mathf.Max(0.0001f, rewardVar));

        // Map noise for C
        // If rewards are noisy (std high) => explore more (higher C)
        // If rewards stable (std low) => explore less (lower C)
        float c = baseC * (1f + std);
        return Mathf.Clamp(c, autoCMin, autoCMax);
    }

    TrainingPattern RandomPattern()
    {
        TrainingPattern[] all = (TrainingPattern[])Enum.GetValues(typeof(TrainingPattern));
        return all[UnityEngine.Random.Range(0, all.Length)];
    }

    (FingerZone, FingerZone) GetTop2Zones(Dictionary<FingerZone, int> mistakes)
    {
        var ordered = mistakes.OrderByDescending(kv => kv.Value).ToList();
        FingerZone z1 = ordered[0].Key;
        FingerZone z2 = ordered.Count > 1 ? ordered[1].Key : ordered[0].Key;
        return (z1, z2);
    }

    float ComputeReward(
        TrainingPattern pattern,
        float accPrev, float accNow,
        Dictionary<FingerZone, int> mPrev,
        Dictionary<FingerZone, int> mNow,
        FingerZone focusedZone
    )
    {
        float r = 0f;
        float accDelta = accNow - accPrev;

        if (accDelta > 0.01f)
            r += 1f;
        else if (accDelta < -0.03f)
            r -= 1f;
        if (pattern == TrainingPattern.Balanced || pattern == TrainingPattern.AntiStreak)
        {
            int prevTotal = mPrev.Values.Sum();
            int nowTotal  = mNow.Values.Sum();

            // More tolerance allow because this policy checks all zones
            int tolerance = (pattern == TrainingPattern.Balanced) ? 6 : 4;

            if (nowTotal < prevTotal)
                r += 1f;
            else if (nowTotal > prevTotal + tolerance)
                r -= 1f;
        }
        else
        {
            // evaluate only the targeted zone
            int prevMist = mPrev.TryGetValue(focusedZone, out var pm) ? pm : 0;
            int nowMist  = mNow.TryGetValue(focusedZone, out var nm) ? nm : 0;

            if (nowMist < prevMist)
                r += 1f;
            else if (nowMist > prevMist + 2)
                r -= 1f;
        }

        return r;
    }

    float[] BuildZoneWeights(TrainingPattern pattern, FingerZone z1, FingerZone z2)
    {
        float[] w = new float[8];

        void SetAll(float val)
        {
            for (int i = 0; i < w.Length; i++) w[i] = val;
        }

        int i1 = (int)z1;
        int i2 = (int)z2;

        switch (pattern)
        {
            case TrainingPattern.Balanced:
                SetAll(1f / 8f);
                break;

            case TrainingPattern.FocusWeakest25:
                SetAll((1f - 0.25f) / 7f);
                w[i1] = 0.25f;
                break;

            case TrainingPattern.FocusWeakest40:
                SetAll((1f - 0.40f) / 7f);
                w[i1] = 0.40f;
                break;

            case TrainingPattern.FocusTop2:
                SetAll((1f - 0.50f) / 6f);
                w[i1] = 0.30f;
                w[i2] = 0.20f;
                break;

            case TrainingPattern.AntiStreak:
                SetAll(0f);
                w[i1] = 0.05f;
                w[i2] = 0.10f;
                float remain = 1f - (w[i1] + w[i2]);
                float each = remain / 6f;
                for (int i = 0; i < w.Length; i++)
                {
                    if (i == i1 || i == i2) continue;
                    w[i] = each;
                }
                break;
        }

        float sum = w.Sum();
        if (sum <= 0f) return Enumerable.Repeat(1f / 8f, 8).ToArray();
        for (int i = 0; i < w.Length; i++) w[i] /= sum;

        return w;
    }

    public static bool IsStressHigh(float accPrev, float accNow, int mistakesPrev, int mistakesNow)
    {
        if (accNow < accPrev - 0.08f) return true;
        if (mistakesNow > mistakesPrev + 8) return true;
        return false;
    }

    void DebugPrintPolicyScores(string header = "")
    {
        string msg = "[Bandit Scores]";
        if (!string.IsNullOrEmpty(header))
            msg += $" {header}";

        foreach (var p in Enum.GetValues(typeof(TrainingPattern)))
        {
            var pattern = (TrainingPattern)p;
            float avg = avgReward[pattern];
            int n = trials[pattern];

            msg += $"\n - {pattern,-15} | avg:{avg,5:F2} | trials:{n}";
        }

        Debug.Log(msg);
    }
}
