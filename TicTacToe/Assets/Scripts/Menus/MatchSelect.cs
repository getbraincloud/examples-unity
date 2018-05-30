#region

using System.Collections.Generic;
using LitJson;
using UnityEngine;

#endregion

public class MatchSelect : GameScene
{
    private const int RANGE_DELTA = 500;
    private const int NUMBER_OF_MATCHES = 10;
    private readonly List<MatchInfo> completedMatches = new List<MatchInfo>();
    private readonly List<PlayerInfo> matchedProfiles = new List<PlayerInfo>();
    private readonly List<MatchInfo> matches = new List<MatchInfo>();

    private Vector2 _scrollPos;
    private eState _state = eState.LOADING;
    private string editablePlayerName = "";

    private bool isEditingPlayerName;


    // Use this for initialization
    private void Start()
    {
        gameObject.transform.parent.gameObject.GetComponentInChildren<Camera>().rect = App.ViewportRect;

        // Enable Match Making, so other Users can also challege this Profile
        // http://getbraincloud.com/apidocs/apiref/#capi-matchmaking-enablematchmaking
        App.Bc.MatchMakingService
            .EnableMatchMaking();

        App.Bc.MatchMakingService.FindPlayers(RANGE_DELTA, NUMBER_OF_MATCHES, OnReadMatchedPlayerData);
    }

    private void OnReadMatchedPlayerData(string responseData, object cbPostObject)
    {
        matchedProfiles.Clear();

        // Construct our matched players list using response data
        var matchesData = JsonMapper.ToObject(responseData)["data"]["matchesFound"];


        foreach (JsonData match in matchesData) matchedProfiles.Add(new PlayerInfo(match));


        // After, fetch our game list from Braincloud
        App.Bc.AsyncMatchService.FindMatches(OnFindMatchesSuccess);
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

        _state = eState.GAME_PICKER;
    }

