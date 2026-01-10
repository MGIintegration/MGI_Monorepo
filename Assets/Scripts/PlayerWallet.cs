using System;
using UnityEngine;
using CCAS.Config;

/// <summary>
/// Simple player currency manager for Phase 1.
/// Tracks coins (only) and handles affordability checks.
/// </summary>
public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance;

    [Header("Starting Balances")]
    public int coins = 500;

    public event Action OnChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadWallet();
    }

    // ----------------------------------------------------------
    // Core Logic
    // ----------------------------------------------------------

    public bool CanAfford(PackType p)
    {
        // Simplified: cost is now just an int (coins)
        return coins >= p.cost;
    }

    public bool SpendForPack(PackType p)
    {
        if (!CanAfford(p))
            return false;

        coins = Mathf.Max(0, coins - p.cost);
        SaveWallet();
        OnChanged?.Invoke();
        return true;
    }

    // ----------------------------------------------------------
    // Optional Helpers
    // ----------------------------------------------------------

    public void AddCoins(int amount)
    {
        coins = Mathf.Max(0, coins + amount);
        SaveWallet();
        OnChanged?.Invoke();
    }

    // ----------------------------------------------------------
    // Persistence (Optional)
    // ----------------------------------------------------------

    private void SaveWallet()
    {
        PlayerPrefs.SetInt("wallet_coins", coins);
        PlayerPrefs.Save();
    }

    private void LoadWallet()
    {
        coins = PlayerPrefs.GetInt("wallet_coins", coins);
    }
}
