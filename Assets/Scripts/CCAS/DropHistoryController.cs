using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;
using System.Text;

/// <summary>
    /// Displays recent pack pull results and emotional summaries (Phase 2 telemetry view).
/// </summary>
public class DropHistoryController : MonoBehaviour
{
    [Header("Panels & Navigation")]
    public GameObject hubPanel;
    public GameObject dropHistoryPanel;
    public Button backToHubButton;

    [Header("Pull Results UI")]
    public Transform contentParent;
    public GameObject resultTemplate;
    public ScrollRect scrollRect;

    [Header("Fixed emotion labels (Pos_after / Neg_after in PullResultsScroll)")]
    [Tooltip("Fixed label for positive_after value; text is updated in place (not instantiated into scroll).")]
    [FormerlySerializedAs("satisfactionTemplate")]
    public GameObject posAfterLabel;
    [Tooltip("Fixed label for negative_after value; text is updated in place (not instantiated into scroll).")]
    [FormerlySerializedAs("frustrationTemplate")]
    public GameObject negAfterLabel;

    [Header("Display")]
    [Range(1, 20)] public int recentPullsToShow = 3;

    private bool _pendingRefresh;
    private bool _isPopulating;
    private System.Action<TelemetryLogger.PackPullLog> _onPullLoggedHandler;

    void Awake()
    {
        if (resultTemplate != null && resultTemplate.activeSelf)
            resultTemplate.SetActive(false);
        
        // posAfterLabel / negAfterLabel stay visible; we update their text in place
    }

    void Start()
    {
        if (backToHubButton != null)
        {
            backToHubButton.onClick.AddListener(() =>
            {

                FindFirstObjectByType<AcquisitionHubController>()?.ShowHub();

            });
        }

        _onPullLoggedHandler = OnPullLogged;
        if (TelemetryLogger.Instance != null)
            TelemetryLogger.Instance.OnPullLogged += _onPullLoggedHandler;
    }

    void OnEnable()
    {
        if (_pendingRefresh)
        {
            _pendingRefresh = false;
            RefreshDropHistory();
        }
    }

    void OnDestroy()
    {
        if (TelemetryLogger.Instance != null && _onPullLoggedHandler != null)
            TelemetryLogger.Instance.OnPullLogged -= _onPullLoggedHandler;
    }

