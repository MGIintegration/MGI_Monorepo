using System.IO;
using UnityEngine;

/// <summary>
/// Central helper for resolving per-player JSON storage paths.
/// All runtime state lives under Application.persistentDataPath/mgi_state.
/// </summary>
public static class FilePathResolver
{
    private const string RootFolderName = "mgi_state";

    public static string GetRoot()
    {
        var root = Path.Combine(Application.persistentDataPath, RootFolderName);
        EnsureDirectory(root);
        return root;
    }

    public static string GetPlayerDataRoot(string playerId)
    {
        var path = Path.Combine(GetRoot(), SanitizeId(playerId));
        EnsureDirectory(path);
        return path;
    }

    public static string GetEconomyPath(string playerId, string fileName)
    {
        return GetSubsystemPath(playerId, "economy", fileName);
    }

    public static string GetProgressionPath(string playerId, string fileName)
    {
        return GetSubsystemPath(playerId, "progression", fileName);
    }

    public static string GetCCASPath(string playerId, string fileName)
    {
        return GetSubsystemPath(playerId, "ccas", fileName);
    }

    public static string GetFacilitiesPath(string playerId, string fileName)
    {
        return GetSubsystemPath(playerId, "facilities", fileName);
    }

    public static string GetCoachesPath(string playerId, string fileName)
    {
        return GetSubsystemPath(playerId, "coaches", fileName);
    }

    /// <summary>
    /// Global events log path (not per-player).
    /// </summary>
    public static string GetEventsLogPath()
    {
        var dir = Path.Combine(GetRoot(), "events");
        EnsureDirectory(dir);
        return Path.Combine(dir, "events.log.jsonl");
    }

    /// <summary>
    /// Global file tracking processed events (idempotency helper).
    /// </summary>
    public static string GetProcessedEventsPath()
    {
        var dir = Path.Combine(GetRoot(), "events");
        EnsureDirectory(dir);
        return Path.Combine(dir, "processed_events.json");
    }

    private static string GetSubsystemPath(string playerId, string subsystemFolder, string fileName)
    {
        var playerRoot = GetPlayerDataRoot(playerId);
        var dir = Path.Combine(playerRoot, subsystemFolder);
        EnsureDirectory(dir);
        return Path.Combine(dir, fileName);
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static string SanitizeId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "unknown_player";

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            id = id.Replace(c, '_');
        }

        return id;
    }
}

