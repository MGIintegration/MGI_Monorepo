// using UnityEngine;
// public class ThemeContext : MonoBehaviour {
//   public UITheme theme;
//   public UITextStyles textStyles;
//   public static ThemeContext I { get; private set; }
//   void Awake(){ I = this; }
// }
using UnityEngine;

[ExecuteAlways]
public class ThemeContext : MonoBehaviour
{
    public UITheme theme;
    public UITextStyles textStyles;

    public static ThemeContext I { get; private set; }

    void Awake()    { I = this; }
    void OnEnable() { I = this; }       // <-- also set during prefab editing
    void OnDisable(){ if (I == this) I = null; }
}
