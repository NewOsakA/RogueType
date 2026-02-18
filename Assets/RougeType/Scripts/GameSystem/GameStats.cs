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

    private GameManager gameManager;
    private TypingManager typingManager;
    private PlayerStats playerStats;

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

    public void ResetRunStats()
    {
        TotalPlayTime = 0f;
        HighestWave = 0;
        CurrentCurrency = 0;
        CurrentEssence = 0;
        HighestWPM = 0f;
        Score = 0;
    }
}
