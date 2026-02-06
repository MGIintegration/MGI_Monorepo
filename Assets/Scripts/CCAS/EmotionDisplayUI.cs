using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Phase 2 UI: displays negative and positive family levels (0–100) with progress bars.
/// Hierarchy: Border → Background → Progress bar (Image). Bars grow from inner edge of border.
/// </summary>
public class EmotionDisplayUI : MonoBehaviour
{
    [Header("Text")]
    public TextMeshProUGUI negativeText;
    public TextMeshProUGUI positiveText;

    [Header("Negative")]
    public Image negativeBorderImage;
    public RectTransform negativeBar;

    [Header("Positive")]
    public Image positiveBorderImage;
    public RectTransform positiveBar;

    [Header("Colors")]
    public Color negativeColor = Color.red;
    public Color positiveColor = Color.green;

    [Header("Animation")]
    [Range(0.1f, 5f)] public float lerpSpeed = 2.5f;

    [Header("Popups (Phase 2)")]
    [Tooltip("Prefab containing a TextMeshProUGUI component.")]
    public GameObject popupTextPrefab;
    [Tooltip("Parent/anchor under the Negative bar where popups spawn.")]
    public RectTransform negativePopupAnchor;
    [Tooltip("Parent/anchor under the Positive bar where popups spawn.")]
    public RectTransform positivePopupAnchor;
    [Range(0.5f, 4f)] public float popupLifetimeSeconds = 1.5f;
    public float popupRisePixels = 24f;
    [Tooltip("Vertical spacing between multiple popups in the same family.")]
    public float popupStackSpacing = 18f;

    const float FallbackBarWidth = 200f;

    float _displayNeg;
    float _displayPos;
    Image _negImage;
    Image _posImage;

    // Track last seen breakdown so we only pop once per pull.
    string _lastEventKey = "";

    void Start()
    {
        if (EmotionalStateManager.Instance != null)
        {
            var (neg, pos) = EmotionalStateManager.Instance.Snapshot();
            _displayNeg = neg;
            _displayPos = pos;
        }

        if (negativeBar != null)
        {
            var bg = negativeBar.parent != null ? negativeBar.parent.GetComponent<Image>() : null;
            if (bg != null) bg.color = Color.white;
            negativeBar.anchorMin = negativeBar.anchorMax = new Vector2(1f, 0.5f);
            negativeBar.pivot = new Vector2(1f, 0.5f);
            negativeBar.anchoredPosition = Vector2.zero;
            negativeBar.sizeDelta = new Vector2(0f, negativeBar.sizeDelta.y);
            _negImage = negativeBar.GetComponent<Image>();
            if (_negImage != null)
                _negImage.color = negativeColor;
        }

        if (positiveBar != null)
        {
            var bg = positiveBar.parent != null ? positiveBar.parent.GetComponent<Image>() : null;
            if (bg != null) bg.color = Color.white;
            positiveBar.anchorMin = positiveBar.anchorMax = new Vector2(0f, 0.5f);
            positiveBar.pivot = new Vector2(0f, 0.5f);
            positiveBar.anchoredPosition = Vector2.zero;
            positiveBar.sizeDelta = new Vector2(0f, positiveBar.sizeDelta.y);
            _posImage = positiveBar.GetComponent<Image>();
            if (_posImage != null)
                _posImage.color = positiveColor;
        }
    }

    void Update()
    {
        if (EmotionalStateManager.Instance == null) return;

        var (neg, pos) = EmotionalStateManager.Instance.Snapshot();
        _displayNeg = Mathf.Lerp(_displayNeg, neg, Time.deltaTime * lerpSpeed);
        _displayPos = Mathf.Lerp(_displayPos, pos, Time.deltaTime * lerpSpeed);

        float nNeg = Mathf.Clamp01(_displayNeg / 100f);
        float nPos = Mathf.Clamp01(_displayPos / 100f);

        if (negativeText != null)
        {
            negativeText.text = $"Negative: {_displayNeg:F1}";
            float i = Mathf.InverseLerp(0f, 100f, _displayNeg);
            negativeText.color = Color.Lerp(Color.white, Color.red, i);
            negativeText.alpha = Mathf.Clamp01(i + 0.3f);
        }

        if (positiveText != null)
        {
            positiveText.text = $"Positive: {_displayPos:F1}";
            float i = Mathf.InverseLerp(0f, 100f, _displayPos);
            positiveText.color = Color.Lerp(Color.white, Color.green, i);
            positiveText.alpha = Mathf.Clamp01(i + 0.3f);
        }

        float maxNeg = GetBarMaxWidth(negativeBar);
        float maxPos = GetBarMaxWidth(positiveBar);

        if (negativeBar != null)
        {
            float w = Mathf.Lerp(negativeBar.sizeDelta.x, nNeg * maxNeg, Time.deltaTime * lerpSpeed);
            negativeBar.sizeDelta = new Vector2(w, negativeBar.sizeDelta.y);
            if (_negImage != null)
                _negImage.color = Color.Lerp(new Color(negativeColor.r, negativeColor.g, negativeColor.b, 0.3f), negativeColor, nNeg);
        }

        if (positiveBar != null)
        {
            float w = Mathf.Lerp(positiveBar.sizeDelta.x, nPos * maxPos, Time.deltaTime * lerpSpeed);
            positiveBar.sizeDelta = new Vector2(w, positiveBar.sizeDelta.y);
            if (_posImage != null)
                _posImage.color = Color.Lerp(new Color(positiveColor.r, positiveColor.g, positiveColor.b, 0.3f), positiveColor, nPos);
        }

        if (negativeBorderImage != null)
            negativeBorderImage.color = Color.Lerp(new Color(0f, 0f, 0f, 0.25f), Color.black, nNeg);
        if (positiveBorderImage != null)
            positiveBorderImage.color = Color.Lerp(new Color(0f, 0f, 0f, 0.25f), Color.black, nPos);

        // Popups (emotion-y labels), driven from the exact breakdown values.
        TrySpawnPopups();
    }

