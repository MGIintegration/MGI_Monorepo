using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays frustration and satisfaction (0–100) with progress bars and optional borders.
/// Hierarchy: Border → Background → Progress bar (Image). Bars grow from inner edge of border.
/// </summary>
public class EmotionDisplayUI : MonoBehaviour
{
    [Header("Text")]
    public TextMeshProUGUI frustrationText;
    public TextMeshProUGUI satisfactionText;

    [Header("Frustration")]
    public Image frustrationBorderImage;
    public RectTransform frustrationBar;

    [Header("Satisfaction")]
    public Image satisfactionBorderImage;
    public RectTransform satisfactionBar;

    [Header("Colors")]
    public Color frustrationColor = Color.red;
    public Color satisfactionColor = Color.green;

    [Header("Animation")]
    [Range(0.1f, 5f)] public float lerpSpeed = 2.5f;

    const float FallbackBarWidth = 200f;

    float _displayFr;
    float _displaySa;
    Image _frustrationImage;
    Image _satisfactionImage;

    void Start()
    {
        if (EmotionalStateManager.Instance != null)
        {
            var (fr, sa) = EmotionalStateManager.Instance.Snapshot();
            _displayFr = fr;
            _displaySa = sa;
        }

        if (frustrationBar != null)
        {
            var bg = frustrationBar.parent != null ? frustrationBar.parent.GetComponent<Image>() : null;
            if (bg != null) bg.color = Color.white;
            frustrationBar.anchorMin = frustrationBar.anchorMax = new Vector2(1f, 0.5f);
            frustrationBar.pivot = new Vector2(1f, 0.5f);
            frustrationBar.anchoredPosition = Vector2.zero;
            frustrationBar.sizeDelta = new Vector2(0f, frustrationBar.sizeDelta.y);
            _frustrationImage = frustrationBar.GetComponent<Image>();
            if (_frustrationImage != null) _frustrationImage.color = frustrationColor;
        }

        if (satisfactionBar != null)
        {
            var bg = satisfactionBar.parent != null ? satisfactionBar.parent.GetComponent<Image>() : null;
            if (bg != null) bg.color = Color.white;
            satisfactionBar.anchorMin = satisfactionBar.anchorMax = new Vector2(0f, 0.5f);
            satisfactionBar.pivot = new Vector2(0f, 0.5f);
            satisfactionBar.anchoredPosition = Vector2.zero;
            satisfactionBar.sizeDelta = new Vector2(0f, satisfactionBar.sizeDelta.y);
            _satisfactionImage = satisfactionBar.GetComponent<Image>();
            if (_satisfactionImage != null) _satisfactionImage.color = satisfactionColor;
        }
    }

    void Update()
    {
        if (EmotionalStateManager.Instance == null) return;

        var (fr, sa) = EmotionalStateManager.Instance.Snapshot();
        _displayFr = Mathf.Lerp(_displayFr, fr, Time.deltaTime * lerpSpeed);
        _displaySa = Mathf.Lerp(_displaySa, sa, Time.deltaTime * lerpSpeed);

        float nFr = Mathf.Clamp01(_displayFr / 100f);
        float nSa = Mathf.Clamp01(_displaySa / 100f);

        if (frustrationText != null)
        {
            frustrationText.text = $"Frustration: {_displayFr:F1}";
            float i = Mathf.InverseLerp(0f, 100f, _displayFr);
            frustrationText.color = Color.Lerp(Color.white, Color.red, i);
            frustrationText.alpha = Mathf.Clamp01(i + 0.3f);
        }

        if (satisfactionText != null)
        {
            satisfactionText.text = $"Satisfaction: {_displaySa:F1}";
            float i = Mathf.InverseLerp(0f, 100f, _displaySa);
            satisfactionText.color = Color.Lerp(Color.white, Color.green, i);
            satisfactionText.alpha = Mathf.Clamp01(i + 0.3f);
        }

        float maxFr = GetBarMaxWidth(frustrationBar);
        float maxSa = GetBarMaxWidth(satisfactionBar);

        if (frustrationBar != null)
        {
            float w = Mathf.Lerp(frustrationBar.sizeDelta.x, nFr * maxFr, Time.deltaTime * lerpSpeed);
            frustrationBar.sizeDelta = new Vector2(w, frustrationBar.sizeDelta.y);
            if (_frustrationImage != null)
                _frustrationImage.color = Color.Lerp(new Color(frustrationColor.r, frustrationColor.g, frustrationColor.b, 0.3f), frustrationColor, nFr);
        }

        if (satisfactionBar != null)
        {
            float w = Mathf.Lerp(satisfactionBar.sizeDelta.x, nSa * maxSa, Time.deltaTime * lerpSpeed);
            satisfactionBar.sizeDelta = new Vector2(w, satisfactionBar.sizeDelta.y);
            if (_satisfactionImage != null)
                _satisfactionImage.color = Color.Lerp(new Color(satisfactionColor.r, satisfactionColor.g, satisfactionColor.b, 0.3f), satisfactionColor, nSa);
        }

        if (frustrationBorderImage != null)
            frustrationBorderImage.color = Color.Lerp(new Color(0f, 0f, 0f, 0.25f), Color.black, nFr);
        if (satisfactionBorderImage != null)
            satisfactionBorderImage.color = Color.Lerp(new Color(0f, 0f, 0f, 0.25f), Color.black, nSa);
    }

    static float GetBarMaxWidth(RectTransform bar)
    {
        if (bar == null) return FallbackBarWidth;
        var parent = bar.parent as RectTransform;
        return parent != null && parent.rect.width > 0 ? parent.rect.width : FallbackBarWidth;
    }
}
