using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


    public class XPUI : MonoBehaviour
    {
        public TextMeshProUGUI currentXPText;
        public Transform xpListParent;
        public GameObject xpEntryPrefab;

        [Header("XP Chart (VIEW XP CHART button)")]
        public TextMeshProUGUI chartToggleButtonLabel;
        [SerializeField] private float chartRowHeight = 160f;
        [SerializeField] private float chartBarWidth = 28f;
        [SerializeField] private Color chartBarGainColor = new Color(0.4549f, 0.7529f, 0.9882f); // #74C0FC, matches the list view's XP-gain color
        [SerializeField] private Color chartBarLossColor = new Color(1f, 0.4196f, 0.4196f); // #FF6B6B, matches the list view's loss color

        private const float ChartValueLabelHeight = 18f;
        private const float ChartIndexLabelHeight = 18f;

        private bool _showingChart;

        void OnEnable()
        {
            if (SeasonManager.Instance != null)
                SeasonManager.Instance.OnSeasonDataUpdated += Refresh;
            else
                StartCoroutine(WaitAndSubscribe());
        }

        void OnDisable()
        {
            if (SeasonManager.Instance != null)
                SeasonManager.Instance.OnSeasonDataUpdated -= Refresh;
        }

        private System.Collections.IEnumerator WaitAndSubscribe()
        {
            while (SeasonManager.Instance == null)
                yield return null;
            SeasonManager.Instance.OnSeasonDataUpdated += Refresh;
            Refresh();
        }

        /// <summary>
        /// Flips between the XP history list and the per-week XP bar chart, both
        /// rendered into the same scroll view content. Wired to the XP screen's
        /// "VIEW XP CHART" button OnClick.
        /// </summary>
        public void ToggleChartView()
        {
            _showingChart = !_showingChart;
            if (chartToggleButtonLabel != null)
                chartToggleButtonLabel.text = _showingChart ? "VIEW XP LIST" : "VIEW XP CHART";
            Refresh();
        }

        public void Refresh()
        {
            if (_showingChart) RefreshXpChart();
            else RefreshXPHistory();
        }

        /// <summary>
        /// Resets to the list view and refreshes it. Called on screen entry
        /// (ScreenManager.ShowXP) so every visit to the XP screen starts from a
        /// predictable default, regardless of which view was showing last time.
        /// </summary>
        public void ResetToListView()
        {
            _showingChart = false;
            if (chartToggleButtonLabel != null)
                chartToggleButtonLabel.text = "VIEW XP CHART";
            RefreshXPHistory();
        }

        public void RefreshXPHistory()
        {
            var sm = SeasonManager.Instance;
            if (sm == null || xpListParent == null || xpEntryPrefab == null) return;

            UpdateHeader(sm);
            ClearListParent();

            var prog = sm.XpHistoryEntries;
            if (prog == null || prog.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            // Create new entries (Iterate backwards to show NEWEST first)
            for (int i = prog.Count - 1; i >= 0; i--)
            {
                var entry = prog[i];

                var go = Instantiate(xpEntryPrefab, xpListParent);
                var texts = go.GetComponentsInChildren<TextMeshProUGUI>();

                if (texts.Length >= 3)
                {
                    texts[0].text = $"Week {i + 1}";

                    texts[1].text = FormatResult(entry.source);

                    string color = entry.xp_gained > 0 ? "#74C0FC" : "#FF6B6B";
                    texts[2].text = $"<color={color}>+{entry.xp_gained} XP</color>";
                    Debug.Log("XPUI three text fields found");
                }
                else if (texts.Length == 1)
                {
                    texts[0].text = $"{FormatResult(entry.source)}: +{entry.xp_gained}";
                    Debug.Log("XPUI single text field found");
                }
            }
        }

        /// <summary>
        /// Builds a per-week XP bar chart entirely at runtime (mirroring how
        /// RefreshXPHistory() instantiates list rows) into the same scroll view
        /// content used by the list, so it needs no dedicated scene UI of its own.
        /// Each bar's height is that entry's own xp_gained (not a running total),
        /// so the chart answers "where did my XP come from, and when" the same way
        /// the list view does, per the reviewed spec.
        /// </summary>
        private void RefreshXpChart()
        {
            var sm = SeasonManager.Instance;
            if (sm == null || xpListParent == null) return;

            UpdateHeader(sm);
            ClearListParent();

            var entries = sm.XpHistoryEntries;
            if (entries == null || entries.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            int maxGain = Mathf.Max(1, entries.Max(e => Mathf.Abs(e.xp_gained)));
            float barAreaHeight = Mathf.Max(10f, chartRowHeight - ChartValueLabelHeight - ChartIndexLabelHeight);

            var row = new GameObject("ChartRow", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(xpListParent, false);

            var rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = chartRowHeight;
            rowLayout.minHeight = chartRowHeight;

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.LowerCenter;
            hlg.spacing = 6;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                // Each bar gets its own fixed-size column so a value label can sit
                // above it and a source label below it, without fighting the row's
                // HorizontalLayoutGroup (which only controls x-position, not sizing -
                // see the sizeDelta comment below).
                var column = new GameObject($"BarColumn_{i}", typeof(RectTransform));
                var columnRt = column.GetComponent<RectTransform>();
                columnRt.SetParent(row.transform, false);
                columnRt.sizeDelta = new Vector2(chartBarWidth + 16f, chartRowHeight);

                float heightRatio = (float)Mathf.Abs(entry.xp_gained) / maxGain;
                float barPixelHeight = Mathf.Max(2f, heightRatio * barAreaHeight);

                var barGO = new GameObject("Bar", typeof(RectTransform), typeof(Image));
                var barRt = barGO.GetComponent<RectTransform>();
                barRt.SetParent(columnRt, false);
                barRt.anchorMin = barRt.anchorMax = new Vector2(0.5f, 0f);
                barRt.pivot = new Vector2(0.5f, 0f);
                barRt.anchoredPosition = new Vector2(0, ChartIndexLabelHeight);
                // childControlWidth/childControlHeight are off on the row's
                // HorizontalLayoutGroup, so it positions each column using its actual
                // RectTransform size but never applies LayoutElement.preferred* to
                // it - size has to be set directly here, or every bar renders at the
                // same default size regardless of this loop's math.
                barRt.sizeDelta = new Vector2(chartBarWidth, barPixelHeight);
                barGO.GetComponent<Image>().color = entry.xp_gained >= 0 ? chartBarGainColor : chartBarLossColor;

                var valueLabel = CreateChartLabel(columnRt, "ValueLabel", $"+{entry.xp_gained}",
                    new Vector2(0, ChartIndexLabelHeight + barPixelHeight + 2f), ChartValueLabelHeight, 14, Color.white);
                valueLabel.fontStyle = FontStyles.Bold;

                CreateChartLabel(columnRt, "SourceLabel", NormalizeSourceShort(entry.source),
                    Vector2.zero, ChartIndexLabelHeight, 10, new Color(1f, 1f, 1f, 0.7f));
            }
        }

        /// <summary>
        /// Creates a small bottom-anchored TMP label (used for the XP-gained value
        /// above a bar, the source label below it, and the empty-state message),
        /// matching the screen's existing font so it doesn't look like a foreign element.
        /// </summary>
        private TextMeshProUGUI CreateChartLabel(RectTransform parent, string name, string text, Vector2 anchoredPosition, float height, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(0f, height);

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            if (currentXPText != null) label.font = currentXPText.font;
            return label;
        }

        private void ClearListParent()
        {
            for (int i = xpListParent.childCount - 1; i >= 0; i--)
            {
                Transform child = xpListParent.GetChild(i);
                child.SetParent(null); // Important: Detach from layout first
                Destroy(child.gameObject);
            }
        }

        private void UpdateHeader(SeasonManager sm)
        {
            if (currentXPText == null) return;
            currentXPText.text = $"Total XP: {sm.PlayerXP}, Tier: {FormatTierLabel(sm.PlayerTier)}";
        }

        private void ShowEmptyState()
        {
            if (xpListParent == null) return;
            CreateChartLabel((RectTransform)xpListParent, "EmptyState", "No XP earned yet",
                Vector2.zero, 40f, 16, new Color(1f, 1f, 1f, 0.6f));
        }

        private static string FormatTierLabel(string tier)
        {
            if (string.IsNullOrEmpty(tier)) return "Unknown";
            var spaced = tier.Replace('_', ' ');
            return char.ToUpper(spaced[0]) + spaced.Substring(1);
        }

        // Short per-bar category label for the chart - narrow bars can't fit the raw
        // source string (e.g. "duplicate_card_rare"), so this collapses it to one of
        // the three buckets the reviewed spec asked for.
        private static string NormalizeSourceShort(string source)
        {
            if (string.IsNullOrEmpty(source)) return "Other";
            string lower = source.ToLowerInvariant();
            if (lower.Contains("duplicate")) return "Dup";
            if (lower.Contains("match") || lower.Contains("win") || lower.Contains("loss")) return "Match";
            return "Other";
        }

        // Helper function to clean up the source string
        private string FormatResult(string sourceRaw)
        {
            if (string.IsNullOrEmpty(sourceRaw)) return "Unknown";

            string lower = sourceRaw.ToLower();

            if (lower.Contains("loss")) return "Match Loss";
            if (lower.Contains("win")) return "Match Win";

            // Fallback: just Capitalize the raw string if it's something else
            return char.ToUpper(sourceRaw[0]) + sourceRaw.Substring(1);
        }
    }
