using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class ThemedText : MonoBehaviour {
  public TextStyleKey styleKey = TextStyleKey.Body;

  void Awake()    => Apply();
  void OnEnable() => Apply();
#if UNITY_EDITOR
  void OnValidate()=> Apply();
#endif

  void Apply(){
    if (!isActiveAndEnabled) return;
    if (ThemeContext.I == null || ThemeContext.I.textStyles == null) return;

    var txt = GetComponent<TMP_Text>();
    var s = ThemeContext.I.textStyles;
    var st = styleKey switch {
      TextStyleKey.H1         => s.H1,
      TextStyleKey.H2         => s.H2,
      TextStyleKey.H3         => s.H3,
      TextStyleKey.Label      => s.Label,
      TextStyleKey.ButtonText => s.ButtonText,
      _                       => s.Body
    };
    if (st == null) return;

    if (st.font) txt.font = st.font;
    txt.fontStyle = st.fontStyle;
    txt.enableAutoSizing = st.enableAutoSize;
    if (txt.enableAutoSizing){ txt.fontSizeMin = st.autoSizeMin; txt.fontSizeMax = st.autoSizeMax; }
  }
}
