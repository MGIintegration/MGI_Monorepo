using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Populates the History screen with hire/fire entries read from
/// CoachHistoryReader. Attach this to the HistoryScreen GameObject.
/// </summary>
public class CoachHistoryPopulator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject historyRowPrefab;

    private void OnEnable()
    {
        Refresh();
    }

    /// <summary>
    /// Clears and repopulates the row list from the current events log.
    /// Safe to call multiple times (e.g. on screen open, or on refresh).
    /// </summary>
    public void Refresh()
    {
        if (contentContainer == null || historyRowPrefab == null)
        {
            Debug.LogWarning("[CoachHistoryPopulator] contentContainer or historyRowPrefab not assigned.");
            return;
        }

        ClearContainerChildren(contentContainer);

        var entries = CoachHistoryReader.GetCoachHistory();

        if (entries.Count == 0)
        {
            var emptyRow = Instantiate(historyRowPrefab, contentContainer);
            var emptyText = emptyRow.GetComponentInChildren<TextMeshProUGUI>();
            if (emptyText != null)
                emptyText.text = "No coach history yet.";
            return;
        }

        foreach (var entry in entries)
        {
            var row = Instantiate(historyRowPrefab, contentContainer);
            var text = row.GetComponentInChildren<TextMeshProUGUI>();
            if (text == null) continue;

            text.text = FormatEntry(entry);
        }

        Debug.Log($"[CoachHistoryPopulator] Populated {entries.Count} history entries.");
    }

    private string FormatEntry(CoachHistoryReader.CoachHistoryEntry entry)
    {
        string dateStr = entry.timestamp != System.DateTime.MinValue
            ? entry.timestamp.ToLocalTime().ToString("MM/dd/yyyy h:mm tt")
            : "Unknown date";

        if (entry.eventType == CoachHistoryReader.HistoryEventType.Hired)
        {
            return $"Hired — {entry.coachName} ({entry.coachType}) — ${entry.costPaidCoins:N0} — {dateStr}";
        }
        else
        {
            string contractInfo = entry.contractLength > 0
                ? $" — {entry.gamesServed}/{entry.contractLength} games served"
                : "";
            return $"Fired — {entry.coachName} ({entry.coachType}) — Refund ${entry.refundAmountCoins:N0}{contractInfo} — {dateStr}";
        }
    }

    /// <summary>
    /// Destroys all children safely, iterating backward by index to avoid
    /// the classic foreach + DestroyImmediate skip-every-other-child bug.
    /// </summary>
    private void ClearContainerChildren(Transform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }
}