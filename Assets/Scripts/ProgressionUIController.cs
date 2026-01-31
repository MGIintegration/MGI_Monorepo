using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System;

[RequireComponent(typeof(UIDocument))]
public class ProgressionUIController : MonoBehaviour
{
    private VisualElement _root;
    private SeasonManager _seasonManager;

    // Screen Containers
    private VisualElement _hubScreen, _bracketScreen, _xpScreen, _simFeedbackScreen;

    // Common Elements
    private Button _simulateWeekButton;

    // Hub Screen Elements
    private Label _hubWeekLabel, _hubStandingLabel, _hubMatchesLabel, _hubXpLabel;
    private VisualElement _hubXpBarFill;

    // Bracket Screen Elements
    private VisualElement _bracketTable;
    private Label _bracketWeekLabel, _bracketInfoLabel;

    // XP Screen Elements
    private Label _xpCurrentLabel;
    private ScrollView _xpHistoryScrollView;
    private VisualElement _xpScreenBarFill;

    // Sim Feedback Screen Elements
    private Label _simTitleLabel, _simResultLabel, _simOpponentLabel, _simXpGainLabel, _simRewardLabel;
    private Button _continueToHubButton;

    // Nav Bar Buttons
    private Button _navHubButton, _navBracketButton, _navXpButton;

    void OnEnable()
    {
        _seasonManager = SeasonManager.Instance;
        _root = GetComponent<UIDocument>().rootVisualElement;

        // Query all elements
        QueryElements();
        RegisterCallbacks();

        // Initial UI state
        ShowScreen("Screen_Hub");
    }

    public void OnSeasonDataReady()
    {
        UpdateAllUI();
    }

    private void QueryElements()
    {
        // Screens
        _hubScreen = _root.Q<VisualElement>("Screen_Hub");
        _bracketScreen = _root.Q<VisualElement>("Screen_Bracket");
        _xpScreen = _root.Q<VisualElement>("Screen_XP");
        _simFeedbackScreen = _root.Q<VisualElement>("Screen_SimFeedback");

        // Hub
        _hubWeekLabel = _root.Q<Label>("Hub_Label_Week");
        _hubStandingLabel = _root.Q<Label>("Hub_Label_Standing");
        _hubMatchesLabel = _root.Q<Label>("Hub_Label_Matches");
        _hubXpLabel = _root.Q<Label>("Hub_Label_XP");
        _hubXpBarFill = _root.Q<VisualElement>("XP_ProgressBar_Fill");
        _simulateWeekButton = _root.Q<Button>("Button_SimulateWeek");

        // Bracket
        _bracketTable = _root.Q<VisualElement>("Bracket_Table");
        _bracketWeekLabel = _root.Q<Label>("Bracket_Label_Week");
        _bracketInfoLabel = _root.Q<Label>("Bracket_Label_Info");

        // XP
        _xpCurrentLabel = _root.Q<Label>("XP_Label_Current");
        _xpScreenBarFill = _root.Q<VisualElement>("XP_Screen_ProgressBar_Fill");
        _xpHistoryScrollView = _root.Q<ScrollView>("XP_History_ScrollView");

        // Sim Feedback
        _simTitleLabel = _root.Q<Label>("Sim_Label_Title");
        _simResultLabel = _root.Q<Label>("Sim_Label_Result");
        _simOpponentLabel = _root.Q<Label>("Sim_Label_Opponent");
        _simXpGainLabel = _root.Q<Label>("Sim_Label_XPGain");
        _simRewardLabel = _root.Q<Label>("Sim_Label_Reward");
        _continueToHubButton = _root.Q<Button>("Button_ContinueToHub");

        // Nav Bar
        _navHubButton = _root.Q<Button>("Nav_Button_Hub");
        _navBracketButton = _root.Q<Button>("Nav_Button_Bracket");
        _navXpButton = _root.Q<Button>("Nav_Button_XP");
    }

    private void RegisterCallbacks()
    {
        _simulateWeekButton.RegisterCallback<ClickEvent>(evt => OnSimulateWeek());
        _continueToHubButton.RegisterCallback<ClickEvent>(evt =>
        {
            ShowScreen("Screen_Hub");
            UpdateAllUI();
        });

        // Nav Bar
        _navHubButton.RegisterCallback<ClickEvent>(evt =>
        {
            ShowScreen("Screen_Hub");
            UpdateHubScreen();
        });
        _navBracketButton.RegisterCallback<ClickEvent>(evt =>
        {
            ShowScreen("Screen_Bracket");
            UpdateBracketScreen();
        });
        _navXpButton.RegisterCallback<ClickEvent>(evt =>
        {
            ShowScreen("Screen_XP");
            UpdateXpScreen();
        });
    }

