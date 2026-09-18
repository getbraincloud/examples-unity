using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One row on the Match Summary screen: rank, colour, name, coverage %, points pill,
/// and a rank-up/personal-best pill once the leaderboard delta lands.</summary>
public class MatchSummaryEntry : MonoBehaviour
{
    [Header("Header row")]
    public TMP_Text RankText;
    public Image ColourDot;
    public TMP_Text NameText;
    public TMP_Text CoverageText;

    [Header("Points pill")]
    public TMP_Text PointsPillText;

    [Header("Leaderboard result — exactly one of these is shown at a time")]
    public GameObject UpdatingLabel; // "Updating leaderboards..." / "Leaderboard unavailable"
    public TMP_Text UpdatingLabelText;
    public GameObject RankUpPill;
    public TMP_Text RankUpPillText;
    public GameObject PersonalBestPill;
    public TMP_Text PersonalBestPillText;
    public GameObject NoChangeLabel;

    public string ProfileId { get; private set; }

    public void Setup(MatchResultEntry result, string name, bool isMe)
    {
        ProfileId = result.ProfileId;

        RankText.text = "#" + result.Rank;
        RankText.color = result.Rank == 1 ? new Color(1f, 0.84f, 0f) : Color.white;
        if (ColourDot != null) ColourDot.color = GameManager.ReturnUserColor(result.ColorIndex);

        Color nameColor = isMe ? new Color(0.35f, 1f, 0.45f) : Color.white;
        NameText.text = name + (isMe ? " (YOU)" : "");
        NameText.color = nameColor;
        CoverageText.text = $"{result.CoveragePct:F0}%   COVERAGE";
        CoverageText.color = nameColor;

        PointsPillText.text = $"+{result.Beaten + 1} pts  ({result.Beaten} beaten + 1 for playing)";

        ApplyLbResult(null, false);
    }

    /// <summary>timedOut: no leaderboard delta after LB_RESULT_FALLBACK_MS — show "unavailable"
    /// instead of hanging on "Updating...".</summary>
    public void ApplyLbResult(LbResultEntry delta, bool timedOut)
    {
        bool ready = delta != null && delta.Ready;
        UpdatingLabel.SetActive(!ready);
        RankUpPill.SetActive(false);
        PersonalBestPill.SetActive(false);
        NoChangeLabel.SetActive(false);

        if (!ready)
        {
            if (UpdatingLabelText != null)
                UpdatingLabelText.text = timedOut ? "Leaderboard unavailable" : "Updating leaderboards...";
            return;
        }

        bool anyPointsUp = delta.PointsLifetime.Improved || delta.PointsQuarterly.Improved;
        bool anyCoverageBest = delta.CoverageLifetime.Improved || delta.CoverageQuarterly.Improved;

        RankUpPill.SetActive(anyPointsUp);
        if (anyPointsUp)
            RankUpPillText.text = "^ Rank up - Opponents Beaten   " + PeriodsText(delta.PointsLifetime, delta.PointsQuarterly);

        PersonalBestPill.SetActive(anyCoverageBest);
        if (anyCoverageBest)
            PersonalBestPillText.text = "* Personal best - Coverage %   " + PeriodsText(delta.CoverageLifetime, delta.CoverageQuarterly);

        NoChangeLabel.SetActive(!anyPointsUp && !anyCoverageBest);
    }

    // "Lifetime #before -> #after   Quarterly #before -> #after", omitting periods that didn't improve
    private static string PeriodsText(LeaderboardPeriodDelta lifetime, LeaderboardPeriodDelta quarterly)
    {
        var parts = new List<string>();
        if (lifetime.Improved)
            parts.Add($"Lifetime #{(lifetime.RankBefore < 0 ? "-" : lifetime.RankBefore.ToString())} -> #{lifetime.RankAfter}");
        if (quarterly.Improved)
            parts.Add($"Quarterly #{(quarterly.RankBefore < 0 ? "-" : quarterly.RankBefore.ToString())} -> #{quarterly.RankAfter}");
        return string.Join("   |   ", parts);
    }
}
