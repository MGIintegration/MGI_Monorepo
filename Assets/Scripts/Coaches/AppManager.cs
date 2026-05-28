using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AppManager : MonoBehaviour
{
    [Header("Main Screen Components")]

    // Game objects for the screens
    public GameObject mainMenu;
    public GameObject coachHiringScreen;
    public GameObject coachDetailsScreen;
    public GameObject performanceScreen;

    [Header("Navigation Buttons for Screen 1")]

    // Buttons used in Screen 1: FMG Coach Main Menu
    public Button viewOffenseCoachButton;
    public Button fireOffenseCoachButton;
    public Button viewDefenseCoachButton;
    public Button fireDefenseCoachButton;
    public Button hiringCoachMarketButton;
    public Button performanceButton;
    public Button historyButton;

    [Header("Navigation Buttons for Screen 2")]
    public Button backToMainMenuButton;
    public Button refreshButton;

    public Button hireCoach1Button;
    public Button compareCoach1Button;
    public Button viewCoach1Button;
    public Button hireCoach2Button;
    public Button compareCoach2Button;
    public Button viewCoach2Button;

    [Header("Navigation Buttons for Screen 3")]
    // Buttons used in Screen 3: View Coach Details Page
    public Button compareButton;
    public Button backToMarketButton;
    public Button hireButton;

    [Header("Navigation Buttons for Screen 4")]
    public Button backButton;
    public Button detailedStatsButton;
    private void Start()
    {
        if (viewOffenseCoachButton != null)
            viewOffenseCoachButton.onClick.AddListener(() =>
            {
                var state = CoachesService.GetTeamState();
                if (string.IsNullOrEmpty(state?.offence_coach)) return;
                var coach = CoachesService.GetCoachById(state.offence_coach);
                if (coach == null)
                {
                    Debug.LogWarning($"[AppManager] Offence coach '{state.offence_coach}' not found in catalog.");
                    return;
                }
                var populator = coachDetailsScreen?.GetComponentInChildren<CoachProfilePopulator>(true);
                populator?.PopulateFromRecord(coach);
                ShowScreen(coachDetailsScreen);
            });
        if (fireOffenseCoachButton != null)
            fireOffenseCoachButton.onClick.AddListener(() => Debug.Log("Fire offence coach button was clicked"));
        if (viewDefenseCoachButton != null)
            viewDefenseCoachButton.onClick.AddListener(() =>
            {
                var state = CoachesService.GetTeamState();
                if (string.IsNullOrEmpty(state?.defence_coach)) return;
                var coach = CoachesService.GetCoachById(state.defence_coach);
                if (coach == null)
                {
                    Debug.LogWarning($"[AppManager] Defence coach '{state.defence_coach}' not found in catalog.");
                    return;
                }
                var populator = coachDetailsScreen?.GetComponentInChildren<CoachProfilePopulator>(true);
                populator?.PopulateFromRecord(coach);
                ShowScreen(coachDetailsScreen);
            });
        if (fireDefenseCoachButton != null)
            fireDefenseCoachButton.onClick.AddListener(() => Debug.Log("Fire defence coach button was clicked"));
        if (hiringCoachMarketButton != null)
            hiringCoachMarketButton.onClick.AddListener(() => ShowScreen(coachHiringScreen));
        if (performanceButton != null)
            performanceButton.onClick.AddListener(() => ShowScreen(performanceScreen));
        if (historyButton != null)
            historyButton.onClick.AddListener(() => Debug.Log("History button was clicked"));

        if (backToMainMenuButton != null)
            backToMainMenuButton.onClick.AddListener(() => ShowScreen(mainMenu));
        if (refreshButton != null)
            refreshButton.onClick.AddListener(() => coachHiringScreen?.GetComponent<CoachHiringMarket>()?.RefreshCoaches());

        if (hireCoach1Button != null)
            hireCoach1Button.onClick.AddListener(() => Debug.Log("Hire Coach 1 button was clicked"));
        if (compareCoach1Button != null)
            compareCoach1Button.onClick.AddListener(() => Debug.Log("Compare Coach 1 button was clicked"));
        if (viewCoach1Button != null)
            viewCoach1Button.onClick.AddListener(() =>
            {
                var market = coachHiringScreen?.GetComponent<CoachHiringMarket>();
                var populator = coachDetailsScreen?.GetComponentInChildren<CoachProfilePopulator>(true);
                if (market != null && populator != null)
                    populator.PopulateFromRecord(market.GetCoach(1));
                ShowScreen(coachDetailsScreen);
            });

        if (hireCoach2Button != null)
            hireCoach2Button.onClick.AddListener(() => Debug.Log("Hire Coach 2 button was clicked"));
        if (compareCoach2Button != null)
            compareCoach2Button.onClick.AddListener(() => Debug.Log("Compare Coach 2 button was clicked"));
        if (viewCoach2Button != null)
            viewCoach2Button.onClick.AddListener(() =>
            {
                var market = coachHiringScreen?.GetComponent<CoachHiringMarket>();
                var populator = coachDetailsScreen?.GetComponentInChildren<CoachProfilePopulator>(true);
                if (market != null && populator != null)
                    populator.PopulateFromRecord(market.GetCoach(2));
                ShowScreen(coachDetailsScreen);
            });

        if (compareButton != null)
            compareButton.onClick.AddListener(() => Debug.Log("Compare button was clicked"));
        if (backToMarketButton != null)
            backToMarketButton.onClick.AddListener(() => ShowScreen(coachHiringScreen));
        if (hireButton != null)
            hireButton.onClick.AddListener(() => Debug.Log("Hire button was clicked"));

        if (backButton != null)
            backButton.onClick.AddListener(() => ShowScreen(mainMenu));
        if (detailedStatsButton != null)
            detailedStatsButton.onClick.AddListener(() => Debug.Log("Detailed Stats button was clicked"));

        ShowScreen(mainMenu);
    }

    private void ShowScreen(GameObject targetScreen)
    {
        // Deactivate all
        mainMenu.SetActive(false);
        coachHiringScreen.SetActive(false);
        coachDetailsScreen.SetActive(false);
        performanceScreen.SetActive(false);

        // Activate the one you want
        if (targetScreen != null)
            targetScreen.SetActive(true);
    }
}

