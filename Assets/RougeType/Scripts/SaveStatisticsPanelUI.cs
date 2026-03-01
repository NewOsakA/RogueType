using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private float lineThickness = 3f;
    [SerializeField] private float pointSize = 8f;
    [SerializeField] private Color axisColor = Color.black;
    [SerializeField] private float axisThickness = 2f;
    [SerializeField] private bool showAxisTitles = true;
    [SerializeField] private int axisTitleFontSize = 16;
    [SerializeField] private Color axisTitleColor = Color.black;
    [SerializeField] private Vector2 xAxisTitlePositionOffset = new Vector2(-4f, 2f);
    [SerializeField] private Vector2 yAxisTitlePositionOffset = new Vector2(4f, -2f);
    [SerializeField] private Vector2 xAxisTitleSize = new Vector2(80f, 24f);
    [SerializeField] private Vector2 yAxisTitleSize = new Vector2(60f, 24f);

    [Header("Point Labels")]
    [SerializeField] private bool showPointLabels = true;
    [SerializeField] private int pointLabelFontSize = 14;
    [SerializeField] private Vector2 pointLabelOffset = new Vector2(0f, 16f);
    [SerializeField] private Color pointLabelColor = Color.black;

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

    private readonly List<GameObject> graphItems = new List<GameObject>();

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

        DrawWpmGraph(rangeData, selection.startIndex);
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

    private void DrawWpmGraph(List<SaveRunStatsData> data, int rangeStartIndex)
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

        float minWpm = float.MaxValue;
        float maxWpm = float.MinValue;

        for (int i = 0; i < data.Count; i++)
        {
            float wpm = Mathf.Max(0f, data[i].highestWPM);
            minWpm = Mathf.Min(minWpm, wpm);
            maxWpm = Mathf.Max(maxWpm, wpm);
        }

        if (Mathf.Approximately(minWpm, maxWpm))
        {
            minWpm = Mathf.Max(0f, minWpm - 1f);
            maxWpm = maxWpm + 1f;
        }

        var points = new List<Vector2>(data.Count);
        for (int i = 0; i < data.Count; i++)
        {
            float tX = data.Count == 1 ? 0.5f : i / (float)(data.Count - 1);
            float x = tX * width;

            float wpm = Mathf.Max(0f, data[i].highestWPM);
            float tY = Mathf.InverseLerp(minWpm, maxWpm, wpm);
            float y = tY * height;

            points.Add(new Vector2(x, y));
        }

        for (int i = 0; i < points.Count - 1; i++)
            CreateLine(points[i], points[i + 1]);

        for (int i = 0; i < points.Count; i++)
        {
            int playNumber = rangeStartIndex + i + 1;
            float wpm = Mathf.Max(0f, data[i].highestWPM);
            CreatePoint(points[i], playNumber, wpm);
        }
    }

    private void CreatePoint(Vector2 anchoredPos, int playNumber, float wpm)
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
        image.color = pointColor;

        graphItems.Add(go);

        if (showPointLabels)
            CreatePointLabel(anchoredPos, playNumber, wpm);
    }

    private void CreateLine(Vector2 from, Vector2 to)
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
        image.color = lineColor;

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
    }

    private void DrawAxes(float width, float height)
    {
        CreateHorizontalAxis(width);
        CreateVerticalAxis(height);

        if (showAxisTitles)
            CreateAxisTitles(width, height);
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

    private void CreateAxisTitles(float width, float height)
    {
        Vector2 xBase = new Vector2(width, 0f);
        Vector2 yBase = new Vector2(0f, height);

        CreateAxisTitle(
            "XAxisTitle",
            "Plays",
            xBase + xAxisTitlePositionOffset,
            new Vector2(1f, 0f),
            xAxisTitleSize);

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

    private void CreatePointLabel(Vector2 anchoredPos, int playNumber, float wpm)
    {
        var go = new GameObject("GraphPointLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(graphContentRoot, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPos + pointLabelOffset;
        rect.sizeDelta = new Vector2(120f, 32f);

        var label = go.GetComponent<TextMeshProUGUI>();
        label.fontSize = pointLabelFontSize;
        label.color = pointLabelColor;
        label.alignment = TextAlignmentOptions.Center;
        label.text = $"P{playNumber} | {wpm:F1}";
        label.raycastTarget = false;

        graphItems.Add(go);
    }

    private void UpdateAxisLabels(List<SaveRunStatsData> rangeData, int rangeStartIndex, int totalCount)
    {
        if (rangeData == null || rangeData.Count == 0)
        {
            ClearAxisLabels();
            return;
        }

        float minWpm = float.MaxValue;
        float maxWpm = float.MinValue;
        for (int i = 0; i < rangeData.Count; i++)
        {
            float wpm = Mathf.Max(0f, rangeData[i].highestWPM);
            minWpm = Mathf.Min(minWpm, wpm);
            maxWpm = Mathf.Max(maxWpm, wpm);
        }

        if (Mathf.Approximately(minWpm, maxWpm))
        {
            minWpm = Mathf.Max(0f, minWpm - 1f);
            maxWpm = maxWpm + 1f;
        }

        float midWpm = (minWpm + maxWpm) * 0.5f;
        SetLabel(yMinLabel, $"{minWpm:F1}");
        SetLabel(yMidLabel, $"{midWpm:F1}");
        SetLabel(yMaxLabel, $"{maxWpm:F1}");

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
}
