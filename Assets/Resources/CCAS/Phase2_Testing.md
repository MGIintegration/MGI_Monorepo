# Phase 2 Emotion System – How to Test

## 1. Console logs (no setup)

- **EmotionalStateManager** has a **Verbose** checkbox (default: on). When you open a pack you should see a line like:
  - `[Emotion] pack=bronze_pack raw=5 q=0.50 dS=1.20 dF=0.80 → pos=12.3 neg=8.1 (W=4.1 R=4.0 T=4.2 | Reg=2.7 L=2.7 D=2.7) | Phase2 popups +:[Worth, Relief, Thrill] -:[Regret]`
- **Meaning:** `pack`, `raw` score, `q` (quality 0–1), `dS`/`dF` (positive/negative deltas), `pos`/`neg` (family levels 0–100), then the six emotions (W, R, T = Worth, Relief, Thrill; Reg, L, D = Regret, Letdown, Disappointment), and **Phase2 popups** = which emotions got a share this pull.
- If you **don’t** see `[Emotion]` lines: select the GameObject that has **EmotionalStateManager** and ensure **Verbose** is checked. Then open a pack again.

- **EmotionDisplayUI** can log popups to the Console when the popup text fields are not assigned:
  - Select the GameObject with **EmotionDisplayUI**.
  - Under **Testing**, leave **Log Phase2 Popups To Console** checked (default).
  - Open a pack; you should see e.g. `[EmotionDisplay] Phase 2 POSITIVE popup: Worth + Relief + Thrill (assign positivePopupText in Inspector to show in UI)` and/or a NEGATIVE line.
- If you see those lines, Phase 2 is running; the in-world popups are missing only because the popup Text references are not set.

## 2. Bars

- The **red bar** = negative family (Regret + Letdown + Disappointment).
- The **green bar** = positive family (Worth + Relief + Thrill).
- Open several packs (e.g. bronze then gold). Good pulls should move the green bar up and the red bar down; bad pulls the opposite. The numbers in the `[Emotion]` log match these bars.

## 3. Popups in the UI

- Popups only appear if you assign the **popup Text** components on **EmotionDisplayUI**:
  - **Positive Popup Text**: a TextMeshProUGUI under or near the **green** bar.
  - **Negative Popup Text**: a TextMeshProUGUI under or near the **red** bar.
- In your Canvas:
  1. Create two Text (TMP) objects (e.g. “PositivePopupLabel”, “NegativePopupLabel”) under the emotion bar area.
  2. Select the GameObject with **EmotionDisplayUI** and drag these two Text objects into **Positive Popup Text** and **Negative Popup Text**.
- After opening a pack, the label(s) will show for about 1.5 seconds (e.g. “Worth + Thrill” or “Regret”) then disappear.

## 4. Quick checklist

| Check | What to do |
|-------|------------|
| See `[Emotion]` in Console | Open a pack; EmotionalStateManager.Verbose = true |
| See Phase2 popups in log | Look for `Phase2 popups +:[...] -:[...]` in the same line |
| See `[EmotionDisplay] Phase 2 … popup:` | Open a pack; EmotionDisplayUI.Log Phase2 Popups To Console = true |
| Bars move | Open packs; watch red/green bars and the `pos`/`neg` values in the log |
| Popups in UI | Assign Positive Popup Text and Negative Popup Text in the Inspector |
