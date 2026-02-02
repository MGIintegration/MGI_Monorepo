using TMPro;
using UnityEngine;

[CreateAssetMenu(menuName="UISample/Text Styles")]
public class UITextStyles : ScriptableObject {
  [System.Serializable] public class Style {
    public TMP_FontAsset font;
    public FontStyles fontStyle = FontStyles.Normal;
    public bool enableAutoSize = true;
    public float autoSizeMin = 32f;
    public float autoSizeMax = 48f;
  }
  public Style H1;         // Main headings
  public Style H2;         // Subheadings
  public Style H3;         // Tertiary headings
  public Style Body;       // Content text
  public Style Label;      // Small labels
  public Style ButtonText; // Button captions
}
public enum TextStyleKey { H1, H2, H3, Body, Label, ButtonText }
