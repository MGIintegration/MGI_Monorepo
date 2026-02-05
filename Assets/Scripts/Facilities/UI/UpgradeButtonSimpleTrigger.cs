using UnityEngine;

public class WeeklyReport_UIManager : MonoBehaviour
{
    public GameObject facilitiesOverviewPanel;
    public GameObject weeklyReportPanel;

    public void ShowWeeklyReport()
    {
        facilitiesOverviewPanel.SetActive(false);
        weeklyReportPanel.SetActive(true);
    }

    public void ShowFacilitiesOverview()
    {
        weeklyReportPanel.SetActive(false);
        facilitiesOverviewPanel.SetActive(true);
    }
}