    public void RefreshDropHistory()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            _pendingRefresh = true;
            return;
        }
        if (_isPopulating) return;

        StartCoroutine(PopulateAndScroll());
    }

    void OnPullLogged(TelemetryLogger.PackPullLog _) => RefreshDropHistory();

    IEnumerator PopulateAndScroll()
    {
        _isPopulating = true;

        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            var child = contentParent.GetChild(i).gameObject;
            if (child == resultTemplate) continue;
            Destroy(child);
        }

        var logger = TelemetryLogger.Instance;
        var logs = logger != null ? logger.GetRecent(recentPullsToShow) : new List<TelemetryLogger.PackPullLog>();

        if (logs == null || logs.Count == 0)
        {
            var entry = Instantiate(resultTemplate, contentParent);
            entry.SetActive(true);
            entry.GetComponentInChildren<TextMeshProUGUI>().text = "No pulls yet.";
            var posT = posAfterLabel != null ? posAfterLabel.GetComponentInChildren<TextMeshProUGUI>() : null;
            var negT = negAfterLabel != null ? negAfterLabel.GetComponentInChildren<TextMeshProUGUI>() : null;
            if (posT != null) posT.text = "";
            if (negT != null) negT.text = "";
            _isPopulating = false;
            yield break;
        }

        int cardCount = 0;
        for (int i = logs.Count - 1; i >= 0; i--)
        {
            var log = logs[i];
            var cardNamesLine = new StringBuilder();

            // Display cards - prefer card names from pulled_cards, fallback to rarity
            if (log.pulled_cards != null && log.pulled_cards.Count > 0)
            {
                foreach (var cardData in log.pulled_cards)
                {
                    if (cardData == null) continue;

                    var entry = Instantiate(resultTemplate, contentParent);
                    entry.SetActive(true);

                    var tmp = entry.GetComponentInChildren<TextMeshProUGUI>();
                    string cardName = !string.IsNullOrEmpty(cardData.name) ? cardData.name : "Unknown Card";
                    string rarity = !string.IsNullOrEmpty(cardData.rarity) ? cardData.rarity.ToLowerInvariant() : "common";

                    // If this card was a duplicate, show XP next to it.
                    bool isDuplicate = cardData.is_duplicate;
                    int xp = cardData.xp_gained;
                    string label = cardName;
                    if (isDuplicate && xp > 0)
                    {
                        label = $"{cardName} (DUPLICATE +{xp} XP)";
                    }

                    tmp.text = label;
                    tmp.color = RarityColorUtility.GetColorForRarity(rarity);
                    cardCount++;

                    if (cardNamesLine.Length > 0) cardNamesLine.Append(", ");
                    cardNamesLine.Append(label);
                }
            }
            else
            {
                // Fallback to rarity display if card data not available
                foreach (var rarityRaw in log.pull_results ?? new List<string>())
                {
                    string rarity = (rarityRaw ?? "common").ToLowerInvariant();

                    var entry = Instantiate(resultTemplate, contentParent);
                    entry.SetActive(true);

                    var tmp = entry.GetComponentInChildren<TextMeshProUGUI>();
                    tmp.text = $"{rarity.ToUpper()} CARD";
                    tmp.color = RarityColorUtility.GetColorForRarity(rarity);
                    cardCount++;

                    if (cardNamesLine.Length > 0) cardNamesLine.Append(", ");
                    cardNamesLine.Append(rarity);
                }
            }

            // Keep latest positive/negative for fixed labels (updated after loop)
            float neg = log.negative_after;
            float pos = log.positive_after;

            Debug.Log($"[History] Rendered pull {log.event_id} ({log.pack_type}) → [{cardNamesLine}] | POS={pos:F1} NEG={neg:F1}");
        }

        // Update fixed Pos_after / Neg_after labels in place (no instantiate = positions stay as in Unity)
        var latest = logs[logs.Count - 1];
        float posAfter = latest.positive_after;
        float negAfter = latest.negative_after;
        var posText = posAfterLabel != null ? posAfterLabel.GetComponentInChildren<TextMeshProUGUI>() : null;
        var negText = negAfterLabel != null ? negAfterLabel.GetComponentInChildren<TextMeshProUGUI>() : null;

        const float minLabelWidth = 140f; // Avoid narrow rect causing "one letter per row"
        if (posText != null)
        {
            posText.text = $"Positive: {posAfter:F2}";
            posText.overflowMode = TMPro.TextOverflowModes.Overflow; // Keep on one line
            var posRect = posText.rectTransform;
            if (posRect.sizeDelta.x < minLabelWidth)
                posRect.sizeDelta = new Vector2(minLabelWidth, posRect.sizeDelta.y);
        }
        if (negText != null)
        {
            negText.text = $"Negative: {negAfter:F2}";
            negText.overflowMode = TMPro.TextOverflowModes.Overflow; // Keep on one line
            var negRect = negText.rectTransform;
            if (negRect.sizeDelta.x < minLabelWidth)
                negRect.sizeDelta = new Vector2(minLabelWidth, negRect.sizeDelta.y);
        }
        if (posAfterLabel != null && !posAfterLabel.activeSelf) posAfterLabel.SetActive(true);
        if (negAfterLabel != null && !negAfterLabel.activeSelf) negAfterLabel.SetActive(true);

        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
        yield return null;
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;

        Debug.Log($"[History] Listed {cardCount} card(s) across {logs.Count} pull(s).");
        _isPopulating = false;
    }
}
