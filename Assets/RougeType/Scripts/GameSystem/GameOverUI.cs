using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Shows Game Over popup and fills player statistics.
/// Attach this to GameOverPanel.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Stat Texts (TMP)")]
    public TMP_Text scoreText;
    public TMP_Text timeText;
    public TMP_Text waveText;
    public TMP_Text currencyText;
    public TMP_Text essenceText;
    public TMP_Text wpmText;

    /// <summary>
    /// Show the panel and update all stat labels.
    /// Uses unscaled time so it works even when Time.timeScale = 0.
    /// </summary>
    public void Show(int score, float totalTime, int highestWave, int currency, int essence, float highestWPM)
    {
        gameObject.SetActive(true);

        if (scoreText) scoreText.text = $"Score: {score}";
        if (timeText) timeText.text = $"Total Time: {totalTime:F1}s";
        if (waveText) waveText.text = $"Highest Wave: {highestWave}";
        if (currencyText) currencyText.text = $"Currency: {currency}";
        if (essenceText) essenceText.text = $"Essence: {essence}";
        if (wpmText) wpmText.text = $"Highest WPM: {highestWPM:F1}";
    }

    /// <summary>
    /// Called by the button.
    /// </summary>
    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Upgrade");
    }
}
