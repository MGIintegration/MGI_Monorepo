using TMPro;
using UnityEngine;

public class TransactionLedgerTopBarUI : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text gemsText;

    [Header("Linked UI References")]
    [SerializeField] private WalletInfoPanel walletInfoPanel;

    private void OnEnable()
    {
        WalletInfoPanel.OnWalletUpdated += RefreshFromWallet;
        RefreshFromWallet(); // immediate update when active
    }

    private void OnDisable()
    {
        WalletInfoPanel.OnWalletUpdated -= RefreshFromWallet;
    }

    private void RefreshFromWallet()
    {
        if (walletInfoPanel == null) return;

        if (coinsText != null)
            coinsText.text = $"Coins: {walletInfoPanel.GetCoins():N0}";

        if (gemsText != null)
            gemsText.text = $"Gems: {walletInfoPanel.GetGems():N0}";
    }
}