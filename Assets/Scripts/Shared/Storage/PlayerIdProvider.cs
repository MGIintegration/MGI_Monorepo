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
}
