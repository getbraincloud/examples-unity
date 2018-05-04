using System.Collections;
using System.Collections.Generic;
using System.Text;
using LitJson;
using UnityEngine;

public class TicTacToe : MonoBehaviour
{
    public static string boardState = "#########";
    public static PlayerInfo whosTurn;
    public static PlayerInfo playerInfoX = new PlayerInfo();
    public static PlayerInfo playerInfoO = new PlayerInfo();
    public static string matchId;
    public static string ownerId;
    public static ulong matchVersion;

    private static readonly int[,] m_WinningCond =
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

    private static readonly Vector3[] m_tokenPositions =
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

    private readonly int[] grid = new int[9];
    public List<GameObject> gridObjList;
    private List<string> m_history;
    private bool m_isHistoryMatch;
    private bool m_turnPlayed;

    private int m_winner;
    public GameObject playerO;
    public GUIText playerTurnText;

    public GameObject playerX;

    private void Start()
    {
        m_winner = 0;

        // Read the state and assembly the board
        BuildBoardFromState(boardState);

        // Check we if are not seeing a done match
        m_winner = CheckForWinner();

        // Setup HUD with player pics and names
        var playerXPic = GameObject.Find("PlayerXPic").GetComponent<GUITexture>();
        var playerOPic = GameObject.Find("PlayerOPic").GetComponent<GUITexture>();
        var playerXName = GameObject.Find("PlayerXName").GetComponent<GUIText>();
        var playerOName = GameObject.Find("PlayerOName").GetComponent<GUIText>();

        StartCoroutine(SetProfilePic(playerInfoX.picUrl, playerXPic));
        StartCoroutine(SetProfilePic(playerInfoO.picUrl, playerOPic));

        playerXName.text = playerInfoX.name;
        playerOName.text = playerInfoO.name;
        playerTurnText.text = "Your Turn";

        m_turnPlayed = false;

        m_winner = CheckForWinner();
        if (m_winner != 0)
        {
            m_isHistoryMatch = true;
            m_turnPlayed = true;
            if (m_winner == -1)
                playerTurnText.text = "Match Tied";
            else
                playerTurnText.text = "Match Completed";

            // Read match history
            BrainCloudWrapper.GetBC().AsyncMatchService
                .ReadMatchHistory(ownerId, matchId, OnReadMatchHistory, null, null);
        }
    }

    private void OnReadMatchHistory(string responseData, object cbPostObject)
    {
        var turns = JsonMapper.ToObject(responseData)["data"]["turns"];

        m_history = new List<string>();
        for (var i = 0; i < turns.Count; ++i)
        {
            var turn = turns[i];
            var turnState = (string) turn["matchState"]["board"];
            m_history.Add(turnState);
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
        gridObjList.Add(Instantiate(token == "X" ? playerX : playerO, m_tokenPositions[index],
            Quaternion.Euler(Random.Range(-7.0f, 7.0f), Random.Range(-7.0f, 7.0f), Random.Range(-7.0f, 7.0f))));
        grid[index] = token == "X" ? 1 : 2;
    }

    public void PlayTurn(int index, PlayerInfo player)
    {
        var token = player == playerInfoX ? "X" : "O";
        AddToken(index, token);
        // Modify the boardState
        var boardStateBuilder = new StringBuilder(boardState);
        boardStateBuilder[index] = token[0];
        boardState = boardStateBuilder.ToString();

        m_turnPlayed = true;
        if (whosTurn == playerInfoX) whosTurn = playerInfoO;
        else whosTurn = playerInfoX;
        m_winner = CheckForWinner();

        if (m_winner < 0)
        {
            playerTurnText.text = "Game Tied!";
        }
        else if (m_winner > 0)
        {
            if (m_winner == 1) playerTurnText.text = playerInfoX.name + " Wins!";
            else playerTurnText.text = playerInfoO.name + " Wins!";
        }
        else
        {
            playerTurnText.text = whosTurn.name + " Turn";
        }
    }

    private void ClearTokens()
    {
        //Clear logical grid
        for (var i = 0; i < grid.Length; i++)
            grid[i] = 0;

        //Clear instanciated game objects
        foreach (var obj in gridObjList) Destroy(obj);
        gridObjList.Clear();
    }

    public bool AvailableSlot(int index)
    {
        if (m_turnPlayed) return false;
        if (grid[index] == 0)
            return true;
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
            int a = m_WinningCond[i, 0], b = m_WinningCond[i, 1], c = m_WinningCond[i, 2];
            int b1 = grid[a], b2 = grid[b], b3 = grid[c];

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

        if (gameEnded && ourWinner == 0)
            ourWinner = -1;

        return ourWinner;
    }

    private void OnGUI()
    {
        // Display History HUD
        OnHistoryGUI();

        if (!m_turnPlayed) return;

        var btnText = "Submit Turn";
        if (m_winner != 0) btnText = "Complete Game";

        if (m_isHistoryMatch)
        {
            if (GUI.Button(new Rect(Screen.width / 2 - 70, 60, 140, 30), "Leave")) Application.LoadLevel("GamePicker");
        }
        else if (GUI.Button(new Rect(Screen.width / 2 - 70, 60, 140, 30), btnText))
        {
            // Ask the user to submit his turn
            var boardStateJson = new JsonData();
            boardStateJson["board"] = boardState;

            BrainCloudWrapper.GetBC().AsyncMatchService.SubmitTurn(
                ownerId,
                matchId,
                matchVersion,
                boardStateJson.ToJson(),
                "A turn has been played",
                null,
                null,
                null,
                OnTurnSubmitted);
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
        if (m_history == null) return;

        var i = 0;
        GUI.Label(new Rect(Screen.width - 90, 130, 70, 30), "History:");
        foreach (var turnState in m_history)
        {
            if (GUI.Button(new Rect(Screen.width - 90, 150 + i * 40, 70, 30), "Turn " + i))
                BuildBoardFromState(turnState);
            ++i;
        }
    }

    private void OnTurnSubmitted(string responseData, object cbPostObject)
    {
        if (m_winner == 0)
        {
            // Go back to game select scene
            Application.LoadLevel("GamePicker");
            return;
        }

        // Otherwise, the game was done. Send a complete turn
        BrainCloudWrapper.GetBC().AsyncMatchService.CompleteMatch(
            ownerId,
            matchId,
            OnMatchCompleted);
    }

    private void OnMatchCompleted(string responseData, object cbPostObject)
    {
        // Go back to game select scene
        Application.LoadLevel("GamePicker");
    }

    public class PlayerInfo
    {
        public string name;
        public string picUrl;
    }
}