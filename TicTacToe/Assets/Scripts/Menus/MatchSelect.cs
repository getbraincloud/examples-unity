using System.Collections.Generic;
using LitJson;
using UnityEngine;

public class MatchSelect : GameScene
{
    private const int RANGE_DELTA = 10;
    private const int NUMBER_OF_MATCHES = 10;
    private readonly List<MatchInfo> completedMatches = new List<MatchInfo>();
    private readonly List<MatchedProfile> matchedProfiles = new List<MatchedProfile>();
    private readonly List<MatchInfo> matches = new List<MatchInfo>();

    private Vector2 scrollPos;
    private eState state = eState.LOADING;


    // Use this for initialization
    private void Start()
    {
        gameObject.transform.parent.gameObject.GetComponentInChildren<Camera>().rect = app.viewportRect;

        // Enable Match Making, so other Users can also challege this Profile
        // http://getbraincloud.com/apidocs/apiref/#capi-matchmaking-enablematchmaking
        app.bc.MatchMakingService
            .EnableMatchMaking((response, cbObject) => { Debug.Log(response); },
                (status, code, error, cbObject) => { Debug.Log(error); });

        app.bc.MatchMakingService.FindPlayers(RANGE_DELTA, NUMBER_OF_MATCHES, OnReadMatchedPlayerData);

        //App.BC.FriendService.ListFriends(BrainCloudFriend.FriendPlatform.Facebook, false, OnReadFriendData, null, null);// ReadFriendData(OnReadFriendData, null, null);
    }

    private void OnReadMatchedPlayerData(string responseData, object cbPostObject)
    {
        matchedProfiles.Clear();

        // Construct our matched players list using response data
        var matchesData = JsonMapper.ToObject(responseData)["data"]["matchesFound"];


        foreach (JsonData match in matchesData) matchedProfiles.Add(new MatchedProfile(match));


        app.bc.AsyncMatchService.FindMatches(OnFindMatchesSuccess);

        // After, fetch our game list from Braincloud
        app.bc.AsyncMatchService.FindMatches(OnFindMatchesSuccess);
    }

    private void OnFindMatchesSuccess(string responseData, object cbPostObject)
    {
        matches.Clear();

        // Construct our game list using response data
        var jsonMatches = JsonMapper.ToObject(responseData)["data"]["results"];
        for (var i = 0; i < jsonMatches.Count; ++i)
        {
            var jsonMatch = jsonMatches[i];

            var match = new MatchInfo(jsonMatch, this);
            matches.Add(match);
        }

        // Now, find completed matches so the user can go see the history
        app.bc.AsyncMatchService.FindCompleteMatches(OnFindCompletedMatches);
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

        state = eState.GAME_PICKER;
    }

    private void OnGUI()
    {
        switch (state)
        {
            case eState.LOADING:
            case eState.STARTING_MATCH:
            {
                GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUILayout.Label("Loading...");

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndArea();
                break;
            }
            case eState.GAME_PICKER:
            {
                GUILayout.Window(app.windowId,
                    new Rect(Screen.width / 2 - 150 + app.offset, Screen.height / 2 - 250, 300, 500),
                    OnPickGameWindow, "Pick Game");
                break;
            }
            case eState.NEW_GAME:
            {
                GUILayout.Window(app.windowId,
                    new Rect(Screen.width / 2 - 150 + app.offset, Screen.height / 2 - 250, 300, 500),
                    OnNewGameWindow, "Pick Opponent");
                break;
            }
        }
    }

    private void OnPickGameWindow(int windowId)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();

        scrollPos = GUILayout.BeginScrollView(scrollPos, false, false);

