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
    private bool isLookingForOnlineMatch = false;
    private int index;
    [SerializeField] private RectTransform MyGamesScrollView = null;
    [SerializeField] private Button CancelButton = null;
    [SerializeField] public TextMeshProUGUI MyGames;
    [SerializeField] private Spinner Spinner = null;
    [SerializeField] public TextMeshProUGUI QuickPlayText, OnlinePlayersText;
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
            App.Bc.RTTService.EnableRTT(onRTTEnabled, onRTTFailure);
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
            OnlinePlayersText.text = "FIND ONLINE PLAYERS";
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
    private string _onlineEntryId = "";
    private string _lobbyId = "";
    
    public void OnFindOnlinePlayers()
    {
        if (!isLookingForOnlineMatch)
        {
            SuccessCallback success = (responseData, cbObject) =>
            {
                Debug.Log("Successfully started Quick Find and Join Lobby");
                // Register for the Lobby Callbacks
                //
                // Once two players are in the lobby, we will launch an Async Match between them
                OnlinePlayersText.text = "SEARCHING FOR PLAYERS...";

                // store the lobby id in the app so we can use it later
                var data = JsonReader.Deserialize<Dictionary<string, object>>(responseData)["data"] as Dictionary<string, object>;
                _onlineEntryId = (string)data["entryId"];
            };

            FailureCallback failure = (status, code, error, cbObject) =>
            {
                Debug.Log("Failed to start Quick Find and Join Lobby");
                Debug.Log(status);
                Debug.Log(code);
                Debug.Log(error);

                OnlinePlayersText.text = "FIND ONLINE PLAYERS";
            };

            LobbyParams lobbyParams = CreateLobbyParams();

            // This will start a Quick Find and Play Online with the Lobby Match Making Service
            // start a generic quick find and play for anyone looking for a TicTacToe match
            App.Bc.LobbyService.FindOrCreateLobby(
                    "TicTacToe",
                    0,
                    1,
                    lobbyParams.algo,
                    lobbyParams.filters,
                    false,
                    lobbyParams.extra,
                    Random.Range(0, 100) <= 50 ? "blue" : "yellow",
                    lobbyParams.settings,
                    null,
                    success,
                    failure
                );

            isLookingForOnlineMatch = true;
            App.Bc.RTTService.RegisterRTTLobbyCallback(OnLobbyEventCallback);
        }
        else
        {
            CleanupOnlineLobby();
        }
    }

    private void CleanupOnlineLobby()
    {
        isLookingForOnlineMatch = false;
        // we are still looking for a match, so cancel the request
        if (_onlineEntryId != "")
        {
            App.Bc.LobbyService.CancelFindRequest("TicTacToe", _onlineEntryId);
        }
        if (_lobbyId != "")
        {
            App.Bc.LobbyService.LeaveLobby(_lobbyId, null, null);
        }
        _onlineEntryId = "";
        _lobbyId = "";
        App.Bc.RTTService.DeregisterRTTLobbyCallback();
        OnlinePlayersText.text = "FIND ONLINE PLAYERS";
    }

    private void OnLobbyEventCallback(string jsonResponse)
    {
        // This is where we will handle the lobby events
        Debug.Log("Lobby Event Callback: " + jsonResponse);
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        if (response.ContainsKey("operation"))
        {
            var eventType = (string)response["operation"];
            if (eventType == "MEMBER_JOIN")
            {
                Debug.Log("A player has joined the lobby");

                // Parse the lobby data
                if (response.ContainsKey("data"))
                {
                    var data = response["data"] as Dictionary<string, object>;
                    if (data != null && data.ContainsKey("lobby"))
                    {
                        _lobbyId = (string)data["lobbyId"];
                        var lobby = data["lobby"] as Dictionary<string, object>;
                        if (lobby != null && lobby.ContainsKey("numMembers"))
                        {
                            int numMembers = 0;
                            var numMembersObj = lobby["numMembers"];
                            if (numMembersObj is int)
                                numMembers = (int)numMembersObj;
                            else if (numMembersObj is long)
                                numMembers = (int)(long)numMembersObj;
                            else if (numMembersObj is double)
                                numMembers = (int)(double)numMembersObj;

                            OnlinePlayersText.text = "PLAYERS " + numMembers;

                            // Start match only if there are at least 2 members
                            if (numMembers >= 2)
                            {
                                Debug.Log("Enough players in lobby, starting match...");
                                // Parse ownerCxId and extract profileId
                                if (lobby.ContainsKey("ownerCxId"))
                                {
                                    string ownerCxId = (string)lobby["ownerCxId"];
                                    Debug.Log($"ownerCxId: {ownerCxId}");
                                    string[] parts = ownerCxId.Split(':');
                                    string ownerProfileId = parts.Length > 1 ? parts[1] : ownerCxId;
                                    Debug.Log($"ownerCxId: {ownerCxId}, extracted profileId: {ownerProfileId}");
                                    if (ownerProfileId == App.Bc.Client.ProfileId)
                                    {
                                        Debug.Log("We are the owner, starting match...");
                                        // Create PlayerInfo from the other player
                                        if (lobby.ContainsKey("members"))
                                        {
                                            var members = lobby["members"] as Dictionary<string, object>[];
                                            if (members != null)
                                            {
                                                foreach (var member in members)
                                                {
                                                    string memberCxId = (string)member["cxId"];
                                                    string[] memberParts = memberCxId.Split(':');
                                                    string memberProfileId = memberParts.Length > 1 ? memberParts[1] :
                                                        memberCxId;
                                                    if (memberProfileId != App.Bc.Client.ProfileId)
                                                    {
                                                        var matchedProfile = new PlayerInfo(member);
                                                        OnPickOpponent(matchedProfile, false);
                                                        Debug.Log("Starting match with opponent: " + matchedProfile.PlayerName);
                                                        CleanupOnlineLobby();
                                                        break;
                                                    }
                                                }
                                            }
                                        }

                                        CleanupOnlineLobby();
                                    }
                                    else
                                    {
                                        Debug.Log("Waiting for more players to join the lobby...");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class LobbyParams
    {
        public Dictionary<string, object> algo;
        public Dictionary<string, object> filters;
        public Dictionary<string, object> extra;
        public Dictionary<string, object> settings;
    }

    private LobbyParams CreateLobbyParams()
    {
        var algo = new Dictionary<string, object>
        {
            ["strategy"] = "ranged-absolute",
            ["alignment"] = "center",
            ["ranges"] = new List<int> { 1000 }
        };

        var filters = new Dictionary<string, object>();
        var extra = new Dictionary<string, object>();
        var settings = new Dictionary<string, object>();

        return new LobbyParams
        {
            algo = algo,
            filters = filters,
            extra = extra,
            settings = settings
        };
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
    //Callback for when a new match is created    
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

        // before adding the cell, make sure there isn't another already defined in the list
        for (int i = 0; i < m_itemCell.Count; ++i)
        {
            if (m_itemCell[i].MatchInfo.matchId == match.matchId)
            {
                Debug.Log("Match cell already exists.");
                return;
            }
        }

        //Create game button cell
        GameButtonCell newItem = CreateItemCell(MyGamesScrollView, (index % 2) == 0);
        newItem.Init(match, this);
        newItem.transform.localPosition = Vector3.zero;
        index = index < matches.Count ? index++ : 0;
        m_itemCell.Add(newItem);
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

    public void OnPickOpponent(PlayerInfo matchedProfile, bool randomTurnFirst = true)
    {
        var yourTurnFirst = randomTurnFirst ? Random.Range(0, 100) < 50 : true;

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
                isLookingForOnlineMatch = false;
                return;
            }
        }
        else
        {
            match = new MatchInfo(data, this);
        }

        // Go to the game if it's your turn
        if (match.yourTurn)
        {
            Debug.Log("You are the first player, entering match");
            EnterMatch(match);
        }
        else
        {
            Debug.Log("You are not the first player, waiting for opponent to play");
            App.GotoMatchSelectScene(gameObject);
        }
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
        App.WhosTurn = match.yourToken == "X" ? App.PlayerInfoX : App.PlayerInfoO;
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
