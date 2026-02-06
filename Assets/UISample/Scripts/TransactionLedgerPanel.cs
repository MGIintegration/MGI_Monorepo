using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class TransactionLedgerPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform transactionListParent; // ContentContainer
    [SerializeField] private GameObject transactionEntryPrefab; // Optional prefab
    [SerializeField] private TMP_Text emptyStateText; // Optional: "No transactions" message

    [Header("Transaction History")]
    private List<Transaction> transactions = new List<Transaction>();

    private void OnEnable()
    {
        RefreshTransactionList();
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
    }

    /// <summary>
    /// Create a single transaction entry in the UI
    /// </summary>
    private void CreateTransactionEntry(Transaction transaction)
    {
        if (transactionEntryPrefab == null)
        {
            // If no prefab, create a simple text entry
            CreateSimpleTextEntry(transaction);
            return;
        }

        // Instantiate prefab
        GameObject entry = Instantiate(transactionEntryPrefab, transactionListParent);
        
        // Find and update the text component
        TMP_Text entryText = entry.GetComponentInChildren<TMP_Text>();
        if (entryText != null)
        {
            entryText.text = transaction.GetFormattedText();
            entryText.color = transaction.amount >= 0 ? Color.green : Color.red;
        }
    }

    /// <summary>
    /// Create a simple, properly formatted text entry
    /// </summary>
    private void CreateSimpleTextEntry(Transaction transaction)
    {
        // Create GameObject
        GameObject entry = new GameObject("TransactionEntry");
        entry.transform.SetParent(transactionListParent, false);
        
        // Add RectTransform and configure it
        RectTransform rectTransform = entry.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1); // Top-left anchor
        rectTransform.anchorMax = new Vector2(1, 1); // Top-right anchor
        rectTransform.pivot = new Vector2(0, 1); // Top-left pivot
        rectTransform.sizeDelta = new Vector2(0, 40); // Height of each entry
        rectTransform.anchoredPosition = Vector2.zero; // Will be positioned by layout group
        
        // Add TextMeshPro component
        TextMeshProUGUI text = entry.AddComponent<TextMeshProUGUI>();
        text.text = transaction.GetFormattedText();
        text.fontSize = 36; // Larger, more readable font
        text.color = transaction.amount >= 0 ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f); // Green/Red
        text.alignment = TextAlignmentOptions.Left; // Left align
        text.enableWordWrapping = false; // Don't wrap text
        
        // Add LayoutElement for proper spacing in Vertical Layout Group
        LayoutElement layoutElement = entry.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 40;
        layoutElement.flexibleWidth = 1; // Take full width
    }

    /// <summary>
    /// Initialize with some example transactions (for testing)
    /// </summary>
    private void Start()
    {
        // Example transactions - remove this in production
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
}