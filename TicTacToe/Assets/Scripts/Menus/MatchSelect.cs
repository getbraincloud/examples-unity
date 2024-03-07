using BrainCloud;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Random = UnityEngine.Random;

public class MatchSelect : ResourcesManager
{
    private const int RANGE_DELTA = 500;
    private const int NUMBER_OF_MATCHES = 20;
    private readonly List<MatchInfo> completedMatches = new List<MatchInfo>();
    private readonly List<PlayerInfo> matchedProfiles = new List<PlayerInfo>();
    private readonly List<MatchInfo> matches = new List<MatchInfo>();
    
    private Vector2 _scrollPos;
    private GameButtonCell _selectedCell;
    private bool isLookingForMatch = false;
    private int index;
    [SerializeField]
    private RectTransform MyGamesScrollView = null;
    [SerializeField]
    private Button CancelButton = null;
    [SerializeField]
    public TextMeshProUGUI MyGames;
    [SerializeField]
    private Spinner Spinner = null;
    [SerializeField]
    public TextMeshProUGUI QuickPlayText;
    public GameObject AskToRematchScreen;
    public GameObject ErrorMessageScreen;
    public TMP_Text ErrorMessageText;
    public GameObject RetryRTTButton;
    // Use this for initialization
    private void Start()
    {
        gameObject.transform.parent.gameObject.GetComponentInChildren<Camera>().rect = App.ViewportRect;

        // Enable Match Making, so other Users can also challege this Profile
        // http://getbraincloud.com/apidocs/apiref/#capi-matchmaking-enablematchmaking
        App.Bc.MatchMakingService.EnableMatchMaking(null, (status, code, error, cbObject) =>
        {
            Debug.Log("MatchMaking enabled failed");
        });
        enableRTT();
        m_itemCell = new List<GameButtonCell>();
        
        //Disable UI Elements
        CancelButton.gameObject.SetActive(false);
        RetryRTTButton.SetActive(false);
        ErrorMessageScreen.SetActive(false);
        AskToRematchScreen.SetActive(false);
        
        if (UserName != null)
            UserName.text = App.Name;
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
        queryMatchState();
        // LISTEN TO THE ASYNC CALLS, when we get one of these calls, lets just refresh 
        // match state
        App.Bc.RTTService.RegisterRTTAsyncMatchCallback(queryMatchStateRTT);
    }
    
    private void onRTTFailure(int status, int reasonCode, string responseData, object cbPostObject)
    {
        if (this == null || gameObject == null) return;
        //Failure to connect to RTT so we display a dialog window to inform the user
        //A button will be on the dialog that will direct them to enableRTT()
        ErrorMessageText.text = "Error: Poor Connection. \n Try Again ?";
        RetryRTTButton.SetActive(true);
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
        Spinner.gameObject.SetActive(true);
        App.Bc.MatchMakingService.FindPlayers(RANGE_DELTA, NUMBER_OF_MATCHES, OnFindPlayers);
    }

    private void OnFindPlayers(string responseData, object cbPostObject)
    {
        matchedProfiles.Clear();

        // Construct our matched players list using response data
        var matchesData = (JsonReader.Deserialize<Dictionary<string, object>>(responseData)
                            ["data"] as Dictionary<string, object>)
                            ["matchesFound"] as Dictionary<string, object>[];

        if (matchesData != null && matchesData.Length > 0)
        {
            foreach (Dictionary<string, object> match in matchesData)
            {
                matchedProfiles.Add(new PlayerInfo(match));
            }
        }

        // After, fetch our game list from Braincloud
        App.Bc.AsyncMatchService.FindMatches(OnFindMatches);
    }

    private void OnFindMatches(string responseData, object cbPostObject)
    {
        matches.Clear();

        // Construct our game list using response data
        var jsonMatches = (JsonReader.Deserialize<Dictionary<string, object>>(responseData)
                                ["data"] as Dictionary<string, object>)
                                ["results"] as object[];

        for (var i = 0; i < jsonMatches.Length; ++i)
        {
            var jsonMatch = jsonMatches[i] as Dictionary<string, object>;
            var match = new MatchInfo(jsonMatch, this);

            if (!match.expired)
                matches.Add(match);
        }

        // Now, find completed matches so the user can go see the history
        App.Bc.AsyncMatchService.FindCompleteMatches(OnFindCompletedMatches);
    }