    private void OnSimulateWeek()
    {
        _seasonManager.SimulateNextWeek(updatedData =>
        {
            // Update feedback screen with new state
            var playerTeam = updatedData.teams.FirstOrDefault(t => t.is_player_team);
            if (playerTeam == null) return;

            var playerProg = updatedData.player_progression
                .FirstOrDefault(p => p.player_id == playerTeam.team_id || p.player_id == playerTeam.player_id);

            if (playerProg == null || playerProg.xp_history == null || playerProg.xp_history.Count == 0)
                return;

            var lastEntry = playerProg.xp_history.Last();

            string source = string.IsNullOrEmpty(lastEntry.source) ? "unknown" : lastEntry.source;
            string xp = lastEntry.xp_gained.ToString();
            string facility = lastEntry.facility_multiplier.ToString("0.##");
            string coaching = lastEntry.coaching_bonus.ToString("0.##");



            _simTitleLabel.text = $"🎮 MATCH RESULT - WEEK {updatedData.current_week}";
            _simResultLabel.text = $"🏆 Result: {source.ToUpper()}";
            _simXpGainLabel.text = $"📈 XP Earned: +{xp}";
            _simOpponentLabel.text = "🆚 Opponent: TBD"; // replace if API sends opponent info

            ShowScreen("Screen_SimFeedback");
            UpdateAllUI();
        });
    }

    private void UpdateAllUI()
    {
        UpdateHubScreen();
        UpdateBracketScreen();
        UpdateXpScreen();
    }

    private void UpdateHubScreen()
    {
        if (_seasonManager == null || _seasonManager.Teams == null)
        {
            Debug.LogWarning("SeasonManager not initialized yet.");
            return;
        }

        int remaining = _seasonManager.TotalWeeks - _seasonManager.CurrentWeek;
        _hubWeekLabel.text = $"📅 Week: {_seasonManager.CurrentWeek}/{_seasonManager.TotalWeeks}";

        _hubStandingLabel.text = $"🏆 Standing: {_seasonManager.PlayerRank}th Place";
        _hubMatchesLabel.text = $"🔁 Remaining Matches: {remaining}";

        float xpProgress = (float)_seasonManager.PlayerXP / 1000f;
        _hubXpLabel.text = $"⭐ XP: {_seasonManager.PlayerXP} / 1000";
        _hubXpBarFill.style.width = Length.Percent(xpProgress * 100);
    }

    private void UpdateBracketScreen()
    {
        _bracketWeekLabel.text = $"STANDINGS AFTER WEEK {_seasonManager.CurrentWeek - 1}";

        // Clear previous entries (except header)
        _bracketTable.Query(className: "table-row").ForEach(row => row.RemoveFromHierarchy());

        var sortedTeams = _seasonManager.Teams
            .OrderByDescending(t => t.stats.points)
            .ThenByDescending(t => t.stats.wins)
            .ToList();

        
        Debug.Log("Hello, World!");

        for (int i = 0; i < sortedTeams.Count(); i++)
        {
            var team = sortedTeams[i];
            var row = new VisualElement();
            row.AddToClassList("table-row");
            if (team.is_player_team) row.AddToClassList("player-row");

            var rankLabel = new Label((i + 1).ToString()) { style = { flexGrow = 1 } };
            var nameLabel = new Label(team.team_name) { style = { flexGrow = 3 } };
            var wlLabel = new Label($"{team.stats.wins}-{team.stats.losses}") { style = { flexGrow = 2 } };
            // var xpLabel = new Label(team.progression.total_xp.ToString()) { style = { flexGrow = 2 } };

            row.Add(rankLabel);
            row.Add(nameLabel);
            row.Add(wlLabel);
            // row.Add(xpLabel);
            _bracketTable.Add(row);
        }

        _bracketInfoLabel.text = $"📍 You are currently ranked {_seasonManager.PlayerRank}th";
    }

    private void UpdateXpScreen()
    {
        float xpProgress = (float)_seasonManager.PlayerXP / 1000f;
        _xpCurrentLabel.text = $"Current XP: {_seasonManager.PlayerXP} / 1000 ({PlayerTier()})";
        _xpScreenBarFill.style.width = Length.Percent(xpProgress * 100);

        // Update XP History
        _xpHistoryScrollView.Clear();
        foreach (var entry in _seasonManager.XpHistory)
        {
            var label = new Label(entry);
            label.AddToClassList("xp-history-entry");
            _xpHistoryScrollView.Add(label);
        }
    }

    private string PlayerTier()
    {
        var player = _seasonManager.PlayerTeam;
        return ApiClient.Instance.PlayerProgressionSaveData?.current_tier ?? "Rookie";

    }

    private void ShowScreen(string screenName)
    {
        _hubScreen.AddToClassList("hidden");
        _bracketScreen.AddToClassList("hidden");
        _xpScreen.AddToClassList("hidden");
        _simFeedbackScreen.AddToClassList("hidden");

        switch (screenName)
        {
            case "Screen_Hub": _hubScreen.RemoveFromClassList("hidden"); break;
            case "Screen_Bracket": _bracketScreen.RemoveFromClassList("hidden"); break;
            case "Screen_XP": _xpScreen.RemoveFromClassList("hidden"); break;
            case "Screen_SimFeedback": _simFeedbackScreen.RemoveFromClassList("hidden"); break;
        }
    }
}
