using BrainCloud;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

public class TicTacToe : GameScene
{
    #region Public Variables
    public GameObject PlayerO;
    public GameObject PlayerX;
    public GameObject DuringGameDisplay;
    public GameObject AfterGameDisplay;
    public GameObject PlayAgainButton;
    public GameObject AskToRematchScreen;
    public GameObject ErrorMessageScreen;
    public TMP_Text ErrorMessageText;
    #endregion
    //Used to know if this object is alive in scene
    private bool _isActive;
    private const int MAX_CHARS = 10;

    private void Start()
    {
        App.Winner = 0;

        var parent = gameObject.transform.parent.gameObject;

        parent.transform.localPosition = new Vector3(parent.transform.localPosition.x + App.Offset,
            parent.transform.localPosition.y, parent.transform.localPosition.z);

        for (var i = 0; i < _tokenPositions.Length; i++) _tokenPositions[i].x += App.Offset;

        parent.GetComponentInChildren<Camera>().rect = App.ViewportRect;
        
        // Read the state and assembly the board
        BuildBoardFromState(App.BoardState);
        
        _isActive = true;
        ErrorMessageScreen.SetActive(false);
        AskToRematchScreen.SetActive(false);
        // also updates _winner status
        updateHud();
        enableRTT();
    }

    private void OnDestroy()
    {
        _isActive = false;
    }

    public void onReturnToMainMenu()
    {
        App.GotoMatchSelectScene(gameObject);
    }

    public static string Truncate(string value, int maxChars)
    {
        return value.Length <= maxChars ? value : value.Substring(0, maxChars) + (char)0x2026; // Ellipsis char
    }

