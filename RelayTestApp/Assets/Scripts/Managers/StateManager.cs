using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrainCloud;

/// <summary>
/// - Responsible for switching states from either button events or loading events
/// - If there's an error, it will give an error pop up and send you back to Sign In game state.
/// - Holding information such as
///     - What state I'm currently in
///     - Transition from State to State
///     - Clean up game objects when game is finished
///     - Info about Server and Lobby
/// </summary>

public enum GameStates { SignIn, MainMenu, Lobby, Match, Connecting, Reconnecting, MatchSummary }

/// <summary>Match lifecycle phase (parity with cpp/react/Godot RelayTestApp clients).</summary>
public enum MatchPhase { Running, ResultsBroadcast, Ended }

/// <summary>One leaderboard period's before/after rank for a single board (points or coverage,
/// lifetime or quarterly). Absent/not-improved means "no change" — matches the other RelayTestApp
/// clients, which likewise only ever transmit a period when it actually improved.</summary>
[Serializable]
public class LeaderboardPeriodDelta
{
    public bool Improved;
    public int RankBefore = -1;
    public int RankAfter = -1;
}

/// <summary>One player's result for the round that just ended (rank/coverage, not yet the
/// leaderboard delta — that arrives later, separately, as an LbResultEntry).</summary>
[Serializable]
public class MatchResultEntry
{
    public string ProfileId;
    public string CxId;
    public int ColorIndex;
    public float CoveragePct;
    public int Rank;
    public int Beaten;
}

/// <summary>One player's PostMatchResults leaderboard deltas, across all 4 boards. Ready=true
/// as soon as this exists at all — even if none of the 4 periods actually improved, so the
/// Match Summary screen can tell "no change" apart from "still waiting."</summary>
[Serializable]
public class ChatMessage
{
    public string MsgId;
    public string FromCxId;
    public string FromName;
    public string Text;
}

[Serializable]
public class LbResultEntry
{
    public bool Ready;
    public LeaderboardPeriodDelta PointsLifetime = new LeaderboardPeriodDelta();
    public LeaderboardPeriodDelta PointsQuarterly = new LeaderboardPeriodDelta();
    public LeaderboardPeriodDelta CoverageLifetime = new LeaderboardPeriodDelta();
    public LeaderboardPeriodDelta CoverageQuarterly = new LeaderboardPeriodDelta();
}

/// <summary>Splotch record kept for host-to-client JIP canvas sync.</summary>
[Serializable]
public struct SplotchRecord
{
    public Vector2 Position;
    public int ColorIndex;
    public TeamCodes TeamCode;
    public TeamCodes InstigatorCode;
    public long StartTimeMs;
    // Painter's cxId, for Coverage attribution (so shared colours don't merge). Empty for
    // JIP-synced splotches; Coverage falls back to ColorIndex there.
    public string SenderCxId;
    // Chosen once by the sender, carried on the wire — never re-rolled on receive/sync.
    public float Angle;
}

public class StateManager : MonoBehaviour
{
    //Game States
    public List<GameState> ListOfStates = new List<GameState>();
    public GameStates CurrentGameState;
    public ConnectingGameState LoadingGameState;
    public DialogueMessage ErrorMessage;
    public GameObject LobbyFFAView;
    public GameObject LobbyTeamView;
    public GameObject MatchFFAView;
    public GameObject MatchTeamView;
    public GameObject DisconnectButtonGroup;
    //List of users to keep as reference for players that disconnect then reconnect
    public List<UserInfo> SessionPlayers = new List<UserInfo>();
    //Network info needed
    [SerializeField]
    public Lobby CurrentLobby;
    [SerializeField]
    public Server CurrentServer;
    internal RelayConnectionType Protocol { get; set; }
    // WSS = WEBSOCKET + this flag (RelayConnectOptions.ssl), not its own enum value.
    internal bool UseSSL { get; set; }

    //Specific for loading and waiting
    public bool isReady;
    public bool isLoading;

    // Match timer — fixed duration, host-authoritative, synced off one shared epoch.
    public const long MATCH_DURATION_MS = 90000;
    public MatchPhase CurrentMatchPhase = MatchPhase.Running;
    public long MatchStartTimeMs = 0;

    // Match result / leaderboard posting + rematch queue.
    public const long RESULT_GRACE_MS = 3000;         // match_result -> EndMatch(), regardless of the script
    public const long LB_RESULT_FALLBACK_MS = 8000;   // "Updating..." -> "unavailable" backstop
    public const long MATCH_SUMMARY_REMATCH_MS = 45000;

