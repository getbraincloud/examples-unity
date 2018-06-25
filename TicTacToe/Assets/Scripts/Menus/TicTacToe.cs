#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using LitJson;
using UnityEngine;
using UnityEngine.UI;

#endregion

public class TicTacToe : GameScene
{
    
    #region Variables
    private readonly int[] _grid = new int[9];

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

    private readonly int[,] _winningCond =
    {
        //List of possible winning conditions
        {0, 1, 2},
        {3, 4, 5},
        {6, 7, 8},
        {0, 3, 6},
        {1, 4, 7},
        {2, 5, 8},
        {0, 4, 8},
        {2, 4, 6}
    };

    public List<GameObject> GridObjList;
    private List<string> _history;
    private bool _isHistoryMatch;

    private MatchState _matchState;
    public GameObject PlayerO;
    public TextMesh PlayerTurnText;

    public GameObject PlayerX;
    private bool _turnPlayed;
    private bool _turnSubmitted;

    private int _winner;

    public PlayerInfo WinnerInfo;
    public PlayerInfo LoserInfo;

    private bool hasNoNewTurn;

    public TextMesh PlayerXName;
    public TextMesh PlayerOName;
    
    #endregion

    private void Start()
    {
        _winner = 0;

        var parent = gameObject.transform.parent.gameObject;

        parent.transform.localPosition = new Vector3(parent.transform.localPosition.x + App.Offset,
            parent.transform.localPosition.y, parent.transform.localPosition.z);

        for (var i = 0; i < _tokenPositions.Length; i++) _tokenPositions[i].x += App.Offset;

        parent.GetComponentInChildren<Camera>().rect = App.ViewportRect;


        // Read the state and assembly the board
        BuildBoardFromState(App.BoardState);

        // Check we if are not seeing a done match
        _winner = CheckForWinner();

        // Setup HUD with player pics and names
        

        PlayerXName.text = App.PlayerInfoX.PlayerName;
        PlayerOName.text = App.PlayerInfoO.PlayerName;
        PlayerTurnText.text = "Your Turn";

        _turnPlayed = false;

        _winner = CheckForWinner();
        if (_winner != 0)
        {
            _isHistoryMatch = true;
            _turnPlayed = true;

            _matchState = MatchState.MATCH_HISTORY;

            if (_winner == -1)
                PlayerTurnText.text = "Match Tied";
            else
                PlayerTurnText.text = "Match Completed";

            // Read match history
            App.Bc.AsyncMatchService
                .ReadMatchHistory(App.OwnerId, App.MatchId, OnReadMatchHistory, null, null);
        }
    }

    private void OnReadMatchHistory(string responseData, object cbPostObject)
    {
        var turns = JsonMapper.ToObject(responseData)["data"]["turns"];

        _history = new List<string>();
        for (var i = 0; i < turns.Count; ++i)
        {
            var turn = turns[i];
            var turnState = (string) turn["matchState"]["board"];
            _history.Add(turnState);
        }
    }

    private void AddToken(int index, string token)
    {
        GridObjList.Add(Instantiate(token == "X" ? PlayerX : PlayerO, _tokenPositions[index],
            Quaternion.Euler(Random.Range(-7.0f, 7.0f), Random.Range(-7.0f, 7.0f), Random.Range(-7.0f, 7.0f))));
        GridObjList.Last().transform.parent = gameObject.transform;
        _grid[index] = token == "X" ? 1 : 2;
    }

    public void PlayTurn(int index, PlayerInfo player)
    {
        var token = player == App.PlayerInfoX ? "X" : "O";
        AddToken(index, token);
        // Modify the boardState
        var boardStateBuilder = new StringBuilder(App.BoardState);
        boardStateBuilder[index] = token[0];
        App.BoardState = boardStateBuilder.ToString();

        _turnPlayed = true;

        _matchState = MatchState.TURN_PLAYED;

        if (App.WhosTurn == App.PlayerInfoX)
            App.WhosTurn = App.PlayerInfoO;
        else
            App.WhosTurn = App.PlayerInfoX;

        _winner = CheckForWinner();

        if (_winner < 0)
        {
            PlayerTurnText.text = "Game Tied!";
        }
        else if (_winner > 0)
        {
            if (_winner == 1)
            {
                PlayerTurnText.text = App.PlayerInfoX.PlayerName + " Wins!";
                WinnerInfo = App.PlayerInfoX;
                LoserInfo = App.PlayerInfoO;
            }
            else
            {
                PlayerTurnText.text = App.PlayerInfoO.PlayerName + " Wins!";
                WinnerInfo = App.PlayerInfoO;
                LoserInfo = App.PlayerInfoX;
            }
        }
        else
        {
            PlayerTurnText.text = App.WhosTurn.PlayerName + " Turn";
        }
    }

