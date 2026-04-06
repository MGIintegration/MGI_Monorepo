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
        // Assigning button clicks to corresponding screens

        // Screen 1 Buttons
        viewOffenseCoachButton.onClick.AddListener(() => ShowScreen(coachDetailsScreen));
        fireOffenseCoachButton.onClick.AddListener(() => Debug.Log("Fire offence coach button was clicked"));
        viewDefenseCoachButton.onClick.AddListener(() => ShowScreen(coachDetailsScreen));
        fireDefenseCoachButton.onClick.AddListener(() => Debug.Log("Fire deffence coach button was clicked"));
        hiringCoachMarketButton.onClick.AddListener(() => ShowScreen(coachHiringScreen));
        performanceButton.onClick.AddListener(() => ShowScreen(performanceScreen));
        historyButton.onClick.AddListener(() => Debug.Log("History button was clicked"));

        // Screen 2 Buttons
        backToMainMenuButton.onClick.AddListener(() => ShowScreen(mainMenu));
        refreshButton.onClick.AddListener(() => Debug.Log("Refresh button was clicked"));

        hireCoach1Button.onClick.AddListener(() => Debug.Log("Hire Coach 1 button was clicked"));
        compareCoach1Button.onClick.AddListener(() => Debug.Log("Compare Coach 1 button was clicked"));
        viewCoach1Button.onClick.AddListener(() => ShowScreen(coachDetailsScreen));

        hireCoach2Button.onClick.AddListener(() => Debug.Log("Hire Coach 2 button was clicked"));
        compareCoach2Button.onClick.AddListener(() => Debug.Log("Compare Coach 2 button was clicked"));
        viewCoach2Button.onClick.AddListener(() => ShowScreen(coachDetailsScreen));

        // Screen 3 Buttons
        compareButton.onClick.AddListener(() => Debug.Log("Compare button was clicked"));
        backToMarketButton.onClick.AddListener(() => ShowScreen(coachHiringScreen));
        hireButton.onClick.AddListener(() => Debug.Log("Hire button was clicked"));

        // Screen 4 Buttons
        backButton.onClick.AddListener(() => ShowScreen(mainMenu));
        detailedStatsButton.onClick.AddListener(() => Debug.Log("Detailed Stats button was clicked"));

        ShowScreen(mainMenu); // Default starting screen
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