    // Keyed by profileId — survives CurrentLobby getting fully rebuilt on lobby events.
    public Dictionary<string, MatchResultEntry> MatchResults = new Dictionary<string, MatchResultEntry>();
    public Dictionary<string, LbResultEntry> LbResults = new Dictionary<string, LbResultEntry>();
    // lb_result chunks that beat their match_result entry into existence.
    public Dictionary<string, LbResultEntry> PendingLbResults = new Dictionary<string, LbResultEntry>();

    // Local optimistic "queued for rematch" flag — the server-echoed list lags after END_MATCH.
    public bool LocalWantsRematch = false;
    public long MatchResultSentAtMs = 0;
    public long MatchSummaryArrivalTimeMs = 0;

    // Live in-match rank/coverage sidebar, broadcast ~1x/sec (separate from match_result).
    public List<Coverage.Entry> LiveCoverage = new List<Coverage.Entry>();

    // Chat history — kept here so it survives a chat panel being disabled/re-enabled.
    public List<ChatMessage> GlobalChatHistory = new List<ChatMessage>();
    public List<ChatMessage> LobbyChatHistory = new List<ChatMessage>();

    //Used to clean up objects when game is finished
    public List<GameObject> Splatters = new List<GameObject>();

    // Full splotch history — host reads this to sync a JIP player
    public List<SplotchRecord> AllSplotches = new List<SplotchRecord>();
    // Pending splotches received via splotch_sync — processed by GameArea each frame
    public List<SplotchRecord> PendingSyncSplotches = new List<SplotchRecord>();
    // Set true on the first sync chunk so GameArea clears the canvas before rebuilding
    public bool PendingSyncIsFirst = false;
    // Set true when host broadcasts a canvas clear
    public bool PendingClearSplatters = false;

    //Messages for loading screen
    private const string LOGGING_IN_MESSAGE = "Logging in...";
    private const string LOOKING_FOR_LOBBY_MESSAGE = "Joining Lobby...";
    private const string JOINING_MATCH_MESSAGE = "Joining Match...";

    //Singleton
    private static StateManager _instance;
    public static StateManager Instance => _instance;

    protected virtual void Awake()
    {
        if (!_instance)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateDisconnectButtons(false);


    }

    public void AutoSignIn()
    {
        UnityEngine.Debug.Log("STARTED WrapperName: " + BrainCloudManager.Instance.Wrapper.WrapperName);
        if (BrainCloudManager.Instance.Wrapper.CanReconnect())
        {
            ButtonPressed_ChangeState(GameStates.Reconnecting);
        }
        else
        {
            ChangeState(GameStates.SignIn);
        }
    }

    public void AbortToSignIn(string errorMessage)
    {
        ErrorMessage.SetUpPopUpMessage(errorMessage);
        LoadingGameState.CancelNextState = true;
        ChangeState(GameStates.SignIn);
    }

    public void PopupMessageToMainMenu(string message)
    {
        ErrorMessage.SetUpPopUpMessage(message);
        LoadingGameState.CancelNextState = true;
        ChangeState(GameStates.MainMenu);
        ResetData();
        ClearMatchResultsData();
    }

    public void LeaveToMainMenu()
    {
        StartCoroutine(DelayToDisconnect());
    }

    IEnumerator DelayToDisconnect()
    {

        yield return new WaitForSeconds(0.2f);
        GameManager.Instance.LobbyIdText.enabled = false;
        BrainCloudManager.Instance.CloseGame();
        BrainCloudManager.Instance.LeaveLobby();
        ChangeState(GameStates.MainMenu);
        ResetData();
        ClearMatchResultsData();
        yield return new WaitForFixedUpdate();
    }

    public void LeaveMatchBackToMenu()
    {
        GameManager.Instance.LobbyIdText.enabled = false;
        ResetData();
        ClearMatchResultsData();
        ChangeState(GameStates.SignIn);
    }

    public void ResetData()
    {
        CurrentServer = null;
        isReady = false;

        foreach (GameObject splatter in Splatters)
        {
            if (splatter != null)
            {
                Destroy(splatter);
            }
        }
        Splatters = new List<GameObject>();
        AllSplotches.Clear();
        PendingSyncSplotches.Clear();
        PendingSyncIsFirst = false;
        PendingClearSplatters = false;
        CurrentMatchPhase = MatchPhase.Running;
        MatchStartTimeMs = 0;
        LiveCoverage.Clear();
        GameManager.Instance.EmptyCursorList();
        GameManager.Instance.CurrentUserInfo.IsAlive = false;
        GameManager.Instance.CurrentUserInfo.MousePosition = Vector2.zero;
    }

    // Kept out of ResetData() on purpose — clearing these there raced the Match Summary screen
    // back to empty. Call only when actually leaving (main menu / sign-in), not into Summary.
    public void ClearMatchResultsData()
    {
        MatchResults.Clear();
        LbResults.Clear();
        PendingLbResults.Clear();
        LocalWantsRematch = false;
        MatchResultSentAtMs = 0;
    }

