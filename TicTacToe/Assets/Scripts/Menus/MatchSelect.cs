#region
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.LitJson;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using TMPro;
#endregion

public class MatchSelect : ResourcesManager
{
    private const int RANGE_DELTA = 500;
    private const int NUMBER_OF_MATCHES = 20;
    private readonly List<MatchInfo> completedMatches = new List<MatchInfo>();
    private readonly List<PlayerInfo> matchedProfiles = new List<PlayerInfo>();
    private readonly List<MatchInfo> matches = new List<MatchInfo>();

    private Vector2 _scrollPos;

    private bool isLookingForMatch = false;

    [SerializeField]
    private RectTransform MyGamesScrollView = null;
    [SerializeField]
    private Button CancelButton = null;
    [SerializeField]
    public TextMeshProUGUI MyGames;
    [SerializeField]
    private Spinner Spinner = null;

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

        m_itemCell = new List<GameButtonCell>();
        CancelButton.gameObject.SetActive(false);

        enableRTT();

        if (UserName != null)
            UserName.text = App.Name;
    }

    // Enable RTT
    private void enableRTT()
    {
        // Only Enable RTT if its not already started
        if (!App.Bc.RTTService.IsRTTEnabled())
        {
            App.Bc.RTTService.EnableRTT(RTTConnectionType.WEBSOCKET, onRTTEnabled, onRTTFailure);
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

    private void onRTTFailure(int status, int reasonCode, string responseData, object cbPostObject)
    {
        // TODO! Bring up a user dialog to inform of poor connection
        // for now, try to auto connect 
        if (this != null && this.gameObject != null) Invoke("enableRTT", 5.0f);
    }

    private void OnFindPlayers(string responseData, object cbPostObject)
    {
        matchedProfiles.Clear();

        // Construct our matched players list using response data
        var matchesData = JsonMapper.ToObject(responseData)["data"]["matchesFound"];
        foreach (JsonData match in matchesData) matchedProfiles.Add(new PlayerInfo(match));

        // After, fetch our game list from Braincloud
        App.Bc.AsyncMatchService.FindMatches(OnFindMatches);
    }

    private void OnFindMatches(string responseData, object cbPostObject)
    {
        matches.Clear();

        // Construct our game list using response data
        var jsonMatches = JsonMapper.ToObject(responseData)["data"]["results"];
        for (var i = 0; i < jsonMatches.Count; ++i)
        {
            var jsonMatch = jsonMatches[i];

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
        var jsonMatches = JsonMapper.ToObject(responseData)["data"]["results"];
        for (var i = 0; i < jsonMatches.Count; ++i)
        {
            var jsonMatch = jsonMatches[i];
            var match = new MatchInfo(jsonMatch, this);
            completedMatches.Add(match);
        }

        OnPopulateMatches();
    }

    private void OnPopulateMatches()
    {
        if (this != null)
        {
            Spinner.gameObject.SetActive(false);
            MyGames.text = "My Games";
            RemoveAllCellsInView(m_itemCell);
            PopulateMatchesScrollView(matches, m_itemCell, MyGamesScrollView);
            PopulateMatchesScrollView(completedMatches, m_itemCell, MyGamesScrollView);
        }
    }

    public void OnNewGameButton(int WindowId)
    {
        OnGoToMatchSelectScene();
        Spinner.gameObject.SetActive(false);
        MyGames.text = "Pick Opponent";
        CancelButton.gameObject.SetActive(true);
        PopulatePlayersScrollView(matchedProfiles, m_itemCell, MyGamesScrollView);
    }

    public void OnCancelButton()
    {
        CancelButton.gameObject.SetActive(false);
        RemoveAllCellsInView(m_itemCell);
        OnPopulateMatches();
    }

    public void OnMatchSelected(MatchInfo match)
    {
        if (match != null)
        {
            App.CurrentMatch = match;

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

            var MATCH_STATE = "MATCH_STATE";
            var CANCEL_LOOKING = "CANCEL_LOOKING";

            var attributesJson = new JsonData();
            attributesJson[MATCH_STATE] = CANCEL_LOOKING;

            App.Bc.PlayerStateService.UpdateAttributes(attributesJson.ToJson(), false);
        }
        else
        {
            var scriptDataJson = new JsonData();
            scriptDataJson["rankRangeDelta"] = RANGE_DELTA;
            scriptDataJson["pushNotificationMessage"] = null;

            App.Bc.ScriptService.RunScript("RankGame_AutoJoinMatch", scriptDataJson.ToJson(), OnCreateMatchSuccess, OnCreateMatchFailed);
        }
    }

    private void PopulateMatchesScrollView(List<MatchInfo> in_itemItems, List<GameButtonCell> in_itemCell, RectTransform in_scrollView)
    {
        if (in_itemItems.Count == 0)
        {
            return;
        }

        if (in_scrollView != null)
        {
            int i = 0;
            //            foreach (var profile in in_itemItems)
            foreach (var match in in_itemItems)
            {
                GameButtonCell newItem = CreateItemCell(in_scrollView, (i % 2) == 0);
                newItem.Init(match, this);
                newItem.transform.localPosition = Vector3.zero;
                in_itemCell.Add(newItem);
                i++;
            }
        }
    }

    private void PopulatePlayersScrollView(List<PlayerInfo> in_itemItems, List<GameButtonCell> in_itemCell, RectTransform in_scrollView)
    {
        RemoveAllCellsInView(in_itemCell);
        if (in_itemItems.Count == 0)
        {
            return;
        }

        if (in_scrollView != null)
        {
            int i = 0;
            foreach (var profile in in_itemItems)
            {
                GameButtonCell newItem = CreateItemCell(in_scrollView, (i % 2) == 0);
                newItem.Init(profile, this);
                newItem.transform.localPosition = Vector3.zero;
                in_itemCell.Add(newItem);
                i++;
            }
        }
    }

    private void OnPickOpponent(PlayerInfo matchedProfile)
    {
        var yourTurnFirst = Random.Range(0, 100) < 50;

        // Setup our summary data. This is what we see when we query
        // the list of games.
        var summaryData = new JsonData();
        summaryData["players"] = new JsonData();
        {
            // Us
            var playerData = new JsonData();
            playerData["profileId"] = App.ProfileId;
            //playerData["facebookId"] = FB.UserId;
            if (yourTurnFirst)
                playerData["token"] = "X"; // First player has X
            else
                playerData["token"] = "O";

            summaryData["players"].Add(playerData);
        }
        {
            // Our friend
            var playerData = new JsonData();
            playerData["profileId"] = matchedProfile.ProfileId;
            if (!yourTurnFirst)
                playerData["token"] = "X"; // First player has X
            else
                playerData["token"] = "O";

            summaryData["players"].Add(playerData);
        }

        // Setup our match State. We only store where Os and Xs are in
        // the tic tac toe board. 
        var matchState = new JsonData();
        matchState["board"] = "#########"; // Empty the board. # = nothing, O,X = tokens

        // Setup our opponent list. In this case, we have just one opponent.
        //JsonData opponentIds = new JsonData();

        // Create the match
        App.Bc.AsyncMatchService.CreateMatchWithInitialTurn(
            "[{\"platform\":\"BC\",\"id\":\"" + matchedProfile.ProfileId + "\"}]", // Opponents
            matchState.ToJson(), // Current match state
            "A friend has challenged you to a match of Tic Tac Toe.", // Push notification Message
            yourTurnFirst ? App.ProfileId : matchedProfile.ProfileId, // Which turn it is. We picked randomly
            summaryData.ToJson(), // Summary data
            OnCreateMatchSuccess,
            OnCreateMatchFailed,
            null);
    }

    private void OnCreateMatchSuccess(string responseData, object cbPostObject)
    {
        var data = JsonMapper.ToObject(responseData);
        MatchInfo match;

        // Cloud Code returns wrap the data in a responseJson
        if (data["data"].Keys.Contains("response"))
        {
            if (data["data"]["response"].IsObject && data["data"]["response"].Keys.Contains("data"))
            {
                match = new MatchInfo(data["data"]["response"]["data"], this);
            }
            else
            {
                // No match found. Handle this result
                Debug.Log(data["data"]["response"].ToString());

                isLookingForMatch = true;
                return;
            }
        }
        else
        {
            match = new MatchInfo(data["data"], this);
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
        var data = JsonMapper.ToObject(responseData)["data"];

        // Setup a couple stuff into our TicTacToe scene
        App.BoardState = (string)data["matchState"]["board"];
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
        Debug.LogError("Failed to Read Match");
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

        public MatchInfo(JsonData jsonMatch, MatchSelect matchSelect)
        {
            version = (int)jsonMatch["version"];
            ownerId = (string)jsonMatch["ownerId"];
            matchId = (string)jsonMatch["matchId"];
            yourTurn = (string)jsonMatch["status"]["currentPlayer"] == matchSelect.App.ProfileId;
            complete = (string)jsonMatch["status"]["status"] == "COMPLETE";
            expired = (string)jsonMatch["status"]["status"] == "EXPIRED";

            this.matchSelect = matchSelect;

            // Load player info
            LoadPlayerInfo(jsonMatch, 0);
            LoadPlayerInfo(jsonMatch, 1);
        }

        private void LoadPlayerInfo(JsonData jsonMatch, int index)
        {
            JsonData playerData = jsonMatch["players"][index];
            JsonData playerSummaryData = jsonMatch["summary"]["players"][index];

            var token = (string)playerSummaryData["token"];
            PlayerInfo playerInfo = new PlayerInfo();
            if (token == "X") playerInfo = playerXInfo;
            else playerInfo = playerOInfo;

            if ((string)playerSummaryData["profileId"] == matchSelect.App.ProfileId)
            {
                playerInfo.PlayerName = matchSelect.App.Name;
                playerInfo.PlayerRating = matchSelect.App.PlayerRating;
                playerInfo.ProfileId = matchSelect.App.ProfileId;
                yourToken = token;
            }
            else
            {
                playerInfo.PlayerName = (string)playerData["playerName"];
                playerInfo.ProfileId = (string)playerSummaryData["profileId"];
                playerInfo.PlayerRating = "1000";
                matchedProfile = playerInfo;
            }
        }
    }
}
