// GameManager.cs

using UnityEngine;
using TMPro;

public enum GamePhase { BaseManagement, WaveDefense }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool isPaused = false;

    public GamePhase currentPhase = GamePhase.BaseManagement;
    public int currentWave = 0;
    private int globalAliveEnemies = 0;

    [Header("UI Elements")]
    public GameObject nextWaveButton;
    public Canvas baseCanvas;
    public Canvas defenseCanvas;
    public TMP_Text waveText;

    [Header("References")]
    public CameraController cam;
    public PlayerStats playerStats;
    public PenaltyManager penaltyManager;
    public TypingManager typingManager;

    [Header("AI (Local Model)")]
    public LocalDifficultyPredictor difficultyPredictor;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (cam == null && Camera.main != null)
            cam = Camera.main.GetComponent<CameraController>();

        Wall wall = Object.FindFirstObjectByType<Wall>();
        wall?.ApplyMetaUpgrades();

        playerStats?.ApplyMetaUpgrades();

        UpdateWaveText();
        EnterBaseManagement();
    }

    public void StartNextWave()
    {
        if (!IsBasePhase()) return;

        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            Destroy(enemy.gameObject);
        }

        currentWave++;
        Debug.Log($"Starting Wave {currentWave}");

        if (penaltyManager != null)
            penaltyManager.SetWave(currentWave);

        UpdateWaveText();
        EnterWaveDefense();
    }

    void EnterWaveDefense()
    {
        globalAliveEnemies = 0;

        currentPhase = GamePhase.WaveDefense;

        if (nextWaveButton != null) nextWaveButton.SetActive(false);

        if (baseCanvas != null) baseCanvas.enabled = false;
        if (defenseCanvas != null) defenseCanvas.enabled = true;

        cam?.MoveToWave();
        // Reset word every turn
        typingManager?.ResetWordsForNewWave();

        foreach (var spawner in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None))
        {
            spawner.BeginWave(currentWave);
        }

        Wall wall = Object.FindFirstObjectByType<Wall>();
        if (wall != null)
        {
            wall.RechargeShield();
            wall.StartAutoRepair();
        }
    }

    public void EndWave()
    {
        Debug.Log($"Wave {currentWave} Ended");

        if (playerStats != null && playerStats.interestRate > 0f)
        {
            int currentGold = CurrencyManager.Instance.GetCurrentCurrency();
            int interest = Mathf.FloorToInt(currentGold * playerStats.interestRate);

            if (interest > 0)
            {
                CurrencyManager.Instance.AddCurrency(interest);
                Debug.Log($"Interest +{interest} gold");
            }
        }


        if (typingManager != null && difficultyPredictor != null)
        {
            PlayerData data = new PlayerData
            {
                wpm = typingManager.GetWPM(),
                combo_length = typingManager.GetComboLength(),
                mistake_count = typingManager.GetMistakeCount(),
                recent_accuracy = typingManager.GetAccuracy(),
                wave_number = currentWave
            };

            try
            {
                int prediction = difficultyPredictor.Predict(data);

                Debug.Log(
                    $"[AI] Wave {currentWave} | Pred:{prediction} | " +
                    $"WPM:{data.wpm:F1} Combo:{data.combo_length} " +
                    $"Mistake:{data.mistake_count} Acc:{data.recent_accuracy:F2}"
                );

                // Apply model at wave 3
                if (currentWave >= 3)
                {
                    AdjustDifficulty(prediction);
                }
                else
                {
                    Debug.Log("AI adjustment skipped (warm-up wave)");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("AI Predict failed: " + e.Message);

                if (currentWave >= 3)
                {
                    AdjustDifficulty(1);
                }
            }
        }

        EnterBaseManagement();
    }


    void EnterBaseManagement()
    {
        currentPhase = GamePhase.BaseManagement;

        if (nextWaveButton != null) nextWaveButton.SetActive(true);

        if (baseCanvas != null) baseCanvas.enabled = true;
        if (defenseCanvas != null) defenseCanvas.enabled = false;

        cam?.MoveToBase();
        Wall wall = Object.FindFirstObjectByType<Wall>();
        if (wall != null)
        {
            wall.StopAutoRepair();
        }
    }

    void UpdateWaveText()
    {
        if (waveText != null)
            waveText.text = $"Wave: {currentWave}";
    }

    public void AdjustDifficulty(int prediction)
    {
        switch (prediction)
        {
            case 0:
                Debug.Log("AI: Game too hard → Lower difficulty");
                // TODO
                break;

            case 1:
                Debug.Log("AI: Difficulty balanced → Keep current");
                break;

            case 2:
                Debug.Log("AI: Game too easy → Increase difficulty");
                // TODO
                break;

            default:
                Debug.LogWarning($"Unknown prediction {prediction}");
                break;
        }
    }

    public bool IsBasePhase() => currentPhase == GamePhase.BaseManagement;
    public bool IsWavePhase() => currentPhase == GamePhase.WaveDefense;

    public void OnPlayerDefeated()
    {
        Debug.Log("GAME OVER!");

        int baseReward = (currentWave - 1) * 10; 
        int reward = Mathf.RoundToInt(baseReward); 

        MetaGameManager.Instance.AddMetaCoins(reward);

        Debug.Log($"Earned {reward} coins from Wave {currentWave}");

        UnityEngine.SceneManagement.SceneManager.LoadScene("Upgrade");
    }

    public void EndRunWithoutSave()
    {
        Debug.Log("Run ended");
        // reset runtime state
        // clear enemy and projectile
        currentWave = 0;

        if (playerStats != null)
            playerStats.ResetRunStats();

        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            Destroy(enemy.gameObject);
        }
    }

    public void RegisterEnemy()
    {
        globalAliveEnemies++;
    }

    public void UnregisterEnemy()
    {
        globalAliveEnemies = Mathf.Max(0, globalAliveEnemies - 1);

        if (IsWavePhase() && globalAliveEnemies == 0)
        {
            EndWave();
        }
    }


}