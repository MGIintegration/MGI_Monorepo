using System;

/// <summary>
/// XP is stored and calculated as a float so bonuses never lose precision to
/// per-step rounding; only what the player sees is turned into an integer.
/// </summary>
public static class XpFormat
{
    // float math leaves noise on values that are mathematically whole or exactly
    // half (100 * 1.08f is 108.0000076). Rounding the noise away first keeps a value
    // that should sit on a .5 boundary from landing on the wrong side of it.
    private const int NoiseDecimals = 3;

    // Persisted JSON is written through double, which would otherwise store
    // float noise like 15.449999809265137 instead of 15.45.
    private const int StorageDecimals = 4;

    /// <summary>
    /// Integer XP for the UI: the nearest whole number, with .5 rounding up.
    /// Always call this on the running total or on a single value, never on a sum
    /// of already-rounded values, so two 15.4 gains show 31 (30.8), not 30.
    /// </summary>
    public static int ToDisplay(float xp)
    {
        double clean = Math.Round(xp, NoiseDecimals);
        return (int)Math.Round(clean, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Full-precision XP with float noise removed, for writing to JSON.
    /// </summary>
    public static double ToStorage(float xp)
    {
        return Math.Round(xp, StorageDecimals);
    }
}
