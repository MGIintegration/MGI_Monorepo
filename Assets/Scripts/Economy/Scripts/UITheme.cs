using UnityEngine;

[CreateAssetMenu(menuName="UISample/UI Theme")]
public class UITheme : ScriptableObject
{
    // 5 colors from your screenshots (RGB 0–255)

    [Header("Screens / Bars")]
    public Color ScreenBackground = new Color32(49, 77, 121, 255);   // #314D79 (Shot 5)
    public Color BarBackground    = new Color32(63, 47, 47, 100);    // #3F2F2F, A=100 (Shot 1)

    [Header("Cards")]
    public Color CardBorderColor  = new Color32(235, 241, 155, 255); // #EBF19B (Shot 2)
    public Color CardBackground   = new Color32(255, 255, 255, 100); // #FFFFFF, A=100 (Shot 3)

    [Header("Buttons")]
    public Color ButtonFill       = new Color32(255, 255, 255, 255); // #FFFFFF (Shot 4)

    [Header("Optional sprites in theme")]
    public Sprite CardBorderSprite;   // your source border sprite (9-sliced)
}

// Pickers used by components
public enum ThemeColor {
    ScreenBackground,
    BarBackground,
    CardBorderColor,
    CardBackground,
    ButtonFill
}

public enum ThemeSprite {
    None,
    CardBorderSprite
}
