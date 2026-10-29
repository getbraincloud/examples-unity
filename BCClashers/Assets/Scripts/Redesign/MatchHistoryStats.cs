using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Redesign: the numbers behind the "My Stats" panel, derived entirely from the two
/// PlaybackStream reads the dashboard already performs (see NetworkManager.ParseRecentStreams).
///
/// IMPORTANT - these are "last N matches", NOT lifetime totals. N is
/// NetworkManager._recentMatchCount, which is how many streams brainCloud hands back. True
/// lifetime counters need server-side Player Statistics (defined in the brainCloud portal and
/// incremented from cloud code); until that exists, anything labelled "lifetime" here would be a
/// lie, so the panel says "last N" instead. Everything below is exact within that window.
/// </summary>
public class MatchHistoryStats
{
    //true once real brainCloud user statistics have been overlaid: the counts/streaks/peak/
    //gold/breach figures are then lifetime totals, not the last-N sample. Trend, nemesis and
    //times-invaded-7d stay stream-derived either way (they aren't single-counter statistics).
    public bool IsLifetime;

    //How many matches actually backed these numbers - the honest denominator.
    public int AttackSampleSize;
    public int DefenseSampleSize;

    //Counts per outcome tier (index 0 = <20% ... 4 = 100%), matching MatchOutcome.TierIndex.
    public int[] AttackTierCounts = new int[MatchOutcome.TIER_COUNT];
    public int[] DefenseTierCounts = new int[MatchOutcome.TIER_COUNT];

    //Attack
    public int CurrentWinStreak;
    public int BestWinStreak;
    public int PeakElo;
    public int EloTrend;              // rating now - rating N attacks ago
    public int EloTrendWindow;        // how many attacks that trend covers
    public long TotalGoldLooted;
    public int AverageLootPerAttack;
    public float FastestVictorySeconds = -1f;   // -1 = no outright victory yet

    //Defense
    public long TotalGoldLost;
    public int TimesInvadedRecently;
    public string Nemesis = "-";      // who hit you most often
    public int NemesisCount;

    //Severe breaches (>=51% taken) per defense layout, indexed by ArmyDivisionRank Easy/Medium/Hard.
    public int[] SevereBreachesByLayout = new int[3];
    public int SevereBreachTotal;

    private const int ELO_TREND_MAX_WINDOW = 10;
    private const int SEVERE_BREACH_MIN_PERCENT = 51;
    private const float RECENT_INVASION_DAYS = 7f;

    /// <summary>
    /// Both lists arrive newest-first (ParseRecentStreams sorts them), which matters for the
    /// streak and ELO-trend walks below.
    /// </summary>
    public static MatchHistoryStats Compute(List<MatchSummary> in_attacks, List<MatchSummary> in_invasions,
                                            Dictionary<string, object> in_serverStats = null)
    {
        var stats = new MatchHistoryStats();
        in_attacks ??= new List<MatchSummary>();
        in_invasions ??= new List<MatchSummary>();

        stats.AttackSampleSize = in_attacks.Count;
        stats.DefenseSampleSize = in_invasions.Count;

        stats.ComputeAttacks(in_attacks);
        stats.ComputeDefense(in_invasions);
        stats.ComputePeakElo(in_attacks, in_invasions);

        //Overlay real lifetime totals when the server has them. Trend/nemesis/7d stay as computed
        //above (they can't be single counters); everything else is replaced with the true total.
        stats.OverlayServerStats(in_serverStats);
        return stats;
    }

    private static int Stat(Dictionary<string, object> s, string key)
    {
        if (s == null || !s.ContainsKey(key) || s[key] == null) return 0;
        try { return System.Convert.ToInt32(s[key]); } catch { return 0; }
    }

    private void OverlayServerStats(Dictionary<string, object> s)
    {
        //Treat the overlay as present only when our schema is actually defined - keyed on
        //clashers_attacks. Until the portal statistics exist, this is a no-op and the panel
        //keeps showing the honest stream-derived "last N".
        if (s == null || !s.ContainsKey("clashers_attacks")) return;
        IsLifetime = true;

        AttackSampleSize = Stat(s, "clashers_attacks");
        DefenseSampleSize = Stat(s, "clashers_defenses");
        for (int i = 0; i < MatchOutcome.TIER_COUNT; ++i)
        {
            AttackTierCounts[i] = Stat(s, "clashers_atkTier" + i);
            DefenseTierCounts[i] = Stat(s, "clashers_defTier" + i);
        }

        CurrentWinStreak = Stat(s, "clashers_curStreak");
        BestWinStreak = Stat(s, "clashers_bestStreak");
        PeakElo = Mathf.Max(PeakElo, Stat(s, "clashers_peakElo"));

        TotalGoldLooted = Stat(s, "clashers_goldLooted");
        TotalGoldLost = Stat(s, "clashers_goldLost");
        AverageLootPerAttack = AttackSampleSize > 0 ? (int) (TotalGoldLooted / AttackSampleSize) : 0;

        //Stored as deciseconds (see the cloud script); 0 = never achieved.
        int fastestDs = Stat(s, "clashers_fastestVic");
        FastestVictorySeconds = fastestDs > 0 ? fastestDs / 10f : -1f;

        SevereBreachesByLayout[0] = Stat(s, "clashers_breachLine");
        SevereBreachesByLayout[1] = Stat(s, "clashers_breachCross");
        SevereBreachesByLayout[2] = Stat(s, "clashers_breachDiamond");
        SevereBreachTotal = SevereBreachesByLayout[0] + SevereBreachesByLayout[1] + SevereBreachesByLayout[2];
    }

