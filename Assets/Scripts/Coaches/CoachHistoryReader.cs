using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Reads and parses coach hire/fire events from the shared events.log.jsonl
/// file for the History screen. Read-only — never writes to the log.
/// </summary>
public static class CoachHistoryReader
{
    public enum HistoryEventType { Hired, Fired }

    [Serializable]
    public class CoachHistoryEntry
    {
        public HistoryEventType eventType;
        public string coachId;
        public string coachName;
        public string coachType;   // "O" / "D" / "S"
        public string teamId;
        public DateTime timestamp;

        // Hire-specific
        public int costPaidCoins;

        // Fire-specific
        public int refundAmountCoins;
        public int contractLength;
        public int gamesServed;
    }

    /// <summary>
    /// Reads the full events log and returns all hire/fire entries,
    /// most recent first. Returns an empty list if the log doesn't exist yet.
    /// </summary>
    public static List<CoachHistoryEntry> GetCoachHistory()
    {
        var entries = new List<CoachHistoryEntry>();
        var path = FilePathResolver.GetEventsLogPath();

        if (!File.Exists(path))
        {
            Debug.Log("[CoachHistoryReader] No events log found yet.");
            return entries;
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CoachHistoryReader] Failed to read events log: {e.Message}");
            return entries;
        }

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            EventBus.EventEnvelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<EventBus.EventEnvelope>(line);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CoachHistoryReader] Skipping malformed log line: {e.Message}");
                continue;
            }

            if (envelope == null) continue;

            if (envelope.event_type == "hire_coach")
            {
                var entry = ParseHireEvent(envelope);
                if (entry != null) entries.Add(entry);
            }
            else if (envelope.event_type == "fire_coach")
            {
                var entry = ParseFireEvent(envelope);
                if (entry != null) entries.Add(entry);
            }
        }

        entries.Sort((a, b) => b.timestamp.CompareTo(a.timestamp));
        return entries;
    }

    private static CoachHistoryEntry ParseHireEvent(EventBus.EventEnvelope envelope)
    {
        if (string.IsNullOrEmpty(envelope.payloadJson)) return null;

        HireCoachPayload payload;
        try
        {
            payload = JsonUtility.FromJson<HireCoachPayload>(envelope.payloadJson);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CoachHistoryReader] Failed to parse hire_coach payload: {e.Message}");
            return null;
        }
        if (payload == null) return null;

        return new CoachHistoryEntry
        {
            eventType = HistoryEventType.Hired,
            coachId = payload.coach_id,
            coachName = ResolveCoachName(payload.coach_id),
            coachType = payload.coach_type,
            teamId = payload.team_id,
            timestamp = ParseTimestamp(envelope.timestamp),
            costPaidCoins = payload.cost_paid_coins
        };
    }

    private static CoachHistoryEntry ParseFireEvent(EventBus.EventEnvelope envelope)
    {
        if (string.IsNullOrEmpty(envelope.payloadJson)) return null;

        FireCoachPayload payload;
        try
        {
            payload = JsonUtility.FromJson<FireCoachPayload>(envelope.payloadJson);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CoachHistoryReader] Failed to parse fire_coach payload: {e.Message}");
            return null;
        }
        if (payload == null) return null;

        return new CoachHistoryEntry
        {
            eventType = HistoryEventType.Fired,
            coachId = payload.coach_id,
            coachName = ResolveCoachName(payload.coach_id),
            coachType = payload.coach_type,
            teamId = payload.team_id,
            timestamp = ParseTimestamp(envelope.timestamp),
            refundAmountCoins = payload.refund_amount_coins
        };
    }

    private static string ResolveCoachName(string coachId)
    {
        if (string.IsNullOrEmpty(coachId)) return "Unknown Coach";
        var record = CoachesService.GetCoachById(coachId);
        return record != null ? record.coach_name : "Unknown Coach";
    }

    private static DateTime ParseTimestamp(string isoTimestamp)
    {
        if (DateTime.TryParse(isoTimestamp, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var result))
        {
            return result;
        }
        return DateTime.MinValue;
    }
}