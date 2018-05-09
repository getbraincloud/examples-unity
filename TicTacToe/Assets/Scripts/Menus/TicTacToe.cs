using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LitJson;
using UnityEngine;

public class TicTacToe : GameScene
{
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

    public List<GameObject> _gridObjList;
    private List<string> _history;
    private bool _isHistoryMatch;

    private MatchState _matchState;
    public GameObject _playerO;
    public GUIText _playerTurnText;

    public GameObject _playerX;
    private bool _turnPlayed;

    private int _winner;

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
        var playerXPic = GameObject.Find("PlayerXPic").GetComponent<GUITexture>();
        var playerOPic = GameObject.Find("PlayerOPic").GetComponent<GUITexture>();
        var playerXName = GameObject.Find("PlayerXName").GetComponent<GUIText>();
        var playerOName = GameObject.Find("PlayerOName").GetComponent<GUIText>();

        StartCoroutine(SetProfilePic(App.PlayerInfoX.PictureUrl, playerXPic));
        StartCoroutine(SetProfilePic(App.PlayerInfoO.PictureUrl, playerOPic));

        playerXName.text = App.PlayerInfoX.Name;
        playerOName.text = App.PlayerInfoO.Name;
        _playerTurnText.text = "Your Turn";

        _turnPlayed = false;

        _winner = CheckForWinner();
        if (_winner != 0)
        {
            _isHistoryMatch = true;
            _turnPlayed = true;

            _matchState = MatchState.MATCH_HISTORY;

            if (_winner == -1)
                _playerTurnText.text = "Match Tied";
            else
                _playerTurnText.text = "Match Completed";

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

    private IEnumerator SetProfilePic(string url, GUITexture playerPic)
    {
        var www = new WWW(url);
        yield return www;
        playerPic.texture = www.texture;
    }

    private void AddToken(int index, string token)
    {
        _gridObjList.Add(Instantiate(token == "X" ? _playerX : _playerO, _tokenPositions[index],
            Quaternion.Euler(Random.Range(-7.0f, 7.0f), Random.Range(-7.0f, 7.0f), Random.Range(-7.0f, 7.0f))));
        _gridObjList.Last().transform.parent = gameObject.transform;
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
            _playerTurnText.text = "Game Tied!";
        }
        else if (_winner > 0)
        {
            if (_winner == 1)
                _playerTurnText.text = App.PlayerInfoX.Name + " Wins!";
            else
                _playerTurnText.text = App.PlayerInfoO.Name + " Wins!";
        }
        else
        {
            _playerTurnText.text = App.WhosTurn.Name + " Turn";
        }
    }

    private void ClearTokens()
    {
        //Clear logical grid
        for (var i = 0; i < _grid.Length; i++) _grid[i] = 0;

        //Clear instanciated game objects
        foreach (var obj in _gridObjList) Destroy(obj);
        _gridObjList.Clear();
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
                OnTurnSubmitted, (status, code, error, cbObject) => { Debug.Log(error); });
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
            // Go back to game select scene
            App.GotoMatchSelectScene(gameObject);
            return;
        }

        // Otherwise, the game was done. Send a complete turn
        App.Bc.AsyncMatchService.CompleteMatch(
            App.OwnerId,
            App.MatchId,
            OnMatchCompleted);
    }

    private void OnMatchCompleted(string responseData, object cbPostObject)
    {
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