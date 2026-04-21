using System;

[Serializable]
public enum ResourceType
{
    Coins,
    Gems,
    CoachingCredits
}

[Serializable]
public class Transaction
{
    public ResourceType resourceType;
    public int amount; // Positive for additions, negative for subtractions
    public string description; // Optional source/description
    public DateTime timestamp;

    public Transaction(ResourceType type, int amount, string description = "")
        : this(type, amount, description, DateTime.Now)
    {
    }

    public Transaction(ResourceType type, int amount, string description, DateTime timestamp)
    {
        this.resourceType = type;
        this.amount = amount;
        this.description = description;
        this.timestamp = timestamp;
    }

    public string GetDisplayTitle()
    {
        string sign = amount >= 0 ? "[+] +" : "[-] -";
        string resourceName = resourceType switch
        {
            ResourceType.Coins => "Coins",
            ResourceType.Gems => "Gems",
            ResourceType.CoachingCredits => "Coaching Credits",
            _ => "Unknown"
        };

        return $"{sign} {Math.Abs(amount)} {resourceName}";
    }

    public string GetDisplaySource()
    {
        return $"Source: {FormatLabel(description)}";
    }

    public string GetDisplayTimestamp()
    {
        var displayTimestamp = timestamp.Kind == DateTimeKind.Unspecified
            ? timestamp
            : timestamp.ToLocalTime();

        return $"Time: {displayTimestamp:MMM d, h:mm tt}";
    }

    public string GetFormattedText()
    {
        return $"{GetDisplayTitle()} | {GetDisplaySource()} | {GetDisplayTimestamp()}";
    }

    private static string FormatLabel(string rawLabel)
    {
        if (string.IsNullOrWhiteSpace(rawLabel))
        {
            return "Unknown";
        }

        return rawLabel.Replace('_', ' ').Trim();
    }
}
