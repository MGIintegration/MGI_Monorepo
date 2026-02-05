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
            case FacilityType.WeightRoom: details.ShowWeightRoomFromServer(); break;
            case FacilityType.Rehab: details.ShowRehabFromServer(); break;
            case FacilityType.Film: details.ShowFilmFromServer(); break;
        }
    }
}
