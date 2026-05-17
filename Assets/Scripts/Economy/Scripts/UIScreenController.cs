using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private GameObject topBarModifiers; // ✅ ADD THIS

    // These are optional top bar buttons (if you have buttons inside top bars)
    [Header("Top Bar Buttons (Optional)")]
    [SerializeField] private GameObject topBarTransactionLedgerButton;
    [SerializeField] private GameObject topBarModifiersButton;

    [Header("Side Buttons")]
    [SerializeField] private Button economicForecastButton;
    [SerializeField] private Button transactionLedgerButton;
    [SerializeField] private Button modifiersButton;
    [SerializeField] private Button walletButton;

    private void Awake()
    {
        ResolveMissingReferences();
        HideAllPanels();
        HideAllTopBars();
    }

    private void Start()
    {
        ResolveMissingReferences();

        // Optional: Setup top bar buttons (if they exist)
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

        // Default screen on Play
        ShowEconomyForecastPanel();
    }

    public void ShowEconomyForecastPanel()
    {
        HideAllPanels();
        HideAllTopBars();

        ShowPanel(economyForecastPanel);

        if (topBarEconomy != null)
            topBarEconomy.SetActive(true);

        SetAllButtonsActive(true);
        if (economicForecastButton != null)
            economicForecastButton.gameObject.SetActive(false);
    }

    public void ShowTransactionLedgerPanel()
    {
        HideAllPanels();
        HideAllTopBars();

        ShowPanel(transactionLedgerPanel);

        if (topBarTransactionLedger != null)
            topBarTransactionLedger.SetActive(true);

        SetAllButtonsActive(true);
        if (transactionLedgerButton != null)
            transactionLedgerButton.gameObject.SetActive(false);
    }

    public void ShowModifiersPanel()
    {
        HideAllPanels();
        HideAllTopBars();

        ShowPanel(modifiersPanel);
        RefreshModifiersPanel();

        // ✅ Show only the Modifiers top bar (NOT wallet)
        if (topBarModifiers != null)
            topBarModifiers.SetActive(true);

        SetAllButtonsActive(true);
        if (modifiersButton != null)
            modifiersButton.gameObject.SetActive(false);
    }

    public void ShowWalletPanel()
    {
        HideAllPanels();
        HideAllTopBars();

        ShowPanel(walletInfoPanel);

        if (topBarWallet != null)
            topBarWallet.SetActive(true);

        SetAllButtonsActive(true);
        if (walletButton != null)
            walletButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Hide ALL top bars
    /// </summary>
    private void HideAllTopBars()
    {
        if (topBarWallet != null) topBarWallet.SetActive(false);
        if (topBarEconomy != null) topBarEconomy.SetActive(false);
        if (topBarTransactionLedger != null) topBarTransactionLedger.SetActive(false);
        if (topBarModifiers != null) topBarModifiers.SetActive(false); // ✅ ADD THIS
    }

    /// <summary>
    /// Hide ALL content panels
    /// </summary>
    private void HideAllPanels()
    {
        if (economyForecastPanel != null) economyForecastPanel.SetActive(false);
        if (transactionLedgerPanel != null) transactionLedgerPanel.SetActive(false);
        if (modifiersPanel != null) modifiersPanel.SetActive(false);
        if (walletInfoPanel != null) walletInfoPanel.SetActive(false);
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(true);
    }

    private void SetAllButtonsActive(bool state)
    {
        if (economicForecastButton != null) economicForecastButton.gameObject.SetActive(state);
        if (transactionLedgerButton != null) transactionLedgerButton.gameObject.SetActive(state);
        if (modifiersButton != null) modifiersButton.gameObject.SetActive(state);
        if (walletButton != null) walletButton.gameObject.SetActive(state);
    }

    private void RefreshModifiersPanel()
    {
        if (modifiersPanel == null)
        {
            return;
        }

        var forecastService = new EconomyForecastService();
        if (!forecastService.TryGetSnapshot(out var snapshot))
        {
            snapshot = EconomyForecastService.CreateZeroSnapshot();
        }

        UpdateModifierRowEffect("Row_Contract", $"-{snapshot.salary:N0} Coins");
        UpdateModifierRowEffect("Row_Facility", $"-{snapshot.operatingExpense:N0} Coins");
        UpdateModifierRowEffect("Row_GlobalPolicy", $"{snapshot.bonus:+#;-#;0} Coins");
    }

    private void UpdateModifierRowEffect(string rowName, string effectText)
    {
        var row = FindChildRecursive(modifiersPanel.transform, rowName);
        if (row == null)
        {
            Debug.LogWarning($"[UIManager] Could not find modifier row '{rowName}'.");
            return;
        }

        var effectTransform = FindChildRecursive(row, "Mod_Effect");
        if (effectTransform == null)
        {
            Debug.LogWarning($"[UIManager] Could not find Mod_Effect under '{rowName}'.");
            return;
        }

        var effectLabel = effectTransform.GetComponent<TMP_Text>();
        if (effectLabel == null)
        {
            effectLabel = effectTransform.GetComponentInChildren<TMP_Text>(true);
        }

        if (effectLabel == null)
        {
            Debug.LogWarning($"[UIManager] Modifier row '{rowName}' is missing a TMP_Text component.");
            return;
        }

        effectLabel.text = effectText;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root)
        {
            if (child.name == childName)
            {
                return child;
            }

            var result = FindChildRecursive(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void ResolveMissingReferences()
    {
        if (topBarEconomy == null)
        {
            topBarEconomy = FindSceneObjectByName("TopBar_Economy");
        }

        if (topBarTransactionLedger == null)
        {
            topBarTransactionLedger = FindSceneObjectByName("TopBar_TransactionLedger");
        }

        if (topBarWallet == null)
        {
            topBarWallet = FindSceneObjectByName("TopBar_Wallet");
        }

        if (topBarModifiers == null)
        {
            topBarModifiers =
                FindSceneObjectByName("TopBar_Modifiers") ??
                FindSceneObjectByName("TopBar_Modifier") ??
                FindSceneObjectByName("TopBar_ Modifier");
        }
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
        var rootTransforms = transform.root.GetComponentsInChildren<Transform>(true);
        foreach (var candidate in rootTransforms)
        {
            if (candidate != null && candidate.name == objectName)
            {
                return candidate.gameObject;
            }
        }

        return null;
    }
}
