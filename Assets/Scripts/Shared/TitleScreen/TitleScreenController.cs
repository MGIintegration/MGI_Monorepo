using UnityEngine;
using UnityEngine.SceneManagement;

namespace MGI.Shared.TitleScreen
{
    /// <summary>
    /// Main MGI title / hub screen. Assign button OnClick in the Inspector to these public methods.
    /// </summary>
    public class TitleScreenController : MonoBehaviour
    {
        public const string SceneName = "TitleScreen";

        [Header("Scene Names")]
        [SerializeField] string ccasSceneName = "CCAS";
        [SerializeField] string facilitiesSceneName = "FacilitiesOverview";
        [SerializeField] string coachesTitleSceneName = CoachesTitleScreenController.SceneName;

        public void LoadCcas() => LoadScene(ccasSceneName);
        public void LoadFacilities() => LoadScene(facilitiesSceneName);
        public void LoadCoachesTitle() => LoadScene(coachesTitleSceneName);

        void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[TitleScreen] Missing scene name.");
                return;
            }

            Debug.Log($"[TitleScreen] Loading scene '{sceneName}'.");
            SceneManager.LoadScene(sceneName);
        }
    }
}
