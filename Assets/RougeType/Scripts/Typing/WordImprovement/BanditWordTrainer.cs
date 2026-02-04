using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WordLengthBucket { Short, Medium, Long }

public enum TrainingPattern
{
    Balanced,
    FocusWeakest,
    FocusTop2,
    AntiStreak,
    ConsistencyTrainer
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

    [Header("Upper Confidence Bound Exploit (avg + confidence bonus)")]
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

    // last decision (used to score reward when wave ends)
    private TrainingPattern lastChosenPattern = TrainingPattern.Balanced;
    private FingerZone lastWeakestZone;
    private FingerZone lastSecondZone;
    private bool hasLastDecision = false;

    // if decision was forced by stress, don't train from it
    private bool lastDecisionForcedByStress = false;

    // current policy used for generating words in the next wave
    private TrainingPattern currentPattern = TrainingPattern.Balanced;
    private FingerZone currentWeakestZone;
    private FingerZone currentSecondZone;

    // reward noise stats for auto C
    private bool rewardStatInit = false;
    private float rewardMean = 0f;
    private float rewardVar = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

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
        // 1) Update reward for the pattern used in THIS wave
        if (hasLastDecision)
        {
            bool canLearn = silentLearningBeforeApply || waveNumber >= applyFromWave;

            if (canLearn && !lastDecisionForcedByStress)
            {
                float reward = ComputeReward(
                    lastChosenPattern,
                    accuracyPrevWave, accuracyThisWave,
                    mistakesPrevWave, mistakesThisWave,
                    lastWeakestZone,
                    lastSecondZone
                );

                UpdateRewardStats(reward);
                UpdateBandit(lastChosenPattern, reward);
                DebugPrintPolicyScores($"after Wave {waveNumber} | C={GetCurrentC():F2}");
            }
        }

        // 2) Choose policy for NEXT wave
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

        // Use AntiStreak(override) if stress high
        if (stressHigh)
        {
            ApplyNextPolicy(TrainingPattern.AntiStreak, z1, z2, true, waveNumber);
            return;
        }

        // Force try each policy at least once to prevents early lock
        if (forceTryAllPoliciesOnce)
        {
            var untried = trials.Where(kv => kv.Value == 0).Select(kv => kv.Key).ToList();
            if (untried.Count > 0)
            {
                var chosen = untried[UnityEngine.Random.Range(0, untried.Count)];
                ApplyNextPolicy(chosen, z1, z2, false, waveNumber);
                return;
            }
        }

        // epsilon-greedy
        float eps = GetEpsilonForWave(waveNumber);
        bool explore = UnityEngine.Random.value < eps;

        TrainingPattern pick;
        if (explore)
        {
            pick = RandomPattern();
        }
        else
        {
            pick = useUCBExploit ? SelectBestByUCB() : avgReward.OrderByDescending(kv => kv.Value).First().Key;
        }

