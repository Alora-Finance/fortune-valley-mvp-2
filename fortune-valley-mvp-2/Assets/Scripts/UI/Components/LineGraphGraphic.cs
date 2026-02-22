using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.UI;

// Allow the Editor test assembly to access the internal GraphLayout class.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("FortuneValley.Tests.Editor")]

namespace FortuneValley.UI.Components
{
    /// <summary>
    /// Reusable UI Graphic that renders a line graph using VertexHelper quads.
    /// Integrates with Canvas batching — no Texture2D pixel-painting (WebGL safe).
    /// Used for both the portfolio overview graph and per-stock price history.
    /// </summary>
    public class LineGraphGraphic : Graphic
    {
        // ═══════════════════════════════════════════════════════════════
        // INSPECTOR
        // ═══════════════════════════════════════════════════════════════

        [SerializeField] private Color _lineColor = new Color(0.3f, 0.9f, 1f); // cyan
        [SerializeField] private float _lineWidth = 2.5f;
        [SerializeField] private Color _gridLineColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField] private int _gridLineCount = 4;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private List<float> _data = new List<float>();
        private int _startDayLabel;

        // Axis labels created lazily on the first SetData() call (rect is valid by then).
        private bool _labelsCreated;
        private TextMeshProUGUI[] _xLabels;      // 5 X-axis labels
        private TextMeshProUGUI[] _yLabels;      // 4 Y-axis labels (one per grid line)
        private TextMeshProUGUI _collectingLabel; // shown when data < 2

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Supply data to display.
        /// Axis labels are created lazily on the first call (guarantees Canvas rect is valid).
        /// If data is null or fewer than 2 points, shows "Collecting data…" and clears the mesh.
        /// </summary>
        public void SetData(IReadOnlyList<float> data, int startDayLabel)
        {
            // Lazy creation — only after layout is established
            if (!_labelsCreated)
                CreateAxisLabels();

            bool hasEnoughData = data != null && data.Count >= 2;

            if (!hasEnoughData)
            {
                _data.Clear();
                SetLabelsVisible(false);
                if (_collectingLabel != null) _collectingLabel.gameObject.SetActive(true);
                SetVerticesDirty();
                return;
            }

            // Copy data (caller retains ownership of the source list)
            _data.Clear();
            foreach (float v in data) _data.Add(v);
            _startDayLabel = startDayLabel;

            if (_collectingLabel != null) _collectingLabel.gameObject.SetActive(false);
            SetLabelsVisible(true);
            UpdateAxisLabels();
            SetVerticesDirty(); // defers mesh rebuild to Canvas batch pass
        }

        // ═══════════════════════════════════════════════════════════════
        // MESH GENERATION
        // ═══════════════════════════════════════════════════════════════

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (_data == null || _data.Count < 2) return;

            Rect rect = rectTransform.rect;
            var (paddedMin, paddedMax) = GraphLayout.ComputeYRange(_data);

            // Draw 4 horizontal grid lines — critical for educational readability
            for (int g = 0; g < _gridLineCount; g++)
            {
                float t = (g + 1f) / (_gridLineCount + 1f);
                float y = Mathf.Lerp(rect.yMin, rect.yMax, t);
                AddHorizontalQuad(vh, rect.xMin, rect.xMax, y, 0.5f, _gridLineColor);
            }

