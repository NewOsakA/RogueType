using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotRowUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button statisticButton;

    private int slotIndex;
    private Action<int> onPlay;
    private Action<int> onDelete;
    private Action<int> onShowStats;

    public void Initialize(int slotIndex, Action<int> onPlay, Action<int> onDelete, Action<int> onShowStats)
    {
        this.slotIndex = slotIndex;
        this.onPlay = onPlay;
        this.onDelete = onDelete;
        this.onShowStats = onShowStats;

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() => this.onPlay?.Invoke(this.slotIndex));
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => this.onDelete?.Invoke(this.slotIndex));
        }

        if (statisticButton != null)
        {
            statisticButton.onClick.RemoveAllListeners();
            statisticButton.onClick.AddListener(() => this.onShowStats?.Invoke(this.slotIndex));
        }
    }

    public void Refresh(SaveSlotData slot)
    {
        if (slot == null || !slot.hasData)
        {
            if (titleText != null)
                titleText.text = $"Slot {slotIndex + 1}: Empty";

            if (subtitleText != null)
                subtitleText.text = "Click this slot to create a new save.";

            SetButtons(true, false, false);
            return;
        }

        string lastPlayed = "N/A";
        if (DateTime.TryParse(slot.lastPlayedUtc, out DateTime dt))
            lastPlayed = dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

        SaveRunStatsData runStats = slot.lastRunStats ?? new SaveRunStatsData();

        if (titleText != null)
            titleText.text = $"Slot {slotIndex + 1}: Continue";

        if (subtitleText != null)
        {
            subtitleText.text =
                $"Last Played: {lastPlayed}  |  Wave: {runStats.highestWave}  |  Highest WPM: {runStats.highestWPM:F1}";
        }

        SetButtons(true, true, true);
    }

    private void SetButtons(bool playInteractable, bool deleteInteractable, bool statsInteractable)
    {
        if (playButton != null)
            playButton.interactable = playInteractable;

        if (deleteButton != null)
            deleteButton.interactable = deleteInteractable;

        if (statisticButton != null)
            statisticButton.interactable = statsInteractable;
    }
}
