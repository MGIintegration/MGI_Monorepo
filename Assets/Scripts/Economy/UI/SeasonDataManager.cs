using System;
using System.Collections.Generic;
using UnityEngine;

public class SeasonDataManager : MonoBehaviour
{
    public static SeasonDataManager Instance;

    [Header("Season Data")]
    public int CurrentWeek = 1;
    public int PlayerXP = 220;

    [Header("Tracking")]
    public List<string> XPHistory = new List<string>();

    public event Action OnSeasonDataUpdated; // <— notify all screens

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddXPHistory(string label, int amount)
    {
        XPHistory.Add($"{label} +{amount} XP");
        Debug.Log($"XP history updated: {label} +{amount}");
        OnSeasonDataUpdated?.Invoke();
    }

    public void AddXP(int amount)
    {
        PlayerXP += amount;
        OnSeasonDataUpdated?.Invoke();
    }

    public void NextWeek()
    {
        CurrentWeek++;
        OnSeasonDataUpdated?.Invoke();
    }
}
