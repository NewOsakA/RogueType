using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveRunStatsData
{
    public int score;
    public float totalTime;
    public int highestWave;
    public int currency;
    public float highestWPM;
    public float averageWPM;
    public float averageAccuracy;
    public string worstFingerArea = "N/A";
    public int leftPinkyMistakes;
    public int leftRingMistakes;
    public int leftMiddleMistakes;
    public int leftIndexMistakes;
    public int rightIndexMistakes;
    public int rightMiddleMistakes;
    public int rightRingMistakes;
    public int rightPinkyMistakes;
}

[Serializable]
public class SaveSlotData
{
    public int slotIndex;
    public bool hasData;
    public string createdAtUtc;
    public string lastPlayedUtc;

    public int metaCoins;
    public int damageLevel;
    public int wallHpLevel;
    public int selectedDifficultyMode;

    public SaveRunStatsData lastRunStats = new SaveRunStatsData();
    public List<SaveRunStatsData> runHistory = new List<SaveRunStatsData>();

    public static SaveSlotData CreateNew(int slotIndex)
    {
        string now = DateTime.UtcNow.ToString("o");
        return new SaveSlotData
        {
            slotIndex = slotIndex,
            hasData = true,
            createdAtUtc = now,
            lastPlayedUtc = now,
            metaCoins = 0,
            damageLevel = 0,
            wallHpLevel = 0,
            selectedDifficultyMode = (int)GameDifficultyMode.Normal,
            lastRunStats = new SaveRunStatsData(),
            runHistory = new List<SaveRunStatsData>()
        };
    }

    public static SaveSlotData CreateEmpty(int slotIndex)
    {
        return new SaveSlotData
        {
            slotIndex = slotIndex,
            hasData = false,
            createdAtUtc = string.Empty,
            lastPlayedUtc = string.Empty,
            metaCoins = 0,
            damageLevel = 0,
            wallHpLevel = 0,
            selectedDifficultyMode = (int)GameDifficultyMode.Normal,
            lastRunStats = new SaveRunStatsData(),
            runHistory = new List<SaveRunStatsData>()
        };
    }
}

[Serializable]
public class SaveSettingsData
{
    public int activeSlotIndex = -1;
}

public static class SaveSlotManager
{
    public const int SlotCount = 3;

    private const string LegacyActiveSlotKey = "rougetype_active_slot";
    private const string LegacySlotKeyPrefix = "rougetype_save_slot_";

    private static string SaveDirectoryPath => Path.Combine(Application.persistentDataPath, "Saves");
    private static string SettingsFilePath => Path.Combine(SaveDirectoryPath, "save_settings.json");

    public static string GetSaveDirectoryPath()
    {
        EnsureSaveDirectory();
        return SaveDirectoryPath;
    }

    public static SaveSlotData GetSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return SaveSlotData.CreateEmpty(slotIndex);

        EnsureSaveDirectory();

