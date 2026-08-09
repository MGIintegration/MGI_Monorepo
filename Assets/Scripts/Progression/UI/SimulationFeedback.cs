using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SimulationFeedbackUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI opponentText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI xpEarnedText;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI offenseText;
    public TextMeshProUGUI defenseText;

    [Header("Buttons")]
    public Button btnSimNextWeek;
    public Button btnBackToHub;

    // SeasonManager is a singleton that's safe to read anytime once it exists.
    // Resolving it live (instead of caching a reference set by a coroutine) avoids
    // a startup race: if this GameObject gets deactivated by ScreenManager.Start()
    // before the old wait-and-cache coroutine resolved, the cached reference was
    // left permanently null and OnSimulateNextWeek() would silently no-op forever.
    private SeasonManager seasonManager => SeasonManager.Instance;
    private ScreenManager screenManager;
    private SeasonManager subscribedManager;

    void OnEnable()
    {
        screenManager = FindObjectOfType<ScreenManager>();

        if (btnBackToHub != null)
        {
            btnBackToHub.onClick.RemoveAllListeners();
            btnBackToHub.onClick.AddListener(() => screenManager?.ShowHub());
        }

        RefreshUsingSeasonData();
        StartCoroutine(SubscribeToSeasonManager());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (subscribedManager != null)
        {
            subscribedManager.OnSeasonDataUpdated -= OnSeasonDataUpdated;
            subscribedManager = null;
        }
    }

    private IEnumerator SubscribeToSeasonManager()
    {
        while (SeasonManager.Instance == null)
            yield return null;

        subscribedManager = SeasonManager.Instance;
        subscribedManager.OnSeasonDataUpdated += OnSeasonDataUpdated;

        RefreshUsingSeasonData();
    }

    private void OnSeasonDataUpdated()
    {
        // called when SeasonManager got fresh data
        RefreshUsingSeasonData();
    }

    private void RefreshUsingSeasonData()
    {
        if (seasonManager == null) return;

        // Show some basic info — week and current XP
        int wk = seasonManager.CurrentWeek;
        int xp_earned = seasonManager.LastXpGained;
       
        titleText?.SetText($"MATCH SIMULATION RESULT - WEEK {wk}");
        xpEarnedText?.SetText($"XP Gained: {xp_earned}");
        resultText?.SetText(seasonManager.WasLastMatchWin ? "Result: WIN" : "Result: LOSS");
        if (resultText != null)
            resultText.color = seasonManager.WasLastMatchWin ? Color.green : Color.red;
        rewardText?.SetText($"Tier: {seasonManager.PlayerTier}");


    }

    public void OnSimulateNextWeek()
    {
        if (seasonManager == null)
        {
            Debug.LogWarning("SimulationFeedbackUI: SeasonManager not ready.");
            return;
        }

        if (!seasonManager.CanSimulateWeek)
        {
            if (btnSimNextWeek != null) btnSimNextWeek.interactable = false;
            return;
        }

        btnSimNextWeek.interactable = false;

        // Use SeasonManager to simulate — it will fetch progression and trigger OnSeasonDataUpdated
        seasonManager.SimulateNextWeek(updatedSeason =>
        {
            // UI update based on freshly returned season state
            // pick an opponent (simple find first non-player)
            var player = seasonManager.PlayerTeam;
            var opponent = updatedSeason.teams.Find(t => !t.is_player_team);

            bool playerWon = seasonManager.WasLastMatchWin;

            titleText?.SetText($"MATCH SIMULATION RESULT - WEEK {updatedSeason.current_week}");
            opponentText?.SetText(opponent != null ? $"Opponent: {opponent.team_name}" : "Opponent: -");
            resultText?.SetText(playerWon ? "Result: WIN" : "Result: LOSS");
            if (resultText != null)
                resultText.color = playerWon ? Color.green : Color.red;

            // xp displayed from SeasonManager progression (just updated)
            int xp_earned = seasonManager.LastXpGained;
            xpEarnedText?.SetText($"XP Gained: {xp_earned}");
            int offenseBoost = Random.Range(5, 15);
            int defenseBoost = Random.Range(3, 10);
            offenseText?.SetText($"Offense: +{offenseBoost}%");
            defenseText?.SetText($"Defense: +{defenseBoost}%");

            // update hub screen and re-enable button
            screenManager?.UpdateHubDisplay();
            btnSimNextWeek.interactable = true;

            // optionally auto-return to hub after short delay
            StartCoroutine(ReturnToHubAfterDelay(1.2f));
        });
    }

    private IEnumerator ReturnToHubAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        screenManager?.ShowHub();
    }
}
