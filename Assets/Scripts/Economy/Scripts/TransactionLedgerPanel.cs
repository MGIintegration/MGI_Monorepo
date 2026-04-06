using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;

public class TransactionLedgerPanel : MonoBehaviour
{
    private static readonly Color PositiveAmountColor = new Color(0.31f, 0.95f, 0.48f);
    private static readonly Color NegativeAmountColor = new Color(1f, 0.45f, 0.45f);
    private static readonly Color CardBackgroundColor = new Color(0.09f, 0.13f, 0.22f, 0.9f);
    private static readonly Color MetaTextColor = new Color(0.86f, 0.9f, 0.98f, 0.92f);
    private static readonly Color ScrollTrackColor = new Color(0.12f, 0.18f, 0.3f, 0.72f);
    private static readonly Color ScrollHandleColor = new Color(0.86f, 0.93f, 1f, 0.92f);

    private const string ScrollRootName = "TransactionLedgerScrollRoot";
    private const string ScrollViewportName = "TransactionLedgerViewport";
    private const string ScrollbarName = "TransactionLedgerScrollbar";
    private const string ScrollbarHandleName = "Handle";
    private const float ScrollViewportWidth = 920f;
    private const float ScrollViewportHeight = 560f;
    private const float ScrollbarWidth = 18f;
    private const float ScrollbarRightInset = 6f;

    [Header("UI References")]
    [SerializeField] private Transform transactionListParent; // ContentContainer
    [SerializeField] private GameObject transactionEntryPrefab; // Optional prefab
    [SerializeField] private TMP_Text emptyStateText; // Optional: "No transactions" message

    [Header("Transaction History")]
    private List<Transaction> transactions = new List<Transaction>();

    [Header("Economy Service")]
    [SerializeField] private string playerId = "local_player";
    [SerializeField] private int recentTransactionLimit = 100;
    [SerializeField] private bool loadFromEconomyServiceOnEnable = true;
    [SerializeField] private bool subscribeToWalletUpdatedEvent = true;

    private EconomyService economyService;
    private IDisposable walletUpdatedSubscription;
    private ScrollRect scrollRect;
    private RectTransform scrollRootRect;
    private RectTransform scrollViewportRect;
    private Scrollbar verticalScrollbar;

    private void OnEnable()
    {
        if (economyService == null)
        {
            economyService = new EconomyService();
        }

        EnsureScrollInfrastructure();
        EnsureTransactionListLayout();
        EnsureEmptyStateText();

        if (loadFromEconomyServiceOnEnable)
        {
            ReloadTransactionsFromStorage();
        }

        if (subscribeToWalletUpdatedEvent)
        {
            walletUpdatedSubscription = EventBus.Subscribe("wallet_updated", OnWalletUpdatedEventMessage);
        }

        RefreshTransactionList();
    }

    private void OnDisable()
    {
        walletUpdatedSubscription?.Dispose();
        walletUpdatedSubscription = null;
    }

    /// <summary>
    /// Add a new transaction to the ledger
    /// </summary>
    public void AddTransaction(ResourceType resourceType, int amount, string description = "")
    {
        Transaction newTransaction = new Transaction(resourceType, amount, description);
        transactions.Add(newTransaction);
        
        // Keep only the most recent transactions (optional limit)
        if (transactions.Count > 100)
        {
            transactions.RemoveAt(0);
        }
        
        RefreshTransactionList();
    }

    /// <summary>
    /// Clear all transactions
    /// </summary>
    public void ClearAllTransactions()
    {
        transactions.Clear();
        RefreshTransactionList();
    }

