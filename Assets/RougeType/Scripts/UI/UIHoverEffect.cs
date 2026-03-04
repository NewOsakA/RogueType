using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image image;
    private Color originalColor;
    private Vector3 originalScale;

    [SerializeField] private float darkenMultiplier = 0.8f;   // ทำให้เข้มขึ้น
    [SerializeField] private float scaleMultiplier = 1.05f;  // ขยายเล็กน้อย

    void Awake()
    {
        image = GetComponent<Image>();
        originalColor = image.color;
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        image.color = originalColor * darkenMultiplier;
        transform.localScale = originalScale * scaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        image.color = originalColor;
        transform.localScale = originalScale;
    }
}