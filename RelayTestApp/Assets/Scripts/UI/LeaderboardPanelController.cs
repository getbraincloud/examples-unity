using System.Collections.Generic;
using System.Text;
using BrainCloud;
using BrainCloud.JsonFx.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Global leaderboard browser — "Most Opponents Beaten" (points) / "Highest Coverage %"
/// sub-tabs, LIFETIME / QUARTERLY period pills, a top-10 list, and the local player's own
/// rank highlighted at the bottom (matching the Main Menu / Lobby LEADERBOARDS mockups).
/// Reused as-is on both the Main Menu and the Lobby screen.
/// </summary>
public class LeaderboardPanelController : MonoBehaviour
{
    private enum BoardKind { Points, Coverage }
    private enum Period { Lifetime, Quarterly }

    private const string POINTS_LIFETIME_ID = "CursorParty_Points";
    private const string POINTS_QUARTERLY_ID = "CursorParty_Points_Quarterly";
    private const string COVERAGE_LIFETIME_ID = "CursorParty_HighestCoverage";
    private const string COVERAGE_QUARTERLY_ID = "CursorParty_HighestCoverage_Quarterly";
    private const int TOP_COUNT = 10;

    [Header("Board tabs")]
    public Button PointsTabButton;
    public Button CoverageTabButton;

    [Header("Period pills")]
    public Button LifetimeButton;
    public Button QuarterlyButton;

    [Header("Content")]
    public TMP_Text RowsText;
    public TMP_Text OwnRowText;
    public TMP_Text StatusText;

    // Board-kind tabs reuse the shared accent blue (cpp: theme ButtonActive); period pills get
    // their own distinct green — cpp deliberately uses two different active colours here, not one.
    private static readonly Color ActiveColour = new Color(0.30f, 0.45f, 0.90f);
    private static readonly Color PeriodActiveColour = new Color(0.25f, 0.55f, 0.35f);
    private static readonly Color InactiveColour = new Color(0.20f, 0.22f, 0.30f);

    private BoardKind _boardKind = BoardKind.Points;
    private Period _period = Period.Lifetime;
    private int _fetchGeneration;

    private void OnEnable()
    {
        PointsTabButton.onClick.AddListener(() => SetBoardKind(BoardKind.Points));
        CoverageTabButton.onClick.AddListener(() => SetBoardKind(BoardKind.Coverage));
        LifetimeButton.onClick.AddListener(() => SetPeriod(Period.Lifetime));
        QuarterlyButton.onClick.AddListener(() => SetPeriod(Period.Quarterly));

        // Long scores (e.g. "10,000") were wrapping the right-aligned score onto its own line —
        // these rows are single-line by design, so let them overflow rather than wrap.
        if (RowsText != null) RowsText.enableWordWrapping = false;
        if (OwnRowText != null) OwnRowText.enableWordWrapping = false;

        UpdateTabHighlight();
        Refresh();
    }

    private static void SetSelected(Button button, bool selected, Color activeColour)
    {
        if (button == null) return;
        var colours = button.colors;
        colours.normalColor = selected ? activeColour : InactiveColour;
        button.colors = colours;
    }

    // The board/period buttons had no active-state feedback at all — fix so the selected
    // tab/pill is visually distinct, matching the cpp reference client's two-colour convention.
    private void UpdateTabHighlight()
    {
        SetSelected(PointsTabButton, _boardKind == BoardKind.Points, ActiveColour);
        SetSelected(CoverageTabButton, _boardKind == BoardKind.Coverage, ActiveColour);
        SetSelected(LifetimeButton, _period == Period.Lifetime, PeriodActiveColour);
        SetSelected(QuarterlyButton, _period == Period.Quarterly, PeriodActiveColour);
    }

    private void OnDisable()
    {
        PointsTabButton.onClick.RemoveAllListeners();
        CoverageTabButton.onClick.RemoveAllListeners();
        LifetimeButton.onClick.RemoveAllListeners();
        QuarterlyButton.onClick.RemoveAllListeners();
    }

    private void SetBoardKind(BoardKind kind)
    {
        if (_boardKind == kind) return;
        _boardKind = kind;
        UpdateTabHighlight();
        Refresh();
    }

    private void SetPeriod(Period period)
    {
        if (_period == period) return;
        _period = period;
        UpdateTabHighlight();
        Refresh();
    }

