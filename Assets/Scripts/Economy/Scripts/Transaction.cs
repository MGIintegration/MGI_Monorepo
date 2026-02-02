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
    public string description; // Optional description
    public DateTime timestamp;

    public Transaction(ResourceType type, int amount, string description = "")
    {
        this.resourceType = type;
        this.amount = amount;
        this.description = description;
        this.timestamp = DateTime.Now;
    }

    public string GetFormattedText()
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
}