    private void updateHud(bool updateNames = true)
    {
        // Check we if are not seeing a done match
        App.Winner = BoardUtility.CheckForWinner();
        
        // Read match history
        if (_history == null && App.Winner != 0)
        {
            _turnPlayed = true;
            App.Bc.AsyncMatchService
                .ReadMatchHistory(App.OwnerId, App.MatchId, OnReadMatchHistory);
        }

        enableDuringGameDisplay(App.Winner == 0);

        Transform[] toCheckDisplay = { DuringGameDisplay.transform, AfterGameDisplay.transform };
        //Game is finished
        if (App.Winner != 0)
        {
            App.IsAskingToRematch = false;
            App.AskedToRematch = false;
        }
        if (DuringGameDisplay.activeInHierarchy)
        {
            TextMeshProUGUI status = toCheckDisplay[0].Find("StatusOverlay").Find("StatusText").GetComponent<TextMeshProUGUI>();
            // update the during Game Display
            status.text = App.Winner != 0 ? App.Winner == -1 ? "Match Tied" : "Match Completed" :
                                            (App.WhosTurn == App.PlayerInfoX && App.CurrentMatch.yourToken == "X" ||
                                             App.WhosTurn == App.PlayerInfoO && App.CurrentMatch.yourToken == "O") ? "Your Turn" :
                                             Truncate(App.WhosTurn.PlayerName, MAX_CHARS) + "'s Turn";
        }
        else
        {
            Transform statusOverlay = toCheckDisplay[1].Find("StatusOverlay");
            TextMeshProUGUI status = statusOverlay.Find("StatusText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI statusOutline = statusOverlay.Find("StatusTextOutline").GetComponent<TextMeshProUGUI>();
            if (App.Winner < 0)
            {
                status.text = "Game Tied!";
            }
            else if (App.Winner > 0)
            {
                if (App.Winner == 1)
                {
                    status.text = Truncate(App.PlayerInfoX.PlayerName, MAX_CHARS) + " Wins!";
                    App.WinnerInfo = App.PlayerInfoX;
                    App.LoserInfo = App.PlayerInfoO;
                }
                else
                {
                    status.text = Truncate(App.PlayerInfoO.PlayerName, MAX_CHARS) + " Wins!";
                    App.WinnerInfo = App.PlayerInfoO;
                    App.LoserInfo = App.PlayerInfoX;
                }
                if (!App.CurrentMatch.scoreSubmitted)
                {
                    if (App.WinnerInfo.ProfileId == App.ProfileId)
                    {
                        App.CheckAchievements();
                        App.PostToLeaderboard();
                    }

                    App.CurrentMatch.scoreSubmitted = true;

                    var jsonData = new Dictionary<string, object> { { "scoreSubmitted", true },
                                                                    { "opponentProfileID", App.ProfileId },
                                                                    { "opponentName", App.Name },
                                                                    { "matchID", App.MatchId },
                                                                    { "ownerID", App.OwnerId } };

                    //Send event to opponent to prompt them to play again
                    App.Bc.EventService.SendEvent(App.CurrentMatch.matchedProfile.ProfileId, "gameConcluded", JsonWriter.Serialize(jsonData));
                }
            }
            else
            {
                status.text = Truncate(App.WhosTurn.PlayerName, MAX_CHARS) + " Turn";
            }
            statusOutline.text = status.text;
        }

        if (updateNames)
        {
            Transform playerVsOpp;
            TextMeshProUGUI playerXName, playerXNameOutline, playerOName, playerONameOutline;
            // update the names
            for (int i = 0; i < toCheckDisplay.Length; ++i)
            {
                playerVsOpp = toCheckDisplay[i].Find("PlayerVSOpponent");
                playerXName = playerVsOpp.Find("PlayerName").GetComponent<TextMeshProUGUI>();
                playerXNameOutline = playerVsOpp.Find("PlayerNameOutline").GetComponent<TextMeshProUGUI>();
                playerXName.text = Truncate(App.PlayerInfoX.PlayerName, MAX_CHARS);
                playerXNameOutline.text = playerXName.text;

                playerOName = playerVsOpp.Find("OpponentName").GetComponent<TextMeshProUGUI>();
                playerONameOutline = playerVsOpp.Find("OpponentNameOutline").GetComponent<TextMeshProUGUI>();
                playerOName.text = Truncate(App.PlayerInfoO.PlayerName, MAX_CHARS);
                playerONameOutline.text = playerOName.text;
            }
        }
    }
    
    // Enable RTT 
    public void enableRTT()
    {
        // Only Enable RTT if its not already started
        if (!App.Bc.RTTService.IsRTTEnabled())
        {
            App.Bc.RTTService.EnableRTT(RTTConnectionType.WEBSOCKET, onRTTEnabled, onRTTFailure);
            App.Bc.RTTService.RegisterRTTEventCallback(App.RTTEventCallback);
        }
        else
        {
            // its already started, lets call our success delegate 
            onRTTEnabled("", null);
        }
    }
    
    // rtt enabled, ensure we now request the updated match state
    private void onRTTEnabled(string responseData, object cbPostObject)
    {
        // LISTEN TO THE ASYNC CALLS, when we get one of these calls, lets just refresh 
        // match state
        queryMatchState();

        App.Bc.RTTService.RegisterRTTAsyncMatchCallback(queryMatchStateRTT);
    }
    
    private void onRTTFailure(int status, int reasonCode, string responseData, object cbPostObject)
    {
        //Failure to connect to RTT so we display a dialog window to inform the user
        //A button will be on the dialog that will direct them to enableRTT()
        ErrorMessageText.text = "Error: Poor Connection. \n Try Again ?";
        ErrorMessageScreen.SetActive(true);
    }

    // the listener, can parse the json and request just the updated match 
    // in this example, just re-request it all
    private void queryMatchStateRTT(string in_json)
    {
        queryMatchState();
    }

    private void queryMatchState()
    {
        // Read match history
        // Query more detail state about the match
        App.Bc.AsyncMatchService
            .ReadMatch(App.OwnerId, App.MatchId, (response, cbObject) =>
            {
                var match = App.CurrentMatch;
                var data = JsonReader.Deserialize<Dictionary<string, object>>(response)["data"] as Dictionary<string, object>;

                int newVersion = int.Parse(data["version"].ToString());

                if (App.MatchVersion + 1 < (ulong)newVersion)
                {
                    App.MatchVersion = (ulong)newVersion;

                    // Setup a couple stuff into our TicTacToe scene
                    App.BoardState = (string)(data["matchState"] as Dictionary<string, object>)["board"];
                    App.PlayerInfoX = match.playerXInfo;
                    App.PlayerInfoO = match.playerOInfo;
                    App.WhosTurn = match.yourToken == "X" ? App.PlayerInfoX : match.playerOInfo;
                    App.OwnerId = match.ownerId;
                    App.MatchId = match.matchId;
                    
                    //Checking if game is completed to assign winner and loser info
                    BuildBoardFromState(App.BoardState);
                    App.Winner = BoardUtility.CheckForWinner();
                    if (App.Winner != 0)
                    {
                        Transform[] toCheckDisplay = { DuringGameDisplay.transform, AfterGameDisplay.transform };
                        Transform statusOverlay = toCheckDisplay[1].Find("StatusOverlay");
                        TextMeshProUGUI status = statusOverlay.Find("StatusText").GetComponent<TextMeshProUGUI>();
                        if (App.Winner == 1)
                        {
                            status.text = Truncate(App.PlayerInfoX.PlayerName, MAX_CHARS) + " Wins!";
                            App.WinnerInfo = App.PlayerInfoX;
                            App.LoserInfo = App.PlayerInfoO;
                        }
                        else
                        {
                            status.text = Truncate(App.PlayerInfoO.PlayerName, MAX_CHARS) + " Wins!";
                            App.WinnerInfo = App.PlayerInfoO;
                            App.LoserInfo = App.PlayerInfoX;
                        }
                    }
                    
                    // Load the Tic Tac Toe scene
                    if (this != null && this.gameObject != null)
                        App.GotoTicTacToeScene(gameObject);
                }
            });
    }

    private void OnReadMatchHistory(string responseData, object cbPostObject)
    {
        var turns = (JsonReader.Deserialize<Dictionary<string, object>>(responseData)
                        ["data"] as Dictionary<string, object>)
                        ["turns"] as Dictionary<string, object>[];

        _history = new List<string>();
        for (var i = 0; i < turns.Length; ++i)
        {
            var turn = turns[i];
            var turnState = (string)(turn["matchState"] as Dictionary<string, object>)["board"];
            _history.Add(turnState);
        }
    }

    private void AddToken(int index, string token)
    {
        GridObjList.Add(Instantiate(token == "X" ? PlayerX : PlayerO, _tokenPositions[index],
            Quaternion.Euler(Random.Range(-7.0f, 7.0f), Random.Range(-7.0f, 7.0f), Random.Range(-7.0f, 7.0f))));
        GridObjList.Last().transform.parent = gameObject.transform;
        BoardUtility.Grid[index] = token == "X" ? 1 : 2;
    }

    public void PlayTurn(int index, PlayerInfo player)
    {
        if (_turnPlayed) return;
        _turnPlayed = true;

        var token = player == App.PlayerInfoX ? "X" : "O";
        AddToken(index, token);
        // Modify the boardState
        var boardStateBuilder = new StringBuilder(App.BoardState);
        boardStateBuilder[index] = token[0];
        App.BoardState = boardStateBuilder.ToString();

        // send the info off
        var boardStateJson = new Dictionary<string, object> { { "board", App.BoardState } };

        App.Bc.AsyncMatchService.SubmitTurn(
            App.OwnerId,
            App.MatchId,
            App.MatchVersion,
            JsonWriter.Serialize(boardStateJson),
            "A turn has been played",
            null,
            null,
            null,
            OnTurnSubmitted, (status, code, error, cbObject) =>
            {
                Debug.Log(status);
                Debug.Log(code);
                Debug.Log(error.ToString());
            });

        if (App.WhosTurn == App.PlayerInfoX)
            App.WhosTurn = App.PlayerInfoO;
        else
            App.WhosTurn = App.PlayerInfoX;

        updateHud(false);
    }

    private void ClearTokens()
    {
        //Clear logical grid
        for (var i = 0; i < BoardUtility.Grid.Length; i++) BoardUtility.Grid[i] = 0;

        //Clear instanciated game objects
        foreach (var obj in GridObjList) Destroy(obj);
        GridObjList.Clear();
    }

    public bool AvailableSlot(int index)
    {
        if (_turnPlayed) return false;
        if (BoardUtility.Grid[index] == 0) return true;
        return false;
    }
    
    private void BuildBoardFromState(string boardState)
    {
        ClearTokens();
        var j = 0;
        foreach (var c in boardState)
        {
            if (c != '#') AddToken(j, c.ToString());
            ++j;
        }
    }

    private void enableDuringGameDisplay(bool in_enable)
    {
        DuringGameDisplay.SetActive(in_enable);
        AfterGameDisplay.SetActive(!in_enable);
        PlayAgainButton.SetActive(!in_enable);

    }
    private void OnTurnSubmitted(string responseData, object cbPostObject)
    {
        if (App.Winner == 0)
        {
            return;
        }

        enableDuringGameDisplay(false);
    }

    private int _replayTurnIndex = 0;
    public void onReplay()
    {
        AfterGameDisplay.transform.Find("TurnCycleButton").gameObject.SetActive(true);
        _replayTurnIndex = 1;
        AfterGameDisplay.transform.Find("TurnCycleButton").Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "TURN " + (_replayTurnIndex);
        BuildBoardFromState(_history[_replayTurnIndex]);
    }

    public void onIncrementReplayTurn(int in_value)
    {
        if (in_value > 0)
        {
            if (_replayTurnIndex + in_value < _history.Count)
                _replayTurnIndex += in_value;
            else
            {
                _replayTurnIndex = _replayTurnIndex + in_value - _history.Count;
            }
        }
        else
        {
            if (_replayTurnIndex + in_value > 0)
                _replayTurnIndex += in_value;
            else
            {
                _replayTurnIndex = (_history.Count - 1) + (_replayTurnIndex + in_value);
            }
        }

        if (_replayTurnIndex <= 0) _replayTurnIndex = 1;

        AfterGameDisplay.transform.Find("TurnCycleButton").Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "TURN " + (_replayTurnIndex);
        BuildBoardFromState(_history[_replayTurnIndex]);
    }

    public void onCompleteGame()
    {
        if (_isActive)
        {
            App.OnCompleteGame();    
        }
    }
    
    //Called from Unity Button
    public void onPlayAgain()
    {
        PlayAgainButton.SetActive(false);
        var jsonData = new Dictionary<string, object> { { "isReady", true },
                                                        { "opponentProfileID", App.ProfileId },
                                                        { "opponentName", App.Name },
                                                        { "matchID", App.MatchId },
                                                        { "ownerID", App.OwnerId } };

        // Send event to opponent to prompt them to play again
        App.Bc.EventService.SendEvent(App.CurrentMatch.matchedProfile.ProfileId,
                                      "playAgain",
                                      JsonWriter.Serialize(jsonData));
        App.IsAskingToRematch = true;
    }

    //Called from Unity Button
    public void AcceptRematch()
    {
        App.AcceptRematch(gameObject);
    }
    
    //Called from Unity Button
    public void DeclineRematch()
    {
        App.DeclineMatch();
    }
    
    #region private variables 

    private List<GameObject> GridObjList = new List<GameObject>();
    
    private readonly Vector3[] _tokenPositions =
    {
        new Vector3(-2.1f, 12, 2.1f),
        new Vector3(0, 12, 2.1f),
        new Vector3(2.1f, 12, 2.1f),
        new Vector3(-2.1f, 12, 0),
        new Vector3(0, 12, 0),
        new Vector3(2.1f, 12, 0),
        new Vector3(-2.1f, 12, -2.1f),
        new Vector3(0, 12, -2.1f),
        new Vector3(2.1f, 12, -2.1f)
    };

    private List<string> _history;
    private bool _turnPlayed = false;
    #endregion
}
