using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class TypingManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text wordDisplayText;
    public TMP_Text wpmText;
    public TMP_Text wordCountText;
    public TMP_Text nextWordText;
    public TMP_Text timerText;
    public TMP_Text gameOverText;
    public TMP_Text comboStreakText;

    [Header("Word System")]
    public WordLoader wordLoader;

    [Header("Word Difficulty (Default)")]
    public WordLoader.Difficulty defaultWordDifficulty = WordLoader.Difficulty.Easy;

    [Header("Combat")]
    public Enemy activeEnemy;
    private List<Enemy> activeEnemies = new List<Enemy>();

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform shootPoint;

    [Header("Player Reference")]
    public PlayerStats playerStats;

    [Header("Penalty")]
    public PenaltyManager penaltyManager;

    // ===== Typing State =====
    private string currentWord = "";
    private string nextWord = "";
    private int currentLetterIndex = 0;

    // Gameplay stats
    private int wordCount = 0;

    // Typing stats (for WPM)
    private int totalTypedCharacters = 0;
    private float startTime;
    private float elapsedTime = 0f;

    private bool isShaking = false;
    private bool isGameOver = false;
    private bool isStunned = false;

    private int lastComboBonus = 0;

    // =========================
    void Start()
    {
        ResetTypingStats();
        LoadNextWord();

        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);

        if (comboStreakText != null)
            comboStreakText.text = "";
    }

    void Update()
    {
        if (isGameOver || GameManager.Instance.IsBasePhase())
            return;

        elapsedTime += Time.deltaTime;
        UpdateTimerText();

        if (Input.anyKeyDown && !isShaking && !isStunned)
        {
            string input = Input.inputString;
            if (!string.IsNullOrEmpty(input))
            {
                char typedChar = input[0];
                CheckLetter(typedChar);
            }
        }

        UpdateWordStats();
    }

    // =========================
    // Typing Logic
    void CheckLetter(char typedChar)
    {
        if (currentLetterIndex >= currentWord.Length || isGameOver)
            return;

        char expectedChar = currentWord[currentLetterIndex];

        if (char.ToLower(typedChar) == char.ToLower(expectedChar))
        {
            currentLetterIndex++;
            totalTypedCharacters++;

            ShootProjectile();
            UpdateWordDisplay();

            if (currentLetterIndex >= currentWord.Length)
            {
                wordCount++;

                playerStats?.OnCorrectType(true);
                penaltyManager?.RegisterCorrectWord();

                UpdateComboUI();
                LoadNextWord();
            }
        }
        else
        {
            StartCoroutine(ShakeText());

            playerStats?.OnCorrectType(false);
            penaltyManager?.RegisterMistake();
            UpdateComboUI();

            if (penaltyManager != null && penaltyManager.ShouldStun())
                StartCoroutine(StunInput(penaltyManager.stunDurationSeconds));
        }
    }

    // =========================
    // Projectile
    // =========================
    void ShootProjectile()
    {
        if (projectilePrefab == null || shootPoint == null)
            return;

        GameObject proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
        Projectile p = proj.GetComponent<Projectile>();

        if (p == null) return;

        float baseDmg = playerStats != null ? playerStats.currentDamage : 1;
        float finalDmg = penaltyManager != null
            ? baseDmg * penaltyManager.GetDamageMultiplier()
            : baseDmg;

        p.damage = Mathf.RoundToInt(finalDmg);

        Enemy target = GetClosestEnemy();
        if (target != null)
            p.SetTarget(target.transform);
    }

    // =========================
    // Word Handling
    void LoadNextWord()
    {
        WordLoader.Difficulty currentDiff = GetMixedDifficulty();
        WordLoader.Difficulty nextDiff = GetMixedDifficulty();

        currentWord = string.IsNullOrEmpty(nextWord)
            ? wordLoader.GetRandomWord(currentDiff)
            : nextWord;

        nextWord = wordLoader.GetRandomWord(nextDiff);

        currentLetterIndex = 0;
        UpdateWordDisplay();

        if (nextWordText != null)
            nextWordText.text = $"Next: <i>{nextWord}</i>";

        activeEnemy = null;
    }

    WordLoader.Difficulty GetMixedDifficulty()
    {
        // random value 0 to 1
        float roll = Random.value; 

        switch (defaultWordDifficulty)
        {
            case WordLoader.Difficulty.Easy:
                return roll < 0.7f
                    ? WordLoader.Difficulty.Easy
                    : WordLoader.Difficulty.Medium;

            case WordLoader.Difficulty.Medium:
                return roll < 0.7f
                    ? WordLoader.Difficulty.Medium
                    : WordLoader.Difficulty.Hard;

            case WordLoader.Difficulty.Hard:
                return WordLoader.Difficulty.Hard;

            default:
                return WordLoader.Difficulty.Easy;
        }
    }

    void UpdateWordDisplay()
    {
        string display = "";

        for (int i = 0; i < currentWord.Length; i++)
        {
            char c = currentWord[i];

            if (i < currentLetterIndex)
                display += $"<color=green>{c}</color>";
            else if (i == currentLetterIndex)
                display += $"<u>{c}</u>";
            else
                display += c;
        }

        wordDisplayText.text = display;
    }

    // =========================
    // UI Updates
    void UpdateWordStats()
    {
        wpmText.text = $"WPM: {Mathf.FloorToInt(GetWPM())}";
        wordCountText.text = $"Words: {wordCount}";
    }

    void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    void UpdateComboUI()
    {
        if (comboStreakText == null || playerStats == null)
            return;

        int bonus = playerStats.currentComboBonus;

        if (playerStats.comboCount == 0)
        {
            comboStreakText.text = "";
            lastComboBonus = 0;
            return;
        }

        if (bonus > lastComboBonus)
            StartCoroutine(FlashComboText());

        lastComboBonus = bonus;

        string bonusText = bonus >= playerStats.maxComboBonus
            ? "<color=yellow>MAX COMBO!</color>"
            : $"+{bonus} DMG";

        comboStreakText.text = $"Combo: {playerStats.comboCount} ({bonusText})";
    }

    IEnumerator FlashComboText()
    {
        Color originalColor = comboStreakText.color;
        comboStreakText.color = Color.yellow;
        yield return new WaitForSeconds(0.2f);
        comboStreakText.color = originalColor;
    }

    // =========================
    // Effects
    IEnumerator ShakeText()
    {
        isShaking = true;
        Vector3 originalPos = wordDisplayText.rectTransform.localPosition;

        for (int i = 0; i < 6; i++)
        {
            wordDisplayText.rectTransform.localPosition =
                originalPos + new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
            yield return new WaitForSeconds(0.02f);
        }

        wordDisplayText.rectTransform.localPosition = originalPos;
        isShaking = false;
    }

    IEnumerator StunInput(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    // =========================
    // Enemy Tracking
    public void RegisterEnemy(Enemy e)
    {
        if (!activeEnemies.Contains(e))
            activeEnemies.Add(e);
    }

    public void UnregisterEnemy(Enemy e)
    {
        activeEnemies.Remove(e);
    }

    Enemy GetClosestEnemy()
    {
        activeEnemies = activeEnemies.Where(e => e != null).ToList();
        return activeEnemies.Count == 0
            ? null
            : activeEnemies.OrderBy(e => e.transform.position.x).First();
    }

    // =========================
    // Public API Used by GameManager / AI
    public void ResetTypingStats()
    {
        startTime = Time.time;
        elapsedTime = 0f;
        totalTypedCharacters = 0;
        wordCount = 0;
    }

    public void SetDefaultWordDifficulty(WordLoader.Difficulty difficulty)
    {
        defaultWordDifficulty = difficulty;
    }


    public float GetWPM()
    {
        float timeElapsed = Time.time - startTime;
        float minutes = timeElapsed / 60f;
        if (minutes <= 0f) return 0f;
        return (totalTypedCharacters / 5f) / minutes;
    }

    public int GetMistakeCount()
    {
        return penaltyManager != null ? penaltyManager.GetMistakeCount() : 0;
    }

    public float GetAccuracy()
    {
        int correct = playerStats != null ? playerStats.totalCorrect : 0;
        int mistakes = playerStats != null ? playerStats.totalMistakes : 0;
        int total = correct + mistakes;
        return total > 0 ? (float)correct / total : 1f;
    }

    public int GetComboLength()
    {
        return playerStats != null ? playerStats.comboCount : 0;
    }

    public bool IsGameOver() => isGameOver;

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "GAME OVER";
        }

        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.speed = 0;
                enemy.enabled = false;
            }
        }
    }
}