    private string CurrentLeaderboardId()
    {
        if (_boardKind == BoardKind.Points)
            return _period == Period.Lifetime ? POINTS_LIFETIME_ID : POINTS_QUARTERLY_ID;
        return _period == Period.Lifetime ? COVERAGE_LIFETIME_ID : COVERAGE_QUARTERLY_ID;
    }

    // Bumped on every Refresh() so a slow response from a since-abandoned tab/period can't
    // overwrite what the user is looking at now.
    private void Refresh()
    {
        int generation = ++_fetchGeneration;
        string leaderboardId = CurrentLeaderboardId();

        if (StatusText != null) StatusText.text = "Loading...";
        if (RowsText != null) RowsText.text = "";
        if (OwnRowText != null) OwnRowText.text = "";

        var svc = BrainCloudManager.Instance.Wrapper.SocialLeaderboardService;

        svc.GetGlobalLeaderboardPageIfExists(leaderboardId, BrainCloudSocialLeaderboard.SortOrder.HIGH_TO_LOW, 0, TOP_COUNT - 1,
            (jsonResponse, cbObject) =>
            {
                if (generation != _fetchGeneration) return;
                RenderTop(ParseLeaderboardEntries(jsonResponse));
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                if (generation != _fetchGeneration) return;
                if (StatusText != null) StatusText.text = "Leaderboard unavailable.";
            });

        svc.GetGlobalLeaderboardViewIfExists(leaderboardId, BrainCloudSocialLeaderboard.SortOrder.HIGH_TO_LOW, 0, 0,
            (jsonResponse, cbObject) =>
            {
                if (generation != _fetchGeneration) return;
                var entries = ParseLeaderboardEntries(jsonResponse);
                RenderOwnRow(entries.Count > 0 ? entries[0] : null);
            },
            (status, reasonCode, jsonError, cbObject) => { /* own rank just stays blank */ });
    }

    private struct Entry { public int Rank; public string Name; public long Score; public string ProfileId; }

    private static List<Entry> ParseLeaderboardEntries(string jsonResponse)
    {
        var result = new List<Entry>();
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        if (response == null || !response.ContainsKey("data")) return result;
        var data = response["data"] as Dictionary<string, object>;
        if (data == null || !data.ContainsKey("leaderboard") || !(data["leaderboard"] is object[] arr)) return result;

        foreach (var e in arr)
        {
            if (!(e is Dictionary<string, object> ed)) continue;
            result.Add(new Entry
            {
                Rank = ed.ContainsKey("rank") ? System.Convert.ToInt32(ed["rank"]) : 0,
                Name = ed.ContainsKey("name") ? ed["name"] as string : "?",
                Score = ed.ContainsKey("score") ? System.Convert.ToInt64(ed["score"]) : 0,
                ProfileId = ed.ContainsKey("playerId") ? ed["playerId"] as string : ""
            });
        }
        return result;
    }

    // Exact hex values from the cpp reference client's rankColorFor() (game.cpp/leaderboardPanel.cpp),
    // reused verbatim there across the leaderboard panel, in-match sidebar, and match summary.
    private static string RankColor(int rank)
    {
        switch (rank)
        {
            case 1: return "#FFD700"; // gold
            case 2: return "#BFBFBF"; // silver
            case 3: return "#CC8033"; // bronze
            default: return "#FFFFFF"; // plain white
        }
    }

    private void RenderTop(List<Entry> entries)
    {
        if (StatusText != null) StatusText.text = entries.Count == 0 ? "No scores yet." : "";
        if (RowsText == null) return;

        var sb = new StringBuilder();
        foreach (var e in entries)
        {
            sb.Append($"<color={RankColor(e.Rank)}>#{e.Rank}</color>  <b>{e.Name}</b>");
            sb.Append($"<pos=90%><align=right><b>{e.Score:N0}</b></align>");
            sb.AppendLine();
        }
        RowsText.text = sb.ToString();
    }

    private void RenderOwnRow(Entry? own)
    {
        if (OwnRowText == null) return;
        if (own == null)
        {
            OwnRowText.text = "";
            return;
        }
        var e = own.Value;
        OwnRowText.text = $"<color=#59ff73>#{e.Rank}  {e.Name} (You)   {e.Score:N0}</color>";
    }
}
