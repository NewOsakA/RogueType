using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SaveSlotRowUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button statisticButton;

    [Header("Main Slot Visuals")]
    [SerializeField] private Image slotBackgroundImage;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite pressedSprite;
    [SerializeField] private Image hoverBorderImage;

    [Header("Hover Text Swap")]
    [SerializeField] private TMP_Text slotNumberText;
    [SerializeField] private TMP_Text saveSubDetailText;
    [SerializeField] private bool showSubDetailOnlyOnHover = true;

    private int slotIndex;
    private Action<int> onPlay;
    private Action<int> onDelete;
    private Action<int> onShowStats;
    private EventTrigger playEventTrigger;
    private bool isHovered;
    private bool isPressed;
    private bool isSelected;

    private enum VisualState
    {
        Normal,
        Hover,
        Pressed
    }

    private void Awake()
    {
        ApplyVisualState(VisualState.Normal);
    }

    private void OnDisable()
    {
        isHovered = false;
        isPressed = false;
        isSelected = false;
        ApplyVisualState(VisualState.Normal);
    }

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
            EnsurePlayPointerEvents();
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
            ApplyVisualState(CurrentVisualState());
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
                $"Last Played: {lastPlayed}\n" + $"Score: {runStats.score} | Wave: {runStats.highestWave} | Highest WPM: {runStats.highestWPM:F1}";
        }

        SetButtons(true, true, true);
        ApplyVisualState(CurrentVisualState());
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

    private void EnsurePlayPointerEvents()
    {
        if (playButton == null)
            return;

        playEventTrigger = playButton.GetComponent<EventTrigger>();
        if (playEventTrigger == null)
            playEventTrigger = playButton.gameObject.AddComponent<EventTrigger>();

        playEventTrigger.triggers.Clear();
        AddTrigger(EventTriggerType.PointerEnter, _ => OnPointerEnterMain());
        AddTrigger(EventTriggerType.PointerExit, _ => OnPointerExitMain());
        AddTrigger(EventTriggerType.PointerDown, _ => OnPointerDownMain());
        AddTrigger(EventTriggerType.PointerUp, _ => OnPointerUpMain());
        AddTrigger(EventTriggerType.Select, _ => OnSelectMain());
        AddTrigger(EventTriggerType.Deselect, _ => OnDeselectMain());
    }

    private void AddTrigger(EventTriggerType eventType, Action<BaseEventData> callback)
    {
        if (playEventTrigger == null || callback == null)
            return;

        var entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(data => callback(data));
        playEventTrigger.triggers.Add(entry);
    }

    private void OnPointerEnterMain()
    {
        isHovered = true;
        ApplyVisualState(CurrentVisualState());
    }

    private void OnPointerExitMain()
    {
        isHovered = false;
        isPressed = false;
        ApplyVisualState(CurrentVisualState());
    }

    private void OnPointerDownMain()
    {
        if (!IsMainButtonInteractable())
            return;

        isPressed = true;
        ApplyVisualState(CurrentVisualState());
    }

    private void OnPointerUpMain()
    {
        isPressed = false;
        ApplyVisualState(CurrentVisualState());
    }

    private void OnSelectMain()
    {
        isSelected = true;
        ApplyVisualState(CurrentVisualState());
    }

    private void OnDeselectMain()
    {
        isSelected = false;
        isPressed = false;
        ApplyVisualState(CurrentVisualState());
    }

    private bool IsMainButtonInteractable()
    {
        return playButton != null && playButton.interactable;
    }

    private VisualState CurrentVisualState()
    {
        if (!IsMainButtonInteractable())
            return VisualState.Normal;

        if (isPressed && (isHovered || isSelected))
            return VisualState.Pressed;

        if (isHovered || isSelected)
            return VisualState.Hover;

        return VisualState.Normal;
    }

    private void ApplyVisualState(VisualState state)
    {
        if (slotBackgroundImage != null)
        {
            Sprite targetSprite = normalSprite;
            if (state == VisualState.Pressed && pressedSprite != null)
                targetSprite = pressedSprite;
            else if (state == VisualState.Hover && hoverSprite != null)
                targetSprite = hoverSprite;
            else if (normalSprite == null)
                targetSprite = slotBackgroundImage.sprite;

            slotBackgroundImage.sprite = targetSprite;
        }

        if (hoverBorderImage != null)
            hoverBorderImage.enabled = state != VisualState.Normal;

        bool showSubDetail = !showSubDetailOnlyOnHover || state != VisualState.Normal;
        if (slotNumberText != null)
            slotNumberText.gameObject.SetActive(!showSubDetail);
        if (saveSubDetailText != null)
            saveSubDetailText.gameObject.SetActive(showSubDetail);

    }
}
