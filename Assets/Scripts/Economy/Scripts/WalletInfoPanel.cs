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
    [SerializeField] private int fallbackWeeklyForecast = 0;

    public static event Action OnWalletUpdated;

    private EconomyService economyService;
    private readonly EconomyForecastService forecastService = new EconomyForecastService();
    private IDisposable walletUpdatedSubscription;

    private int coins;
    private int gems;
    private int coachingCredits;

    private void OnEnable()
    {
        if (economyService == null)
        {
            economyService = new EconomyService();
        }

        LoadWalletFromStorage();
        UpdateWalletDisplay();
        UpdateForecast();
    }

    private void Start()
    {
        if (economyService == null)
        {
            economyService = new EconomyService();
        }

        if (refreshOnWalletUpdatedEvent)
        {
            walletUpdatedSubscription = EventBus.Subscribe("wallet_updated", OnWalletUpdatedEventMessage);
        }
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
        if (weeklyForecastUI == null)
        {
            return;
        }

        if (forecastService.TryGetSnapshot(playerId, out var snapshot))
        {
            weeklyForecastUI.SetForecast(snapshot.netDelta);
            return;
        }

        weeklyForecastUI.SetForecast(fallbackWeeklyForecast);
    }

    public void AddCoins(int amount)
    {
        ApplyWalletDelta(amount, 0, 0, "ui_add_coins", "ui_spend_coins");
    }

    public void AddGems(int amount)
    {
        ApplyWalletDelta(0, amount, 0, "ui_add_gems", "ui_spend_gems");
    }

    public void AddCredits(int amount)
    {
        ApplyWalletDelta(0, 0, amount, "ui_add_coaching_credits", "ui_spend_coaching_credits");
    }

    private void ApplyWalletDelta(
        int coinsDelta,
        int gemsDelta,
        int coachingCreditsDelta,
        string earnSource,
        string spendSource)
    {
        if (economyService == null)
        {
            return;
        }

        var spendCoins = Mathf.Max(0, -coinsDelta);
        var spendGems = Mathf.Max(0, -gemsDelta);
        var spendCoachingCredits = Mathf.Max(0, -coachingCreditsDelta);
        var addCoins = Mathf.Max(0, coinsDelta);
        var addGems = Mathf.Max(0, gemsDelta);
        var addCoachingCredits = Mathf.Max(0, coachingCreditsDelta);

        var updated = true;
        if (addCoins > 0 || addGems > 0 || addCoachingCredits > 0)
        {
            economyService.AddCurrency(playerId, addCoins, addGems, addCoachingCredits, earnSource);
        }
        else if (spendCoins > 0 || spendGems > 0 || spendCoachingCredits > 0)
        {
            updated = economyService.TrySpend(
                playerId,
                spendCoins,
                spendGems,
                spendCoachingCredits,
                spendSource,
                out _);
        }

        if (!updated)
        {
            Debug.LogWarning("[WalletInfoPanel] Wallet update failed due to insufficient balance or invalid input.");
            return;
        }

        LoadWalletFromStorage();
        UpdateWalletDisplay();
        if (!refreshOnWalletUpdatedEvent)
        {
            UpdateForecast();
        }

        if (transactionLedgerPanel != null)
            transactionLedgerPanel.ReloadTransactionsFromStorage();
    }

    public void SetWeeklyForecast(int amount)
    {
        fallbackWeeklyForecast = amount;
        UpdateForecast();
    }

    // 🧠 Public getter for other UIs (like TopBar_Economy)
    public int GetCoins() => coins;
}
