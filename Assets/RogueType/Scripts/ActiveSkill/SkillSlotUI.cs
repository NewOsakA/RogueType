using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI cooldownText;

    public Color readyColor = Color.white;
    public Color cooldownColor = new Color(0.7f, 0.7f, 0.7f);
    public Color lockedColor = new Color(0.35f, 0.35f, 0.35f);

    void ClearText()
    {
        if (cooldownText != null)
            cooldownText.text = "";
    }

    public void SetReady(int essenceCost)
    {
        if (icon != null)
            icon.color = readyColor;

        if (cooldownText != null)
            cooldownText.text = $"{essenceCost}";
    }

    public void SetCooldown(float time)
    {
        if (icon != null)
            icon.color = cooldownColor;

        if (cooldownText != null)
            cooldownText.text = $"{time:F1}s";
    }

    public void SetLocked()
    {
        if (icon != null)
            icon.color = lockedColor;

        ClearText();
    }

    public void SetNoEssence(int essenceCost)
    {
        if (icon != null)
            icon.color = cooldownColor;

        if (cooldownText != null)
            cooldownText.text = $"{essenceCost}";
    }
}