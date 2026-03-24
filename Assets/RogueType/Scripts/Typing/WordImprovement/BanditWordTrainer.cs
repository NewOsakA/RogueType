using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WordLengthBucket { Short, Medium, Long }

public enum TrainingPattern
{
    Balanced,
    TargetedWeakness,
    ConsistencyTrainer,
    SpeedTrainer
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

    [Header("UCB Exploit")]
    public bool useUCBExploit = true;
    [Range(0.05f, 2f)] public float baseC = 0.3f;
    public bool autoC = true;
    [Range(0.05f, 2f)] public float autoCMin = 0.15f;
    [Range(0.05f, 4f)] public float autoCMax = 1.5f;
    [Range(0.01f, 0.5f)] public float rewardStatAlpha = 0.15f;

    [Header("Reward Fairness Thresholds")]
    [Tooltip("Minimum mistakes in weakest zone before TargetedWeakness can be rewarded/penalized.")]
    public int minTargetZoneMistakes = 3;

    [Tooltip("Minimum total mistakes before ConsistencyTrainer can be rewarded/penalized.")]
    public int minConsistencyTotalMistakes = 6;

    [Tooltip("Minimum total mistakes before Balanced compares mistake ratio more confidently.")]
    public int minBalancedTotalMistakes = 5;

    private readonly Dictionary<TrainingPattern, float> avgReward = new();
    private readonly Dictionary<TrainingPattern, int> trials = new();

    private TrainingPattern currentPattern = TrainingPattern.Balanced;
    private FingerZone currentWeakestZone;
    private FingerZone currentSecondZone;

    private TrainingPattern lastChosenPattern;
    private FingerZone lastWeakestZone;
    private FingerZone lastSecondZone;

