using UnityEngine;

public class FacilityUIManager : MonoBehaviour
{
    public GameObject rehabPanel;
    public GameObject filmPanel;
    public GameObject weightPanel;
    public GameObject borderHighlight;


    public void ShowRehabDetails()
    {
        CloseAll();
        rehabPanel.SetActive(true);
    }

    public void ShowFilmDetails()
    {
        CloseAll();
        filmPanel.SetActive(true);
    }

    public void ShowWeightDetails()
    {
        CloseAll();
        weightPanel.SetActive(true);
    }

    public void CloseAll()
    {
        rehabPanel.SetActive(false);
        filmPanel.SetActive(false);
        weightPanel.SetActive(false);
    }
    public void ShowBorder()
    {
        if (borderHighlight != null)
            borderHighlight.SetActive(true);
    }

    public void HideBorder()
    {
        if (borderHighlight != null)
            borderHighlight.SetActive(false);
    }
}
