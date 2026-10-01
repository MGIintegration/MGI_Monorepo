#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Focused editor test harness for the static coach overall-rating calculation.</summary>
public static class CoachOverallRatingTests
{
    private const float Tolerance = 0.0001f;
    private static int _passed;
    private static int _failed;
    private static readonly List<string> Failures = new List<string>();

    public static void RunAll()
    {
        _passed = 0;
        _failed = 0;
        Failures.Clear();
        TestOffensiveRating();
        TestDefensiveRating();
        TestSpecialTeamsRating();
        TestDonShulaRating();
        TestFractionalRating();
        TestIrrelevantStatsDoNotAffectRating();
        TestUnknownCoachTypeReturnsZero();
        TestDerivedRatingFeedsExistingXpBonusFormula();
        Debug.Log($"[CoachOverallRatingTests] {_passed} passed, {_failed} failed.");
        if (_failed > 0) Debug.LogError("[CoachOverallRatingTests] Failures:\n - " + string.Join("\n - ", Failures));
        if (Application.isBatchMode) EditorApplication.Exit(_failed == 0 ? 0 : 1);
    }

    private static void TestOffensiveRating()
    {
        var coach = NewCoach("O");
        coach.passing_efficiency = 4f; coach.rush = 6f; coach.red_zone_conversion = 5f; coach.play_variation = 7f;
        AssertApproximately(5.5f, CoachesService.CalculateStaticOverallRating(coach), "Offensive rating");
    }

    private static void TestDefensiveRating()
    {
        var coach = NewCoach("D");
        coach.coverage_discipline = 4f; coach.run_defence = 6f; coach.turnover = 5f; coach.pressure_control = 7f;
        AssertApproximately(5.5f, CoachesService.CalculateStaticOverallRating(coach), "Defensive rating");
    }

    private static void TestSpecialTeamsRating()
    {
        var coach = NewCoach("S");
        coach.kickoff_instance = 4f; coach.return_coverage = 6f; coach.field_goal_accuracy = 5f; coach.return_speed = 7f;
        AssertApproximately(5.5f, CoachesService.CalculateStaticOverallRating(coach), "Special Teams rating");
    }

    private static void TestDonShulaRating()
    {
        var coach = NewCoach("O");
        coach.passing_efficiency = 6f; coach.rush = 6f; coach.red_zone_conversion = 6f; coach.play_variation = 6f;
        AssertApproximately(6f, CoachesService.CalculateStaticOverallRating(coach), "Don Shula 6,6,6,6 rating");
    }

    private static void TestFractionalRating()
    {
        var coach = NewCoach("O");
        coach.passing_efficiency = 5f; coach.rush = 6f; coach.red_zone_conversion = 4f; coach.play_variation = 6f;
        AssertApproximately(5.25f, CoachesService.CalculateStaticOverallRating(coach), "Fractional rating");
    }

    private static void TestIrrelevantStatsDoNotAffectRating()
    {
        var coach = NewCoach("O");
        coach.passing_efficiency = 5f; coach.rush = 6f; coach.red_zone_conversion = 4f; coach.play_variation = 6f;
        coach.coverage_discipline = 100f; coach.run_defence = 100f; coach.turnover = 100f; coach.pressure_control = 100f;
        coach.kickoff_instance = 100f; coach.return_coverage = 100f; coach.field_goal_accuracy = 100f; coach.return_speed = 100f;
        AssertApproximately(5.25f, CoachesService.CalculateStaticOverallRating(coach), "Irrelevant role stats");
    }

    private static void TestUnknownCoachTypeReturnsZero()
    {
        var coach = NewCoach("QB"); coach.passing_efficiency = 10f;
        AssertApproximately(0f, CoachesService.CalculateStaticOverallRating(coach), "Unknown coach type");
    }

    private static void TestDerivedRatingFeedsExistingXpBonusFormula()
    {
        var coach = NewCoach("O");
        coach.passing_efficiency = 5f; coach.rush = 6f; coach.red_zone_conversion = 4f; coach.play_variation = 6f;
        coach.overall_rating = CoachesService.CalculateStaticOverallRating(coach);
        var rule = new XpSourceBonusRule { base_bonus = 0.03f, rating_multiplier = 0.0004f };
        var method = typeof(CoachesService).GetMethod("CalculateCoachRuleBonus", BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null) { Fail("Existing XP bonus formula method was not found."); return; }
        var actual = (float)method.Invoke(null, new object[] { coach, rule });
        AssertApproximately(0.03f + (5.25f * 0.0004f), actual, "Derived rating feeds existing XP bonus formula");
    }

    private static CoachDatabaseRecord NewCoach(string coachType) => new CoachDatabaseRecord { coach_id = "coach-overall-rating-test", coach_type = coachType };

    private static void AssertApproximately(float expected, float actual, string testName)
    {
        if (Mathf.Abs(expected - actual) <= Tolerance) { _passed++; return; }
        Fail($"{testName}: expected {expected}, got {actual}.");
    }

    private static void Fail(string message) { _failed++; Failures.Add(message); }
}
#endif