        if (GUILayout.Button("+ New Game", GUILayout.MinHeight(50), GUILayout.MaxWidth(250))) state = eState.NEW_GAME;
        foreach (var match in matches)
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUI.enabled = match.yourTurn;
            if (GUILayout.Button(
                match.matchedProfile.playerName + "\n" + (match.yourTurn ? "(Your Turn)" : "(His Turn)"),
                GUILayout.MinHeight(50), GUILayout.MaxWidth(200))) EnterMatch(match);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        foreach (var match in completedMatches)
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(match.matchedProfile.playerName + "\n(Completed)", GUILayout.MinHeight(50),
                GUILayout.MaxWidth(200))) EnterMatch(match);
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();


        if (GUILayout.Button("REFRESH"))
            app.bc.MatchMakingService.FindPlayers(RANGE_DELTA, NUMBER_OF_MATCHES, OnReadMatchedPlayerData);

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();


        GUILayout.EndHorizontal();
    }

    private void OnNewGameWindow(int windowId)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();

        scrollPos = GUILayout.BeginScrollView(scrollPos, false, false);

        if (GUILayout.Button("<- Cancel", GUILayout.MinHeight(32), GUILayout.MaxWidth(75)))
            state = eState.GAME_PICKER;

        foreach (var profile in matchedProfiles)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(profile.playerName, GUILayout.MinHeight(50), GUILayout.MaxWidth(200)))
                OnPickOpponent(profile);
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void OnPickOpponent(MatchedProfile matchedProfile)
    {
        state = eState.STARTING_MATCH;
        var yourTurnFirst = Random.Range(0, 100) < 50;

        // Setup our summary data. This is what we see when we query
        // the list of games.
        var summaryData = new JsonData();
        summaryData["players"] = new JsonData();
        {
            // Us
            var playerData = new JsonData();
            playerData["profileId"] = app.ProfileId;
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
            playerData["profileId"] = matchedProfile.playerId;
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
        app.bc.AsyncMatchService.CreateMatchWithInitialTurn(
            "[{\"platform\":\"BC\",\"id\":\"" + matchedProfile.playerId + "\"}]", // Opponents
            matchState.ToJson(), // Current match state
            "A friend has challenged you to a match of Tic Tac Toe.", // Push notification Message
            yourTurnFirst ? app.ProfileId : matchedProfile.playerId, // Which turn it is. We picked randomly
            summaryData.ToJson(), // Summary data
            OnCreateMatchSuccess,
            OnCreateMatchFailed,
            null);
    }

    private void OnCreateMatchSuccess(string responseData, object cbPostObject)
    {
        var data = JsonMapper.ToObject(responseData);
        var match = new MatchInfo(data["data"], this);

        // Go to the game if it's your turn
        if (match.yourTurn)
            EnterMatch(match);
        else
            app.GotoMatchSelectScene(gameObject);
    }

    private void OnCreateMatchFailed(int a, int b, string responseData, object cbPostObject)
    {
        Debug.LogError("Failed to create Async Match");
        Debug.Log(a);
        Debug.Log(b);
        Debug.Log(responseData);
        state = eState.GAME_PICKER; // Just go back to game selection
    }

    private void EnterMatch(MatchInfo match)
    {
        state = eState.LOADING;

        // Query more detail state about the match
        app.bc.AsyncMatchService
            .ReadMatch(match.ownerId, match.matchId, OnReadMatch, OnReadMatchFailed, match);
    }

    private void OnReadMatch(string responseData, object cbPostObject)
    {
        var match = cbPostObject as MatchInfo;
        var data = JsonMapper.ToObject(responseData)["data"];


        // Setup a couple stuff into our TicTacToe scene
        app.boardState = (string) data["matchState"]["board"];
        app.playerInfoX = match.playerXInfo;
        app.playerInfoO = match.playerOInfo;
        app.whosTurn = match.yourToken == "X" ? app.playerInfoX : match.playerOInfo;
        app.ownerId = match.ownerId;
        app.matchId = match.matchId;
        app.matchVersion = (ulong) match.version;

        // Load the Tic Tac Toe scene

        app.GotoTicTacToeScene(gameObject);
    }

    private void OnReadMatchFailed(int a, int b, string responseData, object cbPostObject)
    {
        Debug.LogError("Failed to Read Match");
    }

    public class MatchedProfile
    {
        public string playerId;
        public string playerName;
        public int playerRating;

        public MatchedProfile(JsonData jsonData)
        {
            playerName = (string) jsonData["playerName"];
            playerRating = (int) jsonData["playerRating"];
            playerId = (string) jsonData["playerId"];
        }
    }

    public class MatchInfo
    {
        public MatchedProfile matchedProfile;
        public string matchId;

        private readonly MatchSelect matchSelect;
        public string ownerId;
        public PlayerInfo playerOInfo = new PlayerInfo();
        public PlayerInfo playerXInfo = new PlayerInfo();
        public int version;
        public string yourToken;
        public bool yourTurn;

        public MatchInfo(JsonData jsonMatch, MatchSelect matchSelect)
        {
            version = (int) jsonMatch["version"];
            ownerId = (string) jsonMatch["ownerId"];
            matchId = (string) jsonMatch["matchId"];
            yourTurn = (string) jsonMatch["status"]["currentPlayer"] == matchSelect.app.ProfileId;

            this.matchSelect = matchSelect;

            // Load player info
            LoadPlayerInfo(jsonMatch["summary"]["players"][0]);
            LoadPlayerInfo(jsonMatch["summary"]["players"][1]);
        }

        private void LoadPlayerInfo(JsonData playerData)
        {
            var token = (string) playerData["token"];
            PlayerInfo playerInfo;
            if (token == "X") playerInfo = playerXInfo;
            else playerInfo = playerOInfo;

            if ((string) playerData["profileId"] == matchSelect.app.ProfileId)
            {
                playerInfo.name = matchSelect.app.PlayerName;
                //playerInfo.picUrl = FacebookLogin.PlayerPicUrl;				
                yourToken = token;
            }
            else
            {
                foreach (var profile in matchSelect.matchedProfiles)
                    if (profile.playerId == (string) playerData["profileId"])
                    {
                        matchedProfile = profile;
                        break;
                    }

                playerInfo.name = matchedProfile.playerName;
            }
        }
    }

    private enum eState
    {
        LOADING,
        GAME_PICKER,
        NEW_GAME,
        STARTING_MATCH
    }
}