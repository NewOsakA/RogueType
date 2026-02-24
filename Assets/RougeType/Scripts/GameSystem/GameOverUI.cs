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
    public TMP_Text wpmText;
    public TMP_Text avgWpmText;
    public TMP_Text avgAccuracyText;
    public TMP_Text worstFingerAreaText;

    /// <summary>
    /// Show the panel and update all stat labels.
    /// Uses unscaled time so it works even when Time.timeScale = 0.
    /// </summary>
    public void Show(
        int score,
        float totalTime,
        int highestWave,
        int currency,
        float highestWPM,
        float averageAccuracy,
        float averageWPM,
        string worstFingerArea)
    {
        gameObject.SetActive(true);

        if (scoreText) scoreText.text = $"Score: {score}";
        if (timeText) timeText.text = $"Total Time: {totalTime:F1}s";
        if (waveText) waveText.text = $"Highest Wave: {highestWave}";
        if (currencyText) currencyText.text = $"Currency: {currency}";
        if (wpmText) wpmText.text = $"Highest WPM: {highestWPM:F1}";
        if (avgWpmText) avgWpmText.text = $"Average WPM: {averageWPM:F1}";
        if (avgAccuracyText) avgAccuracyText.text = $"Average Accuracy: {averageAccuracy * 100f:F1}%";
        if (worstFingerAreaText) worstFingerAreaText.text = $"Worst Finger Area: {worstFingerArea}";
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
