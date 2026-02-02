# UISample – Standardized UI Template

A ready-to-use Unity UI kit that lets every team ship **consistent-looking** screens fast:

- Screen Space **Overlay** canvas, **1920×1080** reference scaling  
- Central **Theme** (colors + 9-sliced border sprite)  
- Central **Text Styles** (font + auto-size ranges)  
- Reusable **Primitives** (texts, buttons, card shell, background panel, toast panel)  
- Prebuilt **Composites** (TopBar, BottomBar, FacilityCard, Modals)  
- Drop-in **Screen** prefabs (Overview/Details)  

---

## 0) Requirements

- Unity **2022.3 LTS**  
- TextMeshPro essentials: `Window → TextMeshPro → Import TMP Essential Resources`  
- Project structure lives under: `Assets/UISample/`

---

## 1) Folder layout

- Assets/UISample/
- Prefabs/
- BaseCanvas.prefab
- Primitives/ ← BackgroundPanel, Text_H1/H2/H3/Body/Label, Button_Primary, Panel_CardShell, ToastPanel
- Composites/ ← TopBar, BottomBar, PanelCard
- Screen_Templates/ ← FacilitiesOverview_Screen, SimulationPreviewScreen
- Styles/
- UITheme.asset ← Global colors + CardBorderSprite
- TextStyles.asset ← H1/H2/H3/Body/Label/ButtonText auto-size + font
- Scripts/ ← ThemeContext, ThemedGraphic, ThemedText, SafeAreaFitter, UIScreen, UIRouter, UITheme
- Widgets/ ← ToastPanel script (UpgradeToast)
- README.md


---

## 2) Quick start (drag-and-play, **no code**)

1. **New scene**.  
2. Drag **`BaseCanvas.prefab`** into the scene (from `UISample/Prefabs/`).  
   - Gives you: **Screen Space – Overlay**, **Canvas Scaler** (Scale With Screen Size, **1920×1080**), **SafeAreaFitter**, and a themed **BackgroundPanel**.  
3. From `UISample/Prefabs/Screens/`, drag **`FacilitiesOverview_Screen.prefab`** (or **`FacilityDetails_Screen.prefab`**) **under the Canvas**.  
4. **Play**. You now have a fully themed page with the correct fonts, bars, cards, and buttons.  

---

## 3) Theming (colors, background, borders)

Everything comes from **`Assets/UISample/Styles/UITheme.asset`**. Editing this asset updates all prefabs that use **ThemedGraphic**.

- **ScreenBackground** = `#314D79` (R49 G77 B121 A255) → whole-screen background  
- **BarBackground**    = `#3F2F2F` (A≈100) → top/bottom bars  
- **CardBorderColor**  = `#EBF19B` → yellow outline  
- **CardBackground**   = `#FFFFFF` (A≈100) → card fill (slightly translucent)  
- **ButtonFill**       = `#FFFFFF` → button background  

Border sprite (9-sliced): assign your sprite to **CardBorderSprite** in `UITheme.asset`.

---

## 4) Typography (font + auto sizes)

All handled by **`TextStyles.asset`** + **ThemedText**.  

- **H1:** 55–60  
- **H2:** 55  
- **H3:** 45–50  
- **Body:** 39–42  
- **Label:** 28–32  
- **ButtonText:** 45-45  

Use prefabs in `Prefabs/Primitives/` (Text_H1, Text_H2, etc.) or attach **ThemedText** to any TMP text.

How to attach ThemedText? 
 - In the Hierarchy, select your TMP text object. In the Inspector, click Add Component → add a ThemedText component. From there, set the styleKey to choose the role you want (e.g., H1, H2, H3, Body, Label, or ButtonText).

---

## 5) Build from scratch with Primitives

- Start with **BaseCanvas.prefab**.  
- Add **BackgroundPanel.prefab** if you need a custom full-screen fill.  
- Add **TopBar.prefab** for headers (BarBackground).  
- Add **BottomBar.prefab** for action buttons (Button_Primary inside).  
- Add **Panel_CardShell.prefab** for card layouts (CardBackground + CardBorder).  
- Add texts (Text_H1/H2/H3/Body/Label) depending on the hierarchy you need.  
- Add **Button_Primary** for actions (500×64, ButtonFill, ButtonText).  
- Add **ToastPanel.prefab** when you need a quick transient notification.  

---

## 6) Toast Panel (new!)

The **UpgradeToast.prefab** (under `Prefabs/Widgets/`) is a small notification banner that appears for **~3 seconds** then disappears automatically. Use it to inform the user of updates like “Upgrade Applied” or “Settings Saved”.

### How to use ToastPanel
1. Drag **UpgradeToast.prefab** into your scene (under the Canvas).  
2. Position it (usually bottom or top center). It comes with a semi-transparent CardBackground.  
3. Set its text by calling the script:  

```csharp
// Example: show toast for 3s
FindObjectOfType<ToastPanel>().Show("Upgrade Applied!");

### Toast Panel

- By default, it auto-hides after **3 seconds**.  
- You can adjust this duration in the **ToastPanel script** (`duration` field).  
- Toast is lightweight and standardized — same font, same theme colors, auto-dismiss after 3s.

---

### 7) Composites

- **TopBar.prefab** → anchored Top-Stretch, Height ≈ 120, uses **BarBackground**, place `Text_H1` inside.  
- **BottomBar.prefab** → anchored Bottom-Stretch, Height ≈ 120, uses **BarBackground**, add **HorizontalLayoutGroup**, drop action buttons inside.  
- **FacilityCard.prefab** → based on `Panel_CardShell`; content includes `Text_H2` (title), `Text_Label` (level), `Text_Body` (effects), and `Button_Primary` (“DETAILS”).  
- **Modals** → Confirmation, SimulationPreview, UpgradePreview. Each has a backdrop + **CardBackground** window + Text/Buttons.

---

### 8) Screens

- **FacilitiesOverview_Screen** → TopBar + CardsRow (HorizontalLayoutGroup with FacilityCards) + BottomBar.  
- **FacilityDetails_Screen** → TopBar + detail text + 3 modals (disabled by default).  

**How to use:** Drag **BaseCanvas** into your scene, then add one Screen prefab under it → **Play**.

---

### 9) Safe Area, Scaling, Anchors

- **Canvas = Overlay**, Canvas Scaler = **1920×1080**.  
- **SafeAreaFitter** keeps content away from notches.  
- Bars and content use **stretch anchors**.  
- Text auto-sizes via **TextStyles.asset** ranges.

---

### 10) Export

1. Select **Assets/UISample** → **Export Package…**  
2. Include: Prefabs, Scripts, Styles, Fonts (if allowed), and README.md.  
3. Share the resulting `.unitypackage`.

---

### 11) Design Conventions

- **Bars:** BarBackground, Height = 120, LayoutGroup (Padding 24, Spacing 8–20)  
- **Cards:** Panel_CardShell (CardBackground + CardBorder)  
- **Buttons:** Button_Primary (500×64) with ButtonFill + ButtonText  
- **Texts:** H1/H2/H3/Body/Label/Button (see §4)  
- **Toast:** ToastPanel for 3s update banners  

---

### TL;DR – How to build your own page

1. Drag **BaseCanvas.prefab**.  
2. Add **BackgroundPanel** (ScreenBackground).  
3. Add **TopBar** with `Text_H1`.  
4. Add content cards: `Panel_CardShell` + texts + `Button_Primary`.  
5. Add **BottomBar** with action buttons.  
6. Add **ToastPanel** if you need lightweight feedback (auto hides in 3s).  
7. **Play** → Standardized look across all pages.
