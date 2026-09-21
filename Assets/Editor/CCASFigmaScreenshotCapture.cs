#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click capture helpers for the CCAS Figma handoff. Select the matching
/// menu item while the desired screen is visible in Play Mode. PNG files are
/// written outside Assets so screenshots do not become Unity project assets.
/// </summary>
public static class CCASFigmaScreenshotCapture
{
    private const string MenuRoot = "Tools/MGI/CCAS Figma Screenshots/";

    [MenuItem(MenuRoot + "Capture 00 - Title Screen CCAS Entry")]
    private static void CaptureTitleScreen() => Capture("00_TitleScreen_CCAS_Entry.png");

    [MenuItem(MenuRoot + "Capture 01 - CCAS Acquisition Hub")]
    private static void CaptureAcquisitionHub() => Capture("01_CCAS_Acquisition_Hub.png");

    [MenuItem(MenuRoot + "Capture 02 - CCAS Booster Market")]
    private static void CaptureBoosterMarket() => Capture("02_CCAS_Booster_Market.png");

    [MenuItem(MenuRoot + "Capture 03 - CCAS Pack Opening")]
    private static void CapturePackOpening() => Capture("03_CCAS_Pack_Opening.png");

    [MenuItem(MenuRoot + "Capture 04 - CCAS Pack History My Packs")]
    private static void CapturePackHistory() => Capture("04_CCAS_Pack_History_My_Packs.png");

    [MenuItem(MenuRoot + "Capture 05 - CCAS Insufficient Funds")]
    private static void CaptureInsufficientFunds() => Capture("05_CCAS_Insufficient_Funds.png");

    private static void Capture(string fileName)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[CCAS Figma] Enter Play Mode and display the target CCAS screen before capturing.");
            return;
        }

        string outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Figma_Screenshots", "CCAS"));
        Directory.CreateDirectory(outputDirectory);
        string outputPath = Path.Combine(outputDirectory, fileName);

        ScreenCapture.CaptureScreenshot(outputPath, 1);
        Debug.Log($"[CCAS Figma] Screenshot requested: {outputPath}. It is written at the end of this frame.");
    }
}
#endif
