using System;
using UnityEngine;

/// <summary>
/// Redesign: one row in the "My Recent Attacks" / "Recent Invasions" dashboard panels.
///
/// Both panels are built from brainCloud PlaybackStream reads - the same stream shape,
/// just viewed from the two ends of the match:
///     My Recent Attacks  -> PlaybackStreamService.GetRecentStreamsForInitiatingPlayer (IsAttack = true)
///     Recent Invasions   -> PlaybackStreamService.GetRecentStreamsForTargetPlayer     (IsAttack = false)
///
/// Everything the card renders comes out of the stream's summary blob (see
/// NetworkManager.CreateEndGameSummaryData), so no extra per-opponent lookups are needed.
/// </summary>
public class MatchSummary
{
    //The other player in this match: who you hit (attack) or who hit you (invasion).
    public string OpponentName;
    public int OpponentRating;
    //Needed so the card's "Attack" button can re-target them (revenge).
    public string OpponentProfileId;

    //Gold destroyed/looted in this match, and the gold that was wagered on it.
    public int DamageGold;
    public int Stake;

    public float DurationSeconds;
    //When the match happened, used for the "~4 hours ago" line.
    public DateTime OccurredAtUtc;

    //Empty when the stream has been purged - the card shows "Replay not available".
    public string PlaybackStreamId;

    //true  = you initiated it  -> badge reads as loot gained.
    //false = it was launched at you -> badge reads as damage taken.
    public bool IsAttack;

    //YOUR rating as it stood for this match (the attacker's rating on an attack, the
    //defender's on an invasion). Replaying these in order reconstructs your ELO history,
    //which is where Peak ELO and the ELO trend come from.
    public int MyRating;

    //Did the invading side win? Read from the attacker's perspective regardless of panel.
    public bool DidAttackerWin;

    //Which defense layout was being raided (ArmyDivisionRank). Drives "breaches by layout".
    public ArmyDivisionRank DefenderRank = ArmyDivisionRank.None;

    /// <summary>Did YOU come out on top? Attacking = invaders won. Defending = invaders lost.</summary>
    public bool DidIWin => IsAttack ? DidAttackerWin : !DidAttackerWin;

    /// <summary>
    /// Damage as a percentage of the stake (0-100). Derived rather than stored so the
    /// "Damage: 20,235  21%" pair on the card can never disagree with itself.
    /// </summary>
    public int DamagePercent => Stake <= 0 ? 0 : MatchOutcome.ToPercent((float) DamageGold / Stake);

    public MatchOutcome.Badge Badge => MatchOutcome.For(IsAttack, DamagePercent);

    public bool HasReplay => !string.IsNullOrEmpty(PlaybackStreamId);

    /// <summary>"2:35.4" - matches the Duration format in the design.</summary>
    public string DurationDisplay
    {
        get
        {
            int minutes = Mathf.FloorToInt(DurationSeconds / 60f);
            float seconds = DurationSeconds - (minutes * 60f);
            return $"{minutes}:{seconds:00.0}";
        }
    }

    /// <summary>"~3 minutes ago" / "~4 hours ago" / "~1 day ago", as per the design.</summary>
    public string TimeAgoDisplay => FormatTimeAgo(DateTime.UtcNow - OccurredAtUtc);

    private static string FormatTimeAgo(TimeSpan span)
    {
        if (span.TotalSeconds < 0) span = TimeSpan.Zero;

        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"~{Plural((int) span.TotalMinutes, "minute")} ago";
        if (span.TotalHours < 24) return $"~{Plural((int) span.TotalHours, "hour")} ago";
        return $"~{Plural((int) span.TotalDays, "day")} ago";
    }

    private static string Plural(int count, string unit) => $"{count} {unit}{(count == 1 ? "" : "s")}";
}