    void OnDisable()
    {
        // When this UI is hidden (e.g., switching panels), clear any active popup clones
        // so they don't hang around at their last position when we come back.
        _lastEventKey = "";
        ClearAllPopups();
    }

    void TrySpawnPopups()
    {
        var esm = EmotionalStateManager.Instance;
        if (esm == null) return;

        var b = esm.GetLastBreakdown();
        // Basic key: pull identity from data we already have. Good enough to avoid spamming popups each frame.
        string key = $"{b.pack_type}|{b.raw_score}|{b.cost_coins}|{b.quality01:F3}|{b.applied_positive_total:F3}|{b.applied_negative_total:F3}";
        if (key == _lastEventKey) return;
        _lastEventKey = key;

        // Positive popups
        if (positivePopupAnchor != null)
        {
            if (b.pos_d_rarity_pack > 0.0001f) SpawnPopup(positivePopupAnchor, "Thrill", positiveColor);
            if (b.pos_d_streak > 0.0001f)      SpawnPopup(positivePopupAnchor, "Relief", positiveColor);
            if (b.pos_d_economy > 0.0001f)     SpawnPopup(positivePopupAnchor, "Worth",  positiveColor);
        }

        // Negative popups
        if (negativePopupAnchor != null)
        {
            if (b.neg_d_rarity_pack > 0.0001f) SpawnPopup(negativePopupAnchor, "Disappointment", negativeColor);
            if (b.neg_d_streak > 0.0001f)      SpawnPopup(negativePopupAnchor, "Letdown",        negativeColor);
            if (b.neg_d_economy > 0.0001f)     SpawnPopup(negativePopupAnchor, "Regret",         negativeColor);
        }
    }

    void SpawnPopup(RectTransform anchor, string text, Color color)
    {
        if (popupTextPrefab == null || anchor == null) return;

        var go = Instantiate(popupTextPrefab, anchor);
        go.SetActive(true);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = text;
            tmp.color = color;
        }

        var rt = go.transform as RectTransform;
        if (rt != null)
        {
            // Stack popups vertically so multiple emotions in the same family don't overlap.
            int index = anchor.childCount - 1; // this instance is the last child
            float y = index * popupStackSpacing;
            rt.anchoredPosition = new Vector2(0f, y);
        }

        StartCoroutine(FadeAndRise(go, popupLifetimeSeconds, popupRisePixels));
    }

    System.Collections.IEnumerator FadeAndRise(GameObject go, float lifetime, float rise)
    {
        if (go == null) yield break;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        var rt = go.transform as RectTransform;

        float t = 0f;
        float startAlpha = tmp != null ? tmp.color.a : 1f;
        Vector2 startPos = rt != null ? rt.anchoredPosition : Vector2.zero;

        while (t < lifetime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / lifetime);

            if (rt != null)
                rt.anchoredPosition = startPos + new Vector2(0f, rise * u);
            if (tmp != null)
            {
                var c = tmp.color;
                c.a = Mathf.Lerp(startAlpha, 0f, u);
                tmp.color = c;
            }

            yield return null;
        }

        if (go != null)
            Destroy(go);
    }

    void ClearAllPopups()
    {
        ClearAnchorChildren(negativePopupAnchor);
        ClearAnchorChildren(positivePopupAnchor);
    }

    static void ClearAnchorChildren(RectTransform anchor)
    {
        if (anchor == null) return;
        for (int i = anchor.childCount - 1; i >= 0; i--)
        {
            var child = anchor.GetChild(i);
            if (child != null)
                Object.Destroy(child.gameObject);
        }
    }

    static float GetBarMaxWidth(RectTransform bar)
    {
        if (bar == null) return FallbackBarWidth;
        var parent = bar.parent as RectTransform;
        return parent != null && parent.rect.width > 0 ? parent.rect.width : FallbackBarWidth;
    }
}
