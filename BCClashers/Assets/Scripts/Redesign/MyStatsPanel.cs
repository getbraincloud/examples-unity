using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Redesign: the "My Stats" dashboard panel.
///
/// Binds MatchHistoryStats to the UI. Every reference is optional - assign only the widgets you
/// have built and the rest are skipped, so the panel can be filled in piece by piece.
///
/// The numbers are computed from the match history already on the client, so this costs no extra
/// brainCloud calls. They cover the last N matches rather than all time (see MatchHistoryStats);
/// the sample-size labels below say so out loud rather than implying lifetime totals.
/// </summary>
public class MyStatsPanel : MonoBehaviour
{
    [System.Serializable]
    public class TierBar
    {
        [Tooltip("Count label for this outcome tier, e.g. '12'.")]
        public TMP_Text CountText;
        [Tooltip("Optional background tinted with the tier's badge colour.")]
        public Image Background;
    }

    [Header("Attack - outcome histogram (Complete Flop -> Total Victory, 5 entries)")]
    public TierBar[] AttackTiers = new TierBar[MatchOutcome.TIER_COUNT];
    public TMP_Text AttackSampleText;

    [Header("Attack - figures")]
    public TMP_Text CurrentWinStreakText;
    public TMP_Text BestWinStreakText;
    public TMP_Text PeakEloText;
    public TMP_Text EloTrendText;
    public TMP_Text TotalGoldLootedText;
    public TMP_Text AverageLootText;
    public TMP_Text FastestVictoryText;

    [Header("Defense - outcome histogram (Solid Defense -> Town Destroyed, 5 entries)")]
    public TierBar[] DefenseTiers = new TierBar[MatchOutcome.TIER_COUNT];
    public TMP_Text DefenseSampleText;

    [Header("Defense - figures")]
    public TMP_Text TotalGoldLostText;
    public TMP_Text TimesInvadedText;
    public TMP_Text NemesisText;

    [Header("Severe breaches by layout (Line / Cross / Diamond)")]
    public TMP_Text SevereBreachBreakdownText;

    private static readonly string[] LAYOUT_NAMES = { "Line", "Cross", "Diamond" };

    public void Bind(MatchHistoryStats in_stats)
    {
        if (in_stats == null) return;

        BindTiers(AttackTiers, in_stats.AttackTierCounts, true);
        BindTiers(DefenseTiers, in_stats.DefenseTierCounts, false);

        Set(AttackSampleText, in_stats.IsLifetime
            ? $"Lifetime attacks: {in_stats.AttackSampleSize}"
            : $"Last {in_stats.AttackSampleSize} attacks");
        Set(DefenseSampleText, in_stats.IsLifetime
            ? $"Lifetime defenses: {in_stats.DefenseSampleSize}"
            : $"Last {in_stats.DefenseSampleSize} defenses");

        Set(CurrentWinStreakText, $"Current win streak: {in_stats.CurrentWinStreak}");
        Set(BestWinStreakText, $"Best: {in_stats.BestWinStreak}");
        Set(PeakEloText, $"Peak Elo: {in_stats.PeakElo:#,0}");
        Set(EloTrendText, in_stats.EloTrendWindow > 0
            ? $"Elo Trend (Last {in_stats.EloTrendWindow} attacks): {Signed(in_stats.EloTrend)}"
            : "Elo Trend: -");
        Set(TotalGoldLootedText, $"Total Gold Looted: {Compact(in_stats.TotalGoldLooted)}");
        Set(AverageLootText, $"Average Loot per Attack: {Compact(in_stats.AverageLootPerAttack)}");
        Set(FastestVictoryText, in_stats.FastestVictorySeconds >= 0f
            ? $"Fastest Absolute Victory: {Duration(in_stats.FastestVictorySeconds)}"
            : "Fastest Absolute Victory: -");

        Set(TotalGoldLostText, $"Total Gold Lost: {Compact(in_stats.TotalGoldLost)}");
        Set(TimesInvadedText, $"Times Invaded (Last 7 Days): {in_stats.TimesInvadedRecently}");
        Set(NemesisText, in_stats.NemesisCount > 0
            ? $"Nemesis: {in_stats.Nemesis} ({in_stats.NemesisCount})"
            : "Nemesis: -");

        BindSevereBreaches(in_stats);
    }

    private void BindTiers(TierBar[] in_bars, int[] in_counts, bool in_isAttack)
    {
        if (in_bars == null) return;
        for (int i = 0; i < in_bars.Length && i < in_counts.Length; ++i)
        {
            if (in_bars[i] == null) continue;
            Set(in_bars[i].CountText, in_counts[i].ToString());
            if (in_bars[i].Background)
            {
                //Same swatch the matching card badge uses, so the columns read as the same scale.
                in_bars[i].Background.color = MatchOutcome.ForTier(in_isAttack, i).Color;
            }
        }
    }

    private void BindSevereBreaches(MatchHistoryStats in_stats)
    {
        if (!SevereBreachBreakdownText) return;

        if (in_stats.SevereBreachTotal <= 0)
        {
            SevereBreachBreakdownText.text = "Severe Breaches by layout: none";
            return;
        }

        var sb = new System.Text.StringBuilder("Severe Breaches by layout: ");
        for (int i = 0; i < LAYOUT_NAMES.Length; ++i)
        {
            if (i > 0) sb.Append("   ");
            sb.Append($"{in_stats.SevereBreachesByLayout[i]} ({in_stats.SevereBreachPercent(i)}%) {LAYOUT_NAMES[i]}");
        }
        SevereBreachBreakdownText.text = sb.ToString();
    }

    private static void Set(TMP_Text in_text, string in_value)
    {
        if (in_text) in_text.text = in_value;
    }

    private static string Signed(int in_value) => in_value > 0 ? $"+{in_value}" : in_value.ToString();

    //1.2M / 124k / 950 - matches the design's shorthand.
    private static string Compact(long in_value)
    {
        if (in_value >= 1000000) return $"{in_value / 1000000f:0.#}M";
        if (in_value >= 1000) return $"{in_value / 1000f:0.#}k";
        return in_value.ToString();
    }

    private static string Duration(float in_seconds)
    {
        int minutes = Mathf.FloorToInt(in_seconds / 60f);
        float seconds = in_seconds - (minutes * 60f);
        return $"{minutes}:{seconds:00.0}";
    }
}
