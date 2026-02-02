using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays real-time emotional state (Frustration & Satisfaction)
/// with smooth transitions and color intensity based on emotion levels (0–100).
/// Features bars that grow from center: Frustration (left), Satisfaction (right).
/// </summary>
public class EmotionDisplayUI : MonoBehaviour
{
    [Header("Text References")]
    public TextMeshProUGUI frustrationText;
    public TextMeshProUGUI satisfactionText;

    [Header("Bar References")]
    [Tooltip("Bar that grows from center to left (Frustration)")]
    public RectTransform frustrationBar;
    [Tooltip("Bar that grows from center to right (Satisfaction)")]
    public RectTransform satisfactionBar;

    [Header("Bar Settings")]
    [Tooltip("Maximum width for each bar (half of total bar area)")]
    public float maxBarWidth = 200f;
    [Tooltip("Bar colors")]
    public Color frustrationColor = Color.red;
    public Color satisfactionColor = Color.green;

    [Header("Border")]
    [Tooltip("Optional image used as a frame around the emotion bars")]
    public Image borderImage;
    [Tooltip("Border color when emotions are low/neutral")]
    public Color borderLowColor = new Color(0f, 0f, 0f, 0.25f);
    [Tooltip("Border color when emotions are high")]
    public Color borderHighColor = Color.black;

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

        // Initialize bars
        InitializeBars();
    }

    void InitializeBars()
    {
        // Set up Frustration bar (grows from center to left)
        if (frustrationBar != null)
        {
            // Anchor to center, pivot at right edge (so bar grows left from center)
            frustrationBar.anchorMin = new Vector2(0.5f, 0.5f);
            frustrationBar.anchorMax = new Vector2(0.5f, 0.5f);
            frustrationBar.pivot = new Vector2(1f, 0.5f); // Right edge pivot
            
            // Set initial size
            frustrationBar.sizeDelta = new Vector2(0f, frustrationBar.sizeDelta.y);
            
            // Set color if Image component exists
            var frustrationImage = frustrationBar.GetComponent<Image>();
            if (frustrationImage != null)
            {
                frustrationImage.color = frustrationColor;
            }
        }

        // Set up Satisfaction bar (grows from center to right)
        if (satisfactionBar != null)
        {
            // Anchor to center, pivot at left edge (so bar grows right from center)
            satisfactionBar.anchorMin = new Vector2(0.5f, 0.5f);
            satisfactionBar.anchorMax = new Vector2(0.5f, 0.5f);
            satisfactionBar.pivot = new Vector2(0f, 0.5f); // Left edge pivot
            
            // Set initial size
            satisfactionBar.sizeDelta = new Vector2(0f, satisfactionBar.sizeDelta.y);
            
            // Set color if Image component exists
            var satisfactionImage = satisfactionBar.GetComponent<Image>();
            if (satisfactionImage != null)
            {
                satisfactionImage.color = satisfactionColor;
            }
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

        // Update bars
        UpdateBars();
    }

    void UpdateBars()
    {
        // Shared normalized values (0–1) for both visual and border use
        float normalizedFr = Mathf.Clamp01(_displayFr / 100f);
        float normalizedSa = Mathf.Clamp01(_displaySa / 100f);

        // Update Frustration bar (grows from center to left)
        if (frustrationBar != null)
        {
            float targetWidth = normalizedFr * maxBarWidth;
            float currentWidth = frustrationBar.sizeDelta.x;
            float newWidth = Mathf.Lerp(currentWidth, targetWidth, Time.deltaTime * lerpSpeed);
            
            frustrationBar.sizeDelta = new Vector2(newWidth, frustrationBar.sizeDelta.y);
            
            // Update color intensity
            var frustrationImage = frustrationBar.GetComponent<Image>();
            if (frustrationImage != null)
            {
                float intensity = normalizedFr;
                frustrationImage.color = Color.Lerp(
                    new Color(frustrationColor.r, frustrationColor.g, frustrationColor.b, 0.3f),
                    frustrationColor,
                    intensity
                );
            }
        }

        // Update Satisfaction bar (grows from center to right)
        if (satisfactionBar != null)
        {
            float targetWidth = normalizedSa * maxBarWidth;
            float currentWidth = satisfactionBar.sizeDelta.x;
            float newWidth = Mathf.Lerp(currentWidth, targetWidth, Time.deltaTime * lerpSpeed);
            
            satisfactionBar.sizeDelta = new Vector2(newWidth, satisfactionBar.sizeDelta.y);
            
            // Update color intensity
            var satisfactionImage = satisfactionBar.GetComponent<Image>();
            if (satisfactionImage != null)
            {
                float intensity = normalizedSa;
                satisfactionImage.color = Color.Lerp(
                    new Color(satisfactionColor.r, satisfactionColor.g, satisfactionColor.b, 0.3f),
                    satisfactionColor,
                    intensity
                );
            }
        }

        // Border reacts to overall emotion intensity so it's easier to notice
        if (borderImage != null)
        {
            float combined = Mathf.Max(normalizedFr, normalizedSa);
            borderImage.color = Color.Lerp(borderLowColor, borderHighColor, combined);
        }
    }
}
