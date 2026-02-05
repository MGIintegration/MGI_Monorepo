using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeeklyReportUI : MonoBehaviour
{
    public GameObject facilitiesOverviewPanel; // assign FacilitiesOverviewPanel
    public GameObject weeklyReportPanel;       // assign WeeklyReportPanel

    void Start() { ShowFacilitiesOverview(); }

    public void ShowWeeklyReport() {
        facilitiesOverviewPanel.SetActive(false);
        weeklyReportPanel.SetActive(true);
    }

    public void ShowFacilitiesOverview() {
        weeklyReportPanel.SetActive(false);
        facilitiesOverviewPanel.SetActive(true);
    }
}
