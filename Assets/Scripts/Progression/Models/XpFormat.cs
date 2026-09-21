using System;

/// <summary>
/// XP is stored and calculated as a float so bonuses never lose precision to
/// per-step rounding; only what the player sees is turned into an integer.
/// </summary>
public static class XpFormat
{
    // float math leaves noise on values that are mathematically whole (100 * 1.08f
    // is 108.0000076), and a bare Ceiling would show that as 109. Rounding away the
    // noise first keeps Ceiling honest without hiding any real fraction.
    private const int NoiseDecimals = 3;

    // Persisted JSON is written through double, which would otherwise store
    // float noise like 15.449999809265137 instead of 15.45.
    private const int StorageDecimals = 4;

    /// <summary>
    /// Integer XP for the UI. Ceiling, not floor, so a small bonus never displays as zero.
    /// </summary>
    public static int ToDisplay(float xp)
    {
        return (int)Math.Ceiling(Math.Round(xp, NoiseDecimals));
    }

    /// <summary>
    /// Full-precision XP with float noise removed, for writing to JSON.
    /// </summary>
    public static double ToStorage(float xp)
    {
        return Math.Round(xp, StorageDecimals);
    }
}
