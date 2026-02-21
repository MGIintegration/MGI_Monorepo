using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;


public class ScreenManager : MonoBehaviour
{
    [Header("Screens")]
    public GameObject screenHub;
    public GameObject screenBracket;
    public GameObject screenXP;
    public GameObject screenSimFeedback;

    [Header("Hub UI References")]
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI weekText;
    public TextMeshProUGUI remainingMatchesText;
    public TextMeshProUGUI rankText; 
    public TextMeshProUGUI tierText; 
    public TextMeshProUGUI offenseText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI opponentText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI xpEarnedText;
    public TextMeshProUGUI rewardText;



    [Header("Optional References (if available)")]
    public BracketUI bracketUI;
    public XPUI xpUI;

    private SeasonManager seasonManager;

 
    void Awake()
    {
        StartCoroutine(WaitForSeasonManager());
    }

    private IEnumerator WaitForSeasonManager()
    {
        while (SeasonManager.Instance == null)
            yield return null;

        seasonManager = SeasonManager.Instance;

        seasonManager.OnSeasonDataUpdated += UpdateAllScreens;


        UpdateAllScreens();
    }

    private void OnDestroy()
    {
        if (seasonManager != null)
            seasonManager.OnSeasonDataUpdated -= UpdateAllScreens;
    }

   
    void Start()
    {
        ShowHub();
    }

    public void ShowHub()
    {
        ShowScreen(screenHub);
        UpdateHubDisplay();
    }

    public void ShowBracket()
    {
        ShowScreen(screenBracket);
        bracketUI?.RefreshBracket();
    }

    public void ShowXP()
    {
        ShowScreen(screenXP);
        xpUI?.RefreshXPHistory();
    }

    public void ShowSimFeedback()
    {

         
        OnSimulateNextWeek();
        ShowScreen(screenSimFeedback);

    }

    private void ShowScreen(GameObject screen)
    {
        if (screenHub != null) screenHub.SetActive(false);
        if (screenBracket != null) screenBracket.SetActive(false);
        if (screenXP != null) screenXP.SetActive(false);
        if (screenSimFeedback != null) screenSimFeedback.SetActive(false);

        screen.SetActive(true);
    }


    public void UpdateAllScreens()
    {
        if (seasonManager == null) return;


        UpdateHubDisplay();
        xpUI?.RefreshXPHistory();
        bracketUI?.RefreshBracket();
    }

    public void UpdateHubDisplay()
    {
        if (seasonManager == null)
        {
            Debug.LogWarning("ScreenManager: SeasonManager not initialized yet");
            return;
        }

        var prog = ApiClient.Instance?.PlayerProgressionSaveData;
        if (xpText != null)
         xpText.text = $"XP: {seasonManager.PlayerXP}/1000";

        if (weekText != null)
            weekText.text = $"Week: {seasonManager.CurrentWeek}/10";
        if (remainingMatchesText != null)
        {
            int remaining = 10 - seasonManager.CurrentWeek;
            remainingMatchesText.text = $"Remaining: {remaining}";
        }

        if (tierText != null)
            tierText.text = $"Tier: {seasonManager.PlayerTier}";


        if (rankText != null)
        rankText.text = $"Rank: {seasonManager.PlayerRank} / {seasonManager.Teams.Count}";

    }
    public void OnSimulateNextWeek()
{
    if (seasonManager == null)
    {
        Debug.LogWarning("SimulationFeedbackUI: SeasonManager not ready.");
        return;
    }

    seasonManager.SimulateNextWeek(updatedSeason =>
    {
        
        var player = seasonManager.PlayerTeam;
        var opponent = updatedSeason.teams.Find(t => t.player_id != player?.player_id);
        
        bool playerWon = false;
        if (opponent != null && player != null)
        {
            int playerWins = player.stats != null ? player.stats.wins : 0;
            int oppWins = opponent.stats != null ? opponent.stats.wins : 0;
            playerWon = playerWins >= oppWins;
        }

        // Generate boosts first
        int offenseBoost = Random.Range(5, 15);
        int defenseBoost = Random.Range(3, 10);


        // Add null checks for all UI elements
        opponentText?.SetText(opponent != null ? $"Opponent: {opponent.team_name}" : "Opponent: -");

        if (titleText != null)
        {
            titleText.SetText($"MATCH SIMULATION RESULT - WEEK {updatedSeason.current_week}");
        }
        
        if (opponentText != null)
        {
            opponentText.SetText(opponent != null ? $"Opponent: {opponent.team_name}" : "Opponent: -");
        }
        
        if (resultText != null)
        {
            resultText.SetText(playerWon ? "Result: WIN" : "Result: LOSS");
            resultText.color = playerWon ? Color.green : Color.red;
        }

        var prog = ApiClient.Instance?.PlayerProgressionSaveData;
        int xp_earned = 0;
        if (prog.xp_history != null)
        {
            foreach (var entry in prog.xp_history)
            {
                xp_earned = entry.xp_gained; // Assuming XPHistoryEntry has xp_gained field
            }
        }
        if (xpEarnedText != null)
        {
            if (prog != null)
                xpEarnedText.SetText($"XP Gained: {xp_earned}");
            else
                xpEarnedText.SetText("XP Gained: -");
        }

        if (offenseText != null)
        {
            offenseText.SetText($"Offense: +{offenseBoost}%");
        }
        
        if (defenseText != null)
        {
            defenseText.SetText($"Defense: +{defenseBoost}%");
        }

        // update hub screen
        UpdateHubDisplay();

        // optionally auto-return to hub after short delay
        // StartCoroutine(ReturnToHubAfterDelay(2.5f)); // Increased delay so user can see results
    });
}
        private IEnumerator ReturnToHubAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowHub();
    }

}