    private void OnFindCompletedMatches(string responseData, object cbPostObject)
    {
        completedMatches.Clear();

        // Construct our game list using response data
        var jsonMatches = (JsonReader.Deserialize<Dictionary<string, object>>(responseData)
                                ["data"] as Dictionary<string, object>)
                                ["results"] as object[];

        for (var i = 0; i < jsonMatches.Length; ++i)
        {
            var jsonMatch = jsonMatches[i] as Dictionary<string, object>;
            var match = new MatchInfo(jsonMatch, this);
            completedMatches.Add(match);
        }

        OnPopulateMatches();
    }

    private void OnPopulateMatches()
    {
        if (this != null)
        {
            QuickPlayText.text = "QUICKPLAY";
            Spinner.gameObject.SetActive(false);
            MyGames.text = "My Games";
            RemoveAllCellsInView(m_itemCell);
            PopulateMatchesScrollView(matches);
            PopulateMatchesScrollView(completedMatches);
        }
    }

    public void OnNewGameButton(int WindowId)
    {
        OnGoToMatchSelectScene();
        Spinner.gameObject.SetActive(false);
        MyGames.text = "Pick Opponent";
        CancelButton.gameObject.SetActive(true);
        PopulatePlayersScrollView(matchedProfiles);
    }

    public void OnCancelButton()
    {
        CancelButton.gameObject.SetActive(false);
        RemoveAllCellsInView(m_itemCell);
        OnPopulateMatches();
    }

    public void OnMatchSelected(MatchInfo match,GameButtonCell cell)
    {
        if (match != null)
        {
            App.CurrentMatch = match;
            _selectedCell = cell;
            // Query more detail state about the match
            App.Bc.AsyncMatchService
                .ReadMatch(match.ownerId, match.matchId, OnReadMatch, OnReadMatchFailed, match);
        }
    }

    public void OnPlayerSelected(PlayerInfo profile)
    {
        if (profile != null)
        {
            OnPickOpponent(profile);
        }
    }

    public void OnQuickPlay()
    {
        if (isLookingForMatch)
        {
            isLookingForMatch = false;
            var attributesJson = new Dictionary<string, object> { { "MATCH_STATE", "CANCEL_LOOKING" } };

            QuickPlayText.text = "QUICKPLAY";

            App.Bc.PlayerStateService.UpdateAttributes(JsonWriter.Serialize(attributesJson), false);
        }
        else
        {
            var scriptDataJson = new Dictionary<string, object> { { "rankRangeDelta", RANGE_DELTA },
                                                                  { "pushNotificationMessage", null } };

            QuickPlayText.text = "Looking\nfor match...";

            App.Bc.ScriptService.RunScript("RankGame_AutoJoinMatch", JsonWriter.Serialize(scriptDataJson), OnCreateMatchSuccess, OnCreateMatchFailed);
        }
    }
    //Populates matches in progress
    private void PopulateMatchesScrollView(List<MatchInfo> in_itemItems)
    {
        if (in_itemItems.Count == 0)
        {
            return;
        }

        if (MyGamesScrollView != null)
        {
            index = 0;
            foreach (var match in in_itemItems)
            {
                App.Bc.AsyncMatchService.ReadMatch(match.ownerId, match.matchId, AddNewCell, null, match);
            }
        }
    }
    
    private void AddNewCell(string jsonResponse, object cbObject)
    {
        var match = cbObject as MatchInfo;
        string board = (string)((JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)
                                ["data"] as Dictionary<string, object>)
                                ["matchState"] as Dictionary<string, object>)["board"];

        //Determining if game is completed
        match.complete = BoardUtility.IsGameCompleted(board);
        if (match.complete)
        {
            match.scoreSubmitted = true;
        }
        App.PlayerInfoO = match.playerOInfo;
        App.PlayerInfoX = match.playerXInfo;
        
        //Create game button cell
        GameButtonCell newItem = CreateItemCell(MyGamesScrollView, (index % 2) == 0);
        newItem.Init(match, this);
        newItem.transform.localPosition = Vector3.zero;
        index = index < matches.Count ? index++ : 0;
        m_itemCell.Add(newItem);
        