    private bool hasLastDecision = false;
    private bool lastDecisionForcedByStress = false;

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
        float accPrev,
        float accNow,
        float wpmPrev,
        float wpmNow,
        Dictionary<FingerZone, int> mPrev,
        Dictionary<FingerZone, int> mNow,
        bool stressHigh)
    {
        if (hasLastDecision)
        {
            bool canLearn = silentLearningBeforeApply || waveNumber >= applyFromWave;

            if (canLearn && !lastDecisionForcedByStress)
            {
                float reward = ComputeReward(
                    lastChosenPattern,
                    accPrev, accNow,
                    wpmPrev, wpmNow,
                    mPrev, mNow,
                    lastWeakestZone
                );

                UpdateRewardStats(reward);
                UpdateBandit(lastChosenPattern, reward);

                DebugPrintPolicyScores($"after Wave {waveNumber} | C={GetCurrentC():F2}");
            }
        }

        lastDecisionForcedByStress = false;

        ChoosePolicyForNextWave(waveNumber, mNow, stressHigh);
    }

    void ChoosePolicyForNextWave(
        int wave,
        Dictionary<FingerZone, int> mistakes,
        bool stressHigh)
    {
        (FingerZone z1, FingerZone z2) = GetTop2Zones(mistakes);

        if (stressHigh)
        {
            ApplyRecoveryMode(z1, z2);
            return;
        }

        if (forceTryAllPoliciesOnce)
        {
            var untried = trials.Where(x => x.Value == 0)
                                .Select(x => x.Key)
                                .ToList();

            if (untried.Count > 0)
            {
                var chosen = untried[UnityEngine.Random.Range(0, untried.Count)];
                ApplyNextPolicy(chosen, z1, z2, false);
                return;
            }
        }

        float eps = wave <= earlyEpsilonWaves
            ? Mathf.Max(epsilon, earlyEpsilon)
            : epsilon;

        bool explore = UnityEngine.Random.value < eps;

        TrainingPattern pick = explore
            ? RandomPattern()
            : useUCBExploit ? SelectBestByUCB()
                            : avgReward.OrderByDescending(x => x.Value).First().Key;

        ApplyNextPolicy(pick, z1, z2, false);
    }

    void ApplyNextPolicy(
        TrainingPattern chosen,
        FingerZone z1,
        FingerZone z2,
        bool forced)
    {
        currentPattern = chosen;
        currentWeakestZone = z1;
        currentSecondZone = z2;

        lastChosenPattern = chosen;
        lastWeakestZone = z1;
        lastSecondZone = z2;

        lastDecisionForcedByStress = forced;
        hasLastDecision = true;

        var len = GetLengthMixForPattern(chosen);

        Debug.Log(
            $"[Bandit] NextWavePolicy\n" +
            $" - chosen : {chosen}\n" +
            $" - zones  : Z1={z1}, Z2={z2}\n" +
            $" - exploitMode : {(useUCBExploit ? "UCB" : "AVG")}\n" +
            $" - C(auto) : {GetCurrentC():F2}\n" +
            $" - LengthMix : S={len.s:F2} M={len.m:F2} L={len.l:F2}"
        );
    }

    void ApplyRecoveryMode(FingerZone z1, FingerZone z2)
    {
        currentPattern = TrainingPattern.Balanced;
        currentWeakestZone = z1;
        currentSecondZone = z2;

        lastDecisionForcedByStress = true;
        hasLastDecision = false;

        Debug.Log("[Bandit] Recovery Mode");
    }

    TrainingPattern SelectBestByUCB()
    {
        float C = GetCurrentC();
        int total = trials.Values.Sum();

        TrainingPattern best = TrainingPattern.Balanced;
        float bestScore = float.NegativeInfinity;

        string scoreLine = "";

        foreach (TrainingPattern p in Enum.GetValues(typeof(TrainingPattern)))
        {
            int n = trials[p];
            float avg = avgReward[p];

            float bonus = C * Mathf.Sqrt(Mathf.Log(total + 1) / (n + 1));
            float score = avg + bonus;

            scoreLine += $"{p}:{score:F2} | ";

            if (score > bestScore)
            {
                bestScore = score;
                best = p;
            }
        }

        Debug.Log(
            $"[Bandit-UCB] total={total} | C={C:F2}\n" +
            $"{scoreLine}\n" +
            $"Best: {best}"
        );

        return best;
    }

    float GetCurrentC()
    {
        if (!autoC) return baseC;

        float std = Mathf.Sqrt(Mathf.Max(0.0001f, rewardVar));
        float c = baseC * (1f + std);

        return Mathf.Clamp(c, autoCMin, autoCMax);
    }

    float ComputeReward(
        TrainingPattern pattern,
        float accPrev,
        float accNow,
        float wpmPrev,
        float wpmNow,
        Dictionary<FingerZone, int> mPrev,
        Dictionary<FingerZone, int> mNow,
        FingerZone weakestZone)
    {
        float reward = 0f;

        float accDelta = accNow - accPrev;
        float wpmDelta = wpmNow - wpmPrev;

        int totalPrev = Mathf.Max(1, mPrev.Values.Sum());
        int totalNow = mNow.Values.Sum();

        switch (pattern)
        {
            case TrainingPattern.Balanced:
            {
                if (totalPrev < minBalancedTotalMistakes && totalNow < minBalancedTotalMistakes)
                {
                    if (accDelta > 0.02f)
                        reward = +1f;
                    else if (accDelta < -0.03f)
                        reward = -1f;
                    else
                        reward = 0f;

                    break;
                }

                float totalRatio = (float)totalNow / Mathf.Max(totalPrev, minBalancedTotalMistakes);

                if (accDelta > 0.015f && totalRatio < 0.95f)
                    reward = +1f;
                else if (accDelta < -0.03f || totalRatio > 1.15f)
                    reward = -1f;
                else
                    reward = 0f;

                break;
            }

            case TrainingPattern.TargetedWeakness:
            {
                int prev = mPrev.ContainsKey(weakestZone) ? mPrev[weakestZone] : 0;
                int now = mNow.ContainsKey(weakestZone) ? mNow[weakestZone] : 0;

                if (prev < minTargetZoneMistakes)
                    return 0f;

                float ratio = (float)now / Mathf.Max(1, prev);

                if (ratio < 0.9f && accDelta >= -0.01f)
                    reward = +1f;
                else if (ratio > 1.25f)
                    reward = -1f;
                else
                    reward = 0f;

                break;
            }

            case TrainingPattern.ConsistencyTrainer:
            {
                if (totalPrev < minConsistencyTotalMistakes && totalNow < minConsistencyTotalMistakes)
                    return 0f;

                float stdPrev = ZoneStd(mPrev);
                float stdNow = ZoneStd(mNow);

                float stdRatio = stdPrev > 0.0001f
                    ? stdNow / stdPrev
                    : 1f;

                if (stdRatio < 0.75f && accDelta >= -0.01f)
                    reward = +1f;
                else if (stdRatio > 1.25f)
                    reward = -1f;
                else
                    reward = 0f;

                break;
            }

            case TrainingPattern.SpeedTrainer:
            {
                if (wpmDelta > 2.5f && accDelta > -0.02f)
                    reward = +1f;
                else if (wpmDelta < -2.5f || accDelta < -0.05f)
                    reward = -1f;
                else
                    reward = 0f;

                break;
            }
        }
        return reward;
    }

    float ZoneStd(Dictionary<FingerZone, int> m)
    {
        if (m == null || m.Count == 0)
            return 0f;

        float mean = (float)m.Values.Average();
        float var = 0f;

        foreach (var v in m.Values)
        {
            float d = v - mean;
            var += d * d;
        }

        return Mathf.Sqrt(var / m.Count);
    }

    void UpdateBandit(TrainingPattern p, float r)
    {
        if (trials[p] == 0 && r < 0f)
            r = 0f;

        trials[p]++;
        avgReward[p] += (r - avgReward[p]) / trials[p];

        // Prevent policy to get to much negative score 
        avgReward[p] = Mathf.Max(avgReward[p], -0.3f);
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

    public (TrainingPattern pattern,
            FingerZone weakest,
            FingerZone second,
            float[] zoneWeights,
            (float s, float m, float l) lenMix)
        GetCurrentPolicy()
    {
        float[] zoneWeights =
            lastDecisionForcedByStress
            ? BuildRecoveryWeights(currentWeakestZone, currentSecondZone)
            : BuildZoneWeights(currentPattern, currentWeakestZone, currentSecondZone);

        var lenMix = GetLengthMixForPattern(currentPattern);

        return (currentPattern,
                currentWeakestZone,
                currentSecondZone,
                zoneWeights,
                lenMix);
    }

    float[] BuildZoneWeights(
        TrainingPattern pattern,
        FingerZone z1,
        FingerZone z2)
    {
        float[] w = new float[8];

        switch (pattern)
        {
            case TrainingPattern.Balanced:
                for (int i = 0; i < 8; i++) w[i] = 1f / 8f;
                break;

            case TrainingPattern.TargetedWeakness:
                for (int i = 0; i < 8; i++) w[i] = (1f - 0.45f) / 7f;
                w[(int)z1] = 0.45f;
                break;

            case TrainingPattern.ConsistencyTrainer:
                for (int i = 0; i < 8; i++) w[i] = 1f / 8f;
                w[(int)z1] *= 0.8f;
                w[(int)z2] *= 0.85f;
                break;

            case TrainingPattern.SpeedTrainer:
                for (int i = 0; i < 8; i++) w[i] = 1f / 8f;
                break;
        }

        float sum = w.Sum();
        for (int i = 0; i < 8; i++) w[i] /= sum;

        return w;
    }

    (float s, float m, float l) GetLengthMixForPattern(TrainingPattern p)
    {
        switch (p)
        {
            case TrainingPattern.Balanced:
                return (0.4f, 0.4f, 0.2f);

            case TrainingPattern.TargetedWeakness:
                return (0.55f, 0.3f, 0.15f);

            case TrainingPattern.ConsistencyTrainer:
                return (0.25f, 0.45f, 0.3f);

            case TrainingPattern.SpeedTrainer:
                return (0.65f, 0.25f, 0.1f);

            default:
                return (0.4f, 0.4f, 0.2f);
        }
    }

    TrainingPattern RandomPattern()
    {
        var all = (TrainingPattern[])Enum.GetValues(typeof(TrainingPattern));
        return all[UnityEngine.Random.Range(0, all.Length)];
    }

    (FingerZone, FingerZone) GetTop2Zones(Dictionary<FingerZone, int> mistakes)
    {
        var ordered = mistakes.OrderByDescending(x => x.Value).ToList();
        return (ordered[0].Key,
                ordered.Count > 1 ? ordered[1].Key : ordered[0].Key);
    }

    public bool IsWarmupPhase(int wave) => wave < applyFromWave;

    public bool ShouldApplyPolicy(int wave)
    {
        return wave >= applyFromWave;
    }

    public static bool IsStressHigh(
        float accPrev, float accNow,
        int mistakesPrev, int mistakesNow,
        float wpmPrev, float wpmNow)
    {
        float accDrop = Mathf.Max(0f, accPrev - accNow);
        float normAcc = Mathf.Clamp01(accDrop / 0.15f);

        int deltaMist = mistakesNow - mistakesPrev;
        float normMist = deltaMist > 0
            ? Mathf.Clamp01(deltaMist / 12f)
            : 0f;

        float wpmDrop = Mathf.Max(0f, wpmPrev - wpmNow);
        float normWpm = Mathf.Clamp01(wpmDrop / 15f);

        float stressScore =
            0.6f * normAcc +
            0.3f * normMist +
            0.1f * normWpm;

        return stressScore >= 0.7f;
    }

    float[] BuildRecoveryWeights(FingerZone z1, FingerZone z2)
    {
        float[] w = new float[8];

        w[(int)z1] = 0.1f;
        w[(int)z2] = 0.15f;

        float remain = 1f - (w[(int)z1] + w[(int)z2]);
        float each = remain / 6f;

        for (int i = 0; i < 8; i++)
        {
            if (i != (int)z1 && i != (int)z2)
                w[i] = each;
        }

        return w;
    }

    void DebugPrintPolicyScores(string header = "")
    {
        string msg = "[Bandit Scores]";
        if (!string.IsNullOrEmpty(header))
            msg += $" {header}";

        foreach (var p in Enum.GetValues(typeof(TrainingPattern)))
        {
            var pattern = (TrainingPattern)p;
            msg += $"\n - {pattern,-20} | avg:{avgReward[pattern],6:F2} | trials:{trials[pattern]}";
        }

        Debug.Log(msg);
    }
}
