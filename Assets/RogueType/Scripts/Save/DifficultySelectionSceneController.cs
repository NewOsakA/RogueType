using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DifficultySelectionSceneController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "Game Scene";
    [SerializeField] private string saveSelectionSceneName = "Save Selection Scene";

    [Header("Buttons")]
    [SerializeField] private Button casualButton;
    [SerializeField] private Button normalButton;
    [SerializeField] private Button hardcoreButton;
    [SerializeField] private Button deathcoreButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        WireButtons();
    }

    private void OnEnable()
    {
        WireButtons();
    }

    private void WireButtons()
    {
        if (casualButton != null)
        {
            casualButton.onClick.RemoveListener(OnSelectCasual);
            casualButton.onClick.AddListener(OnSelectCasual);
        }

        if (normalButton != null)
        {
            normalButton.onClick.RemoveListener(OnSelectNormal);
            normalButton.onClick.AddListener(OnSelectNormal);
        }

        if (hardcoreButton != null)
        {
            hardcoreButton.onClick.RemoveListener(OnSelectHardcore);
            hardcoreButton.onClick.AddListener(OnSelectHardcore);
        }

        if (deathcoreButton != null)
        {
            deathcoreButton.onClick.RemoveListener(OnSelectDeathcore);
            deathcoreButton.onClick.AddListener(OnSelectDeathcore);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackToSaveSelection);
            backButton.onClick.AddListener(OnBackToSaveSelection);
        }
    }

    public void OnSelectCasual()
    {
        StartRun(GameDifficultyMode.Casual);
    }

    public void OnSelectNormal()
    {
        StartRun(GameDifficultyMode.Normal);
    }

    public void OnSelectHardcore()
    {
        StartRun(GameDifficultyMode.Hardcore);
    }

    public void OnSelectDeathcore()
    {
        StartRun(GameDifficultyMode.Deathcore);
    }

    public void OnBackToSaveSelection()
    {
        SceneManager.LoadScene(saveSelectionSceneName);
    }

    public void StartRun(GameDifficultyMode mode)
    {
        MetaGameManager.Instance?.SetSelectedGameMode(mode);
        Debug.Log($"[Difficulty] Selected mode: {mode}");
        SceneManager.LoadScene(gameSceneName);
    }
}