//     // Start is called before the first frame update
//     void Start()
//     {
//         // Assigning button clicks to different menus
//         hireCoachButton.onClick.AddListener(ShowCoachHiringScreen);
//         viewOffenseCoachButton.onClick.AddListener(ShowOffenseCoachDetails);
//         viewDefenseCoachButton.onClick.AddListener(ShowDefenseCoachDetails);
//         hiringCoachMarketButton.onClick.AddListener(ShowHiringMarket);
//         performanceButton.onClick.AddListener(ShowPerformanceScreen);
//         historyButton.onClick.AddListener(ShowHistoryScreen);

//         ShowMainMenu();
//     }

//     public void ShowMainMenu()
//     {
//         mainMenu.SetActive(true);
//         coachHiringScreen.SetActive(false);
//     }

//     public void ShowCoachHiringScreen()
//     {
//         mainMenu.SetActive(false);
//         coachHiringScreen.SetActive(true);
//     }

//     public void ShowOffenseCoachDetails()
//     {
//         mainMenu.SetActive(false);
//         CoachProfilePopulator coachProfilePopulator = FindObjectOfType<CoachProfilePopulator>();

//         // Load coach
//         if (coachProfilePopulator != null)
//         {
//             // coachProfilePopulator.coach.LoadCoachProfile("offense");
//         }
//         coachDetailsScreen.SetActive(true);
//     }

//     public void ShowDefenseCoachDetails()
//     {
//         mainMenu.SetActive(false);
//         CoachProfilePopulator coachProfilePopulator = FindObjectOfType<CoachProfilePopulator>();

//         // Load coach
//         if (coachProfilePopulator != null)
//         {
//             // coachProfilePopulator.coach.LoadCoachProfile("defense");
//         }
//         coachDetailsScreen.SetActive(true);
//     }

//     public void ShowHiringMarket()
//     {
//         mainMenu.SetActive(false);
//         coachHiringScreen.SetActive(true);
//     }

//     public void ShowPerformanceScreen()
//     {
//         mainMenu.SetActive(false);
//         coachDetailsScreen.SetActive(true);

//         // Populate the coach details UI
//     }

//     public void ShowHistoryScreen()
//     {
//         mainMenu.SetActive(false);
//         coachDetailsScreen.SetActive(true);

//         // Populate the coach details UI
//     }
// }