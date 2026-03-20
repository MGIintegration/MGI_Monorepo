using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

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

    [Header("Economy Service")]
    [SerializeField] private string playerId = "local_player";
    [SerializeField] private bool autoCreateWalletIfMissing = true;
    [SerializeField] private bool refreshOnWalletUpdatedEvent = true;

    public static event Action OnWalletUpdated;

    private EconomyService economyService;
    private IDisposable walletUpdatedSubscription;

    private int coins;
    private int gems;
    private int coachingCredits;
    private int weeklyForecast = -200;

    private void Start()
    {
        economyService = new EconomyService();
        LoadWalletFromStorage();

        if (refreshOnWalletUpdatedEvent)
        {
            walletUpdatedSubscription = EventBus.Subscribe("wallet_updated", OnWalletUpdatedEventMessage);
        }

        UpdateWalletDisplay();
        UpdateForecast();
    }

    private void OnDestroy()
    {
        walletUpdatedSubscription?.Dispose();
    }

    private void LoadWalletFromStorage()
    {
        if (economyService == null)
        {
            return;
        }

        var wallet = economyService.GetWallet(playerId, autoCreateWalletIfMissing);
        if (wallet == null)
        {
            return;
        }

        coins = wallet.coins;
        gems = wallet.gems;
        coachingCredits = wallet.coaching_credits;
    }

    private void OnWalletUpdatedEventMessage(EventBus.EventEnvelope evt)
    {
        if (evt == null || evt.player_id != playerId)
        {
            return;
        }

        LoadWalletFromStorage();
        UpdateWalletDisplay();
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
        if (economyService == null) return;

        if (amount >= 0)
        {
            economyService.AddCurrency(playerId, amount, 0, "ui_add_coins");
        }
        else
        {
            economyService.TrySpend(playerId, Mathf.Abs(amount), 0, "ui_spend_coins", out _);
        }

        LoadWalletFromStorage();
        UpdateWalletDisplay();

        if (transactionLedgerPanel != null)
            transactionLedgerPanel.AddTransaction(ResourceType.Coins, amount);
    }

    public void AddGems(int amount)
    {
        if (economyService == null) return;

        if (amount >= 0)
        {
            economyService.AddCurrency(playerId, 0, amount, "ui_add_gems");
        }
        else
        {
            economyService.TrySpend(playerId, 0, Mathf.Abs(amount), "ui_spend_gems", out _);
        }

        LoadWalletFromStorage();
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