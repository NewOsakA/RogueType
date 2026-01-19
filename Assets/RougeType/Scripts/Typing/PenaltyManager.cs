using UnityEngine;
using TMPro;

public class PenaltyManager : MonoBehaviour
{
    [Header("Wave Thresholds")]
    public int waveThresholdDamagePenalty = 3;
    public int waveThresholdStunPenalty = 6;

    [Header("Penalty Effects")]
    public float damageReductionPercent = 0.5f;
    public float stunDurationSeconds = 1f;

    [Header("Penalty Durations")]
    public float durationReducedDamage = 10f;
    public float durationStun = 7f;

    [Header("UI")]
    public TMP_Text penaltyStatusText;

    [Header("Reset Conditions")]
    public int correctWordsToClearPenalty = 2;

    private int mistakeCount = 0;
    private int correctStreak = 0;

    private string currentPenalty = "None";
    private bool penaltyActive = false;
    private float penaltyTimer = 0f;
    private int currentWave = 1;

    void Update()
    {
        if (penaltyActive)
        {
            penaltyTimer -= Time.deltaTime;
            if (penaltyTimer <= 0f)
            {
                ClearPenalty();
            }
            else
            {
                UpdatePenaltyText();
            }
        }
    }

    public void SetWave(int wave)
    {
        currentWave = wave;

        //Reset mistake count each wave
        mistakeCount = 0;
        correctStreak = 0;

        UpdatePenaltyText();
    }

    public void RegisterMistake()
    {
        mistakeCount++;
        correctStreak = 0;

        if (!penaltyActive && mistakeCount >= 5)
        {
            ApplyPenalty();
        }
    }

    public void RegisterCorrectWord()
    {
        correctStreak++;

        if (mistakeCount > 0)
            mistakeCount--;

        if (penaltyActive && correctStreak >= correctWordsToClearPenalty)
        {
            Debug.Log("Penalty cleared by correct typing streak");
            ClearPenalty();
            correctStreak = 0;
        }
    }


    private void ApplyPenalty()
    {
        if (currentWave >= waveThresholdStunPenalty)
        {
            currentPenalty = "Stun";
            penaltyTimer = durationStun;
        }
        else if (currentWave >= waveThresholdDamagePenalty)
        {
            currentPenalty = "ReducedDamage";
            penaltyTimer = durationReducedDamage;
        }
        else
        {
            currentPenalty = "None";
            return;
        }

        penaltyActive = true;
        UpdatePenaltyText();
        Debug.Log($"Penalty Applied: {currentPenalty} for {penaltyTimer} seconds");
    }

    public void ClearPenalty()
    {
        penaltyActive = false;
        currentPenalty = "None";
        penaltyTimer = 0f;
        UpdatePenaltyText();
    }

    public string GetCurrentPenalty()
    {
        return penaltyActive ? currentPenalty : "None";
    }

    public float GetDamageMultiplier()
    {
        return (currentPenalty == "ReducedDamage") ? damageReductionPercent : 1f;
    }

    public bool ShouldStun()
    {
        return currentPenalty == "Stun";
    }

    private void UpdatePenaltyText()
    {
        if (penaltyStatusText != null)
        {
            if (penaltyActive)
            {
                penaltyStatusText.text = $"Penalty: {currentPenalty} ({penaltyTimer:F1}s left)";
            }
            else
            {
                penaltyStatusText.text = "Penalty: None";
            }
        }
    }

    public int GetMistakeCount()
    {
        return mistakeCount;
    }
}
