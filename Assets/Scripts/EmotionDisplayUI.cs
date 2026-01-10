using UnityEngine;
using TMPro;

/// <summary>
/// Displays real-time emotional state (Frustration & Satisfaction)
/// with smooth transitions and color intensity based on emotion levels (0–100).
/// </summary>
public class EmotionDisplayUI : MonoBehaviour
{
    [Header("Text References")]
    public TextMeshProUGUI frustrationText;
    public TextMeshProUGUI satisfactionText;

    [Header("Animation Settings")]
    [Range(0.1f, 5f)] public float lerpSpeed = 2.5f;

    // Internal smoothed values
    private float _displayFr;
    private float _displaySa;

    void Start()
    {
        if (EmotionalStateManager.Instance != null)
        {
            var (fr, sa) = EmotionalStateManager.Instance.Snapshot();
            _displayFr = fr;
            _displaySa = sa;
        }
        else
        {
            _displayFr = _displaySa = 0f;
        }
    }

    void Update()
    {
        var esm = EmotionalStateManager.Instance;
        if (esm == null) return;

        var (fr, sa) = esm.Snapshot();

        // Smooth interpolation for UI readability
        _displayFr = Mathf.Lerp(_displayFr, fr, Time.deltaTime * lerpSpeed);
        _displaySa = Mathf.Lerp(_displaySa, sa, Time.deltaTime * lerpSpeed);

        // Update texts (with null safety)
        if (frustrationText != null)
        {
            frustrationText.text = $"Frustration: {_displayFr:F1}";
            float intensity = Mathf.InverseLerp(0f, 100f, _displayFr);
            frustrationText.color = Color.Lerp(Color.white, Color.red, intensity);
            frustrationText.alpha = Mathf.Clamp01(intensity + 0.3f);
        }

        if (satisfactionText != null)
        {
            satisfactionText.text = $"Satisfaction: {_displaySa:F1}";
            float intensity = Mathf.InverseLerp(0f, 100f, _displaySa);
            satisfactionText.color = Color.Lerp(Color.white, Color.green, intensity);
            satisfactionText.alpha = Mathf.Clamp01(intensity + 0.3f);
        }
    }
}
