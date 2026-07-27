using UnityEngine;

public class FacilityAutoLoader : MonoBehaviour
{
    public FacilityDetailsHandler details;
    public FacilityType type;

    void Start()
    {
        if (details == null) details = GetComponent<FacilityDetailsHandler>();

        switch (type)
        {
            case FacilityType.WeightRoom: details.ShowWeightRoom(); break;
            case FacilityType.Rehab: details.ShowRehab(); break;
            case FacilityType.Film: details.ShowFilm(); break;
        }
    }
}
