using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

/// <summary>
/// Displays recent pack pull results and emotional summaries (Phase-1 telemetry view).
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

    [Header("Emotion Templates (Optional - for separate formatting)")]
    [Tooltip("If set, creates separate entry for satisfaction. If null, uses resultTemplate.")]
    public GameObject satisfactionTemplate;
    [Tooltip("If set, creates separate entry for frustration. If null, uses resultTemplate.")]
    public GameObject frustrationTemplate;

    [Header("Display")]
    [Range(1, 20)] public int recentPullsToShow = 3;

    private bool _pendingRefresh;
    private bool _isPopulating;
    private System.Action<TelemetryLogger.PackPullLog> _onPullLoggedHandler;

    void Awake()
    {
        if (resultTemplate != null && resultTemplate.activeSelf)
            resultTemplate.SetActive(false);
        
        if (satisfactionTemplate != null && satisfactionTemplate.activeSelf)
            satisfactionTemplate.SetActive(false);
        
        if (frustrationTemplate != null && frustrationTemplate.activeSelf)
            frustrationTemplate.SetActive(false);
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
            if (child == resultTemplate || child == satisfactionTemplate || child == frustrationTemplate) continue;
            Destroy(child);
        }

        var logger = TelemetryLogger.Instance;
        var logs = logger != null ? logger.GetRecent(recentPullsToShow) : new List<TelemetryLogger.PackPullLog>();

        if (logs == null || logs.Count == 0)
        {
            var entry = Instantiate(resultTemplate, contentParent);
            entry.SetActive(true);
            entry.GetComponentInChildren<TextMeshProUGUI>().text = "No pulls yet.";
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

            // Display emotional state - create separate entries for satisfaction and frustration
            float fr = log.frustration_after;
            float sa = log.satisfaction_after;

            // Satisfaction entry
            GameObject satTemplate = satisfactionTemplate != null ? satisfactionTemplate : resultTemplate;
            var satEntry = Instantiate(satTemplate, contentParent);
            satEntry.SetActive(true);
            var satText = satEntry.GetComponentInChildren<TextMeshProUGUI>();
            if (satText != null)
            {
                satText.text = $"Satisfaction: {sa:F2}";
            }

            // Frustration entry
            GameObject frusTemplate = frustrationTemplate != null ? frustrationTemplate : resultTemplate;
            var frusEntry = Instantiate(frusTemplate, contentParent);
            frusEntry.SetActive(true);
            var frusText = frusEntry.GetComponentInChildren<TextMeshProUGUI>();
            if (frusText != null)
            {
                frusText.text = $"Frustration: {fr:F2}";
            }

            Debug.Log($"[History] Rendered pull {log.event_id} ({log.pack_type}) → [{cardNamesLine}] | S={sa:F1} F={fr:F1}");
        }

        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
        yield return null;
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;

        Debug.Log($"[History] Listed {cardCount} card(s) across {logs.Count} pull(s).");
        _isPopulating = false;
    }
}
