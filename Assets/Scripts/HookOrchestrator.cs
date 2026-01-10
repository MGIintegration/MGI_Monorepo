using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phase 1 Hook Orchestrator (Simplified)
/// Controls when telemetry or emotion hooks can trigger,
/// using cooldowns, session caps, and a global quiet window.
/// No JSON configs — all values are hardcoded for now.
/// </summary>
public class HookOrchestrator : MonoBehaviour
{
    public static HookOrchestrator Instance;

    [Header("Global Quiet Window (seconds)")]
    [Tooltip("Range of time during which no new hooks can trigger globally.")]
    public Vector2 quietWindowRange = new Vector2(6f, 8f);

    // --- Internal state tracking ---
    private float _globalQuietUntil;
    private readonly Dictionary<string, float> _cooldownsUntil = new();
    private readonly Dictionary<string, int> _sessionCaps = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Attempts to fire a hook, enforcing cooldowns and global quiet windows.
    /// </summary>
    public bool TryFireHook(
        string hookId,
        float cooldownSeconds,
        int sessionCap,
        Action payloadAction,
        out string blockReason)
    {
        float now = Time.time;
        blockReason = null;

        // Global quiet window check
        if (now < _globalQuietUntil)
        {
            blockReason = "global_quiet";
            return false;
        }

        // Per-session cap check
        if (_sessionCaps.TryGetValue(hookId, out var used) && used >= sessionCap)
        {
            blockReason = "session_cap";
            return false;
        }

        // Per-hook cooldown check
        if (_cooldownsUntil.TryGetValue(hookId, out var cdUntil) && now < cdUntil)
        {
            blockReason = "cooldown";
            return false;
        }

        // ✅ Fire payload
        payloadAction?.Invoke();

        // Track usage
        _cooldownsUntil[hookId] = now + cooldownSeconds;
        _sessionCaps[hookId] = _sessionCaps.TryGetValue(hookId, out var cnt) ? cnt + 1 : 1;

        // Update quiet window
        float minQuiet = Mathf.Max(0.5f, quietWindowRange.x);
        float maxQuiet = Mathf.Max(minQuiet, quietWindowRange.y);
        _globalQuietUntil = now + UnityEngine.Random.Range(minQuiet, maxQuiet);

        Debug.Log($"[HookOrchestrator] Fired '{hookId}' | Quiet until {_globalQuietUntil:F2}");
        return true;
    }

    /// <summary>
    /// Triggered when a pack outcome occurs (e.g., after pack open).
    /// Currently the only active hook in Phase 1.
    /// </summary>
    public void TryTriggerOutcomeHooks(List<string> rarities)
    {
        float cooldown = 5f; // simple fixed cooldown
        int cap = 5;         // limit per session

        bool fired = TryFireHook(
            "outcome_streak",
            cooldown,
            cap,
            () => TelemetryLogger.Instance?.LogHookExecution("outcome_streak", true, null, "outcome"),
            out string reason
        );

        if (!fired)
            TelemetryLogger.Instance?.LogHookExecution("outcome_streak", false, reason, "outcome");
    }

    /// <summary>
    /// Clears cooldowns and session caps (useful for new session start).
    /// </summary>
    public void ResetHooks()
    {
        _cooldownsUntil.Clear();
        _sessionCaps.Clear();
        _globalQuietUntil = 0f;
        Debug.Log("[HookOrchestrator] Hook state reset.");
    }
}
