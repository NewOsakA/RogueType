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

    [Header("Statistics Panel")]
    [SerializeField] private SaveStatisticsPanelUI statisticsPanelUI;

    [Header("Delete Confirmation Popup")]
    [SerializeField] private GameObject deleteConfirmPanel;
    [SerializeField] private TMP_Text deleteConfirmText;
    [SerializeField] private Button confirmDeleteButton;
    [SerializeField] private Button cancelDeleteButton;

    private int pendingDeleteSlotIndex = -1;

    private void Awake()
    {
        WireButtons();
        ConfigureRows();
    }

    private void OnEnable()
    {
        statisticsPanelUI?.Hide();
        HideDeleteConfirmPanel();
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

            row.Initialize(i, OnPlay, OnRequestDelete, OnShowStats);
        }
    }

    private void WireButtons()
    {
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveListener(OnBackToMenu);
            backToMenuButton.onClick.AddListener(OnBackToMenu);
        }

        if (confirmDeleteButton != null)
        {
            confirmDeleteButton.onClick.RemoveAllListeners();
            confirmDeleteButton.onClick.AddListener(ConfirmDelete);
        }

        if (cancelDeleteButton != null)
        {
            cancelDeleteButton.onClick.RemoveAllListeners();
            cancelDeleteButton.onClick.AddListener(CancelDelete);
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

    private void OnRequestDelete(int slotIndex)
    {
        if (deleteConfirmPanel == null)
        {
            DeleteSlotNow(slotIndex);
            return;
        }

        pendingDeleteSlotIndex = slotIndex;

        if (deleteConfirmText != null)
            deleteConfirmText.text = $"Are you sure you want to delete save slot {slotIndex + 1}?";

        deleteConfirmPanel.SetActive(true);
    }

    private void ConfirmDelete()
    {
        if (pendingDeleteSlotIndex < 0)
        {
            HideDeleteConfirmPanel();
            return;
        }

        DeleteSlotNow(pendingDeleteSlotIndex);
        HideDeleteConfirmPanel();
    }

    private void CancelDelete()
    {
        HideDeleteConfirmPanel();
    }

    private void DeleteSlotNow(int slotIndex)
    {
        SaveSlotManager.DeleteSlot(slotIndex);

        if (MetaGameManager.Instance != null && SaveSlotManager.GetActiveSlotIndex() < 0)
            MetaGameManager.Instance.LoadFromActiveSlot();

        statisticsPanelUI?.Hide();
        RefreshUI();
    }

    private void OnShowStats(int slotIndex)
    {
        if (statisticsPanelUI == null)
            return;

        SaveSlotData slot = SaveSlotManager.GetSlot(slotIndex);
        statisticsPanelUI.Show(slot);
    }

    private void HideDeleteConfirmPanel()
    {
        pendingDeleteSlotIndex = -1;

        if (deleteConfirmPanel != null)
            deleteConfirmPanel.SetActive(false);
    }

    private void OnBackToMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