    //Takes in the current Game state to then load into the next game state
    public void ButtonPressed_ChangeState(GameStates newState = GameStates.Connecting)
    {
        foreach (GameState state in ListOfStates)
        {
            state.gameObject.SetActive(false);
        }

        if (newState != GameStates.Connecting)
        {
            CurrentGameState = newState;
        }
        if (newState == GameStates.Lobby || newState == GameStates.Match)
        {
            EnableCurrentGameModeScreen();
        }

        isLoading = true;
        //User is in this state and moving onto the next
        switch (CurrentGameState)
        {
            case GameStates.Reconnecting:
                BrainCloudManager.Instance.AuthenticateReconnect();
                CheckToEnableReconnectButton();
                CurrentGameState = GameStates.MainMenu;
                LoadingGameState.ConnectStatesWithLoading(LOGGING_IN_MESSAGE, false, GameStates.MainMenu);
                break;
            //Logging In...
            case GameStates.SignIn:
                CurrentGameState = GameStates.MainMenu;
                CheckToEnableReconnectButton();
                BrainCloudManager.Instance.Login();
                LoadingGameState.ConnectStatesWithLoading(LOGGING_IN_MESSAGE, false, GameStates.MainMenu);
                break;
            //Looking for Lobby...
            case GameStates.MainMenu:
                CurrentGameState = GameStates.Lobby;
                BrainCloudManager.Instance.FindLobby(Protocol);
                LoadingGameState.ConnectStatesWithLoading(LOOKING_FOR_LOBBY_MESSAGE, true, CurrentGameState);
                break;
            //Setting up Match...
            case GameStates.Lobby:
                CurrentGameState = GameStates.Match;
                BrainCloudManager.Instance.StartGame();
                LoadingGameState.ConnectStatesWithLoading(JOINING_MATCH_MESSAGE, false, CurrentGameState);
                break;
        }
    }

    private void CheckToEnableReconnectButton()
    {
        if (CurrentLobby != null && CurrentLobby.LobbyID.Length > 0)
        {
            GameManager.Instance.ReconnectButton.gameObject.SetActive(true);
        }
        else
        {
            GameManager.Instance.ReconnectButton.gameObject.SetActive(false);
        }
    }

    private void EnableCurrentGameModeScreen()
    {

        if (CurrentGameState == GameStates.Lobby)
        {
            if (GameManager.Instance.GameMode == GameMode.FreeForAll)
            {
                LobbyTeamView.SetActive(false);
                LobbyFFAView.SetActive(true);
            }
            else
            {
                LobbyTeamView.SetActive(true);
                LobbyFFAView.SetActive(false);
            }
        }
        else if (CurrentGameState == GameStates.Match)
        {
            if (GameManager.Instance.GameMode == GameMode.FreeForAll)
            {
                MatchTeamView.SetActive(false);
                MatchFFAView.SetActive(true);
            }
            else
            {
                MatchTeamView.SetActive(true);
                MatchFFAView.SetActive(false);
            }
        }
    }

    public void ReconnectToGame()
    {
        foreach (GameState state in ListOfStates)
        {
            state.gameObject.SetActive(false);
        }

        CurrentGameState = GameStates.Match;
        isLoading = true;
        LoadingGameState.ConnectStatesWithLoading(JOINING_MATCH_MESSAGE, false, GameStates.Match);

        BrainCloudManager.Instance.ReconnectUserToLobby();
    }

    public void UpdateDisconnectButtons(bool isEnabled)
    {
        DisconnectButtonGroup.SetActive(isEnabled);
    }

    public void ChangeState(GameStates newGameState)
    {
        CurrentGameState = newGameState;

        if (newGameState == GameStates.MainMenu)
        {
            CheckToEnableReconnectButton();
        }
        if (newGameState == GameStates.Lobby || newGameState == GameStates.Match)
        {
            EnableCurrentGameModeScreen();
        }
        foreach (GameState currentState in ListOfStates)
        {
            currentState.gameObject.SetActive(currentState.CurrentGameState == newGameState);
        }
    }

    public void CheckPlayerReconnecting(string in_cxId)
    {
        List<string> listOfIds = new List<string>();
        for (int i = 0; i < CurrentLobby.Members.Count; i++)
        {
            listOfIds.Add(CurrentLobby.Members[i].cxId);
        }

        for (int i = 0; i < SessionPlayers.Count; i++)
        {
            if (in_cxId.Equals(SessionPlayers[i].cxId))
            {
                for (int j = 0; j < CurrentLobby.Members.Count; j++)
                {
                    if (CurrentLobby.Members[j].cxId.Equals(in_cxId))
                    {
                        CurrentLobby.Members[j].IsAlive = true;
                    }
                }
            }
        }
    }
}
