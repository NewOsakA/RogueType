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
    // public TMP_Text wordCountText;
    // public TMP_Text timerText;
    public TMP_Text comboStreakText;

    [Header("Word System")]
    public WordLoader wordLoader;

    [Header("Combat")]
    public Enemy activeEnemy;
    private List<Enemy> activeEnemies = new List<Enemy>();

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform shootPoint;
    [Header("Animation")]
    public Animator playerAnimator;

    [Header("Player Reference")]
    public PlayerStats playerStats;

    [Header("Warm-up Word Mix")]
    [Range(0f, 1f)] public float warmupEasyWeight = 0.5f;
    [Range(0f, 1f)] public float warmupMediumWeight = 0.3f;
    [Range(0f, 1f)] public float warmupHardWeight = 0.2f;

    // Typing State
    private Queue<string> wordQueue = new Queue<string>();
    private int currentLetterIndex = 0;

    [Header("Total Preview word")]
    [SerializeField] private int previewWordCount = 3;

    // Gameplay stats
    private int wordCount = 0;
    private int mistakeCount = 0;

    // Typing stats (for WPM)
    private int totalTypedCharacters = 0;
    private float startTime;
    private float elapsedTime = 0f;

    // Reaction time
    private float lastKeyTime = 0f;
    private float reactionTimeSum = 0f;
    private int reactionSampleCount = 0;

    // Finger-zone mistake tracking
    private Dictionary<FingerZone, int> zoneMistakes = new Dictionary<FingerZone, int>();
    private bool isShaking = false;
    private bool isGameOver = false;

    private int lastComboBonus = 0;

    private bool IsAlphabet(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == ' ';
    }
    public bool LastWordPerfect { get; private set; }

    void Start()
    {
        ResetZoneMistakes();
        ResetTypingStats();
        InitializeWordStream();

        if (comboStreakText != null)
            comboStreakText.text = "";
    }

    void Update()
    {
        if (isGameOver || GameManager.Instance.IsBasePhase())
            return;

        if (GameManager.Instance != null && GameManager.Instance.isPaused)
            return;

        elapsedTime += Time.deltaTime;
        // UpdateTimerText();

        if (Input.anyKeyDown && !isShaking)
        {
            string input = Input.inputString;
            if (string.IsNullOrEmpty(input))
                return;

            char typedChar = input[0];

            if (!IsAlphabet(typedChar))
                return;

            CheckLetter(char.ToLower(typedChar));
        }


        UpdateWordStats();
    }

    // Typing Logic
    void CheckLetter(char typedChar)
    {
        string word = CurrentWord;

        if (currentLetterIndex >= word.Length || isGameOver)
            return;

        char expectedChar = word[currentLetterIndex];

        if (typedChar == char.ToLower(expectedChar))
        {
            float now = Time.time;
            if (lastKeyTime > 0f)
            {
                reactionTimeSum += (now - lastKeyTime);
                reactionSampleCount++;
            }
            lastKeyTime = now;

            currentLetterIndex++;
            totalTypedCharacters++;

            ShootProjectile();
            UpdateWordDisplay();

            if (currentLetterIndex >= word.Length)
            {
                AdvanceWord();
            }
        }
        else
        {
            mistakeCount++;
            // Track finger-zone mistake
            if (FingerZoneMap.TryGetZone(typedChar, out var zone))
            {
                zoneMistakes[zone]++;
            }

            LastWordPerfect = false;
            StartCoroutine(ShakeText());

            playerStats?.OnCorrectType(false);
            playerStats?.ResetFocusedFire();
            UpdateComboUI();
        }
    }

    // Projectile
    void ShootProjectile()
    {
        if (projectilePrefab == null || shootPoint == null)
            return;

        if (playerAnimator != null)
            playerAnimator.CrossFade("Mage_Attack", 0.05f, 0);

        int count = playerStats != null
            ? Mathf.Max(1, playerStats.projectileCount)
            : 1;

        // Debug.Log("Projectile Count: " + count);

        float damageMultiplier = playerStats != null
            ? playerStats.multiShotDamageMultiplier
            : 1f;    

        if (playerStats != null && playerStats.hasTypingFrenzy)
        {
            StartCoroutine(FireBurst());
        }
        else
        {
            // Debug.Log("Firing " + count + " projectiles.");
            StartCoroutine(FireMultipleProjectiles(count, damageMultiplier));
        }
    }

    IEnumerator FireMultipleProjectiles(int count, float damageMultiplier)
    {
        for (int i = 0; i < count; i++)
        {
            FireSingleProjectile(damageMultiplier);
            yield return new WaitForSeconds(0.04f);
        }
       
    }

    void FireSingleProjectile(float damageMultiplier)
    {
        GameObject proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
        AudioManager.Instance?.PlayPlayerProjectile();

        Projectile p = proj.GetComponent<Projectile>();
        if (p == null) return;

        Enemy target = GetClosestEnemy();

        float baseDmg = playerStats != null ? playerStats.currentDamage : 1f;
        float finalDmg = baseDmg;

        // Focused Fire
        if (playerStats != null && target != null)
        {
            finalDmg *= playerStats.GetFocusedFireMultiplier(target);
        }

        // typing frenzy multiplier
        finalDmg *= damageMultiplier;

        // Debug.Log("Damage: " + finalDmg);

        p.damage = Mathf.RoundToInt(finalDmg);

        if (target != null)
            p.SetTarget(target.transform);
    }


    IEnumerator FireBurst()
    {
        int shots = 3;
        float damageMultiplier = 0.5f;

        for (int i = 0; i < shots; i++)
        {
            FireSingleProjectile(damageMultiplier);
            yield return new WaitForSeconds(0.04f);
        }
    }


    // Word Handling
    void InitializeWordStream()
    {
        wordQueue.Clear();

        int wave = GameManager.Instance.currentWave;

        bool useBandit =
            BanditWordTrainer.Instance != null &&
            BanditWordTrainer.Instance.ShouldApplyPolicy(wave);

        bool isWarmup =
            BanditWordTrainer.Instance != null &&
            BanditWordTrainer.Instance.IsWarmupPhase(wave);

        for (int i = 0; i < previewWordCount; i++)
        {
            wordQueue.Enqueue(GetNextWord());
        }

        currentLetterIndex = 0;
        UpdateWordDisplay();
    }

    string CurrentWord => wordQueue.Peek();

    void AdvanceWord()
    {
        wordQueue.Dequeue();
        wordQueue.Enqueue(GetNextWord());

        currentLetterIndex = 0;
        wordCount++;
        LastWordPerfect = true;

        playerStats?.OnCorrectType(true);
        UpdateComboUI();
        UpdateWordDisplay();
    }

    string GetNextWord()
    {
        int wave = GameManager.Instance.currentWave;

        bool useBandit =
            BanditWordTrainer.Instance != null &&
            BanditWordTrainer.Instance.ShouldApplyPolicy(wave);

        bool isWarmup =
            BanditWordTrainer.Instance != null &&
            BanditWordTrainer.Instance.IsWarmupPhase(wave);

        if (useBandit)
        {
            var policy = BanditWordTrainer.Instance.GetCurrentPolicy();

            WordLengthBucket len = RollLengthBucket(policy.lenMix);
            FingerZone zone = RollZone(policy.zoneWeights);

            return wordLoader.GetRandomWordByLengthAndZone(len, zone);
        }
        else if (isWarmup)
        {
            return wordLoader.GetRandomWordMixed(
                warmupEasyWeight,
                warmupMediumWeight,
                warmupHardWeight
            );
        }
        // fallback
        return wordLoader.GetRandomWordMixed(0.5f, 0.3f, 0.2f);
    }

    public void ResetWordsForNewWave()
    {
        ResetTypingStats();
        ResetZoneMistakes();
        currentLetterIndex = 0;
        LastWordPerfect = false;

        InitializeWordStream();
    }

    void UpdateWordDisplay()
    {
        string display = "";
        int wordIndex = 0;

        foreach (string word in wordQueue)
        {
            if (wordIndex == 0)
            {
                for (int i = 0; i < word.Length; i++)
                {
                    char c = word[i];

                    if (i < currentLetterIndex)
                        display += $"<color=green>{c}</color>";
                    else if (i == currentLetterIndex)
                        display += $"<u>{c}</u>";
                    else
                        display += c;
                }
            }
            else
            {
                display += $"<color=white>{word}</color>";
            }

            display += "  ";
            wordIndex++;
        }

        wordDisplayText.text = display;
    }

    // UI Updates
    void UpdateWordStats()
    {
        wpmText.text = $"WPM: {Mathf.FloorToInt(GetWPM())}";
        // wordCountText.text = $"Words: {wordCount}";
    }

    // void UpdateTimerText()
    // {
    //     int minutes = Mathf.FloorToInt(elapsedTime / 60f);
    //     int seconds = Mathf.FloorToInt(elapsedTime % 60f);
    //     timerText.text = $"{minutes:00}:{seconds:00}";
    // }

    void UpdateComboUI()
    {
        if (comboStreakText == null || playerStats == null)
            return;

        if (!playerStats.comboUpgradeActive)
        {
            comboStreakText.text = "";
            lastComboBonus = 0;
            return;
        }

        int bonus = playerStats.currentComboBonus;

        if (bonus == 0)
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

        comboStreakText.text = $"Combo: {bonus} ({bonusText})";
    }


    IEnumerator FlashComboText()
    {
        Color originalColor = comboStreakText.color;
        comboStreakText.color = Color.yellow;
        yield return new WaitForSeconds(0.2f);
        comboStreakText.color = originalColor;
    }

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

    public void ResetTypingStats()
    {
        startTime = Time.time;
        elapsedTime = 0f;
        totalTypedCharacters = 0;
        wordCount = 0;
        mistakeCount = 0;

        lastKeyTime = 0f;
        reactionTimeSum = 0f;
        reactionSampleCount = 0;
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
        return mistakeCount;
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

        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.speed = 0;
                enemy.enabled = false;
            }
        }
    }
    // Mistakes Zones
    void ResetZoneMistakes()
    {
        zoneMistakes.Clear();
        foreach (FingerZone z in System.Enum.GetValues(typeof(FingerZone)))
            zoneMistakes[z] = 0;
    }

    public Dictionary<FingerZone, int> GetZoneMistakesSnapshot()
    {
        return zoneMistakes.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    WordLengthBucket RollLengthBucket((float s, float m, float l) mix)
    {
        float r = Random.value;
        if (r < mix.s) return WordLengthBucket.Short;
        if (r < mix.s + mix.m) return WordLengthBucket.Medium;
        return WordLengthBucket.Long;
    }

    FingerZone RollZone(float[] weights)
    {
        float r = Random.value;
        float acc = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            acc += weights[i];
            if (r <= acc)
                return (FingerZone)i;
        }
        return FingerZone.LeftIndex; // fallback
    }

    public float GetReactionTimeAvg()
    {
        return reactionSampleCount > 0
            ? reactionTimeSum / reactionSampleCount
            : 0f;
    }
}