    /// <summary>
    /// Refresh the UI to show all transactions
    /// </summary>
    private void RefreshTransactionList()
    {
        if (transactionListParent == null)
        {
            Debug.LogError("Transaction List Parent is not assigned!");
            return;
        }

        EnsureScrollInfrastructure();
        EnsureTransactionListLayout();
        EnsureEmptyStateText();

        // Clear existing UI entries
        foreach (Transform child in transactionListParent)
        {
            if (child.name.Contains("TransactionEntry"))
                Destroy(child.gameObject);
        }

        // Show empty state if no transactions
        if (transactions.Count == 0)
        {
            if (emptyStateText != null)
                emptyStateText.gameObject.SetActive(true);
            return;
        }

        if (emptyStateText != null)
            emptyStateText.gameObject.SetActive(false);

        // Create UI entries for each transaction (newest first)
        foreach (Transaction transaction in transactions.AsEnumerable().Reverse())
        {
            CreateTransactionEntry(transaction);
        }

        if (transactionListParent is RectTransform rectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        if (scrollViewportRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewportRect);
        }

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>
    /// Create a single transaction entry in the UI
    /// </summary>
    private void CreateTransactionEntry(Transaction transaction)
    {
        if (transactionEntryPrefab == null)
        {
            CreateCardEntry(transaction);
            return;
        }

        // Instantiate prefab
        GameObject entry = Instantiate(transactionEntryPrefab, transactionListParent);
        
        // Find and update the text component
        TMP_Text entryText = entry.GetComponentInChildren<TMP_Text>();
        if (entryText != null)
        {
            entryText.text = $"{transaction.GetDisplayTitle()}\n{transaction.GetDisplaySource()}   {transaction.GetDisplayTimestamp()}";
            entryText.enableWordWrapping = true;
            entryText.color = GetAmountColor(transaction.amount);
        }
    }

    private void CreateCardEntry(Transaction transaction)
    {
        var amountColor = GetAmountColor(transaction.amount);

        var entry = new GameObject("TransactionEntry");
        entry.transform.SetParent(transactionListParent, false);
        entry.layer = transactionListParent.gameObject.layer;

        var rectTransform = entry.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.sizeDelta = new Vector2(0f, 88f);

        var layoutElement = entry.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 88f;
        layoutElement.flexibleWidth = 1f;

        var background = entry.AddComponent<Image>();
        background.color = CardBackgroundColor;
        background.raycastTarget = false;

        CreateAccentBar(entry.transform, amountColor);

        var amountText = CreateTextElement(
            entry.transform,
            "TransactionEntryAmount",
            new Vector2(0f, 0f),
            new Vector2(0.52f, 1f),
            new Vector2(28f, 14f),
            new Vector2(-8f, -14f),
            28f,
            amountColor,
            TextAlignmentOptions.MidlineLeft);
        amountText.text = transaction.GetDisplayTitle();

        var detailText = CreateTextElement(
            entry.transform,
            "TransactionEntryDetails",
            new Vector2(0.52f, 0f),
            new Vector2(1f, 1f),
            new Vector2(8f, 16f),
            new Vector2(-24f, -16f),
            18f,
            MetaTextColor,
            TextAlignmentOptions.MidlineRight);
        detailText.text = $"{transaction.GetDisplaySource()}\n{transaction.GetDisplayTimestamp()}";
        detailText.lineSpacing = -8f;
    }

    private void EnsureTransactionListLayout()
    {
        if (transactionListParent == null)
        {
            return;
        }

        if (transactionListParent is RectTransform rectTransform)
        {
            if (scrollViewportRect != null && rectTransform.parent == scrollViewportRect)
            {
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(0f, rectTransform.sizeDelta.y);
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
            }
            else if (rectTransform.sizeDelta.x < 720f)
            {
                rectTransform.sizeDelta = new Vector2(ScrollViewportWidth, rectTransform.sizeDelta.y);
            }
        }

        var verticalLayoutGroup = transactionListParent.GetComponent<VerticalLayoutGroup>();
        if (verticalLayoutGroup == null)
        {
            verticalLayoutGroup = transactionListParent.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        verticalLayoutGroup.padding = new RectOffset(20, 20, 20, 20);
        verticalLayoutGroup.spacing = 12f;
        verticalLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        verticalLayoutGroup.childControlWidth = true;
        verticalLayoutGroup.childControlHeight = false;
        verticalLayoutGroup.childForceExpandWidth = true;
        verticalLayoutGroup.childForceExpandHeight = false;

        var contentSizeFitter = transactionListParent.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter == null)
        {
            contentSizeFitter = transactionListParent.gameObject.AddComponent<ContentSizeFitter>();
        }

        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void EnsureScrollInfrastructure()
    {
        if (transactionListParent == null)
        {
            return;
        }

        var contentRect = transactionListParent as RectTransform;
        if (contentRect == null)
        {
            Debug.LogWarning("[TransactionLedgerPanel] transactionListParent must be a RectTransform.");
            return;
        }

        ResolveExistingScrollInfrastructure();

        if (scrollRootRect == null)
        {
            var rootObject = new GameObject(ScrollRootName);
            rootObject.transform.SetParent(transform, false);
            rootObject.layer = transactionListParent.gameObject.layer;

            scrollRootRect = rootObject.AddComponent<RectTransform>();
            scrollRootRect.anchorMin = contentRect.anchorMin;
            scrollRootRect.anchorMax = contentRect.anchorMax;
            scrollRootRect.pivot = contentRect.pivot;
            scrollRootRect.localPosition = contentRect.localPosition;
            scrollRootRect.localRotation = contentRect.localRotation;
            scrollRootRect.localScale = contentRect.localScale;
            scrollRootRect.anchoredPosition = contentRect.anchoredPosition;
            scrollRootRect.sizeDelta = new Vector2(ScrollViewportWidth, ScrollViewportHeight);

            scrollRect = rootObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.inertia = true;
        }

        if (scrollViewportRect == null)
        {
            var viewportObject = new GameObject(ScrollViewportName);
            viewportObject.transform.SetParent(scrollRootRect, false);
            viewportObject.layer = transactionListParent.gameObject.layer;

            scrollViewportRect = viewportObject.AddComponent<RectTransform>();
            scrollViewportRect.anchorMin = new Vector2(0f, 0f);
            scrollViewportRect.anchorMax = new Vector2(1f, 1f);
            scrollViewportRect.pivot = new Vector2(0.5f, 0.5f);
            scrollViewportRect.offsetMin = Vector2.zero;
            scrollViewportRect.offsetMax = new Vector2(-(ScrollbarWidth + ScrollbarRightInset + 8f), 0f);

            var viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;

            viewportObject.AddComponent<RectMask2D>();
        }

        if (verticalScrollbar == null)
        {
            verticalScrollbar = CreateVerticalScrollbar(scrollRootRect, transactionListParent.gameObject.layer);
        }

        if (contentRect.parent != scrollViewportRect)
        {
            contentRect.SetParent(scrollViewportRect, false);
            contentRect.localScale = Vector3.one;
            contentRect.localRotation = Quaternion.identity;
        }

        scrollRect.viewport = scrollViewportRect;
        scrollRect.content = contentRect;
        scrollRect.verticalScrollbar = verticalScrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scrollRect.verticalScrollbarSpacing = 8f;
    }

    private void ResolveExistingScrollInfrastructure()
    {
        if (scrollRootRect == null)
        {
            scrollRootRect = transform.Find(ScrollRootName) as RectTransform;
        }

        if (scrollRootRect == null)
        {
            return;
        }

        if (scrollRect == null)
        {
            scrollRect = scrollRootRect.GetComponent<ScrollRect>();
        }

        if (scrollViewportRect == null)
        {
            scrollViewportRect = scrollRootRect.Find(ScrollViewportName) as RectTransform;
        }

        if (verticalScrollbar == null)
        {
            var scrollbarTransform = scrollRootRect.Find(ScrollbarName);
            if (scrollbarTransform != null)
            {
                verticalScrollbar = scrollbarTransform.GetComponent<Scrollbar>();
            }
        }
    }

    private void EnsureEmptyStateText()
    {
        if (emptyStateText != null || transactionListParent == null)
        {
            return;
        }

        var emptyStateObject = new GameObject("TransactionLedgerEmptyState");
        emptyStateObject.transform.SetParent(transactionListParent, false);
        emptyStateObject.layer = transactionListParent.gameObject.layer;

        var rectTransform = emptyStateObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.sizeDelta = new Vector2(0f, 72f);

        var layoutElement = emptyStateObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 72f;
        layoutElement.flexibleWidth = 1f;

        emptyStateText = emptyStateObject.AddComponent<TextMeshProUGUI>();
        emptyStateText.text = "No transactions recorded yet.";
        emptyStateText.fontSize = 22f;
        emptyStateText.alignment = TextAlignmentOptions.MidlineLeft;
        emptyStateText.color = MetaTextColor;
        emptyStateText.enableWordWrapping = true;
        emptyStateText.raycastTarget = false;
        emptyStateText.gameObject.SetActive(false);
    }

    private static TextMeshProUGUI CreateTextElement(
        Transform parent,
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        var gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = parent.gameObject.layer;

        var rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        var text = gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(14f, fontSize - 8f);
        text.fontSizeMax = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static Scrollbar CreateVerticalScrollbar(Transform parent, int uiLayer)
    {
        var scrollbarObject = new GameObject(ScrollbarName);
        scrollbarObject.transform.SetParent(parent, false);
        scrollbarObject.layer = uiLayer;

        var scrollbarRect = scrollbarObject.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(ScrollbarWidth, -12f);
        scrollbarRect.anchoredPosition = new Vector2(-ScrollbarRightInset, 0f);

        var trackImage = scrollbarObject.AddComponent<Image>();
        trackImage.color = ScrollTrackColor;
        trackImage.raycastTarget = true;

        var scrollbar = scrollbarObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        var handleObject = new GameObject(ScrollbarHandleName);
        handleObject.transform.SetParent(scrollbarObject.transform, false);
        handleObject.layer = uiLayer;

        var handleRect = handleObject.AddComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0f);
        handleRect.anchorMax = new Vector2(1f, 1f);
        handleRect.offsetMin = new Vector2(2f, 2f);
        handleRect.offsetMax = new Vector2(-2f, -2f);

        var handleImage = handleObject.AddComponent<Image>();
        handleImage.color = ScrollHandleColor;
        handleImage.raycastTarget = true;

        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;

        return scrollbar;
    }

    private static void CreateAccentBar(Transform parent, Color color)
    {
        var accentObject = new GameObject("TransactionEntryAccent");
        accentObject.transform.SetParent(parent, false);
        accentObject.layer = parent.gameObject.layer;

        var rectTransform = accentObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 0.5f);
        rectTransform.sizeDelta = new Vector2(8f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, 0f);

        var image = accentObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static Color GetAmountColor(int amount)
    {
        return amount >= 0 ? PositiveAmountColor : NegativeAmountColor;
    }

    /// <summary>
    /// Initialize with some example transactions (for testing)
    /// </summary>
    private void Start()
    {
        if (loadFromEconomyServiceOnEnable)
        {
            return;
        }

        // Example transactions - used only when not loading from EconomyService.
        if (transactions.Count == 0)
        {
            AddTransaction(ResourceType.Coins, 120);
            AddTransaction(ResourceType.Coins, -10);
            AddTransaction(ResourceType.Coins, 1);
            AddTransaction(ResourceType.Coins, -30);
            AddTransaction(ResourceType.Coins, 61);
            AddTransaction(ResourceType.Coins, -800);
            AddTransaction(ResourceType.Gems, 5);
            AddTransaction(ResourceType.Gems, -2);
            AddTransaction(ResourceType.CoachingCredits, 10);
            AddTransaction(ResourceType.CoachingCredits, -3);
        }
    }

    /// <summary>
    /// Get all transactions (for external access)
    /// </summary>
    public List<Transaction> GetTransactions()
    {
        return new List<Transaction>(transactions);
    }

    public void ReloadTransactionsFromStorage()
    {
        if (economyService == null)
        {
            economyService = new EconomyService();
        }

        LoadTransactionsFromEconomyService();
        RefreshTransactionList();
    }

    private void LoadTransactionsFromEconomyService()
    {
        if (economyService == null)
        {
            return;
        }

        var recent = economyService.GetRecentTransactions(playerId, Mathf.Max(1, recentTransactionLimit));
        transactions = recent
            .Select(MapWalletTransactionToUiTransaction)
            .ToList();
    }

    private static Transaction MapWalletTransactionToUiTransaction(WalletTransaction walletTx)
    {
        var resourceType = walletTx.currency switch
        {
            "coins" => ResourceType.Coins,
            "gems" => ResourceType.Gems,
            "coaching_credits" => ResourceType.CoachingCredits,
            _ => ResourceType.Coins
        };

        var signedAmount = walletTx.type == "spend"
            ? -Mathf.Abs(walletTx.amount)
            : Mathf.Abs(walletTx.amount);

        var timestamp = DateTime.TryParse(walletTx.timestamp, out var parsedTimestamp)
            ? parsedTimestamp
            : DateTime.Now;

        return new Transaction(resourceType, signedAmount, walletTx.source, timestamp);
    }

    private void OnWalletUpdatedEventMessage(EventBus.EventEnvelope evt)
    {
        if (evt == null || evt.player_id != playerId)
        {
            return;
        }

        ReloadTransactionsFromStorage();
    }
}