    private void OnGUI()
    {
        var verticalMargin = 10;


        var profileWindowHeight = Screen.height * 0.20f - verticalMargin * 1.3f;
        var selectorWindowHeight = Screen.height * 0.80f - verticalMargin * 1.3f;


        GUILayout.Window(App.WindowId + 100,
            new Rect(Screen.width / 2 - 150 + App.Offset, verticalMargin, 300, profileWindowHeight),
            OnPlayerInfoWindow, "Profile");

        switch (_state)
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
                GUILayout.Window(App.WindowId,
                    new Rect(Screen.width / 2 - 150 + App.Offset, Screen.height - selectorWindowHeight - verticalMargin,
                        300, selectorWindowHeight),
                    OnPickGameWindow, "Pick Game");
                break;
            }
            case eState.NEW_GAME:
            {
                GUILayout.Window(App.WindowId,
                    new Rect(Screen.width / 2 - 150 + App.Offset, Screen.height - selectorWindowHeight - verticalMargin,
                        300, selectorWindowHeight),
                    OnNewGameWindow, "Pick Opponent");
                break;
            }
        }
    }

    private void OnPlayerInfoWindow(int windowId)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();


        GUILayout.BeginHorizontal();

        if (!isEditingPlayerName)
        {
            GUILayout.Label(string.Format("PlayerName: {0}", App.PlayerName), GUILayout.MinWidth(200));
            if (GUILayout.Button("Edit", GUILayout.MinWidth(50)))
            {
                editablePlayerName = App.PlayerName;
                isEditingPlayerName = true;
            }
        }
        else
        {
            editablePlayerName = GUILayout.TextField(editablePlayerName, GUILayout.MinWidth(200));
            if (GUILayout.Button("Save", GUILayout.MinWidth(50)))
            {
                App.PlayerName = editablePlayerName;
                isEditingPlayerName = false;

                App.Bc.PlayerStateService.UpdateUserName(App.PlayerName,
                    (response, cbObject) => { },
                    (status, code, error, cbObject) => { Debug.Log("Failed to change Player Name"); });
            }
        }


        GUILayout.EndHorizontal();

        GUILayout.Label(string.Format("PlayerRating: {0}", App.PlayerRating), GUILayout.MinWidth(200));

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Leaderboard", GUILayout.MinWidth(50))) App.GotoLeaderboardScene(gameObject);

        if (GUILayout.Button("Achievements", GUILayout.MinWidth(50))) App.GotoAchievementsScene(gameObject);

        GUILayout.EndHorizontal();


        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();


        GUILayout.EndHorizontal();
    }

    private void OnPickGameWindow(int windowId)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();

        _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false);

        if (GUILayout.Button("+ New Game", GUILayout.MinHeight(50), GUILayout.MaxWidth(250))) _state = eState.NEW_GAME;
        foreach (var match in matches)
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUI.enabled = match.yourTurn;
            if (GUILayout.Button(
                match.matchedProfile.PlayerName + "\n" + (match.yourTurn ? "(Your Turn)" : "(His Turn)"),
                GUILayout.MinHeight(50), GUILayout.MaxWidth(200)))
                EnterMatch(match);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        foreach (var match in completedMatches)
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if(match.matchedProfile != null)
            if (GUILayout.Button(match.matchedProfile.PlayerName + "\n(Completed)", GUILayout.MinHeight(50),
                GUILayout.MaxWidth(200))) EnterMatch(match);
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();


        if (GUILayout.Button("REFRESH"))
            App.Bc.MatchMakingService.FindPlayers(RANGE_DELTA, NUMBER_OF_MATCHES, OnReadMatchedPlayerData);

        if (GUILayout.Button("LOGOUT"))
        {
            App.Bc.PlayerStateService.Logout((response, cbObject) => { App.GotoLoginScene(gameObject); });
            PlayerPrefs.SetString(App.WrapperName + "_hasAuthenticated", "false");
        }

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();


        GUILayout.EndHorizontal();
    }

    private void OnNewGameWindow(int windowId)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();

        _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false);

        if (GUILayout.Button("<- Cancel", GUILayout.MinHeight(32), GUILayout.MaxWidth(75)))
            _state = eState.GAME_PICKER;

        foreach (var profile in matchedProfiles)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(string.Format("{0} ({1})", profile.PlayerName, profile.PlayerRating),
                GUILayout.MinHeight(50), GUILayout.MaxWidth(200)))
                OnPickOpponent(profile);
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void OnPickOpponent(PlayerInfo matchedProfile)
    {
        _state = eState.STARTING_MATCH;
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
        var match = new MatchInfo(data["data"], this);

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
        _state = eState.GAME_PICKER; // Just go back to game selection
    }

    private void EnterMatch(MatchInfo match)
    {
        App.CurrentMatch = match;
        _state = eState.LOADING;

        // Query more detail state about the match
        App.Bc.AsyncMatchService
            .ReadMatch(match.ownerId, match.matchId, OnReadMatch, OnReadMatchFailed, match);
    }

    private void OnReadMatch(string responseData, object cbPostObject)
    {
        var match = cbPostObject as MatchInfo;
        var data = JsonMapper.ToObject(responseData)["data"];


        // Setup a couple stuff into our TicTacToe scene
        App.BoardState = (string) data["matchState"]["board"];
        App.PlayerInfoX = match.playerXInfo;
        App.PlayerInfoO = match.playerOInfo;
        App.WhosTurn = match.yourToken == "X" ? App.PlayerInfoX : match.playerOInfo;
        App.OwnerId = match.ownerId;
        App.MatchId = match.matchId;
        App.MatchVersion = (ulong) match.version;

        // Load the Tic Tac Toe scene

        App.GotoTicTacToeScene(gameObject);
    }

    private void OnReadMatchFailed(int a, int b, string responseData, object cbPostObject)
    {
        Debug.LogError("Failed to Read Match");
    }

    public class MatchInfo
    {
        private readonly MatchSelect matchSelect;
        public PlayerInfo matchedProfile;
        public string matchId;
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
            yourTurn = (string) jsonMatch["status"]["currentPlayer"] == matchSelect.App.ProfileId;

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

            if ((string) playerData["profileId"] == matchSelect.App.ProfileId)
            {
                playerInfo.PlayerName = matchSelect.App.PlayerName;
                playerInfo.PlayerRating = matchSelect.App.PlayerRating;
                playerInfo.ProfileId = matchSelect.App.ProfileId;
                
                //playerInfo.picUrl = FacebookLogin.PlayerPicUrl;				
                yourToken = token;
            }
            else
            {
                if (matchSelect.matchedProfiles.Count > 0)
                {

                    foreach (var profile in matchSelect.matchedProfiles)
                        if (profile.ProfileId == (string) playerData["profileId"])
                        {
                            matchedProfile = profile;
                            break;
                        }

                    playerInfo.PlayerName = matchedProfile.PlayerName;
                    playerInfo.ProfileId = matchedProfile.ProfileId;
                    playerInfo.PlayerRating = matchedProfile.PlayerRating;
                }
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