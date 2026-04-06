using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Lightweight in-process event bus with optional JSONL persistence.
/// Used for local, offline domain events (buy_pack, hire_coach, etc.).
/// </summary>
public static class EventBus
{
    [Serializable]
    public class EventEnvelope
    {
        public string event_id;
        public string event_type;
        public string player_id;
        public string timestamp;

        /// <summary>
        /// Opaque JSON payload string. Callers are responsible for serializing
        /// and deserializing the payload to a strongly-typed object.
        /// </summary>
        public string payloadJson;
    }

    private static readonly Dictionary<string, List<Action<EventEnvelope>>> _subscribers =
        new Dictionary<string, List<Action<EventEnvelope>>>();

    private static readonly object _lock = new object();

    /// <summary>
    /// Publish an event to all subscribers and append it to the JSONL log.
    /// </summary>
    public static void Publish(EventEnvelope evt)
    {
        if (evt == null || string.IsNullOrEmpty(evt.event_type))
        {
            Debug.LogWarning("[EventBus] Ignoring null or invalid event.");
            return;
        }

        if (string.IsNullOrEmpty(evt.event_id))
        {
            evt.event_id = Guid.NewGuid().ToString();
        }

        if (string.IsNullOrEmpty(evt.timestamp))
        {
            evt.timestamp = DateTime.UtcNow.ToString("o");
        }

        // Invoke subscribers
        List<Action<EventEnvelope>> snapshot = null;
        lock (_lock)
        {
            if (_subscribers.TryGetValue(evt.event_type, out var handlers))
            {
                snapshot = new List<Action<EventEnvelope>>(handlers);
            }
        }

        if (snapshot != null)
        {
            foreach (var handler in snapshot)
            {
                try
                {
                    handler?.Invoke(evt);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Handler threw exception for event '{evt.event_type}': {ex}");
                }
            }
        }

        // Append to JSONL log (best-effort)
        AppendToLog(evt);
    }

    /// <summary>
    /// Subscribe to a specific event_type. Returns an IDisposable that can be
    /// used to unsubscribe.
    /// </summary>
    public static IDisposable Subscribe(string eventType, Action<EventEnvelope> handler)
    {
        if (string.IsNullOrEmpty(eventType) || handler == null)
            throw new ArgumentException("eventType and handler must be non-null");

        lock (_lock)
        {
            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Action<EventEnvelope>>();
                _subscribers[eventType] = handlers;
            }

            handlers.Add(handler);
        }

        return new Subscription(eventType, handler);
    }

    private static void Unsubscribe(string eventType, Action<EventEnvelope> handler)
    {
        lock (_lock)
        {
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    _subscribers.Remove(eventType);
                }
            }
        }
    }

    private static void AppendToLog(EventEnvelope evt)
    {
        try
        {
            var path = FilePathResolver.GetEventsLogPath();
            var json = JsonUtility.ToJson(evt);
            File.AppendAllText(path, json + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[EventBus] Failed to append to events log: {ex.Message}");
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly string _eventType;
        private readonly Action<EventEnvelope> _handler;
        private bool _disposed;

        public Subscription(string eventType, Action<EventEnvelope> handler)
        {
            _eventType = eventType;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Unsubscribe(_eventType, _handler);
        }
    }
}

