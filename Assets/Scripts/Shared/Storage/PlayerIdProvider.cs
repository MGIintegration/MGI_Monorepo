using UnityEngine;

/// <summary>
/// Shared offline player id for MGI services (Economy, CCAS, Coaches, Facilities, Progression).
/// All systems should resolve the same id so mgi_state/{playerId}/ paths stay aligned.
/// </summary>
public static class PlayerIdProvider
{
    public const string DefaultPlayerId = "local_player";
    public const string PlayerPrefsKey = "player_id";

    /// <summary>
    /// Active player id for read/write under FilePathResolver (mgi_state/{playerId}/...).
    /// Uses PlayerPrefs when set; otherwise <see cref="DefaultPlayerId"/>.
    /// </summary>
    public static string Get()
    {
        var stored = PlayerPrefs.GetString(PlayerPrefsKey, DefaultPlayerId);
        return string.IsNullOrWhiteSpace(stored) ? DefaultPlayerId : stored.Trim();
    }

    /// <summary>
    /// Sets the active local profile used by UI entry points that do not receive
    /// an explicit player id. AI and server-driven actions should still pass a
    /// player id directly to their service methods.
    /// </summary>
    public static void Set(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            throw new System.ArgumentException("A player id is required.", nameof(playerId));

        PlayerPrefs.SetString(PlayerPrefsKey, playerId.Trim());
        PlayerPrefs.Save();
    }
}
