using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveSelectionSceneController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "Main manu";
    [SerializeField] private string upgradeSceneName = "Upgrade";

    [Header("Slot Rows (Size should match SaveSlotManager.SlotCount)")]
    [SerializeField] private SaveSlotRowUI[] slotRows;

    [Header("Top-Level Buttons")]
    [SerializeField] private Button backToMenuButton;

    [Header("Statistics Popup")]
    [SerializeField] private GameObject statisticPanel;
    [SerializeField] private TMP_Text statisticText;
    [SerializeField] private Button closeStatisticButton;

    private void Awake()
    {
        WireButtons();
        ConfigureRows();
    }

    private void OnEnable()
    {
        if (statisticPanel != null)
            statisticPanel.SetActive(false);

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (slotRows == null)
            return;

        int count = Mathf.Min(slotRows.Length, SaveSlotManager.SlotCount);
        for (int i = 0; i < count; i++)
        {
            SaveSlotRowUI row = slotRows[i];
            if (row == null)
                continue;

            row.Refresh(SaveSlotManager.GetSlot(i));
        }
    }

    private void ConfigureRows()
    {
        if (slotRows == null)
            return;

        int count = Mathf.Min(slotRows.Length, SaveSlotManager.SlotCount);
        for (int i = 0; i < count; i++)
        {
            SaveSlotRowUI row = slotRows[i];
            if (row == null)
                continue;

            row.Initialize(i, OnPlay, OnDelete, OnShowStats);
        }
    }

    private void WireButtons()
    {
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveListener(OnBackToMenu);
            backToMenuButton.onClick.AddListener(OnBackToMenu);
        }

        if (closeStatisticButton != null)
        {
            closeStatisticButton.onClick.RemoveAllListeners();
            closeStatisticButton.onClick.AddListener(HideStatisticPanel);
        }
    }

    private void OnPlay(int slotIndex)
    {
        SaveSlotData slot = SaveSlotManager.GetSlot(slotIndex);
        if (!slot.hasData)
            slot = SaveSlotData.CreateNew(slotIndex);

        slot.lastPlayedUtc = DateTime.UtcNow.ToString("o");

        SaveSlotManager.SetSlot(slotIndex, slot);
        SaveSlotManager.SetActiveSlotIndex(slotIndex);

        if (MetaGameManager.Instance != null)
            MetaGameManager.Instance.LoadFromActiveSlot();

        SceneManager.LoadScene(upgradeSceneName);
    }

    private void OnDelete(int slotIndex)
    {
        SaveSlotManager.DeleteSlot(slotIndex);

        if (MetaGameManager.Instance != null && SaveSlotManager.GetActiveSlotIndex() < 0)
            MetaGameManager.Instance.LoadFromActiveSlot();

        HideStatisticPanel();
        RefreshUI();
    }

    private void OnShowStats(int slotIndex)
    {
        if (statisticPanel == null || statisticText == null)
            return;

        SaveSlotData slot = SaveSlotManager.GetSlot(slotIndex);

        if (!slot.hasData)
        {
            statisticText.text = $"Slot {slotIndex + 1} is empty.";
            statisticPanel.SetActive(true);
            return;
        }

        SaveRunStatsData stats = slot.lastRunStats ?? new SaveRunStatsData();
        string worstFingerArea = string.IsNullOrEmpty(stats.worstFingerArea) ? "N/A" : stats.worstFingerArea;

        statisticText.text =
            $"Slot {slotIndex + 1}\n" +
            $"Last Played: {FormatUtc(slot.lastPlayedUtc)}\n" +
            $"Meta Coins: {slot.metaCoins}\n" +
            $"Damage Lv: {slot.damageLevel}\n" +
            $"Wall HP Lv: {slot.wallHpLevel}\n\n" +
            $"Score: {stats.score}\n" +
            $"Total Time: {stats.totalTime:F1}s\n" +
            $"Highest Wave: {stats.highestWave}\n" +
            $"Currency: {stats.currency}\n" +
            $"Highest WPM: {stats.highestWPM:F1}\n" +
            $"Average WPM: {stats.averageWPM:F1}\n" +
            $"Average Accuracy: {stats.averageAccuracy * 100f:F1}%\n" +
            $"Worst Finger Area: {worstFingerArea}";

        statisticPanel.SetActive(true);
    }

    private void HideStatisticPanel()
    {
        if (statisticPanel != null)
            statisticPanel.SetActive(false);
    }

    private void OnBackToMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private static string FormatUtc(string isoUtc)
    {
        if (DateTime.TryParse(isoUtc, out DateTime dt))
            return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

        return "N/A";
    }
}
