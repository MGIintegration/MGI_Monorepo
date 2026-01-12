using System;

/// <summary>
/// Represents a single card from the cards catalog.
/// </summary>
[Serializable]
public class Card
{
    public string uid;
    public int cardTier;      // 1=Common, 2=Uncommon, 3=Rare, 4=Epic, 5=Legendary
    public string name;
    public string team;
    public string element;
    public string position5;

    /// <summary>
    /// Converts cardTier (1-5) to rarity string for compatibility with existing system.
    /// </summary>
    public string GetRarityString()
    {
        return cardTier switch
        {
            1 => "common",
            2 => "uncommon",
            3 => "rare",
            4 => "epic",
            5 => "legendary",
            _ => "common"
        };
    }

    /// <summary>
    /// Converts rarity string to cardTier number.
    /// </summary>
    public static int RarityStringToTier(string rarity)
    {
        return rarity?.ToLowerInvariant() switch
        {
            "common" => 1,
            "uncommon" => 2,
            "rare" => 3,
            "epic" => 4,
            "legendary" => 5,
            _ => 1
        };
    }
}

/// <summary>
/// Root class for cards catalog JSON structure.
/// </summary>
[Serializable]
public class CardsCatalog
{
    public Card[] cards;
}

