using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ToastController : MonoBehaviour
{
    public GameObject toastPanelPrefab;

    public void ShowToast(string message)
    {
        if (toastPanelPrefab == null)
        {
            Debug.LogError("ToastPanel prefab not assigned!");
            return;
        }

        // Instantiate the toast panel
        GameObject toastInstance = Instantiate(toastPanelPrefab, transform);

        // Find its TMP text component and set the message
        TMPro.TextMeshProUGUI textComponent = toastInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textComponent != null)
            textComponent.text = message;

        // Enable the toast animation
        toastInstance.SetActive(true);

        // Destroy after 2.5 seconds
        Destroy(toastInstance, 2.5f);
    }
}
