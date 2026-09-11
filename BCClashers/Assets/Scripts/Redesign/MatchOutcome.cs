using UnityEngine;

/// <summary>
/// Redesign: shared outcome model for the "My Recent Attacks" and "Recent Invasions"
/// dashboard panels.
///
/// A match result is bucketed by a single number: how much of the base was destroyed,
/// expressed as a percentage of the stake (0-100). The SAME percentage is labelled
/// two ways depending on which side of the match you are looking at:
///
///   Attack  (you invaded them) -> read as LOOT GAINED:
///       &lt;20  Complete Flop | 21-50 Minor Damage | 51-75 Good Attack | 76-99 Major Damage | 100 Total Victory
///   Defense (they invaded you) -> read as DAMAGE TAKEN:
///       &lt;20  Solid Defense | 21-50 Partial Breach | 51-75 Severe Breach | 76-99 Town Nearly Lost | 100 Town Destroyed
///
/// Colours mirror the design legend: green = good-for-you, brown/orange = middling,
/// dark red = bad-for-you. (Green is the LOW end for defense but the HIGH end for attack.)
/// </summary>
public static class MatchOutcome
{
    // Legend palette (shared so both perspectives use the exact same swatches).
    private static readonly Color Good     = new Color32(0x3C, 0x7D, 0x3C, 0xFF); // green
    private static readonly Color GoodBright= new Color32(0x2E, 0x8B, 0x2E, 0xFF); // brighter green (100%)
    private static readonly Color Middling  = new Color32(0xA0, 0x52, 0x2D, 0xFF); // brown / orange
    private static readonly Color Bad       = new Color32(0x8B, 0x1A, 0x1A, 0xFF); // dark red

    public struct Badge
    {
        public string Label;
        public Color Color;
        public Badge(string label, Color color) { Label = label; Color = color; }
    }

    /// <summary>Clamp any raw damage/loot ratio to a 0-100 integer percentage.</summary>
    public static int ToPercent(float ratio01) => Mathf.Clamp(Mathf.RoundToInt(ratio01 * 100f), 0, 100);

    /// <summary>How many outcome tiers there are - the width of the My Stats histograms.</summary>
    public const int TIER_COUNT = 5;

    /// <summary>
    /// Bucket a percentage into a tier: 0 = &lt;20 ... 4 = 100. The My Stats histograms key off
    /// this, so a row's badge and its histogram column are guaranteed to agree.
    /// </summary>
    public static int TierIndex(int pct)
    {
        if (pct >= 100) return 4;
        if (pct >= 76) return 3;
        if (pct >= 51) return 2;
        if (pct >= 21) return 1;
        return 0;
    }

    /// <summary>The badge for a tier index, per perspective - used to colour the histogram bars.</summary>
    public static Badge ForTier(bool isAttack, int tier)
    {
        int[] representative = { 0, 21, 51, 76, 100 };
        int pct = representative[Mathf.Clamp(tier, 0, TIER_COUNT - 1)];
        return For(isAttack, pct);
    }

    // Labels kept short so they never clip the badge box; the full legend meaning is:
    //   attack : Flop / Minor / Good / Major / Victory
    //   defense: Solid / Partial / Severe / Near Loss / Destroyed

    /// <summary>Badge for a match YOU initiated, keyed on loot gained as % of stake.</summary>
    public static Badge ForAttack(int pct)
    {
        if (pct >= 100) return new Badge("Victory", GoodBright);
        if (pct >= 76)  return new Badge("Major",   Good);
        if (pct >= 51)  return new Badge("Good",    Good);
        if (pct >= 21)  return new Badge("Minor",   Middling);
        return new Badge("Flop", Bad);
    }

    /// <summary>Badge for a match launched AGAINST you, keyed on damage taken as % of stake.</summary>
    public static Badge ForDefense(int pct)
    {
        if (pct >= 100) return new Badge("Destroyed", Bad);
        if (pct >= 76)  return new Badge("Near Loss", Middling);
        if (pct >= 51)  return new Badge("Severe",    Bad);
        if (pct >= 21)  return new Badge("Partial",   Middling);
        return new Badge("Solid", Good);
    }

    public static Badge For(bool isAttack, int pct) => isAttack ? ForAttack(pct) : ForDefense(pct);
}
