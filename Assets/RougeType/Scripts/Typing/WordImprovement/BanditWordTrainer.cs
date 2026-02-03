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
    // Amount of leaning
    [Range(0f, 0.5f)] public float epsilon = 0.2f;
    public int applyFromWave = 5;
    public bool silentLearningBeforeApply = true;

    [Header("Word Length Mix (avoid length bias)")]
    public float shortRatio = 0.50f;
    public float mediumRatio = 0.30f;
    public float longRatio = 0.20f;

    // bandit score simple average reward
    private readonly Dictionary<TrainingPattern, float> avgReward = new();
    private readonly Dictionary<TrainingPattern, int> trials = new();

    // state for previous decision
    private TrainingPattern lastChosenPattern = TrainingPattern.Balanced;
    private FingerZone lastWeakestZone;
    private FingerZone lastSecondZone;
    private bool hasLastDecision = false;

    // Check that last wave is stress or not 'prev wave will not use to train'
    private bool lastDecisionForcedByStress = false;

    // current policy used for next wave word generation
    private TrainingPattern currentPattern = TrainingPattern.Balanced;
    private FingerZone currentWeakestZone;
    private FingerZone currentSecondZone;

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
        // Update reward for the pattern that was chosen For This wave
        if (hasLastDecision)
        {
            if (silentLearningBeforeApply || waveNumber >= applyFromWave)
            {
                if (!lastDecisionForcedByStress)
                {
                    float reward = ComputeReward(
                        lastChosenPattern,
                        accuracyPrevWave, accuracyThisWave,
                        mistakesPrevWave, mistakesThisWave,
                        lastWeakestZone
                    );

                    UpdateBandit(lastChosenPattern, reward);
                }
            }
        }

        // Choose pattern for Next wave based on current wave mistakes
        ChoosePolicyForNextWave(waveNumber, mistakesThisWave, stressHigh);
    }

    public bool ShouldApplyPolicy(int waveNumber)
        => waveNumber >= applyFromWave;

    public bool IsWarmupPhase(int wave)
        => wave < applyFromWave;

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
        }
        else
        {
            bool explore = UnityEngine.Random.value < epsilon;

            if (explore)
            {
                chosen = RandomPattern();
            }
            else
            {
                chosen = avgReward.OrderByDescending(kv => kv.Value).First().Key;
            }
        }

        // Set policy for next wave
        currentPattern = chosen;
        currentWeakestZone = z1;
        currentSecondZone = z2;

        // Save last decision for next reward update
        lastChosenPattern = chosen;
        lastWeakestZone = z1;
        lastSecondZone = z2;

        // if override cause by 'stress' will not use to train
        lastDecisionForcedByStress = stressHigh;
        hasLastDecision = true;

        var best = avgReward.OrderByDescending(kv => kv.Value).First();
        Debug.Log($"[Bandit] NextWavePolicy | Pattern:{chosen} | Z1:{z1} Z2:{z2} | BestAvg:{best.Key}:{best.Value:F2} | Forced:{stressHigh}");
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

        // 1) accuracy delta for rewarding
        // accuracy improves = 'reward' if not 'penalty'
        float accDelta = accNow - accPrev;
        if (accDelta > 0.01f) r += 1f;
        else if (accDelta < -0.03f) r -= 1f;

        // 2) Reward based on typing mistakes
        if (pattern == TrainingPattern.AntiStreak || pattern == TrainingPattern.Balanced)
        {
            // For Anti-Streak / Balanced:
            // Evaluate overall typing stability using total mistakes
            int prevTotal = mPrev.Values.Sum();
            int nowTotal = mNow.Values.Sum();

            if (nowTotal < prevTotal) r += 1f;
            else if (nowTotal > prevTotal + 3) r -= 1f;
        }
        else
        {
            // For focus-based policies:
            // Evaluate improvement only on the targeted finger zone
            int prevMist = mPrev.TryGetValue(focusedZone, out var pm) ? pm : 0;
            int nowMist = mNow.TryGetValue(focusedZone, out var nm) ? nm : 0;

            if (nowMist < prevMist) r += 1f;
            else if (nowMist > prevMist + 2) r -= 1f;
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

        // normalize
        float sum = w.Sum();
        if (sum <= 0f) return Enumerable.Repeat(1f / 8f, 8).ToArray();
        for (int i = 0; i < w.Length; i++) w[i] /= sum;

        return w;
    }

    // use in GameManager to simple detect current stress
    public static bool IsStressHigh(float accPrev, float accNow, int mistakesPrev, int mistakesNow)
    {
        if (accNow < accPrev - 0.08f) return true;
        if (mistakesNow > mistakesPrev + 8) return true;
        return false;
    }
}
