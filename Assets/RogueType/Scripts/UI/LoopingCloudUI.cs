using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class LoopingCloudUI : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float leftResetOffset = 100f;
    [SerializeField] private float rightBoundsPadding = 100f;

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private float spriteWidth;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = rectTransform.parent as RectTransform;
        spriteWidth = rectTransform.rect.width;
    }

    private void Update()
    {
        if (parentRect == null)
            return;

        Vector2 anchoredPosition = rectTransform.anchoredPosition;
        anchoredPosition.x += speed * Time.deltaTime;

        float rightLimit = parentRect.rect.width + rightBoundsPadding;
        if (anchoredPosition.x > rightLimit)
            anchoredPosition.x = -spriteWidth - leftResetOffset;

        rectTransform.anchoredPosition = anchoredPosition;
    }
}
