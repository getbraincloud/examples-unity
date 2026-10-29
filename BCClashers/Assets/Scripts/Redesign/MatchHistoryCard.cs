using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Redesign: one opponent card inside the "My Recent Attacks" / "Recent Invasions" panels.
///
/// Deliberately perspective-agnostic - the same prefab renders both panels. Everything that
/// differs between an attack and an invasion is resolved by MatchSummary/MatchOutcome, so
/// this class only binds data to widgets.
/// </summary>
public class MatchHistoryCard : MonoBehaviour
{
    [Header("Opponent")]
    public TMP_Text OpponentNameText;
    public TMP_Text OpponentRatingText;
    public TMP_Text TimeAgoText;
    public TMP_Text DurationText;

    [Header("Outcome Badge")]
    public TMP_Text BadgeLabelText;
    public Image BadgeBackground;

    [Header("Damage")]
    public TMP_Text DamageValueText;
    public TMP_Text DamagePercentText;
    [Tooltip("Image with Type=Filled; fillAmount is driven from the damage percentage.")]
    public Image DamageBarFill;

    [Header("Stakes")]
    public TMP_Text StakesText;

    [Header("Actions")]
    public Button WatchButton;
    public TMP_Text WatchButtonText;
    public Button AttackButton;

    private MatchSummary _summary;
    public MatchSummary Summary => _summary;

    private const string REPLAY_UNAVAILABLE_MESSAGE = "Replay not available";
    private const string WATCH_MESSAGE = "Watch";

    public void Bind(MatchSummary in_summary)
    {
        _summary = in_summary;

        OpponentNameText.text = in_summary.OpponentName;
        OpponentRatingText.text = $"ELO: {in_summary.OpponentRating.ToString("#,0")}";
        TimeAgoText.text = in_summary.TimeAgoDisplay;
        DurationText.text = $"Duration: {in_summary.DurationDisplay}";

        MatchOutcome.Badge badge = in_summary.Badge;
        BadgeLabelText.text = badge.Label;
        if (BadgeBackground)
        {
            BadgeBackground.color = badge.Color;
        }

        int percent = in_summary.DamagePercent;
        DamageValueText.text = $"Damage: {in_summary.DamageGold.ToString("#,0")}";
        DamagePercentText.text = $"{percent}%";
        if (DamageBarFill)
        {
            DamageBarFill.fillAmount = percent / 100f;
            //Tint the bar with the same swatch as the badge so the row reads as one outcome.
            DamageBarFill.color = badge.Color;
        }

        StakesText.text = $"Stakes: {in_summary.Stake.ToString("#,0")}";

        //A purged stream leaves the row intact but the replay unplayable.
        bool hasReplay = in_summary.HasReplay;
        if (WatchButton)
        {
            WatchButton.interactable = hasReplay;
        }
        if (WatchButtonText)
        {
            WatchButtonText.text = hasReplay ? WATCH_MESSAGE : REPLAY_UNAVAILABLE_MESSAGE;
        }

        //You can only re-target an opponent we still have a profile id for.
        if (AttackButton)
        {
            AttackButton.interactable = !string.IsNullOrEmpty(in_summary.OpponentProfileId);
        }
    }

    //Called from the card's Watch Button.
    public void WatchPressed()
    {
        if (_summary == null || !_summary.HasReplay) return;
        NetworkManager.Instance.ReadStreamById(_summary.PlaybackStreamId);
    }

    //Called from the card's Attack Button - jumps straight into a raid on this opponent.
    public void AttackPressed()
    {
        if (_summary == null || string.IsNullOrEmpty(_summary.OpponentProfileId)) return;
        MenuManager.Instance.AttackOpponentDirect(_summary);
    }
}
