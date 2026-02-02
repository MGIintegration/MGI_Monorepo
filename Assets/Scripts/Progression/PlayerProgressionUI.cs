using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.Networking;

// ==========================
// DATA CLASSES (SCHEMA MODEL)
// ==========================
[Serializable]
public class TierProgression
{
    public int min_xp;
    public int max_xp;
    public string display_name;
    public string[] unlock_features;
}

[Serializable]
public class XpHistoryEntry
{
    public string timestamp;
    public int xp_gained;
    public string source;
    public float facility_multiplier;
    public float coaching_bonus;
}

[Serializable]
public class PlayerProgression
{
    public string player_id;
    public int current_xp;
    public string current_tier;
    public TierProgression rookie; // Simplified: only one tier shown for example
    public XpHistoryEntry[] xp_history;
}

// ==========================
// MAIN UI CONTROLLER SCRIPT
// ==========================
public class PlayerProgressionUI : MonoBehaviour
{
    [Header("API Settings")]
    public string apiUrl = "http://localhost:8000/api/player_progression";

    [Header("UI References")]
    public Text tierText;
    public Text xpText;
    public Image progressBar;
    public Transform xpHistoryContainer;
    public GameObject xpHistoryEntryPrefab;

    private PlayerProgression playerData;

    void Start()
    {
        // You can replace this with a coroutine calling your backend
        StartCoroutine(FetchProgressionData());
    }

    private IEnumerator FetchProgressionData()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                playerData = JsonUtility.FromJson<PlayerProgression>(json);
                UpdateUI();
            }
            else
            {
                Debug.LogError($"Failed to fetch player data: {request.error}");
            }
        }
    }

    private void UpdateUI()
    {
        if (playerData == null)
        {
            Debug.LogWarning("Player data not loaded yet.");
            return;
        }

        // 1️⃣ Update Tier Text
        tierText.text = $"Tier: {playerData.current_tier}";

        // 2️⃣ Update XP and Progress Bar
        int minXP = playerData.rookie.min_xp;
        int maxXP = playerData.rookie.max_xp;
        float progress = Mathf.InverseLerp(minXP, maxXP, playerData.current_xp);

        xpText.text = $"XP: {playerData.current_xp} / {maxXP}";
        progressBar.fillAmount = progress;

        // 3️⃣ Populate XP History Scroll
        foreach (Transform child in xpHistoryContainer)
            Destroy(child.gameObject);

        foreach (var entry in playerData.xp_history)
        {
            GameObject item = Instantiate(xpHistoryEntryPrefab, xpHistoryContainer);
            Text entryText = item.GetComponent<Text>();
            entryText.text = $"{entry.timestamp}: +{entry.xp_gained} XP ({entry.source})";
        }
    }
}
