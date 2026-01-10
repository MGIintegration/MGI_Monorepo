using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles visual presentation of a single card slot in the Pack Opening screen.
/// Supports rarity-based color mapping and simple fade-in reveals.
/// Phase 1–ready: can be extended to react to emotional state or hooks.
/// </summary>
public class CardView : MonoBehaviour
{
    [Header("References")]
    public Image frame;            // Assign in prefab (auto-finds if null)
    public TextMeshProUGUI label;  // Optional text label (primary - will show card name)
    public TextMeshProUGUI teamLabel;    // Optional - for team display
    public TextMeshProUGUI elementLabel; // Optional - for element display
    public TextMeshProUGUI positionLabel; // Optional - for position display
    public CanvasGroup cg;         // Optional (for fade-in)

    [Header("Animation Settings")]
    [Range(0f, 1f)] public float revealAlpha = 1f;
    [Range(0.1f, 2f)] public float revealSpeed = 1f;

    void Awake()
    {
        if (frame == null) frame = GetComponentInChildren<Image>(true);
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>(true);
        if (cg == null) cg = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// Applies the correct color and label for a card's rarity.
    /// </summary>
    public void Apply(string rarityLower)
    {
        string rarity = (rarityLower ?? "common").ToLowerInvariant();

        // Defensive: make sure we have frame or label
        if (frame == null && label == null)
        {
            Debug.LogWarning($"[CardView] No UI components assigned on {gameObject.name}");
            return;
        }

        // Apply color and label
        if (frame != null)
            frame.color = RarityColorUtility.GetColorForRarity(rarity);

        if (label != null)
            label.text = rarity.ToUpperInvariant();

        // Simple reveal fade (optional)
        if (cg != null)
        {
            cg.alpha = 0f;
            StopAllCoroutines();
            StartCoroutine(FadeIn());
        }
    }

    /// <summary>
    /// Applies card data to the view, displaying Name, Team, Element, and Position5.
    /// </summary>
    public void Apply(Card card)
    {
        if (card == null)
        {
            Debug.LogWarning($"[CardView] Attempted to apply null card to {gameObject.name}");
            return;
        }

        string rarity = card.GetRarityString();

        // Apply rarity color to frame
        if (frame != null)
            frame.color = RarityColorUtility.GetColorForRarity(rarity);

        // Display card information
        // Primary label shows card name
        if (label != null)
        {
            label.text = card.name;
        }

        // If separate labels are assigned, use them for detailed info
        if (teamLabel != null)
        {
            teamLabel.text = card.team;
        }

        if (elementLabel != null)
        {
            elementLabel.text = card.element;
        }

        if (positionLabel != null)
        {
            positionLabel.text = card.position5;
        }

        // If only primary label exists, show formatted info
        if (label != null && teamLabel == null && elementLabel == null && positionLabel == null)
        {
            // Format: "Name\nTeam | Element | Position"
            label.text = $"{card.name}\n{card.team} | {card.element} | {card.position5}";
        }

        // Simple reveal fade (optional)
        if (cg != null)
        {
            cg.alpha = 0f;
            StopAllCoroutines();
            StartCoroutine(FadeIn());
        }
    }

    System.Collections.IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * revealSpeed;
            if (cg != null)
                cg.alpha = Mathf.Lerp(0f, revealAlpha, t);
            yield return null;
        }
    }
}
