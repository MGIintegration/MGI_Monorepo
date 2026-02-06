using TMPro;
using UnityEngine;

public class TransactionLedgerTopBarUI : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text coinsText;

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
        if (walletInfoPanel == null || coinsText == null) return;
        coinsText.text = $"Coins: {walletInfoPanel.GetCoins():N0}";
    }
}