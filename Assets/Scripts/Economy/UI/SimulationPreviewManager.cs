// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class SimulationPreviewManager : MonoBehaviour
// {
//     // Start is called before the first frame update
//     void Start()
//     {
        
//     }

//     // Update is called once per frame
//     void Update()
//     {
        
//     }
// }
using UnityEngine;

public class SimulationPreviewManager : MonoBehaviour
{
    public GameObject simulationPreviewPanel;

    // Called when "Simulation" button is clicked
    public void OpenPreview()
    {
        simulationPreviewPanel.SetActive(true);
    }

    // Called when "Back to Details" is clicked
    public void ClosePreview()
    {
        simulationPreviewPanel.SetActive(false);
    }

    // Optional: Try different facility (add your logic)
    public void TryDifferentFacility()
    {
        Debug.Log("Try Different Facility clicked");
        // Add logic to change displayed comparison, or switch facility options
    }
}
