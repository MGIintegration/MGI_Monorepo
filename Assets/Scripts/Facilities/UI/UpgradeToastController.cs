using UnityEngine;
using TMPro;
using System.Collections;

public class UpgradeToastController : MonoBehaviour
{
    [SerializeField] private GameObject toastPanel;
    [SerializeField] private TextMeshProUGUI toastText;

    private Coroutine currentToast;

    public void ShowToast(string message)
    {
        if (currentToast != null)
        {
            StopCoroutine(currentToast);
        }

        toastText.text = message;
        toastPanel.SetActive(true);
        currentToast = StartCoroutine(HideToastAfterDelay(3f));
    }

    private IEnumerator HideToastAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        toastPanel.SetActive(false);
        currentToast = null;
    }
}
