using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Graphic))]
public class ThemedGraphic : MonoBehaviour
{
    [Header("Color from UITheme")]
    public ThemeColor colorKey = ThemeColor.ScreenBackground;

    [Header("Optional sprite from UITheme (Image only)")]
    public ThemeSprite spriteKey = ThemeSprite.None;

    void Awake()    => Apply();
    void OnEnable() => Apply();
#if UNITY_EDITOR
    void OnValidate()=> Apply();
#endif

    void Apply()
    {
        if (!isActiveAndEnabled) return;
        if (ThemeContext.I == null || ThemeContext.I.theme == null) return;

        var g = GetComponent<Graphic>();
        var t = ThemeContext.I.theme;

        // Apply color
        g.color = colorKey switch
        {
            ThemeColor.ScreenBackground => t.ScreenBackground,
            ThemeColor.BarBackground    => t.BarBackground,
            ThemeColor.CardBorderColor  => t.CardBorderColor,
            ThemeColor.CardBackground   => t.CardBackground,
            ThemeColor.ButtonFill       => t.ButtonFill,
            _ => g.color
        };

        // Optionally apply a sprite (if this is an Image)
        if (spriteKey != ThemeSprite.None && g is Image img)
        {
            img.sprite = spriteKey switch
            {
                ThemeSprite.CardBorderSprite => t.CardBorderSprite,
                _ => img.sprite
            };
        }
    }
}
