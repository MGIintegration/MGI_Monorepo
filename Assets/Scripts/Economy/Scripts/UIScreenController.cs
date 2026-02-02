using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject economyForecastPanel;
    [SerializeField] private GameObject transactionLedgerPanel;
    [SerializeField] private GameObject modifiersPanel;
    [SerializeField] private GameObject walletInfoPanel;

    [Header("Top Bars")]
    [SerializeField] private GameObject topBarWallet;
    [SerializeField] private GameObject topBarEconomy;
    [SerializeField] private GameObject topBarTransactionLedger;
    [SerializeField] private GameObject topBarTransactionLedgerButton;
    [SerializeField] private GameObject topBarModifiersButton;

    [Header("Side Buttons")]
    [SerializeField] private Button economicForecastButton;
    [SerializeField] private Button transactionLedgerButton;
    [SerializeField] private Button modifiersButton;
    [SerializeField] private Button walletButton;

    private void Start()
    {
        // Setup top bar buttons
        if (topBarTransactionLedgerButton != null)
        {
            Button button = topBarTransactionLedgerButton.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(ShowTransactionLedgerPanel);
        }
        
        if (topBarModifiersButton != null)
        {
            Button button = topBarModifiersButton.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(ShowModifiersPanel);
        }

        // Changed from ShowWalletPanel() to ShowEconomyForecastPanel()
        // This will show the Economy Forecast Panel by default when Play is pressed
        ShowEconomyForecastPanel();
    }

    public void ShowEconomyForecastPanel()
    {
        HideAllPanels();
        HideAllTopBars();
        
        ShowPanel(economyForecastPanel);

        // Show only the Economy top bar
        if (topBarEconomy != null)
            topBarEconomy.SetActive(true);
        if (topBarWallet != null)
            topBarWallet.SetActive(false);
        if (topBarTransactionLedger != null)
            topBarTransactionLedger.SetActive(false);

        SetAllButtonsActive(true);
        if (economicForecastButton != null)
            economicForecastButton.gameObject.SetActive(false);
    }

    public void ShowTransactionLedgerPanel()
    {
        HideAllPanels();
        HideAllTopBars();
        
        ShowPanel(transactionLedgerPanel);

        // Show only the Transaction Ledger top bar
        if (topBarTransactionLedger != null)
            topBarTransactionLedger.SetActive(true);
        if (topBarWallet != null)
            topBarWallet.SetActive(false);
        if (topBarEconomy != null)
            topBarEconomy.SetActive(false);

        SetAllButtonsActive(true);
        if (transactionLedgerButton != null)
            transactionLedgerButton.gameObject.SetActive(false);
    }

    public void ShowModifiersPanel()
    {
        HideAllPanels();
        HideAllTopBars();
        
        ShowPanel(modifiersPanel);

        // Show only the Wallet top bar
        if (topBarWallet != null)
            topBarWallet.SetActive(true);
        if (topBarEconomy != null)
            topBarEconomy.SetActive(false);
        if (topBarTransactionLedger != null)
            topBarTransactionLedger.SetActive(false);

        SetAllButtonsActive(true);
        if (modifiersButton != null)
            modifiersButton.gameObject.SetActive(false);
    }

    public void ShowWalletPanel()
    {
        HideAllPanels();
        HideAllTopBars();
        
        ShowPanel(walletInfoPanel);

        // Show only the Wallet top bar
        if (topBarWallet != null)
            topBarWallet.SetActive(true);
        if (topBarEconomy != null)
            topBarEconomy.SetActive(false);
        if (topBarTransactionLedger != null)
            topBarTransactionLedger.SetActive(false);

        SetAllButtonsActive(true);
        if (walletButton != null)
            walletButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Hide ALL top bars
    /// </summary>
    private void HideAllTopBars()
    {
        if (topBarWallet != null)
            topBarWallet.SetActive(false);
        if (topBarEconomy != null)
            topBarEconomy.SetActive(false);
        if (topBarTransactionLedger != null)
            topBarTransactionLedger.SetActive(false);
    }

    private void HideAllPanels()
    {
        if (economyForecastPanel != null)
            economyForecastPanel.SetActive(false);
        if (transactionLedgerPanel != null)
            transactionLedgerPanel.SetActive(false);
        if (modifiersPanel != null)
            modifiersPanel.SetActive(false);
        if (walletInfoPanel != null)
            walletInfoPanel.SetActive(false);
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(true);
    }

    private void SetAllButtonsActive(bool state)
    {
        if (economicForecastButton != null)
            economicForecastButton.gameObject.SetActive(state);
        if (transactionLedgerButton != null)
            transactionLedgerButton.gameObject.SetActive(state);
        if (modifiersButton != null)
            modifiersButton.gameObject.SetActive(state);
        if (walletButton != null)
            walletButton.gameObject.SetActive(state);
    }
}