    private void ClearTokens()
    {
        //Clear logical grid
        for (var i = 0; i < _grid.Length; i++) _grid[i] = 0;

        //Clear instanciated game objects
        foreach (var obj in GridObjList) Destroy(obj);
        GridObjList.Clear();
    }

    public bool AvailableSlot(int index)
    {
        if (_turnPlayed) return false;
        if (_grid[index] == 0) return true;
        return false;
    }

    // Checks if we have a winner yet.
    // Returns -1 = Game Tied, 0 = No winner yet, 1 = Player1 won, 2 = Player2 won
    private int CheckForWinner()
    {
        var ourWinner = 0;
        var gameEnded = true;

        for (var i = 0; i < 8; i++)
        {
            int a = _winningCond[i, 0], b = _winningCond[i, 1], c = _winningCond[i, 2];
            int b1 = _grid[a], b2 = _grid[b], b3 = _grid[c];

            if (b1 == 0 || b2 == 0 || b3 == 0)
            {
                gameEnded = false;
                continue;
            }

            if (b1 == b2 && b2 == b3)
            {
                ourWinner = b1;
                break;
            }
        }

        if (gameEnded && ourWinner == 0) ourWinner = -1;

        return ourWinner;
    }

    private void OnGUI()
    {
        // Display History HUD
        OnHistoryGUI();

        if (!_turnPlayed) return;

        var btnText = "Submit Turn";
        if (_winner != 0) btnText = "Complete Game";

        if (_isHistoryMatch)
        {
            if (GUI.Button(new Rect(Screen.width / 2 - 70 + App.Offset, 60, 140, 30), "Leave"))
                App.GotoMatchSelectScene(gameObject);
        }

        if (_turnSubmitted)
        {
            if (GUI.Button(new Rect(Screen.width / 2 - 70 + App.Offset, 60, 140, 30), "Leave"))
            {
                App.GotoMatchSelectScene(gameObject);
            }
            if (GUI.Button(new Rect(Screen.width / 2 - 70 + App.Offset, 60 - 45, 140, 30), "Refresh"))
            {
                // Query more detail state about the match
                App.Bc.AsyncMatchService
                    .ReadMatch(App.OwnerId, App.MatchId, (response, cbObject) =>
                    {
                                                
                        var match = App.CurrentMatch;
                        var data = JsonMapper.ToObject(response)["data"];

                        
                        int newVersion = int.Parse(data["version"].ToString());

                        if (App.MatchVersion + 1 >= (ulong)newVersion)
                        {
                            hasNoNewTurn = true;
                        }
                        else
                        {
                            App.MatchVersion = (ulong)newVersion;

                            // Setup a couple stuff into our TicTacToe scene
                            App.BoardState = (string) data["matchState"]["board"];
                            App.PlayerInfoX = match.playerXInfo;
                            App.PlayerInfoO = match.playerOInfo;
                            App.WhosTurn = match.yourToken == "X" ? App.PlayerInfoX : match.playerOInfo;
                            App.OwnerId = match.ownerId;
                            App.MatchId = match.matchId;
                        
                        

                            // Load the Tic Tac Toe scene

                            App.GotoTicTacToeScene(gameObject); 

                        }
                    });
            }

            if (hasNoNewTurn)
            {
                GUI.Label(new Rect(Screen.width / 2 - 70 + App.Offset, 60 + 45, 140, 30), "Has no new turn");
            }
            
        }
        else if (GUI.Button(new Rect(Screen.width / 2 - 70 + App.Offset, 60, 140, 30), btnText))
        {
            // Ask the user to submit his turn
            var boardStateJson = new JsonData();
            boardStateJson["board"] = App.BoardState;

            App.Bc.AsyncMatchService.SubmitTurn(
                App.OwnerId,
                App.MatchId,
                App.MatchVersion,
                boardStateJson.ToJson(),
                "A turn has been played",
                null,
                null,
                null,
                OnTurnSubmitted, (status, code, error, cbObject) => { Debug.Log(status); Debug.Log(code); Debug.Log(error.ToString()); });
        }
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

    private void OnHistoryGUI()
    {
        if (_history == null) return;

        var i = 0;
        GUI.Label(new Rect(Screen.width / 2 + App.Offset, 130, 70, 30), "History:");
        foreach (var turnState in _history)
        {
            if (GUI.Button(new Rect(Screen.width / 2 + App.Offset, 150 + i * 40, 70, 30), "Turn " + i))
                BuildBoardFromState(turnState);

            ++i;
        }
    }

    private void OnTurnSubmitted(string responseData, object cbPostObject)
    {
        
        if (_winner == 0)
        {
            _turnSubmitted = true;
            return;
        }

        // Otherwise, the game was done. Can send complete turn
        /*
         App.Bc.AsyncMatchService.CompleteMatch(
             App.OwnerId,
             App.MatchId,
             OnMatchCompleted);
          */

        // However, we are using a custom FINISH_RANK_MATCH script which is set up on brainCloud. View the commented Cloud Code script below
        var matchResults = new JsonData();

        matchResults["ownerId"] = App.OwnerId;
        matchResults["matchId"] = App.MatchId;

        if (_winner < 0)
        {
            matchResults["isTie"] = true;
        }
        else
        {
            matchResults["isTie"] = false;
            matchResults["winnerId"] = WinnerInfo.ProfileId;
            matchResults["loserId"] = LoserInfo.ProfileId;
            matchResults["winnerRating"] = int.Parse(WinnerInfo.PlayerRating);
            matchResults["loserRating"] = int.Parse(LoserInfo.PlayerRating);
        }


        App.Bc.ScriptService.RunScript("FINISH_RANK_MATCH", matchResults.ToJson(), OnMatchCompleted,
            (status, code, error, cbObject) => { });

        /**     Cloud Code Script Contents: FINISH_RANK_MATCH
         
            // cloud code can be created on the brainCloud dashboard, under Design | Cloud Code | Scripts
            var retVal = {};
            
            
            var ownerId = data.ownerId;
            var matchId = data.matchId;
            var winnerId = data.winnerId;
            var loserId = data.loserId;
            
            var winnerRating = data.winnerRating;
            var loserRating = data.loserRating;
            
            var isTie = data.isTie;
            
            
            
            // Complete the match
            var ownerSession = bridge.getSessionForProfile(ownerId);
            var asyncMatchProxy = bridge.getAsyncMatchServiceProxy(ownerSession);
            retVal = asyncMatchProxy.completeMatch(ownerId, matchId);
            
            // If its not a tie, let process the remaining match results
            if(!isTie) {
                // Declare winner and loser session
                var winnerSession = bridge.getSessionForProfile(winnerId);
                var loserSession = bridge.getSessionForProfile(loserId);
            
            
                var winnerMatchMakingProxy = bridge.getMatchMakingServiceProxy(winnerSession);
                var loserMatchMakingProxy = bridge.getMatchMakingServiceProxy(loserSession);
            
            
                // Alter Ratings. Rating defaults and match making controls can be found on the brainCloud dashboard, under Design | Multiplayer | Matchmaking
                var winnerDelta =  ((loserRating + 400) / winnerRating) * 40;
                var loserDelta =  ((loserRating - 400) / winnerRating) * 40;
            
                winnerMatchMakingProxy.incrementPlayerRating(winnerDelta);
                loserMatchMakingProxy.decrementPlayerRating(loserDelta);
            
            
                // Post Scores to Rating Leaderboard
                var leaderboardId = "Player_Rating";
            
                var winnerRating = winnerDelta + winnerRating;
                var loserRating = loserRating - loserDelta;
            
                var winnerLeaderboardProxy = bridge.getLeaderboardServiceProxy(winnerSession);
                var loserLeaderboardProxy = bridge.getLeaderboardServiceProxy(loserSession);
            
                winnerLeaderboardProxy.postScoreToLeaderboard(leaderboardId, winnerRating, null);
                loserLeaderboardProxy.postScoreToLeaderboard(leaderboardId, loserRating, null);
            
            
                // Stats are set on the brainCloud Dashboard under Design | Statistics Rules | User Stats.
                var playerStats = { "WON_RANKED_MATCH" : 1 };
                var playerStatisticsProxy = bridge.getPlayerStatisticsServiceProxy(winnerSession);
                playerStatisticsProxy.incrementPlayerStats(playerStats);
            }
            
            var matchMakingProxy = bridge.getMatchMakingServiceProxy();
            var retVal = matchMakingProxy.read();
            
            retVal;
         */
    }

    private void OnMatchCompleted(string responseData, object cbPostObject)
    {
        // Get the new PlayerRating
        App.PlayerRating = JsonMapper.ToObject(responseData)["data"]["response"]["data"]["playerRating"].ToString();

        
        // Go back to game select scene
        App.GotoMatchSelectScene(gameObject);
    }


    private enum MatchState
    {
        YOUR_TURN,
        TURN_PLAYED,
        WAIT_FOR_TURN,
        MATCH_HISTORY,
        COMPLETED
    }
}