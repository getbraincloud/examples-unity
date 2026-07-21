using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Redesign: fills one of the dashboard's match-history lists with MatchHistoryCards.
///
/// One component drives BOTH panels - drop it on "My Recent Attacks" with IsAttackPanel = true
/// and on "Recent Invasions" with IsAttackPanel = false. The perspective is the only difference;
/// it decides which side of each match becomes "the opponent" and how the badge reads.
/// </summary>
public class MatchHistoryPanel : MonoBehaviour
{
    [Header("Perspective")]
    [Tooltip("true = 'My Recent Attacks' (matches I initiated). false = 'Recent Invasions' (matches launched at me).")]
    public bool IsAttackPanel;

    [Header("References")]
    public MatchHistoryCard CardPrefab;
    [Tooltip("Content transform of the scroll view the cards are instantiated under.")]
    public Transform ListParent;
    [Tooltip("Shown instead of the list when there is no history yet.")]
    public TMP_Text EmptyMessageText;

    [Header("Copy")]
    public string EmptyMessage = "No recent matches";

    [Header("Editor Preview")]
    [Tooltip("EDITOR ONLY. Fills the list with one sample row per outcome tier so the layout, " +
             "colours and badges can be tuned without playing matches - handy for Recent Invasions, " +
             "which otherwise needs a second account to raid you. Compiled out of builds.")]
    public bool PreviewWithSampleData;

    private readonly List<MatchHistoryCard> _cards = new List<MatchHistoryCard>();

    public void Populate(List<MatchSummary> in_matches)
    {
        //Preview wins over live data so the panel still shows every tier after the real
        //(likely empty) read lands. Application.isEditor is compile-time false in a build.
        if (PreviewWithSampleData && Application.isEditor)
        {
            in_matches = BuildSampleRows();
        }

        ClearCards();

        int count = in_matches?.Count ?? 0;

        if (EmptyMessageText)
        {
            EmptyMessageText.text = EmptyMessage;
            EmptyMessageText.gameObject.SetActive(count == 0);
        }

        if (count == 0) return;

        for (int i = 0; i < count; ++i)
        {
            MatchHistoryCard card = Instantiate(CardPrefab, ListParent);
            card.Bind(in_matches[i]);
            _cards.Add(card);
        }
    }

    /// <summary>
    /// Editor-only: one row per outcome tier, so every badge/colour/bar state is visible at once.
    /// Reads as an attack or an invasion depending on IsAttackPanel, exactly like live data.
    /// </summary>
    private List<MatchSummary> BuildSampleRows()
    {
        //percent of stake -> drives the badge tier (see MatchOutcome)
        var samples = new (string name, int rating, int pct, int stake, double minutesAgo, float duration)[]
        {
            ("Dave B.", 1200, 5,   100000, 3,    35.4f),
            ("Rick M",  1173, 35,  100000, 10,   72.0f),
            ("John H",  1235, 60,  100000, 240,  155.4f),
            ("Paul W",  1600, 85,  200000, 480,  105.4f),
            ("Mina E",  1000, 100, 100000, 1440, 180.0f),
        };

        var rows = new List<MatchSummary>();
        foreach (var s in samples)
        {
            rows.Add(new MatchSummary
            {
                IsAttack = IsAttackPanel,
                OpponentName = s.name,
                OpponentRating = s.rating,
                OpponentProfileId = "",                       // no real target -> Attack button disables itself
                Stake = s.stake,
                DamageGold = Mathf.RoundToInt(s.stake * (s.pct / 100f)),
                DurationSeconds = s.duration,
                OccurredAtUtc = DateTime.UtcNow.AddMinutes(-s.minutesAgo),
                PlaybackStreamId = "",                        // no real stream -> shows "Replay not available"
            });
        }
        return rows;
    }

    private void ClearCards()
    {
        for (int i = _cards.Count - 1; i >= 0; --i)
        {
            if (_cards[i])
            {
                Destroy(_cards[i].gameObject);
            }
        }
        _cards.Clear();
    }
}
