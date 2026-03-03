using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SaveStatisticsPanelUI : MonoBehaviour
{
    private struct RangeSelection
    {
        public List<SaveRunStatsData> data;
        public int startIndex;
    }

    private enum RangeMode
    {
        All,
        Last100,
        Last10
    }

    [Header("Root")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private Button backButton;

    [Header("Graph")]
    [SerializeField] private RectTransform graphArea;
    [SerializeField] private RectTransform graphContentRoot;
    [SerializeField] private Color lineColor = new Color(0.15f, 0.65f, 0.9f, 1f);
    [SerializeField] private Color pointColor = new Color(0.08f, 0.38f, 0.65f, 1f);
    [SerializeField] private bool showAccuracyLine = true;
    [SerializeField] private Color accuracyLineColor = new Color(0.95f, 0.45f, 0.2f, 1f);
    [SerializeField] private Color accuracyPointColor = new Color(0.75f, 0.25f, 0.1f, 1f);
    [SerializeField] private float lineThickness = 3f;
    [SerializeField] private float pointSize = 8f;
    [SerializeField] private Color axisColor = Color.black;
    [SerializeField] private float axisThickness = 2f;
    [SerializeField] private bool showXAxisTitles = true;
    [SerializeField] private bool showYAxisTitles = true;
    [SerializeField] private int axisTitleFontSize = 16;
    [SerializeField] private Color axisTitleColor = Color.black;
    [SerializeField] private Vector2 xAxisTitlePositionOffset = new Vector2(-4f, 2f);
    [SerializeField] private Vector2 yAxisTitlePositionOffset = new Vector2(4f, -2f);
    [SerializeField] private Vector2 xAxisTitleSize = new Vector2(80f, 24f);
    [SerializeField] private Vector2 yAxisTitleSize = new Vector2(60f, 24f);

    [Header("Hover Interaction")]
    [SerializeField] private float highlightedPointScale = 1.9f;
    [SerializeField] private Color hoverBandColor = new Color(0f, 0f, 0f, 0.09f);
    [SerializeField] private TMP_Text hoverInfoText;
    [SerializeField] private string hoverInfoDefaultText = "Hover over graph to inspect run details";

    [Header("Range Buttons")]
    [SerializeField] private Button allTimeButton;
    [SerializeField] private Button last100Button;
    [SerializeField] private Button last10Button;

    [Header("Detail")]
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private TMP_Text graphSummaryText;

    [Header("Axis Labels (Optional)")]
    [SerializeField] private TMP_Text yMinLabel;
    [SerializeField] private TMP_Text yMidLabel;
    [SerializeField] private TMP_Text yMaxLabel;
    [SerializeField] private TMP_Text xStartLabel;
    [SerializeField] private TMP_Text xMidLabel;
    [SerializeField] private TMP_Text xEndLabel;

    private SaveSlotData currentSlot;
    private RangeMode currentRangeMode = RangeMode.All;
    private int hoveredIndex = -1;

    private readonly List<GameObject> graphItems = new List<GameObject>();
    private readonly List<PointVisual> pointVisuals = new List<PointVisual>();

    private void Awake()
    {
        WireButtons();
    }

    public void Show(SaveSlotData slot)
    {
        currentSlot = slot;
        currentRangeMode = RangeMode.All;

        if (rootPanel != null)
            rootPanel.SetActive(true);
        else
            gameObject.SetActive(true);

        Refresh();
    }

    public void Hide()
    {
        ClearGraph();
        ClearAxisLabels();

        if (rootPanel != null)
            rootPanel.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public bool IsVisible()
    {
        return rootPanel != null ? rootPanel.activeSelf : gameObject.activeSelf;
    }

    private void WireButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(Hide);
        }

        if (allTimeButton != null)
        {
            allTimeButton.onClick.RemoveAllListeners();
            allTimeButton.onClick.AddListener(() => SetRange(RangeMode.All));
        }

        if (last100Button != null)
        {
            last100Button.onClick.RemoveAllListeners();
            last100Button.onClick.AddListener(() => SetRange(RangeMode.Last100));
        }

        if (last10Button != null)
        {
            last10Button.onClick.RemoveAllListeners();
            last10Button.onClick.AddListener(() => SetRange(RangeMode.Last10));
        }
    }

    private void SetRange(RangeMode mode)
    {
        currentRangeMode = mode;
        Refresh();
    }

    private void Refresh()
    {
        hoveredIndex = -1;
        SetHoverInfoText(hoverInfoDefaultText);

        if (currentSlot == null || !currentSlot.hasData)
        {
            if (detailText != null)
                detailText.text = "No data in this save slot.";

            if (graphSummaryText != null)
                graphSummaryText.text = "No play history";

            ClearAxisLabels();
            ClearGraph();
            return;
        }

        List<SaveRunStatsData> history = currentSlot.runHistory ?? new List<SaveRunStatsData>();
        RangeSelection selection = GetRangeSelection(history, currentRangeMode);
        List<SaveRunStatsData> rangeData = selection.data;

        DrawGraph(rangeData, selection.startIndex);
        UpdateSummary(history.Count, rangeData.Count);
        UpdateAxisLabels(rangeData, selection.startIndex, history.Count);
        UpdateDetail(currentSlot, history);
    }

    private void UpdateSummary(int totalPlayCount, int shownCount)
    {
        if (graphSummaryText == null)
            return;

        string rangeLabel = currentRangeMode switch
        {
            RangeMode.Last10 => "Last 10 plays",
            RangeMode.Last100 => "Last 100 plays",
            _ => "All time plays"
        };

        graphSummaryText.text = $"WPM Trend | {rangeLabel} | Showing {shownCount}/{totalPlayCount}";
    }

    private void UpdateDetail(SaveSlotData slot, List<SaveRunStatsData> history)
    {
        if (detailText == null)
            return;

        SaveRunStatsData latest = history.Count > 0
            ? history[history.Count - 1]
            : (slot.lastRunStats ?? new SaveRunStatsData());

        string worstFinger = string.IsNullOrEmpty(latest.worstFingerArea) ? "N/A" : latest.worstFingerArea;
        detailText.text =
            $"Latest Run\n" +
            $"Score: {latest.score}\n" +
            $"Total Time: {latest.totalTime:F1}s\n" +
            $"Highest Wave: {latest.highestWave}\n" +
            $"Currency: {latest.currency}\n" +
            $"Highest WPM: {latest.highestWPM:F1}\n" +
            $"Average WPM: {latest.averageWPM:F1}\n" +
            $"Average Accuracy: {latest.averageAccuracy * 100f:F1}%\n" +
            $"Worst Finger Area: {worstFinger}\n\n" +
            $"Total Plays in Save: {history.Count}";
    }

    private static RangeSelection GetRangeSelection(List<SaveRunStatsData> history, RangeMode mode)
    {
        if (history == null || history.Count == 0)
        {
            return new RangeSelection
            {
                data = new List<SaveRunStatsData>(),
                startIndex = 0
            };
        }

        int take = mode switch
        {
            RangeMode.Last10 => 10,
            RangeMode.Last100 => 100,
            _ => int.MaxValue
        };

        if (take >= history.Count)
        {
            return new RangeSelection
            {
                data = new List<SaveRunStatsData>(history),
                startIndex = 0
            };
        }

        int start = history.Count - take;
        return new RangeSelection
        {
            data = history.GetRange(start, take),
            startIndex = start
        };
    }

    private void DrawGraph(List<SaveRunStatsData> data, int rangeStartIndex)
    {
        ClearGraph();

        if (graphArea == null || graphContentRoot == null)
            return;

        if (data == null || data.Count == 0)
            return;

        float width = graphArea.rect.width;
        float height = graphArea.rect.height;

        if (width <= 0f || height <= 0f)
            return;

        DrawAxes(width, height);

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int i = 0; i < data.Count; i++)
        {
            float wpm = Mathf.Max(0f, data[i].highestWPM);
            minValue = Mathf.Min(minValue, wpm);
            maxValue = Mathf.Max(maxValue, wpm);

            if (showAccuracyLine)
            {
                float accuracyPercent = Mathf.Clamp01(data[i].averageAccuracy) * 100f;
                minValue = Mathf.Min(minValue, accuracyPercent);
                maxValue = Mathf.Max(maxValue, accuracyPercent);
            }
        }

        if (Mathf.Approximately(minValue, maxValue))
        {
            minValue = Mathf.Max(0f, minValue - 1f);
            maxValue = maxValue + 1f;
        }

        var wpmPoints = new List<Vector2>(data.Count);
        var accuracyPoints = new List<Vector2>(data.Count);
        for (int i = 0; i < data.Count; i++)
        {
            float tX = data.Count == 1 ? 0.5f : i / (float)(data.Count - 1);
            float x = tX * width;

            float wpm = Mathf.Max(0f, data[i].highestWPM);
            float wpmY = Mathf.InverseLerp(minValue, maxValue, wpm) * height;
            wpmPoints.Add(new Vector2(x, wpmY));

            if (showAccuracyLine)
            {
                float accuracyPercent = Mathf.Clamp01(data[i].averageAccuracy) * 100f;
                float accY = Mathf.InverseLerp(minValue, maxValue, accuracyPercent) * height;
                accuracyPoints.Add(new Vector2(x, accY));
            }
        }

        for (int i = 0; i < wpmPoints.Count - 1; i++)
            CreateLine(wpmPoints[i], wpmPoints[i + 1], lineColor);

        if (showAccuracyLine)
        {
            for (int i = 0; i < accuracyPoints.Count - 1; i++)
                CreateLine(accuracyPoints[i], accuracyPoints[i + 1], accuracyLineColor);
        }

        for (int i = 0; i < wpmPoints.Count; i++)
        {
            int playNumber = rangeStartIndex + i + 1;
            float wpm = Mathf.Max(0f, data[i].highestWPM);
            float accuracyPercent = Mathf.Clamp01(data[i].averageAccuracy) * 100f;
            RectTransform wpmPointRect = CreatePoint(wpmPoints[i], pointColor);
            RectTransform accuracyPointRect = null;

            if (showAccuracyLine)
                accuracyPointRect = CreatePoint(accuracyPoints[i], accuracyPointColor);

            pointVisuals.Add(new PointVisual
            {
                playNumber = playNumber,
                wpm = wpm,
                accuracyPercent = accuracyPercent,
                wpmPointRect = wpmPointRect,
                accuracyPointRect = accuracyPointRect,
                hoverBandImage = null
            });
        }

        CreateHoverZones(width, height);
    }

    private RectTransform CreatePoint(Vector2 anchoredPos, Color color)
    {
        var go = new GameObject("GraphPoint", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(graphContentRoot, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(pointSize, pointSize);

        var image = go.GetComponent<Image>();
        image.color = color;

        graphItems.Add(go);
        return rect;
    }

    private void CreateLine(Vector2 from, Vector2 to, Color color)
    {
        Vector2 dir = (to - from);
        float length = dir.magnitude;
        if (length <= 0.01f)
            return;

        var go = new GameObject("GraphLine", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(graphContentRoot, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = from;
        rect.sizeDelta = new Vector2(length, lineThickness);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rect.localEulerAngles = new Vector3(0f, 0f, angle);

        var image = go.GetComponent<Image>();
        image.color = color;

        graphItems.Add(go);
    }

    private void ClearGraph()
    {
        for (int i = 0; i < graphItems.Count; i++)
        {
            if (graphItems[i] != null)
                Destroy(graphItems[i]);
        }

        graphItems.Clear();
        pointVisuals.Clear();
    }

    private void DrawAxes(float width, float height)
    {
        CreateHorizontalAxis(width);
        CreateVerticalAxis(height);

        if (showXAxisTitles)
            CreateXAxisTitles(width);

        if (showYAxisTitles)           
            CreateYAxisTitles(height);
    }

    private void CreateHorizontalAxis(float width)
    {
        var go = new GameObject("GraphXAxis", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(graphContentRoot, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, axisThickness);

        var image = go.GetComponent<Image>();
        image.color = axisColor;

        graphItems.Add(go);
    }

    private void CreateVerticalAxis(float height)
    {
        var go = new GameObject("GraphYAxis", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(graphContentRoot, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(axisThickness, height);

        var image = go.GetComponent<Image>();
        image.color = axisColor;

        graphItems.Add(go);
    }

    private void CreateXAxisTitles(float width)
    {
        Vector2 xBase = new Vector2(width, 0f);

        CreateAxisTitle(
            "XAxisTitle",
            "Plays",
            xBase + xAxisTitlePositionOffset,
            new Vector2(1f, 0f),
            xAxisTitleSize);
    }

    private void CreateYAxisTitles(float height)
    {
        Vector2 yBase = new Vector2(0f, height);

        CreateAxisTitle(
            "YAxisTitle",
            "WPM",
            yBase + yAxisTitlePositionOffset,
            new Vector2(0f, 1f),
            yAxisTitleSize);
    }

    private void CreateAxisTitle(string name, string text, Vector2 anchoredPos, Vector2 pivot, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(graphContentRoot, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = axisTitleFontSize;
        label.color = axisTitleColor;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        graphItems.Add(go);
    }

    private void CreateHoverZones(float width, float height)
    {
        if (pointVisuals.Count == 0)
            return;

        for (int i = 0; i < pointVisuals.Count; i++)
        {
            float xCenter = pointVisuals[i].wpmPointRect != null
                ? pointVisuals[i].wpmPointRect.anchoredPosition.x
                : (i / (float)Mathf.Max(1, pointVisuals.Count - 1)) * width;

            float leftBoundary = i == 0
                ? 0f
                : 0.5f * (xCenter + pointVisuals[i - 1].wpmPointRect.anchoredPosition.x);
            float rightBoundary = i == pointVisuals.Count - 1
                ? width
                : 0.5f * (xCenter + pointVisuals[i + 1].wpmPointRect.anchoredPosition.x);

            float zoneWidth = Mathf.Max(2f, rightBoundary - leftBoundary);
            float zoneCenterX = leftBoundary + zoneWidth * 0.5f;

            Image hoverBand = CreateHoverBand(zoneCenterX, zoneWidth, height);
            var visual = pointVisuals[i];
            visual.hoverBandImage = hoverBand;
            pointVisuals[i] = visual;

            CreateHoverZone(i, zoneCenterX, zoneWidth, height);
        }
    }

    private Image CreateHoverBand(float centerX, float width, float height)
    {
        var go = new GameObject("HoverBand", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(graphContentRoot, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(centerX, 0f);
        rect.sizeDelta = new Vector2(width, height);

        var image = go.GetComponent<Image>();
        image.color = new Color(hoverBandColor.r, hoverBandColor.g, hoverBandColor.b, 0f);
        image.raycastTarget = false;
        rect.SetAsFirstSibling();

        graphItems.Add(go);
        return image;
    }

    private void CreateHoverZone(int index, float centerX, float width, float height)
    {
        var go = new GameObject("HoverZone", typeof(RectTransform), typeof(Image), typeof(HoverZoneHandler));
        go.transform.SetParent(graphContentRoot, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(centerX, 0f);
        rect.sizeDelta = new Vector2(width, height);

        var image = go.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.001f);
        image.raycastTarget = true;

        var handler = go.GetComponent<HoverZoneHandler>();
        handler.Initialize(index, this);

        graphItems.Add(go);
    }

    private void UpdateAxisLabels(List<SaveRunStatsData> rangeData, int rangeStartIndex, int totalCount)
    {
        if (rangeData == null || rangeData.Count == 0)
        {
            ClearAxisLabels();
            return;
        }

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        for (int i = 0; i < rangeData.Count; i++)
        {
            float wpm = Mathf.Max(0f, rangeData[i].highestWPM);
            minValue = Mathf.Min(minValue, wpm);
            maxValue = Mathf.Max(maxValue, wpm);

            if (showAccuracyLine)
            {
                float accuracyPercent = Mathf.Clamp01(rangeData[i].averageAccuracy) * 100f;
                minValue = Mathf.Min(minValue, accuracyPercent);
                maxValue = Mathf.Max(maxValue, accuracyPercent);
            }
        }

        if (Mathf.Approximately(minValue, maxValue))
        {
            minValue = Mathf.Max(0f, minValue - 1f);
            maxValue = maxValue + 1f;
        }

        float midValue = (minValue + maxValue) * 0.5f;
        SetLabel(yMinLabel, $"{minValue:F1}");
        SetLabel(yMidLabel, $"{midValue:F1}");
        SetLabel(yMaxLabel, $"{maxValue:F1}");

        int startPlay = rangeStartIndex + 1;
        int endPlay = rangeStartIndex + rangeData.Count;
        int midPlay = startPlay + (endPlay - startPlay) / 2;

        SetLabel(xStartLabel, $"Play {startPlay}");
        SetLabel(xMidLabel, $"Play {midPlay}");
        SetLabel(xEndLabel, $"Play {endPlay}");

        if (totalCount <= 0)
            SetLabel(xEndLabel, "Play 0");
    }

    private void ClearAxisLabels()
    {
        SetLabel(yMinLabel, "-");
        SetLabel(yMidLabel, "-");
        SetLabel(yMaxLabel, "-");
        SetLabel(xStartLabel, "-");
        SetLabel(xMidLabel, "-");
        SetLabel(xEndLabel, "-");
    }

    private static void SetLabel(TMP_Text label, string value)
    {
        if (label != null)
            label.text = value;
    }

    private void OnHoverZoneEnter(int index)
    {
        if (index < 0 || index >= pointVisuals.Count)
            return;

        hoveredIndex = index;
        ApplyHoverVisualState();

        PointVisual visual = pointVisuals[index];
        SetHoverInfoText($"Play {visual.playNumber} | WPM: {visual.wpm:F1} | Accuracy: {visual.accuracyPercent:F1}%");
    }

    private void OnHoverZoneExit(int index)
    {
        if (hoveredIndex != index)
            return;

        hoveredIndex = -1;
        ApplyHoverVisualState();
        SetHoverInfoText(hoverInfoDefaultText);
    }

    private void ApplyHoverVisualState()
    {
        for (int i = 0; i < pointVisuals.Count; i++)
        {
            bool isHovered = i == hoveredIndex;
            PointVisual visual = pointVisuals[i];

            if (visual.wpmPointRect != null)
                visual.wpmPointRect.sizeDelta = Vector2.one * (isHovered ? pointSize * highlightedPointScale : pointSize);

            if (visual.accuracyPointRect != null)
                visual.accuracyPointRect.sizeDelta = Vector2.one * (isHovered ? pointSize * highlightedPointScale : pointSize);

            if (visual.hoverBandImage != null)
            {
                Color c = hoverBandColor;
                c.a = isHovered ? hoverBandColor.a : 0f;
                visual.hoverBandImage.color = c;
            }
        }
    }

    private void SetHoverInfoText(string message)
    {
        if (hoverInfoText != null)
            hoverInfoText.text = message;
    }

    private struct PointVisual
    {
        public int playNumber;
        public float wpm;
        public float accuracyPercent;
        public RectTransform wpmPointRect;
        public RectTransform accuracyPointRect;
        public Image hoverBandImage;
    }

    private sealed class HoverZoneHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private int zoneIndex;
        private SaveStatisticsPanelUI owner;

        public void Initialize(int zoneIndex, SaveStatisticsPanelUI owner)
        {
            this.zoneIndex = zoneIndex;
            this.owner = owner;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.OnHoverZoneEnter(zoneIndex);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.OnHoverZoneExit(zoneIndex);
        }
    }
}
