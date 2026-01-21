using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System; // 👈 Needed for Action event

public class WalletInfoPanel : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text gemsText;
    [SerializeField] private TMP_Text creditsText;

    [Header("Icon References")]
    [SerializeField] private Image coinsIcon;
    [SerializeField] private Image gemsIcon;
    [SerializeField] private Image creditsIcon;

    [Header("Linked UI References")]
    [SerializeField] private WeeklyForecastUI weeklyForecastUI;
    
    [Header("Transaction Ledger")]
    [SerializeField] private TransactionLedgerPanel transactionLedgerPanel;

    // 🔔 Event that notifies other scripts when wallet values change
    public static event Action OnWalletUpdated;

    // Example dynamic values
    private int coins = 10000;
    private int gems = 20;
    private int coachingCredits = 100;
    private int weeklyForecast = -200;

    private void Start()
    {
        UpdateWalletDisplay();
        UpdateForecast();
    }

    private void UpdateWalletDisplay()
    {
        if (coinsText != null)
            coinsText.text = $"Coins: {coins:N0}";

        if (gemsText != null)
            gemsText.text = $"Gems: {gems:N0}";

        if (creditsText != null)
            creditsText.text = $"Coaching Credits: {coachingCredits:N0}";

        // 🔔 Notify listeners that wallet data changed
        OnWalletUpdated?.Invoke();
    }

    private void UpdateForecast()
    {
        if (weeklyForecastUI != null)
            weeklyForecastUI.SetForecast(weeklyForecast);
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        UpdateWalletDisplay();
        
        // Add transaction to ledger
        if (transactionLedgerPanel != null)
            transactionLedgerPanel.AddTransaction(ResourceType.Coins, amount);
    }

    public void AddGems(int amount)
    {
        gems += amount;
        UpdateWalletDisplay();
        
        if (transactionLedgerPanel != null)
            transactionLedgerPanel.AddTransaction(ResourceType.Gems, amount);
    }

    public void AddCredits(int amount)
    {
        coachingCredits += amount;
        UpdateWalletDisplay();
        
        if (transactionLedgerPanel != null)
            transactionLedgerPanel.AddTransaction(ResourceType.CoachingCredits, amount);
    }

    public void SetWeeklyForecast(int amount)
    {
        weeklyForecast = amount;
        UpdateForecast();
    }

    // 🧠 Public getter for other UIs (like TopBar_Economy)
    public int GetCoins() => coins;
}