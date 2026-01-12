using UnityEngine;

/// <summary>
/// Centralized utility for rarity color mapping.
/// Single source of truth for all rarity colors across the application.
/// </summary>
public static class RarityColorUtility
{
    /// <summary>
    /// Returns the color associated with a given rarity string.
    /// </summary>
    /// <param name="rarity">Rarity string (common, uncommon, rare, epic, legendary)</param>
    /// <returns>Color32 for the rarity, or white if rarity is unknown</returns>
    public static Color32 GetColorForRarity(string rarity)
    {
        if (string.IsNullOrEmpty(rarity))
            return new Color32(255, 255, 255, 255); // white

        return rarity.ToLowerInvariant() switch
        {
            "common" => new Color32(110, 110, 110, 255),
            "uncommon" => new Color32(30, 150, 85, 255),
            "rare" => new Color32(0, 112, 221, 255),
            "epic" => new Color32(163, 53, 238, 255),
            "legendary" => new Color32(255, 204, 0, 255),
            _ => new Color32(255, 255, 255, 255) // white
        };
    }
}

