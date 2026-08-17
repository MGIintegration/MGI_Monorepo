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
        /// Flips between the XP history list and the cumulative-XP bar chart, both
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

            //  Update Total XP Header
            if (currentXPText != null)
                currentXPText.text = $"Total XP: {sm.PlayerXP}";

            var prog = sm.XpHistoryEntries;
            if (prog == null || prog.Count == 0) return;

            ClearListParent();

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
        /// Builds a cumulative-XP-over-time bar chart entirely at runtime (mirroring
        /// how RefreshXPHistory() instantiates list rows) into the same scroll view
        /// content used by the list, so it needs no dedicated scene UI of its own.
        /// </summary>
        private void RefreshXpChart()
        {
            var sm = SeasonManager.Instance;
            if (sm == null || xpListParent == null) return;

            if (currentXPText != null)
                currentXPText.text = $"Total XP: {sm.PlayerXP}";

            ClearListParent();

            var entries = sm.XpHistoryEntries;
            if (entries == null || entries.Count == 0) return;

            // Running total per entry, not the per-entry delta, so the chart reads as
            // "XP over time" rather than a bar graph of individual gains.
            var cumulative = new int[entries.Count];
            int running = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                running += entries[i].xp_gained;
                cumulative[i] = running;
            }
            int maxCumulative = Mathf.Max(1, cumulative.Max());

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
                var barGO = new GameObject($"Bar_{i}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                barGO.transform.SetParent(row.transform, false);

                float heightRatio = (float)cumulative[i] / maxCumulative;
                float barPixelHeight = Mathf.Max(2f, heightRatio * chartRowHeight);

                var barLayout = barGO.GetComponent<LayoutElement>();
                barLayout.preferredWidth = chartBarWidth;
                barLayout.preferredHeight = barPixelHeight;

                var img = barGO.GetComponent<Image>();
                img.color = entries[i].xp_gained >= 0 ? chartBarGainColor : chartBarLossColor;
            }
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