    private void ComputeAttacks(List<MatchSummary> in_attacks)
    {
        long lootTotal = 0;
        bool streakStillRunning = true;
        int runningStreak = 0;

        for (int i = 0; i < in_attacks.Count; ++i)
        {
            MatchSummary match = in_attacks[i];

            AttackTierCounts[MatchOutcome.TierIndex(match.DamagePercent)]++;
            lootTotal += match.DamageGold;

            //An outright win is a levelled town, which is what the design calls an
            //"Absolute Victory" - the fastest one is the record worth showing.
            if (match.DamagePercent >= 100 &&
                (FastestVictorySeconds < 0f || match.DurationSeconds < FastestVictorySeconds))
            {
                FastestVictorySeconds = match.DurationSeconds;
            }

            //Walking newest-first: the current streak is the unbroken run at the head of the list.
            if (match.DidIWin)
            {
                runningStreak++;
                if (streakStillRunning) CurrentWinStreak = runningStreak;
                if (runningStreak > BestWinStreak) BestWinStreak = runningStreak;
            }
            else
            {
                streakStillRunning = false;
                runningStreak = 0;
            }
        }

        TotalGoldLooted = lootTotal;
        AverageLootPerAttack = in_attacks.Count > 0 ? (int) (lootTotal / in_attacks.Count) : 0;

        //ELO trend: my rating at the newest attack vs my rating N attacks back.
        if (in_attacks.Count >= 2)
        {
            int oldestIndex = Mathf.Min(in_attacks.Count, ELO_TREND_MAX_WINDOW) - 1;
            EloTrend = in_attacks[0].MyRating - in_attacks[oldestIndex].MyRating;
            EloTrendWindow = oldestIndex + 1;
        }
    }

    private void ComputeDefense(List<MatchSummary> in_invasions)
    {
        long lostTotal = 0;
        var attackerTally = new Dictionary<string, int>();

        for (int i = 0; i < in_invasions.Count; ++i)
        {
            MatchSummary match = in_invasions[i];

            DefenseTierCounts[MatchOutcome.TierIndex(match.DamagePercent)]++;
            lostTotal += match.DamageGold;

            if ((System.DateTime.UtcNow - match.OccurredAtUtc).TotalDays <= RECENT_INVASION_DAYS)
            {
                TimesInvadedRecently++;
            }

            if (match.DamagePercent >= SEVERE_BREACH_MIN_PERCENT)
            {
                int layout = (int) match.DefenderRank;
                //None/Test have no column - matches recorded before defenderRank existed land here.
                if (layout >= 0 && layout < SevereBreachesByLayout.Length)
                {
                    SevereBreachesByLayout[layout]++;
                    SevereBreachTotal++;
                }
            }

            if (!string.IsNullOrEmpty(match.OpponentName))
            {
                attackerTally.TryGetValue(match.OpponentName, out int count);
                attackerTally[match.OpponentName] = count + 1;
            }
        }

        TotalGoldLost = lostTotal;

        foreach (KeyValuePair<string, int> entry in attackerTally)
        {
            if (entry.Value > NemesisCount)
            {
                NemesisCount = entry.Value;
                Nemesis = entry.Key;
            }
        }
    }

    //My rating is stamped on both sides of every match, so the two lists together are a
    //sampling of my rating over time. The best of them is the peak we can honestly claim.
    private void ComputePeakElo(List<MatchSummary> in_attacks, List<MatchSummary> in_invasions)
    {
        PeakElo = GameManager.Instance != null ? GameManager.Instance.CurrentUserInfo.Rating : 0;
        foreach (MatchSummary m in in_attacks) if (m.MyRating > PeakElo) PeakElo = m.MyRating;
        foreach (MatchSummary m in in_invasions) if (m.MyRating > PeakElo) PeakElo = m.MyRating;
    }

    /// <summary>Percentage share of severe breaches for a layout, e.g. "4 (40%)".</summary>
    public int SevereBreachPercent(int in_layoutIndex)
    {
        if (SevereBreachTotal <= 0 || in_layoutIndex < 0 || in_layoutIndex >= SevereBreachesByLayout.Length) return 0;
        return Mathf.RoundToInt(100f * SevereBreachesByLayout[in_layoutIndex] / SevereBreachTotal);
    }
}
