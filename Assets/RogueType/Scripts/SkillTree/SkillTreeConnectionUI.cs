using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class SkillTreeConnectionUI : MonoBehaviour
{
    private enum ConnectionState
    {
        Dim,
        Blinking,
        Glowing
    }

    [Header("Nodes")]
    [SerializeField] private UpgradeNode fromNode;
    [SerializeField] private UpgradeNode toNode;

    [Header("Line Visual")]
    [SerializeField] private RectTransform lineRect;
    [SerializeField] private Image lineImage;
    [SerializeField] private float lineThickness = 12f;
    [SerializeField] private float nodeEdgePadding = 36f;

    [Header("Colors")]
    [SerializeField] private Color dimColor = new Color(0.45f, 0.45f, 0.5f, 0.45f);
    [SerializeField] private Color blinkingColorA = new Color(0.45f, 0.95f, 1f, 0.45f);
    [SerializeField] private Color blinkingColorB = new Color(0.7f, 1f, 1f, 1f);
    [SerializeField] private Color glowingColorA = new Color(0.3f, 1f, 0.95f, 0.75f);
    [SerializeField] private Color glowingColorB = new Color(0.8f, 1f, 1f, 1f);

    [Header("Animation")]
    [SerializeField] private float blinkingSpeed = 3f;
    [SerializeField] private float glowingSpeed = 1.5f;
    [SerializeField] private float glowingThicknessMultiplier = 1.2f;

    private ConnectionState currentState = ConnectionState.Dim;

    public UpgradeNode FromNode => fromNode;
    public UpgradeNode ToNode => toNode;

    void Reset()
    {
        CacheReferences();
        UpdateLineTransform();
    }

    void Awake()
    {
        CacheReferences();
    }

    void OnEnable()
    {
        CacheReferences();
        Refresh();
    }

    void OnValidate()
    {
        CacheReferences();
        UpdateLineTransform();
        ApplyCurrentState(0f);
    }

    void LateUpdate()
    {
        if (fromNode == null || toNode == null)
            return;

        UpdateLineTransform();
        ApplyCurrentState(Application.isPlaying ? Time.unscaledDeltaTime : 0f);
    }

    public void Refresh()
    {
        CacheReferences();
        UpdateLineTransform();
        ResolveState();
        ApplyCurrentState(0f);
    }

    private void CacheReferences()
    {
        if (lineRect == null)
            lineRect = transform as RectTransform;

        if (lineImage == null)
            lineImage = GetComponent<Image>();
    }

    private void ResolveState()
    {
        SkillTreeUpgradeManager manager = SkillTreeUpgradeManager.Instance;
        if (manager == null || fromNode == null || toNode == null || fromNode.data == null || toNode.data == null)
        {
            currentState = ConnectionState.Dim;
            return;
        }

        bool fromUnlocked = manager.IsUnlocked(fromNode.data);
        bool toUnlocked = manager.IsUnlocked(toNode.data);

        if (fromUnlocked && toUnlocked)
        {
            currentState = ConnectionState.Glowing;
            return;
        }

        if (fromUnlocked && manager.IsPurchasable(toNode.data))
        {
            currentState = ConnectionState.Blinking;
            return;
        }

        currentState = ConnectionState.Dim;
    }

    private void UpdateLineTransform()
    {
        if (lineRect == null || fromNode == null || toNode == null)
            return;

        RectTransform fromRect = fromNode.transform as RectTransform;
        RectTransform toRect = toNode.transform as RectTransform;
        RectTransform parentRect = lineRect.parent as RectTransform;

        if (fromRect == null || toRect == null || parentRect == null)
            return;

        Vector2 start = GetNodeCenterInParentSpace(fromRect, parentRect);
        Vector2 end = GetNodeCenterInParentSpace(toRect, parentRect);

        Vector2 direction = end - start;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
            return;

        Vector2 normalized = direction / distance;
        Vector2 paddedStart = start + normalized * nodeEdgePadding;
        Vector2 paddedEnd = end - normalized * nodeEdgePadding;

        Vector2 paddedDirection = paddedEnd - paddedStart;
        float paddedDistance = Mathf.Max(1f, paddedDirection.magnitude);
        float angle = Mathf.Atan2(paddedDirection.y, paddedDirection.x) * Mathf.Rad2Deg;

        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0f, 0.5f);
        lineRect.anchoredPosition = paddedStart;
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        lineRect.sizeDelta = new Vector2(paddedDistance, GetAnimatedThickness());
    }

    private Vector2 GetNodeCenterInParentSpace(RectTransform nodeRect, RectTransform parentRect)
    {
        Vector3 worldCenter = nodeRect.TransformPoint(nodeRect.rect.center);
        Vector3 localCenter = parentRect.InverseTransformPoint(worldCenter);
        return localCenter;
    }

    private void ApplyCurrentState(float _)
    {
        if (lineImage == null)
            return;

        float pulseTime = Application.isPlaying ? Time.unscaledTime : 0f;

        switch (currentState)
        {
            case ConnectionState.Glowing:
                lineImage.color = Color.Lerp(
                    glowingColorA,
                    glowingColorB,
                    PingPong01(pulseTime * glowingSpeed)
                );
                break;

            case ConnectionState.Blinking:
                lineImage.color = Color.Lerp(
                    blinkingColorA,
                    blinkingColorB,
                    PingPong01(pulseTime * blinkingSpeed)
                );
                break;

            default:
                lineImage.color = dimColor;
                break;
        }

        if (lineRect != null)
            lineRect.sizeDelta = new Vector2(lineRect.sizeDelta.x, GetAnimatedThickness());
    }

    private float GetAnimatedThickness()
    {
        if (currentState != ConnectionState.Glowing || !Application.isPlaying)
            return lineThickness;

        float t = PingPong01(Time.unscaledTime * glowingSpeed);
        return Mathf.Lerp(lineThickness, lineThickness * glowingThicknessMultiplier, t);
    }

    private float PingPong01(float value)
    {
        return Mathf.PingPong(value, 1f);
    }
}
