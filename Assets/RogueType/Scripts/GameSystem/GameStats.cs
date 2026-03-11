using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores global game statistics for current run.
/// </summary>
public class GameStats : MonoBehaviour
{
    public static GameStats Instance;

    public float TotalPlayTime;
    public int HighestWave;
    public int CurrentCurrency;
    public int CurrentEssence;
    public float HighestWPM;
    public int Score;
    public float AverageAccuracy;
    public float AverageWPM;
    public string WorstFingerArea = "N/A";

    private GameManager gameManager;
    private TypingManager typingManager;
    private PlayerStats playerStats;
    private float accuracySum;
    private float wpmSum;
    private int typingSampleCount;
    private bool runFinalized;
    private readonly Dictionary<FingerZone, int> fingerMistakes = new Dictionary<FingerZone, int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnEnable()
    {
        ResetRunStats();
    }

    void Update()
    {
        TotalPlayTime += Time.deltaTime;

        if (gameManager == null) gameManager = GameManager.Instance;
        if (typingManager == null && gameManager != null) typingManager = gameManager.typingManager;
        if (playerStats == null && gameManager != null) playerStats = gameManager.playerStats;

        if (gameManager != null)
        {
            HighestWave = Mathf.Max(HighestWave, gameManager.currentWave);
        }

        if (CurrencyManager.Instance != null)
        {
            CurrentCurrency = CurrencyManager.Instance.GetCurrentCurrency();
        }

        if (EssenceManager.Instance != null)
        {
            CurrentEssence = EssenceManager.Instance.GetEssence();
        }

        if (typingManager != null)
        {
            HighestWPM = Mathf.Max(HighestWPM, typingManager.GetWPM());
        }

        if (playerStats != null)
        {
            // "Score" mapped to real typing performance for this run.
            Score = playerStats.totalCorrect;
        }
    }

    public void RecordTypingSnapshot(float wpm, float accuracy, Dictionary<FingerZone, int> zoneMistakes)
    {
        typingSampleCount++;
        wpmSum += Mathf.Max(0f, wpm);
        accuracySum += Mathf.Clamp01(accuracy);

        AverageWPM = typingSampleCount > 0 ? wpmSum / typingSampleCount : 0f;
        AverageAccuracy = typingSampleCount > 0 ? accuracySum / typingSampleCount : 0f;

        if (zoneMistakes == null) return;

        foreach (var pair in zoneMistakes)
        {
            if (!fingerMistakes.ContainsKey(pair.Key))
                fingerMistakes[pair.Key] = 0;

            fingerMistakes[pair.Key] += Mathf.Max(0, pair.Value);
        }

        WorstFingerArea = GetWorstFingerArea();

    }

    private string GetWorstFingerArea()
    {
        FingerZone worstZone = FingerZone.LeftPinky;
        int worstCount = -1;

        foreach (var zone in System.Enum.GetValues(typeof(FingerZone)))
        {
            var z = (FingerZone)zone;
            int count = fingerMistakes.TryGetValue(z, out int c) ? c : 0;
            if (count > worstCount)
            {
                worstCount = count;
                worstZone = z;
            }
        }

        if (worstCount <= 0) return "N/A";
        return ZoneToLabel(worstZone);
    }

    private string ZoneToLabel(FingerZone zone)
    {
        return zone switch
        {
            FingerZone.LeftPinky => "Left Pinky",
            FingerZone.LeftRing => "Left Ring",
            FingerZone.LeftMiddle => "Left Middle",
            FingerZone.LeftIndex => "Left Index",
            FingerZone.RightIndex => "Right Index",
            FingerZone.RightMiddle => "Right Middle",
            FingerZone.RightRing => "Right Ring",
            FingerZone.RightPinky => "Right Pinky",
            _ => zone.ToString()
        };
    }

    public void ResetRunStats()
    {
        TotalPlayTime = 0f;
        HighestWave = 0;
        CurrentCurrency = 0;
        CurrentEssence = 0;
        HighestWPM = 0f;
        Score = 0;
        AverageAccuracy = 0f;
        AverageWPM = 0f;
        WorstFingerArea = "N/A";
        accuracySum = 0f;
        wpmSum = 0f;
        typingSampleCount = 0;
        runFinalized = false;
        fingerMistakes.Clear();
    }

    public void PersistToActiveSlot()
    {
        if (!SaveSlotManager.TryGetActiveSlotIndex(out int slotIndex))
            return;

        SaveSlotData slot = SaveSlotManager.GetSlot(slotIndex);
        if (!slot.hasData)
            slot = SaveSlotData.CreateNew(slotIndex);

        slot.lastRunStats = BuildCurrentRunSnapshot();
        slot.lastPlayedUtc = System.DateTime.UtcNow.ToString("o");

        SaveSlotManager.SetSlot(slotIndex, slot);
    }

    public void FinalizeRunToActiveSlot()
    {
        if (runFinalized)
            return;

        if (!SaveSlotManager.TryGetActiveSlotIndex(out int slotIndex))
            return;

        SaveSlotData slot = SaveSlotManager.GetSlot(slotIndex);
        if (!slot.hasData)
            slot = SaveSlotData.CreateNew(slotIndex);

        if (slot.runHistory == null)
            slot.runHistory = new List<SaveRunStatsData>();

        SaveRunStatsData snapshot = BuildCurrentRunSnapshot();
        slot.lastRunStats = snapshot;
        slot.runHistory.Add(snapshot);
        slot.lastPlayedUtc = System.DateTime.UtcNow.ToString("o");

        SaveSlotManager.SetSlot(slotIndex, slot);
        runFinalized = true;
    }

    private SaveRunStatsData BuildCurrentRunSnapshot()
    {
        return new SaveRunStatsData
        {
            score = Score,
            totalTime = TotalPlayTime,
            highestWave = HighestWave,
            currency = CurrentCurrency,
            highestWPM = HighestWPM,
            averageWPM = AverageWPM,
            averageAccuracy = AverageAccuracy,
            worstFingerArea = WorstFingerArea
        };
    }
}