            // Draw line as a series of quads — each segment is a rotated rectangle
            for (int i = 0; i < _data.Count - 1; i++)
            {
                Vector2 p0 = GraphLayout.MapPoint(_data[i],     i,     _data.Count, rect, paddedMin, paddedMax);
                Vector2 p1 = GraphLayout.MapPoint(_data[i + 1], i + 1, _data.Count, rect, paddedMin, paddedMax);
                AddLineSegment(vh, p0, p1, _lineWidth, _lineColor);
            }
        }

        /// <summary>Thin horizontal quad for a grid line.</summary>
        private static void AddHorizontalQuad(VertexHelper vh, float xMin, float xMax,
            float y, float halfThick, Color color)
        {
            int idx = vh.currentVertCount;
            UIVertex v = UIVertex.simpleVert;
            v.color = color;

            v.position = new Vector3(xMin, y - halfThick, 0); vh.AddVert(v);
            v.position = new Vector3(xMin, y + halfThick, 0); vh.AddVert(v);
            v.position = new Vector3(xMax, y + halfThick, 0); vh.AddVert(v);
            v.position = new Vector3(xMax, y - halfThick, 0); vh.AddVert(v);

            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 2, idx + 3);
        }

        /// <summary>Line segment as a quad rotated along the segment direction.</summary>
        private static void AddLineSegment(VertexHelper vh, Vector2 p0, Vector2 p1,
            float width, Color color)
        {
            Vector2 dir    = (p1 - p0).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x) * (width * 0.5f);

            int idx = vh.currentVertCount;
            UIVertex v = UIVertex.simpleVert;
            v.color = color;

            v.position = new Vector3(p0.x - normal.x, p0.y - normal.y, 0); vh.AddVert(v);
            v.position = new Vector3(p0.x + normal.x, p0.y + normal.y, 0); vh.AddVert(v);
            v.position = new Vector3(p1.x + normal.x, p1.y + normal.y, 0); vh.AddVert(v);
            v.position = new Vector3(p1.x - normal.x, p1.y - normal.y, 0); vh.AddVert(v);

            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 2, idx + 3);
        }

        // ═══════════════════════════════════════════════════════════════
        // AXIS LABELS (lazy creation — called once after layout)
        // ═══════════════════════════════════════════════════════════════

        private void CreateAxisLabels()
        {
            _labelsCreated = true;

            // "Collecting data…" — shown when fewer than 2 data points
            var cGo = new GameObject("CollectingLabel", typeof(RectTransform));
            cGo.transform.SetParent(transform, false);
            var cRT = cGo.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0.5f, 0.5f);
            cRT.anchorMax = new Vector2(0.5f, 0.5f);
            cRT.anchoredPosition = Vector2.zero;
            cRT.sizeDelta = new Vector2(200f, 30f);
            _collectingLabel = cGo.AddComponent<TextMeshProUGUI>();
            _collectingLabel.text = "Collecting data\u2026";
            _collectingLabel.fontSize = 12;
            _collectingLabel.color = new Color(1f, 1f, 1f, 0.6f);
            _collectingLabel.alignment = TextAlignmentOptions.Center;
            cGo.SetActive(false);

            // 5 X-axis labels positioned below the rect
            _xLabels = new TextMeshProUGUI[5];
            for (int i = 0; i < 5; i++)
            {
                var go = new GameObject($"XLabel_{i}", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.sizeDelta = new Vector2(60f, 20f);
                rt.pivot = new Vector2(0.5f, 1f);
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 9;
                tmp.color = new Color(1f, 1f, 1f, 0.7f);
                tmp.alignment = TextAlignmentOptions.Center;
                _xLabels[i] = tmp;
                go.SetActive(false);
            }

            // 4 Y-axis labels positioned to the left of the rect
            _yLabels = new TextMeshProUGUI[4];
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject($"YLabel_{i}", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.sizeDelta = new Vector2(50f, 20f);
                rt.pivot = new Vector2(1f, 0.5f);
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 9;
                tmp.color = new Color(1f, 1f, 1f, 0.7f);
                tmp.alignment = TextAlignmentOptions.Right;
                _yLabels[i] = tmp;
                go.SetActive(false);
            }
        }

        private void UpdateAxisLabels()
        {
            if (_data == null || _data.Count < 2) return;

            Rect rect = rectTransform.rect;
            int count = _data.Count;
            var (paddedMin, paddedMax) = GraphLayout.ComputeYRange(_data);

            // X labels: 5 evenly spaced across the window (may show negative day numbers)
            if (_xLabels != null)
            {
                for (int i = 0; i < _xLabels.Length; i++)
                {
                    if (_xLabels[i] == null) continue;

                    float frac     = i / (float)(_xLabels.Length - 1);
                    int   dayIndex = Mathf.RoundToInt(frac * (count - 1));
                    int   dayLabel = _startDayLabel + dayIndex;

                    UIBuilderUtils.SetTextIfChanged(_xLabels[i], $"Day {dayLabel}");

                    var rt = _xLabels[i].GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, frac), rect.yMin - 4f);
                    _xLabels[i].gameObject.SetActive(true);
                }
            }

            // Y labels: one per grid line, matching the horizontal lines drawn in OnPopulateMesh
            if (_yLabels != null)
            {
                for (int g = 0; g < _gridLineCount && g < _yLabels.Length; g++)
                {
                    if (_yLabels[g] == null) continue;

                    float t     = (g + 1f) / (_gridLineCount + 1f);
                    float value = Mathf.Lerp(paddedMin, paddedMax, t);
                    float yPos  = Mathf.Lerp(rect.yMin, rect.yMax, t);

                    UIBuilderUtils.SetTextIfChanged(_yLabels[g], $"${Mathf.RoundToInt(value)}");

                    var rt = _yLabels[g].GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(rect.xMin - 4f, yPos);
                    _yLabels[g].gameObject.SetActive(true);
                }
            }
        }

        private void SetLabelsVisible(bool visible)
        {
            if (_xLabels != null)
                foreach (var l in _xLabels)
                    if (l != null) l.gameObject.SetActive(visible);

            if (_yLabels != null)
                foreach (var l in _yLabels)
                    if (l != null) l.gameObject.SetActive(visible);
        }

        // ═══════════════════════════════════════════════════════════════
        // INNER STATIC CLASS — pure math, testable without Canvas setup
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Pure layout helpers with no MonoBehaviour dependency.
        /// Marked internal so unit tests can reach it via InternalsVisibleTo.
        /// </summary>
        internal static class GraphLayout
        {
            /// <summary>
            /// Compute padded Y range for the graph.
            /// Uses a minimum range of 1f to prevent degenerate display on flat data.
            /// Padding: 8% above and below the actual min/max.
            /// </summary>
            internal static (float paddedMin, float paddedMax) ComputeYRange(IReadOnlyList<float> data)
            {
                float min = float.MaxValue;
                float max = float.MinValue;
                foreach (float v in data)
                {
                    if (v < min) min = v;
                    if (v > max) max = v;
                }

                float range = max - min;
                if (range < 1f) range = 1f; // floor avoids divide-by-zero on flat data

                float paddedMin = min - range * 0.08f;
                float paddedMax = max + range * 0.08f;
                return (paddedMin, paddedMax);
            }

            /// <summary>
            /// Map a value + index to a 2D canvas point inside rect.
            /// index 0 → left edge, index count-1 → right edge.
            /// value == paddedMin → bottom of rect, value == paddedMax → top.
            /// </summary>
            internal static Vector2 MapPoint(float value, int index, int count, Rect rect,
                float paddedMin, float paddedMax)
            {
                float xFrac = count <= 1 ? 0f : (float)index / (count - 1);
                float yFrac = (paddedMax - paddedMin) > 0f
                    ? (value - paddedMin) / (paddedMax - paddedMin)
                    : 0.5f;

                float x = Mathf.Lerp(rect.xMin, rect.xMax, xFrac);
                float y = Mathf.Lerp(rect.yMin, rect.yMax, yFrac);
                return new Vector2(x, y);
            }
        }
    }
}