        string path = GetSlotFilePath(slotIndex);
        if (!File.Exists(path))
        {
            if (TryMigrateLegacySlot(slotIndex, out SaveSlotData migrated))
                return migrated;

            return SaveSlotData.CreateEmpty(slotIndex);
        }

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrEmpty(json))
                return SaveSlotData.CreateEmpty(slotIndex);

            SaveSlotData data = JsonUtility.FromJson<SaveSlotData>(json);
            if (data == null)
                return SaveSlotData.CreateEmpty(slotIndex);

            data.slotIndex = slotIndex;
            if (data.lastRunStats == null)
                data.lastRunStats = new SaveRunStatsData();
            if (data.runHistory == null)
                data.runHistory = new List<SaveRunStatsData>();
            if (data.runHistory.Count == 0 && data.hasData && IsRunSnapshotMeaningful(data.lastRunStats))
                data.runHistory.Add(CloneRunStats(data.lastRunStats));

            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load save slot {slotIndex}: {ex.Message}");
            return SaveSlotData.CreateEmpty(slotIndex);
        }
    }

    public static SaveSlotData[] GetAllSlots()
    {
        SaveSlotData[] slots = new SaveSlotData[SlotCount];
        for (int i = 0; i < SlotCount; i++)
            slots[i] = GetSlot(i);
        return slots;
    }

    public static void SetSlot(int slotIndex, SaveSlotData data)
    {
        if (!IsValidSlot(slotIndex) || data == null)
            return;

        EnsureSaveDirectory();

        data.slotIndex = slotIndex;
        if (data.lastRunStats == null)
            data.lastRunStats = new SaveRunStatsData();
        if (data.runHistory == null)
            data.runHistory = new List<SaveRunStatsData>();

        if (string.IsNullOrEmpty(data.createdAtUtc))
            data.createdAtUtc = DateTime.UtcNow.ToString("o");

        if (string.IsNullOrEmpty(data.lastPlayedUtc))
            data.lastPlayedUtc = DateTime.UtcNow.ToString("o");

        string path = GetSlotFilePath(slotIndex);

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to write save slot {slotIndex}: {ex.Message}");
        }
    }

    public static void DeleteSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return;

        EnsureSaveDirectory();

        string path = GetSlotFilePath(slotIndex);
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to delete save slot {slotIndex}: {ex.Message}");
        }

        if (GetActiveSlotIndex() == slotIndex)
            SetActiveSlotIndex(-1);
    }

    public static int GetActiveSlotIndex()
    {
        EnsureSaveDirectory();

        SaveSettingsData settings = LoadSettings();
        if (settings == null)
            return -1;

        if (IsValidSlot(settings.activeSlotIndex))
            return settings.activeSlotIndex;

        return -1;
    }

    public static bool TryGetActiveSlotIndex(out int slotIndex)
    {
        slotIndex = GetActiveSlotIndex();
        return IsValidSlot(slotIndex);
    }

    public static void SetActiveSlotIndex(int slotIndex)
    {
        if (slotIndex != -1 && !IsValidSlot(slotIndex))
            return;

        SaveSettingsData settings = LoadSettings() ?? new SaveSettingsData();
        settings.activeSlotIndex = slotIndex;
        SaveSettings(settings);
    }

    private static bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < SlotCount;
    }

    private static string GetSlotFilePath(int slotIndex)
    {
        return Path.Combine(SaveDirectoryPath, $"slot_{slotIndex}.json");
    }

    private static void EnsureSaveDirectory()
    {
        if (!Directory.Exists(SaveDirectoryPath))
            Directory.CreateDirectory(SaveDirectoryPath);
    }

    private static SaveSettingsData LoadSettings()
    {
        if (!File.Exists(SettingsFilePath))
        {
            if (PlayerPrefs.HasKey(LegacyActiveSlotKey))
            {
                int migrated = PlayerPrefs.GetInt(LegacyActiveSlotKey, -1);
                var migratedSettings = new SaveSettingsData
                {
                    activeSlotIndex = IsValidSlot(migrated) ? migrated : -1
                };
                SaveSettings(migratedSettings);
                PlayerPrefs.DeleteKey(LegacyActiveSlotKey);
                PlayerPrefs.Save();
                return migratedSettings;
            }

            return new SaveSettingsData();
        }

        try
        {
            string json = File.ReadAllText(SettingsFilePath);
            if (string.IsNullOrEmpty(json))
                return new SaveSettingsData();

            SaveSettingsData settings = JsonUtility.FromJson<SaveSettingsData>(json);
            return settings ?? new SaveSettingsData();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load save settings: {ex.Message}");
            return new SaveSettingsData();
        }
    }

    private static void SaveSettings(SaveSettingsData settings)
    {
        EnsureSaveDirectory();

        try
        {
            string json = JsonUtility.ToJson(settings, true);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save settings: {ex.Message}");
        }
    }

    private static bool TryMigrateLegacySlot(int slotIndex, out SaveSlotData migratedData)
    {
        migratedData = null;

        string legacyKey = LegacySlotKeyPrefix + slotIndex;
        if (!PlayerPrefs.HasKey(legacyKey))
            return false;

        string json = PlayerPrefs.GetString(legacyKey);
        if (string.IsNullOrEmpty(json))
            return false;

        try
        {
            SaveSlotData data = JsonUtility.FromJson<SaveSlotData>(json);
            if (data == null)
                return false;

            data.slotIndex = slotIndex;
            if (data.lastRunStats == null)
                data.lastRunStats = new SaveRunStatsData();
            if (data.runHistory == null)
                data.runHistory = new List<SaveRunStatsData>();
            if (data.runHistory.Count == 0 && data.hasData && IsRunSnapshotMeaningful(data.lastRunStats))
                data.runHistory.Add(CloneRunStats(data.lastRunStats));

            SetSlot(slotIndex, data);

            PlayerPrefs.DeleteKey(legacyKey);
            PlayerPrefs.Save();

            migratedData = data;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to migrate legacy slot {slotIndex}: {ex.Message}");
            return false;
        }
    }

    private static bool IsRunSnapshotMeaningful(SaveRunStatsData data)
    {
        if (data == null)
            return false;

        return data.score > 0
            || data.totalTime > 0f
            || data.highestWave > 0
            || data.currency > 0
            || data.highestWPM > 0f
            || data.averageWPM > 0f
            || data.averageAccuracy > 0f
            || data.leftPinkyMistakes > 0
            || data.leftRingMistakes > 0
            || data.leftMiddleMistakes > 0
            || data.leftIndexMistakes > 0
            || data.rightIndexMistakes > 0
            || data.rightMiddleMistakes > 0
            || data.rightRingMistakes > 0
            || data.rightPinkyMistakes > 0
            || (!string.IsNullOrEmpty(data.worstFingerArea) && data.worstFingerArea != "N/A");
    }

    private static SaveRunStatsData CloneRunStats(SaveRunStatsData source)
    {
        if (source == null)
            return new SaveRunStatsData();

        return new SaveRunStatsData
        {
            score = source.score,
            totalTime = source.totalTime,
            highestWave = source.highestWave,
            currency = source.currency,
            highestWPM = source.highestWPM,
            averageWPM = source.averageWPM,
            averageAccuracy = source.averageAccuracy,
            worstFingerArea = source.worstFingerArea,
            leftPinkyMistakes = source.leftPinkyMistakes,
            leftRingMistakes = source.leftRingMistakes,
            leftMiddleMistakes = source.leftMiddleMistakes,
            leftIndexMistakes = source.leftIndexMistakes,
            rightIndexMistakes = source.rightIndexMistakes,
            rightMiddleMistakes = source.rightMiddleMistakes,
            rightRingMistakes = source.rightRingMistakes,
            rightPinkyMistakes = source.rightPinkyMistakes
        };
    }
}
