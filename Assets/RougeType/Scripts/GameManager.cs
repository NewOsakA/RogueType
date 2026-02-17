// GameManager.cs

using UnityEngine;
using TMPro;
using System.Collections.Generic;

public enum GamePhase { BaseManagement, WaveDefense }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool isPaused = false;

    public GamePhase currentPhase = GamePhase.BaseManagement;
    public int currentWave = 0;
    private int globalAliveEnemies = 0;
    private int totalEnemySpawned = 0;
    private bool allEnemiesSpawned = false;
    private int activeSpawners = 0;
    private float totalEnemyLifetime = 0f;

    [Header("UI Elements")]
    public GameObject nextWaveButton;
    public Canvas baseCanvas;
    public Canvas defenseCanvas;
    public TMP_Text waveText;

    [Header("References")]
    public CameraController cam;
    public PlayerStats playerStats;
    public TypingManager typingManager;

    [Header("Word Adaptation")]
    private float prevWPM = 0f;
    private float prevAcc = 1f;
    private int prevMistakes = 0;
    private Dictionary<FingerZone, int> prevZoneMistakes = null;

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

        prevZoneMistakes = new Dictionary<FingerZone, int>();
        foreach (FingerZone z in System.Enum.GetValues(typeof(FingerZone)))
            prevZoneMistakes[z] = 0;

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

        UpdateWaveText();
        EnterWaveDefense();
    }

    void EnterWaveDefense()
    {
        globalAliveEnemies = 0;
        totalEnemySpawned = 0;
        totalEnemyLifetime = 0f;
        allEnemiesSpawned = false;
        activeSpawners = 0;

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

        // word adaptation update
        if (typingManager != null && BanditWordTrainer.Instance != null)
        {
            float accNow = typingManager.GetAccuracy();
            float wpmNow = typingManager.GetWPM();
            int mistakesNow = typingManager.GetMistakeCount();
            var zoneNow = typingManager.GetZoneMistakesSnapshot();

            bool stress = false;
            // Not detect stress in warm-up waves
            if (currentWave >= BanditWordTrainer.Instance.applyFromWave)
            {
                stress = BanditWordTrainer.IsStressHigh(
                    prevAcc, accNow,
                    prevMistakes, mistakesNow,
                    prevWPM, wpmNow
                );
            }

            BanditWordTrainer.Instance.OnWaveEnded(
                currentWave,
                prevAcc,
                accNow,
                zoneNow,
                prevZoneMistakes,
                stress
            );

            // update prev for next wave
            prevAcc = accNow;
            prevWPM = wpmNow;
            prevMistakes = mistakesNow;
            prevZoneMistakes = new Dictionary<FingerZone, int>(zoneNow);
        }

        Debug.Log(
            $"[DEBUG]" +
            $"Mistake:{typingManager.GetMistakeCount()} " +
            $"Acc:{typingManager.GetAccuracy():F2} " +
            $"Typed:{typingManager.GetComboLength()}"
        );

        // TrainingDatalogger
        if (TrainingDataLogger.Instance != null && typingManager != null)
        {
            int label = 1;

            TrainingDataLogger.Instance.Log(
                typingManager,
                this,
                label
            );
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
        totalEnemySpawned++;
    }

    public void NotifySpawnerFinished()
    {
        activeSpawners--;

        if (activeSpawners <= 0)
        {
            allEnemiesSpawned = true;
            CheckWaveEnd();
        }
    }

    public void RegisterSpawner()
    {
        activeSpawners++;
    }

    public void UnregisterEnemy()
    {
        globalAliveEnemies = Mathf.Max(0, globalAliveEnemies - 1);
        CheckWaveEnd();
    }

    private void CheckWaveEnd()
    {
        if (!IsWavePhase()) return;

        if (allEnemiesSpawned && globalAliveEnemies == 0)
        {
            EndWave();
        }
    }

    public void RegisterEnemyLifetime(float time)
    {
        totalEnemyLifetime += time;
    }

    public float GetAvgTimePerEnemy()
    {
        return totalEnemySpawned > 0
            ? totalEnemyLifetime / totalEnemySpawned
            : 0f;
    }

    public int GetTotalEnemy()
    {
        return totalEnemySpawned;
    }

}