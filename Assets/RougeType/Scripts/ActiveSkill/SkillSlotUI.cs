using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI cooldownText;

    public Color readyColor = Color.white;
    public Color cooldownColor = Color.gray;
    public Color noEssenceColor = Color.gray;

    public void SetReady()
    {
        if (icon != null)
            icon.color = readyColor;

        if (cooldownText != null)
            cooldownText.text = "Ready";
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
            icon.color = cooldownColor;

        if (cooldownText != null)
            cooldownText.text = "Locked";
    }

    public void SetNoEssence()
    {
        if (icon != null)
            icon.color = noEssenceColor;

        if (cooldownText != null)
            cooldownText.text = "Wait";
    }
}