        //Ensuring no duplicated game button cells are created 
        var tempList = new List<GameButtonCell>();
        for (int i = m_itemCell.Count - 1; i > -1; i--)
        {
            if (tempList.Count == 0)
            {
                tempList.Add(m_itemCell[i]);
                continue;
            }
            if (tempList[i].MatchInfo.matchId == m_itemCell[i].MatchInfo.matchId)
            {
                Destroy(m_itemCell[i].gameObject);
                m_itemCell.Remove(m_itemCell[i]);
            }
        }
    }

    //Populates players to pick from for a new match
    private void PopulatePlayersScrollView(List<PlayerInfo> in_itemItems)
    {
        RemoveAllCellsInView(m_itemCell);
        if (in_itemItems.Count == 0)
        {
            return;
        }

        if (MyGamesScrollView != null)
        {
            int i = 0;
            foreach (var profile in in_itemItems)
            {
                GameButtonCell newItem = CreateItemCell(MyGamesScrollView, (i % 2) == 0);
                newItem.Init(profile, this);
                newItem.transform.localPosition = Vector3.zero;
                m_itemCell.Add(newItem);
                i++;
            }
        }
    }

    public void OnPickOpponent(PlayerInfo matchedProfile)
    {
        var yourTurnFirst = Random.Range(0, 100) < 50;

        // Setup our summary data. This is what we see when we query the list of games.
        var summaryData = new Dictionary<string, List<object>> { { "players", new List<object>() } };

        // Us
        summaryData["players"].Add(new Dictionary<string, string>() { { "profileId" , App.ProfileId },
                                                                      { "token", yourTurnFirst ? "X" : "O" } }); // First player has X

        // Our Friend
        summaryData["players"].Add(new Dictionary<string, string>() { { "profileId" , matchedProfile.ProfileId },
                                                                      { "token", !yourTurnFirst ? "X" : "O" } });

        // Setup our match State. We only store where Os and Xs are in the tic tac toe board. 
        var matchState = new Dictionary<string, string> { { "board", "#########" } }; // Empty the board. # = nothing, O,X = tokens

        // Create the match
        App.Bc.AsyncMatchService.CreateMatchWithInitialTurn(
            "[{\"platform\":\"BC\",\"id\":\"" + matchedProfile.ProfileId + "\"}]", // Opponents
            JsonWriter.Serialize(matchState), // Current match state
            "A friend has challenged you to a match of Tic Tac Toe.", // Push notification Message
            yourTurnFirst ? App.ProfileId : matchedProfile.ProfileId, // Which turn it is. We picked randomly
            JsonWriter.Serialize(summaryData), // Summary data
            OnCreateMatchSuccess,
            OnCreateMatchFailed,
            null);
    }

    private void OnCreateMatchSuccess(string responseData, object cbPostObject)
    {
        var data = JsonReader.Deserialize<Dictionary<string, object>>(responseData)["data"] as Dictionary<string, object>;
        MatchInfo match;

        // Cloud Code returns wrap the data in a responseJson
        if (data.ContainsKey("response"))
        {
            if (data["response"] is Dictionary<string, object> response &&
                response.ContainsKey("data") &&
                response["data"] is Dictionary<string, object> matchInfo)
            {
                match = new MatchInfo(matchInfo, this);
            }
            else
            {
                // No match found. Handle this result
                Debug.Log(data["response"].ToString());

                isLookingForMatch = true;
                return;
            }
        }
        else
        {
            match = new MatchInfo(data, this);
        }

        // Go to the game if it's your turn
        if (match.yourTurn)
            EnterMatch(match);
        else
            App.GotoMatchSelectScene(gameObject);
    }

    private void OnCreateMatchFailed(int a, int b, string responseData, object cbPostObject)
    {
        Debug.LogError("Failed to create Async Match");
        Debug.Log(a);
        Debug.Log(b);
        Debug.Log(responseData);
        ErrorMessageText.text = "Failed to create a match"; 
        RetryRTTButton.SetActive(false);
        ErrorMessageScreen.SetActive(true);
    }

    private void EnterMatch(MatchInfo match)
    {
        App.CurrentMatch = match;

        // Query more detail state about the match
        App.Bc.AsyncMatchService
            .ReadMatch(match.ownerId, match.matchId, OnReadMatch, OnReadMatchFailed, match);
    }

    private void OnReadMatch(string responseData, object cbPostObject)
    {
        var match = cbPostObject as MatchInfo;
        var data = JsonReader.Deserialize<Dictionary<string, object>>(responseData)["data"] as Dictionary<string, object>;

        // Setup a couple stuff into our TicTacToe scene
        App.BoardState = (string)(data["matchState"] as Dictionary<string, object>)["board"];
        App.PlayerInfoX = match.playerXInfo;
        App.PlayerInfoO = match.playerOInfo;
        App.WhosTurn = match.yourToken == "X" ? App.PlayerInfoX : match.playerOInfo;
        App.OwnerId = match.ownerId;
        App.MatchId = match.matchId;
        App.MatchVersion = (ulong)match.version;

        // Load the Tic Tac Toe scene
        App.GotoTicTacToeScene(gameObject);
    }

    private void OnReadMatchFailed(int a, int b, string responseData, object cbPostObject)
    {
        //If reading a match fails then it might mean the other user has closed the match
        //In response to this, set up a message for the player and remove the selection cell
        ErrorMessageText.text = "Match is closed";
        ErrorMessageScreen.SetActive(true);
        m_itemCell.Remove(_selectedCell);
        Destroy(_selectedCell.gameObject);
    }

    private static Color OPP_COLOR = new Color32(0xFF, 0xFF, 0x49, 0xFF);
    private GameButtonCell CreateItemCell(Transform in_parent = null, bool in_even = false)
    {
        GameButtonCell toReturn = null;
        bool isSecondDisplay = MyGames.color == OPP_COLOR ? true : false;
        toReturn = (CreateResourceAtPath(in_even ? "Prefabs/GameButtonCell" + (isSecondDisplay ? "2" : "1") + "A" : "Prefabs/GameButtonCell" + (isSecondDisplay ? "2" : "1") + "B", in_parent.transform)).GetComponent<GameButtonCell>();
        toReturn.transform.SetParent(in_parent);
        toReturn.transform.localScale = Vector3.one;
        return toReturn;
    }

    private void RemoveAllCellsInView(List<GameButtonCell> in_itemCell)
    {
        GameButtonCell item;
        for (int i = 0; i < in_itemCell.Count; ++i)
        {
            item = in_itemCell[i];
            Destroy(item.gameObject);
        }
        in_itemCell.Clear();
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

    private List<GameButtonCell> m_itemCell = null;

    public class MatchInfo
    {
        private readonly MatchSelect matchSelect;
        public PlayerInfo matchedProfile = new PlayerInfo();
        public string matchId;
        public string ownerId;
        public PlayerInfo playerOInfo = new PlayerInfo();
        public PlayerInfo playerXInfo = new PlayerInfo();
        public int version;
        public string yourToken;
        public bool yourTurn;
        public bool complete = false;
        public bool expired = false;
        public bool scoreSubmitted = false;
        public MatchInfo(Dictionary<string, object> data, MatchSelect matchSelect)
        {
            var statusData = data["status"] as Dictionary<string, object>;
            version = (int)data["version"];
            ownerId = (string)data["ownerId"];
            matchId = (string)data["matchId"];
            yourTurn = (string)statusData["currentPlayer"] == matchSelect.App.ProfileId;
            complete = (string)statusData["status"] == "COMPLETE";
            expired = (string)statusData["status"] == "EXPIRED";

            this.matchSelect = matchSelect;

            // Load player info
            LoadPlayerInfo(data, 0);
            LoadPlayerInfo(data, 1);
        }

        private void LoadPlayerInfo(Dictionary<string, object> data, int index)
        {
            var playerData = (data["players"] as Dictionary<string, object>[])[index];
            var playerSummaryData = ((data["summary"] as Dictionary<string, object>)
                                          ["players"] as Dictionary<string, object>[])[index];
            var token = (string)playerSummaryData["token"];

            PlayerInfo playerInfo;
            if (token == "X") playerInfo = playerXInfo;
            else playerInfo = playerOInfo;

            if ((string)playerSummaryData["profileId"] == matchSelect.App.ProfileId)
            {
                playerInfo.PlayerName = matchSelect.App.Name;
                playerInfo.ProfileId = matchSelect.App.ProfileId;
                yourToken = token;
            }
            else
            {
                playerInfo.PlayerName = (string)playerData["playerName"];
                playerInfo.ProfileId = (string)playerSummaryData["profileId"];
                matchedProfile = playerInfo;
            }
        }
    }
}
