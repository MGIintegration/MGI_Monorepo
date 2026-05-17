using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the full pack-opening history from the Hub's "My Packs" button.
///
/// Each row shows a pack-level summary (one row per pack opening):
///   [Pack Name]  ·  N cards  ·  +XP XP  ·  N dup  ·  timestamp
///
/// Setup in Unity:
///   1. Duplicate the DropHistoryPanel in your scene → rename it MyPacksPanel
///   2. Remove the DropHistoryController component from it
///   3. Add this MyPacksController component to MyPacksPanel
///   4. Assign: contentParent, resultTemplate, scrollRect, backToHubButton
///   5. In AcquisitionHubController inspector, assign MyPacksPanel → myPacksPanel field
/// </summary>
public class MyPacksController : MonoBehaviour
{
    [Header("Scroll List")]
    public Transform  contentParent;
    public GameObject resultTemplate;
    public ScrollRect scrollRect;

    [Header("Navigation")]
    public Button backToHubButton;

    [Header("Summary Labels (optional)")]
    [Tooltip("Set a TMP label here to show total packs opened count.")]
    public TMP_Text totalPacksLabel;

    [Header("Display")]
    [Tooltip("Maximum number of pack entries to show. 0 = show all.")]
    public int maxEntriesToShow = 0;

    private bool   _pendingRefresh;
    private bool   _isPopulating;
    private Action<EventBus.EventEnvelope> _onPullLoggedHandler;
    private IDisposable _buyPackSub;

    void Awake()
    {
        if (resultTemplate != null && resultTemplate.activeSelf)
            resultTemplate.SetActive(false);
    }

    void Start()
    {
        if (backToHubButton != null)
            backToHubButton.onClick.AddListener(
                () => FindFirstObjectByType<AcquisitionHubController>()?.ShowHub());

        _onPullLoggedHandler = _ => Refresh();
        _buyPackSub = EventBus.Subscribe("buy_pack", _onPullLoggedHandler);
    }

    void OnEnable()
    {
        if (_pendingRefresh)
        {
            _pendingRefresh = false;
            Refresh();
        }
        else
        {
            Refresh();
        }
    }

    void OnDestroy()
    {
        _buyPackSub?.Dispose();
    }

    public void Refresh()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            _pendingRefresh = true;
            return;
        }
        if (_isPopulating) return;

        StartCoroutine(Populate());
    }

    IEnumerator Populate()
    {
        _isPopulating = true;

        if (contentParent == null || resultTemplate == null)
        {
            Debug.LogError("[MyPacks] contentParent or resultTemplate is not assigned in the Inspector. " +
                           "Assign: contentParent = ScrollView/Viewport/Content, resultTemplate = a row prefab/GameObject.");
            _isPopulating = false;
            yield break;
        }

        // Clear previous rows (keep resultTemplate in place)
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            var child = contentParent.GetChild(i).gameObject;
            if (child == resultTemplate) continue;
            Destroy(child);
        }

        var logger = TelemetryLogger.Instance;
        int limit  = (maxEntriesToShow > 0) ? maxEntriesToShow : int.MaxValue;
        var logs   = logger != null
            ? logger.GetRecent(limit)
            : new List<TelemetryLogger.PackPullLog>();

        if (totalPacksLabel != null)
            totalPacksLabel.text = $"Total packs opened: {(logs?.Count ?? 0)}";

        if (logs == null || logs.Count == 0)
        {
            var empty = Instantiate(resultTemplate, contentParent);
            empty.SetActive(true);
            empty.GetComponentInChildren<TextMeshProUGUI>().text = "No packs opened yet.";
            _isPopulating = false;
            yield break;
        }

        // Most recent first
        for (int i = logs.Count - 1; i >= 0; i--)
        {
            var log = logs[i];

            int   cardCount = log.pulled_cards?.Count ?? log.pull_results?.Count ?? 0;
            int   xp        = log.total_xp_gained;
            int   dups      = log.duplicate_count;
            string packName = !string.IsNullOrEmpty(log.pack_name) ? log.pack_name : log.pack_type;
            string date     = FormatTimestamp(log.timestamp);

            string line = $"{packName}  ·  {cardCount} cards";
            if (xp > 0)   line += $"  ·  +{xp} XP";
            if (dups > 0)  line += $"  ·  {dups} dup";
            line += $"\n<size=70%><color=#888888>{date}</color></size>";

            var row = Instantiate(resultTemplate, contentParent);
            row.SetActive(true);

            var tmp = row.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text           = line;
                tmp.richText       = true;
                tmp.overflowMode   = TextOverflowModes.Overflow;
                tmp.color          = Color.white;
            }
        }

        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
        yield return null;
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;

        _isPopulating = false;
    }

    private static string FormatTimestamp(long unixMs)
    {
        try
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(unixMs).LocalDateTime;
            return dt.ToString("MMM d, h:mm tt");
        }
        catch
        {
            return string.Empty;
        }
    }
}