        ApplyNextPolicy(pick, z1, z2, false, waveNumber);
    }

    float GetEpsilonForWave(int waveNumber)
    {
        if (waveNumber <= earlyEpsilonWaves) return Mathf.Max(epsilon, earlyEpsilon);
        return epsilon;
    }

    void ApplyNextPolicy(TrainingPattern chosen, FingerZone z1, FingerZone z2, bool forcedByStress, int waveNumberForLog)
    {
        currentPattern = chosen;
        currentWeakestZone = z1;
        currentSecondZone = z2;

        lastChosenPattern = chosen;
        lastWeakestZone = z1;
        lastSecondZone = z2;

        lastDecisionForcedByStress = forcedByStress;
        hasLastDecision = true;

        var bestAvg = avgReward.OrderByDescending(kv => kv.Value).First();
        Debug.Log(
            $"[Bandit] NextWavePolicy\n" +
            $" - chosen : {chosen}\n" +
            $" - zones  : Z1={z1}, Z2={z2}\n" +
            $" - eps    : {GetEpsilonForWave(waveNumberForLog):F2}\n" +
            $" - forced : {forcedByStress}\n" +
            $" - bestAvgOnly : {bestAvg.Key} ({bestAvg.Value:F2})\n" +
            $" - exploitMode : {(useUCBExploit ? "UCB" : "AVG")}\n" +
            $" - C(auto) : {GetCurrentC():F2}"
        );
    }

    // UCB
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

        float delta = r - rewardMean;
        rewardMean += rewardStatAlpha * delta;

        rewardVar = (1f - rewardStatAlpha) * (rewardVar + rewardStatAlpha * delta * delta);
    }

    float GetCurrentC()
    {
        if (!autoC) return baseC;
        float std = Mathf.Sqrt(Mathf.Max(0.0001f, rewardVar));
        float c = baseC * (1f + std);
        return Mathf.Clamp(c, autoCMin, autoCMax);
    }

    // Reward
    float ComputeReward(
        TrainingPattern pattern,
        float accPrev, float accNow,
        Dictionary<FingerZone, int> mPrev,
        Dictionary<FingerZone, int> mNow,
        FingerZone z1,
        FingerZone z2
    )
    {
        float r = 0f;

        // (1) accuracy delta
        float accDelta = accNow - accPrev;
        if (accDelta > 0.01f) r += 1f;
        else if (accDelta < -0.03f) r -= 1f;

        // totals
        int prevTotal = mPrev.Values.Sum();
        int nowTotal  = mNow.Values.Sum();

        // (2) mistakes part depends on policy
        switch (pattern)
        {
            case TrainingPattern.Balanced:
            {
                // more tolerance (spread across all zones)
                if (nowTotal < prevTotal) r += 1f;
                else if (nowTotal > prevTotal + 6) r -= 1f;
                break;
            }

            case TrainingPattern.AntiStreak:
            {
                // slightly stricter than Balanced
                if (nowTotal < prevTotal) r += 1f;
                else if (nowTotal > prevTotal + 4) r -= 1f;
                break;
            }

            case TrainingPattern.FocusWeakest:
            {
                int prevMist = mPrev.TryGetValue(z1, out var pm) ? pm : 0;
                int nowMist  = mNow.TryGetValue(z1, out var nm) ? nm : 0;

                if (nowMist < prevMist) r += 1f;
                else if (nowMist > prevMist + 2) r -= 1f;
                break;
            }

            case TrainingPattern.FocusTop2:
            {
                int prev1 = mPrev.TryGetValue(z1, out var p1) ? p1 : 0;
                int now1  = mNow.TryGetValue(z1, out var n1) ? n1 : 0;

                int prev2 = mPrev.TryGetValue(z2, out var p2) ? p2 : 0;
                int now2  = mNow.TryGetValue(z2, out var n2) ? n2 : 0;

                int prevSum = prev1 + prev2;
                int nowSum  = now1 + now2;

                if (nowSum < prevSum) r += 1f;
                else if (nowSum > prevSum + 3) r -= 1f;
                break;
            }

            case TrainingPattern.ConsistencyTrainer:
            {
                // reward: error distribution becomes more stable
                float stdPrev = ZoneStd(mPrev);
                float stdNow  = ZoneStd(mNow);

                if (stdNow < stdPrev) r += 1f;
                else if (stdNow > stdPrev + 0.8f) r -= 1f;

                // bonus: accuracy doesn't swing too much
                if (Mathf.Abs(accDelta) < 0.02f) r += 0.5f;
                break;
            }
        }

        return r;
    }

    float ZoneStd(Dictionary<FingerZone, int> m)
    {
        if (m == null || m.Count == 0) return 0f;

        float mean = (float)m.Values.Average();
        float var = 0f;

        foreach (var v in m.Values)
        {
            float d = v - mean;
            var += d * d;
        }

        var /= m.Count;
        return Mathf.Sqrt(Mathf.Max(0f, var));
    }
    // Zone weights
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

            case TrainingPattern.FocusWeakest:
                // moderate focus
                SetAll((1f - 0.33f) / 7f);
                w[i1] = 0.33f;
                break;

            case TrainingPattern.FocusTop2:
                SetAll((1f - 0.50f) / 6f);
                w[i1] = 0.30f;
                w[i2] = 0.20f;
                break;

            case TrainingPattern.AntiStreak:
                // keep most zones active; avoid tunnel on weakest
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

            case TrainingPattern.ConsistencyTrainer:
                // near-uniform, slightly downweight the top weak zones
                SetAll(1f / 8f);
                w[i1] *= 0.70f;
                w[i2] *= 0.85f;
                break;
        }

        // normalize
        float sum = w.Sum();
        if (sum <= 0f) return Enumerable.Repeat(1f / 8f, 8).ToArray();
        for (int i = 0; i < w.Length; i++) w[i] /= sum;

        return w;
    }
    // Helpers
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

    public static bool IsStressHigh(
        float accPrev, float accNow,
        int mistakesPrev, int mistakesNow,
        float wpmPrev, float wpmNow
    )
    {
        bool accDrop = accNow < accPrev - 0.08f;

        int deltaMist = mistakesNow - mistakesPrev;
        bool mistakeSpike =
            (mistakesNow >= 10 && deltaMist >= 8) ||
            (deltaMist >= 12);

        float wpmDelta = wpmNow - wpmPrev;
        bool wpmSpike = wpmDelta >= 8f;

        int signals = 0;
        if (accDrop) signals++;
        if (mistakeSpike) signals++;
        if (wpmSpike) signals++;

        return signals >= 2;
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

            msg += $"\n - {pattern,-18} | avg:{avg,6:F2} | trials:{n}";
        }

        Debug.Log(msg);
    }
}
