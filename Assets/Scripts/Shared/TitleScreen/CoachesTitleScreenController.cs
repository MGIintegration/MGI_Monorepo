using UnityEngine;
using UnityEngine.SceneManagement;

namespace MGI.Shared.TitleScreen
{
    /// <summary>
    /// Coaches hub under the main title screen. Assign button OnClick in the Inspector to these public methods.
    /// </summary>
    public class CoachesTitleScreenController : MonoBehaviour
    {
        public const string SceneName = "CoachesTitleScreen";

        [Header("Scene Names")]
        [SerializeField] string mainTitleSceneName = TitleScreenController.SceneName;
        [SerializeField] string fmgCoachSceneName = "FMGCOACH";
        [SerializeField] string coachProfileSceneName = "CoachProfile";
        [SerializeField] string performanceAnalyticsSceneName = "PerformanceAnalytics";

        public void LoadFmgCoach() => LoadScene(fmgCoachSceneName);
        public void LoadCoachProfile() => LoadScene(coachProfileSceneName);
        public void LoadPerformanceAnalytics() => LoadScene(performanceAnalyticsSceneName);
        public void LoadMainTitle() => LoadScene(mainTitleSceneName);

        void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[CoachesTitleScreen] Missing scene name.");
                return;
            }

            Debug.Log($"[CoachesTitleScreen] Loading scene '{sceneName}'.");
            SceneManager.LoadScene(sceneName);
        }
    }
}
