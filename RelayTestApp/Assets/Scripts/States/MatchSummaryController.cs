using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Post-match results + rematch-queue screen. Auto-rematches after
/// MATCH_SUMMARY_REMATCH_MS, or sooner if everyone queues up.</summary>
public class MatchSummaryController : GameState
{
    [Header("Prefab / parent")]
    public MatchSummaryEntry EntryPrefab;
    public Transform EntriesParent;

    [Header("UI references")]
    public TMP_Text LobbyInfoText;
    public TMP_Text WinnerText;
    public TMP_Text CountdownText;
    public Button RematchButton;
    public TMP_Text RematchButtonText;
    public Button MainMenuButton;

    private readonly List<MatchSummaryEntry> _entries = new List<MatchSummaryEntry>();
    private float _clockSec;
    private bool _fallbackApplied;

    private void OnEnable()
    {
        _clockSec = 0f;
        _fallbackApplied = false;
        RematchButton.onClick.AddListener(OnRematchClicked);
        MainMenuButton.onClick.AddListener(OnMainMenuClicked);
        RefreshResults();
    }

    private void OnDisable()
    {
        RematchButton.onClick.RemoveListener(OnRematchClicked);
        MainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
    }

    private void Update()
    {
        _clockSec += Time.deltaTime;
        long elapsedMs = (long)(_clockSec * 1000f);

        long remainingMs = Math.Max(0, StateManager.MATCH_SUMMARY_REMATCH_MS - elapsedMs);
        long remainingSec = (remainingMs + 999) / 1000;
        if (CountdownText != null)
            CountdownText.text = $"Next Round: {remainingSec / 60}:{(remainingSec % 60):D2}";

        // Auto-queue if the player hasn't clicked "Queue for Rematch" themselves in time.
        if (!StateManager.Instance.LocalWantsRematch && elapsedMs >= StateManager.MATCH_SUMMARY_REMATCH_MS)
        {
            SetLocalWantsRematch(true);
        }

        if (!_fallbackApplied && elapsedMs >= StateManager.LB_RESULT_FALLBACK_MS)
        {
            _fallbackApplied = true;
            RefreshResults();
        }
    }

    /// <summary>Rebuilds the winner banner + player cards. Called on open, on new result data,
    /// and on every lobby event so the ready-count stays live.</summary>
    public void RefreshResults()
    {
        var sm = StateManager.Instance;
        Lobby lobby = sm.CurrentLobby;

        if (LobbyInfoText != null)
            LobbyInfoText.text = lobby != null ? $"Lobby {lobby.LobbyID} - {lobby.Members.Count} Players" : "";

        var sorted = sm.MatchResults.Values.OrderBy(e => e.Rank).ToList();
        if (WinnerText != null)
        {
            if (sorted.Count > 0)
            {
                var winner = sorted[0];
                WinnerText.text = $"{FindMemberName(lobby, winner.CxId)} wins the round, covering {winner.CoveragePct:F0}% of the board.";
            }
            else
            {
                WinnerText.text = "Waiting for results...";
            }
        }

        foreach (var e in _entries) if (e != null) Destroy(e.gameObject);
        _entries.Clear();

        foreach (var result in sorted)
        {
            var entry = Instantiate(EntryPrefab, EntriesParent);
            bool isMe = result.ProfileId == GameManager.Instance.CurrentUserInfo.ProfileID;
            string name = FindMemberName(lobby, result.CxId);
            entry.Setup(result, name, isMe);
            sm.LbResults.TryGetValue(result.ProfileId, out var lbDelta);
            entry.ApplyLbResult(lbDelta, _fallbackApplied);
            _entries.Add(entry);
        }

        RefreshRematchButtonLabel();
    }

    private static string FindMemberName(Lobby lobby, string cxId)
    {
        if (lobby == null) return "?";
        foreach (var m in lobby.Members) if (m.cxId == cxId) return m.Username;
        return "?";
    }

    private void RefreshRematchButtonLabel()
    {
        var sm = StateManager.Instance;
        Lobby lobby = sm.CurrentLobby;
        int total = lobby != null ? lobby.Members.Count : 0;
        int readyCount = 0;
        if (lobby != null)
        {
            foreach (var m in lobby.Members)
            {
                // Our own row uses the local optimistic flag — the lobby snapshot is briefly stale here.
                bool ready = m.ProfileID == GameManager.Instance.CurrentUserInfo.ProfileID
                    ? sm.LocalWantsRematch
                    : m.IsReady;
                if (ready) readyCount++;
            }
        }
        if (RematchButtonText != null)
            RematchButtonText.text = (sm.LocalWantsRematch ? "Queued for Rematch" : "Queue for Rematch") + $"  {readyCount}/{total}";
    }

    private void OnRematchClicked()
    {
        SetLocalWantsRematch(!StateManager.Instance.LocalWantsRematch);
    }

    private void SetLocalWantsRematch(bool ready)
    {
        StateManager.Instance.LocalWantsRematch = ready;
        RefreshRematchButtonLabel();

        var extra = new Dictionary<string, object>
        {
            ["colorIndex"] = GameManager.Instance.CurrentUserInfo.UserGameColor,
            ["presentSinceStart"] = GameManager.Instance.CurrentUserInfo.PresentSinceStart
        };
        BrainCloudManager.Instance.Wrapper.LobbyService.UpdateReady(
            StateManager.Instance.CurrentLobby.LobbyID, ready, extra, null, null);

        if (GameManager.Instance.IsLocalUserHost())
        {
            TryHostStartNextRound();
        }
    }

    // Host-only: starts round 2 (same path as round 1) once everyone's queued for a rematch.
    private void TryHostStartNextRound()
    {
        Lobby lobby = StateManager.Instance.CurrentLobby;
        if (lobby == null || lobby.Members.Count == 0) return;
        foreach (var m in lobby.Members)
        {
            bool ready = m.ProfileID == GameManager.Instance.CurrentUserInfo.ProfileID
                ? StateManager.Instance.LocalWantsRematch
                : m.IsReady;
            if (!ready) return;
        }
        StateManager.Instance.ButtonPressed_ChangeState(GameStates.Lobby);
    }

    private void OnMainMenuClicked()
    {
        StateManager.Instance.LeaveToMainMenu();
    }
}
