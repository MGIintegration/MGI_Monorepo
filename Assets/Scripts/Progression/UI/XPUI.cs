using UnityEngine;
using TMPro;


    public class XPUI : MonoBehaviour
    {
        public TextMeshProUGUI currentXPText;
        public Transform xpListParent;
        public GameObject xpEntryPrefab;

        void OnEnable()
        {
            if (SeasonManager.Instance != null)
                SeasonManager.Instance.OnSeasonDataUpdated += RefreshXPHistory;
            else
                StartCoroutine(WaitAndSubscribe());
        }

        void OnDisable()
        {
            if (SeasonManager.Instance != null)
                SeasonManager.Instance.OnSeasonDataUpdated -= RefreshXPHistory;
        }

        private System.Collections.IEnumerator WaitAndSubscribe()
        {
            while (SeasonManager.Instance == null)
                yield return null;
            SeasonManager.Instance.OnSeasonDataUpdated += RefreshXPHistory;
            RefreshXPHistory();
        }

        public void RefreshXPHistory()
        {
            var sm = SeasonManager.Instance;
            if (sm == null || xpListParent == null || xpEntryPrefab == null) return;

            //  Update Total XP Header
            if (currentXPText != null)
                currentXPText.text = $"Total XP: {sm.PlayerXP}";

            var prog = ApiClient.Instance?.PlayerProgressionSaveData;
            if (prog == null || prog.xp_history == null) return;

            for (int i = xpListParent.childCount - 1; i >= 0; i--)
            {
                Transform child = xpListParent.GetChild(i);
                child.SetParent(null); // Important: Detach from layout first
                Destroy(child.gameObject);
            }

            // Create new entries (Iterate backwards to show NEWEST first)
            for (int i = prog.xp_history.Count - 1; i >= 0; i--)
            {
                var entry = prog.xp_history[i];
                
                var go = Instantiate(xpEntryPrefab, xpListParent);
                var texts = go.GetComponentsInChildren<TextMeshProUGUI>();

                if (texts.Length >= 3)
                {
                    texts[0].text = $"Week {i + 1}";

                    texts[1].text = FormatResult(entry.source); 

                    string color = entry.xp_gained > 0 ? "#74C0FC" : "#FF6B6B";
                    texts[2].text = $"<color={color}>+{entry.xp_gained} XP</color>";
                    Debug.Log("XPUI three text fields found");
                }
                else if (texts.Length == 1)
                {
                    texts[0].text = $"{FormatResult(entry.source)}: +{entry.xp_gained}";
                    Debug.Log("XPUI single text field found");
                }
            }
        }

        // Helper function to clean up the source string
        private string FormatResult(string sourceRaw)
        {
            if (string.IsNullOrEmpty(sourceRaw)) return "Unknown";

            string lower = sourceRaw.ToLower();
            
            if (lower.Contains("loss")) return "Match Loss";
            if (lower.Contains("win")) return "Match Win";
            
            // Fallback: just Capitalize the raw string if it's something else
            return char.ToUpper(sourceRaw[0]) + sourceRaw.Substring(1);
        }
    }
