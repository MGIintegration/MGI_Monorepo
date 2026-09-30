#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using CCAS.Backend;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor smoke checks for the CCAS scene asset. These checks deliberately do
/// not buy a pack or change player state: they make sure the scene opens and
/// the inspector wiring needed for the core CCAS journey remains intact.
/// </summary>
public static class CCASSceneSmokeTests
{
    private const string ScenePath = "Assets/Scenes/CCAS/CCAS.unity";
    private static int _passed;
    private static int _failed;
    private static readonly List<string> Failures = new List<string>();

    public static void RunAll()
    {
        bool passed = RunSuite().Passed;
        if (Application.isBatchMode)
            EditorApplication.Exit(passed ? 0 : 1);
    }

    public static TestSuiteRunResult RunSuite()
    {
        _passed = 0;
        _failed = 0;
        Failures.Clear();

        Debug.Log("===== CCASSceneSmokeTests: starting =====");

        try
        {
            Test_SceneIsEnabledInBuildSettings();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Test_SceneOpens(scene);
            Test_RequiredServicesAndEventSystemArePresent(scene);
            Test_AcquisitionHubWiring(scene);
            Test_MarketAndPackOpeningWiring(scene);
            Test_MyPacksWiring(scene);
            Test_ForwardPanelNavigation(scene);
        }
        catch (Exception exception)
        {
            Fail("Unexpected scene-load exception: " + exception);
        }

        Debug.Log($"===== CCASSceneSmokeTests: {_passed} passed, {_failed} failed =====");
        if (_failed > 0)
            Debug.Log("FAILURES:\n - " + string.Join("\n - ", Failures));

        return new TestSuiteRunResult(_passed, _failed, Failures);
    }

    private static void Test_SceneIsEnabledInBuildSettings()
    {
        bool enabled = EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == ScenePath);
        Assert(enabled, "BuildSettings: CCAS scene is enabled");
    }

    private static void Test_SceneOpens(Scene scene)
    {
        Assert(scene.IsValid() && scene.isLoaded, "Scene: CCAS scene opens successfully");
        AssertEqual("CCAS", scene.name, "Scene: active scene is CCAS");
    }

    private static void Test_RequiredServicesAndEventSystemArePresent(Scene scene)
    {
        Assert(FindComponent<CCASService>(scene) != null, "Services: CCASService is present");
        Assert(FindComponent<DropConfigManager>(scene) != null, "Services: DropConfigManager is present");
        Assert(FindComponent<CardCatalogLoader>(scene) != null, "Services: CardCatalogLoader is present");
        Assert(FindComponent<EventSystem>(scene) != null, "UI: EventSystem is present");
    }

    private static void Test_AcquisitionHubWiring(Scene scene)
    {
        var hub = FindComponent<AcquisitionHubController>(scene);
        Assert(hub != null, "Hub: AcquisitionHubController is present");
        if (hub == null)
            return;

        Assert(hub.goToMarketButton != null, "Hub: market button is assigned");
        Assert(hub.myPacksButton != null, "Hub: My Packs button is assigned");
        Assert(hub.hubPanel != null, "Hub: hub panel is assigned");
        Assert(hub.marketPanel != null, "Hub: market panel is assigned");
        Assert(hub.packPanel != null, "Hub: pack-opening panel is assigned");
        Assert(hub.dropHistoryPanel != null, "Hub: drop-history panel is assigned");
        Assert(hub.myPacksPanel != null, "Hub: My Packs panel is assigned");
        Assert(hub.myPacksPanel.GetComponent<MyPacksController>() != null,
            "Hub: My Packs panel has its controller");
    }

    private static void Test_MarketAndPackOpeningWiring(Scene scene)
    {
        var market = FindComponent<BoosterMarketAuto>(scene);
        Assert(market != null, "Market: BoosterMarketAuto is present");
        if (market != null)
        {
            Assert(market.packButtonPrefab != null, "Market: pack-button prefab is assigned");
            Assert(market.contentParent != null, "Market: content parent is assigned");
        }

        var opening = FindComponent<PackOpeningController>(scene);
        Assert(opening != null, "Pack opening: PackOpeningController is present");
        if (opening != null)
        {
            Assert(opening.continueButton != null, "Pack opening: Continue button is assigned");
            Assert(opening.backToHubButton != null, "Pack opening: Back to Hub button is assigned");
            Assert(opening.cardParent != null, "Pack opening: card parent is assigned");
            Assert(opening.cardPrefab != null, "Pack opening: card prefab is assigned");
        }
    }

    private static void Test_MyPacksWiring(Scene scene)
    {
        var myPacks = FindComponent<MyPacksController>(scene);
        Assert(myPacks != null, "My Packs: controller is present");
        if (myPacks == null)
            return;

        Assert(myPacks.contentParent != null, "My Packs: content parent is assigned");
        Assert(myPacks.resultTemplate != null, "My Packs: result template is assigned");
        Assert(myPacks.scrollRect != null, "My Packs: scroll view is assigned");
        Assert(myPacks.backToHubButton != null, "My Packs: Back to Hub button is assigned");
    }

    private static void Test_ForwardPanelNavigation(Scene scene)
    {
        var hub = FindComponent<AcquisitionHubController>(scene);
        if (hub == null || hub.hubPanel == null || hub.marketPanel == null || hub.myPacksPanel == null)
        {
            Fail("Navigation: required hub panels are missing");
            return;
        }

        hub.ShowMarket();
        Assert(hub.marketPanel.activeSelf && !hub.hubPanel.activeSelf,
            "Navigation: ShowMarket activates market and hides hub");

        hub.ShowMyPacks();
        Assert(hub.myPacksPanel.activeSelf && !hub.marketPanel.activeSelf,
            "Navigation: ShowMyPacks activates My Packs and hides market");
    }

    private static T FindComponent<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .FirstOrDefault();
    }

    private static void Assert(bool condition, string description)
    {
        if (condition)
        {
            _passed++;
            Debug.Log("[CCASSceneSmokeTests] PASS: " + description);
            return;
        }

        Fail(description);
    }

    private static void AssertEqual(string expected, string actual, string description)
    {
        Assert(string.Equals(expected, actual, StringComparison.Ordinal),
            $"{description} (expected '{expected}', got '{actual}')");
    }

    private static void Fail(string description)
    {
        _failed++;
        Failures.Add(description);
        Debug.LogError("[CCASSceneSmokeTests] FAIL: " + description);
    }
}
#endif
