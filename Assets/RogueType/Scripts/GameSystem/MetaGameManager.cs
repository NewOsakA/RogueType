using UnityEngine;

public class MetaGameManager : MonoBehaviour
{
    public static MetaGameManager Instance { get; private set; }

    [Header("Meta Currency")]
    public int metaCoins = 0;

    [Header("Permanent Upgrades (max 20)")]
    public int damageLevel = 0;
    public int wallHpLevel = 0;

    [Header("Run Difficulty")]
    [Tooltip("Current selected mode for the next run.")]
    [SerializeField] private GameDifficultyMode selectedGameMode = GameDifficultyMode.Normal;

    [Header("Difficulty Profiles (Editable In Inspector)")]
    [Tooltip("Casual mode tuning.")]
    public DifficultyModeProfile casualProfile = null;
    [Tooltip("Normal mode tuning.")]
    public DifficultyModeProfile normalProfile = null;
    [Tooltip("Hardcore mode tuning.")]
    public DifficultyModeProfile hardcoreProfile = null;
    [Tooltip("Deathcore mode tuning.")]
    public DifficultyModeProfile deathcoreProfile = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureDifficultyProfiles();
        LoadFromActiveSlot();
    }

    private void OnValidate()
    {
        EnsureDifficultyProfiles();
    }

    public void LoadFromActiveSlot()
    {
        if (!SaveSlotManager.TryGetActiveSlotIndex(out int slotIndex))
        {
            metaCoins = 0;
            damageLevel = 0;
            wallHpLevel = 0;
            return;
        }

        LoadFromSlot(slotIndex);
    }

    public void LoadFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SaveSlotManager.SlotCount)
            return;

        SaveSlotData slot = SaveSlotManager.GetSlot(slotIndex);
        if (!slot.hasData)
        {
            slot = SaveSlotData.CreateNew(slotIndex);
            SaveSlotManager.SetSlot(slotIndex, slot);
        }

        SaveSlotManager.SetActiveSlotIndex(slotIndex);

        metaCoins = Mathf.Max(0, slot.metaCoins);
        damageLevel = Mathf.Clamp(slot.damageLevel, 0, 20);
        wallHpLevel = Mathf.Clamp(slot.wallHpLevel, 0, 20);

        int modeValue = Mathf.Clamp(slot.selectedDifficultyMode, 0, (int)GameDifficultyMode.Deathcore);
        selectedGameMode = (GameDifficultyMode)modeValue;
    }

    public void SaveToActiveSlot()
    {
        if (!SaveSlotManager.TryGetActiveSlotIndex(out int slotIndex))
            return;

        SaveSlotData slot = SaveSlotManager.GetSlot(slotIndex);
        if (!slot.hasData)
            slot = SaveSlotData.CreateNew(slotIndex);

        slot.metaCoins = metaCoins;
        slot.damageLevel = damageLevel;
        slot.wallHpLevel = wallHpLevel;
        slot.selectedDifficultyMode = (int)selectedGameMode;
        slot.lastPlayedUtc = System.DateTime.UtcNow.ToString("o");

        SaveSlotManager.SetSlot(slotIndex, slot);
    }

    public GameDifficultyMode GetSelectedGameMode()
    {
        return selectedGameMode;
    }

    public void SetSelectedGameMode(GameDifficultyMode mode)
    {
        selectedGameMode = mode;
        SaveToActiveSlot();
    }

    public DifficultyModeProfile GetSelectedDifficultyProfile()
    {
        return GetDifficultyProfile(selectedGameMode);
    }

    public DifficultyModeProfile GetDifficultyProfile(GameDifficultyMode mode)
    {
        EnsureDifficultyProfiles();

        switch (mode)
        {
            case GameDifficultyMode.Casual:
                return casualProfile;
            case GameDifficultyMode.Hardcore:
                return hardcoreProfile;
            case GameDifficultyMode.Deathcore:
                return deathcoreProfile;
            default:
                return normalProfile;
        }
    }

    private void EnsureDifficultyProfiles()
    {
        if (casualProfile == null)
            casualProfile = DifficultyModeProfile.CreateDefault(GameDifficultyMode.Casual);
        if (normalProfile == null)
            normalProfile = DifficultyModeProfile.CreateDefault(GameDifficultyMode.Normal);
        if (hardcoreProfile == null)
            hardcoreProfile = DifficultyModeProfile.CreateDefault(GameDifficultyMode.Hardcore);
        if (deathcoreProfile == null)
            deathcoreProfile = DifficultyModeProfile.CreateDefault(GameDifficultyMode.Deathcore);

        casualProfile.mode = GameDifficultyMode.Casual;
        normalProfile.mode = GameDifficultyMode.Normal;
        hardcoreProfile.mode = GameDifficultyMode.Hardcore;
        deathcoreProfile.mode = GameDifficultyMode.Deathcore;
    }
}
