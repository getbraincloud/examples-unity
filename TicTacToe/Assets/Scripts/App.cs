#region

using System.Collections.Generic;
using BrainCloud.LitJson;
using UnityEngine;
using JsonReader = BrainCloud.JsonFx.Json.JsonReader;

#endregion

public class App : MonoBehaviour
{
    public BrainCloudWrapper Bc;

    // Setup a couple stuff into our TicTacToe scene
    public string BoardState = "#########";
    
    public string MatchId;
    public ulong MatchVersion;
    public string OwnerId;
    public MatchSelect.MatchInfo CurrentMatch;
    public PlayerInfo PlayerInfoO = new PlayerInfo();
    public PlayerInfo PlayerInfoX = new PlayerInfo();
    
    public PlayerInfo WhosTurn;
    public string Name;
    public string ProfileId;
    
    public bool AskedToRematch;
    // All Game Scenes
    [SerializeField] public GameObject Achievements;
    [SerializeField] public GameObject Leaderboard;
    [SerializeField] public GameObject Login;
    [SerializeField] public GameObject MatchSelect;
    [SerializeField] public GameObject TicTacToeX;
    [SerializeField] public GameObject TicTacToeO;

    // Variables for handling local multiplayer
    [SerializeField] public int Offset;   
    [SerializeField] public Rect ViewportRect;
    [SerializeField] public int WindowId;
    [SerializeField] public string WrapperName;
    
    public PlayerInfo OpponentInfo;
    public bool IsAskingToRematch;
    public int Winner;
    public PlayerInfo WinnerInfo = null;
    public PlayerInfo LoserInfo = null;
    private TicTacToe _localTicTacToe;
    private MatchSelect _localMatchSelect;
    
    public MatchSelect MyMatchSelect
    {
        get => _localMatchSelect;
    }
    
    private void Start()
    {
        var playerOneObject = new GameObject(WrapperName);

        Bc = gameObject.AddComponent<BrainCloudWrapper>(); // Create the brainCloud Wrapper
        DontDestroyOnLoad(this); // on an Object that won't be destroyed on Scene Changes

        Bc.WrapperName = WrapperName; // Optional: Add a WrapperName
        Bc.Init(); // Required: Initialize the Wrapper.
        //Bc.Client.EnableLogging(true);
        
        // Now that brainCloud is setup. Let's go to the Login Scene
        var loginObject = Instantiate(Login, playerOneObject.transform);
        loginObject.GetComponentInChildren<GameScene>().App = this;
    }

    //private void Update()
    //{
        // If you aren't attaching brainCloud as a Component to a gameObject,
        // you must manually update it with this call.
        // _bc.Update();
        // 
        // Given we are using a game Object. Leave _bc.Update commented out.
    //}
    
    //Callback used for "Play Again?" scenario
    public void RTTEventCallback(string json)
    {
        var jsonData = JsonReader.Deserialize<Dictionary<string, object>>(json);
        var data = jsonData["data"] as Dictionary<string, object>;
        
        if (data.ContainsKey("eventData"))
        {
            var eventData = data["eventData"] as Dictionary<string,object>;
            AskedToRematch = (bool)eventData["isReady"];
            string eventID = (string)data["evId"];
            
            //Enable play again screen to the asked user
            if (!IsAskingToRematch && AskedToRematch)
            {
                if (eventData.Count > 1)
                {
                    //Set Up opponent reference that wants to rematch
                    OpponentInfo = new PlayerInfo
                    {
                        ProfileId = (string)eventData["opponentProfileID"],
                        PlayerName = (string)eventData["opponentName"],
                    };
                }
                if (_localTicTacToe)
                {
                    _localTicTacToe.AskToRematchScreen.SetActive(true);    
                }
                else if (_localMatchSelect)
                {
                    _localMatchSelect.AskToRematchScreen.SetActive(true);
                }
            }
            else if (AskedToRematch)
            {
                if (_localTicTacToe)
                {
                    GotoMatchSelectScene(_localTicTacToe.gameObject);
                }
            }
            Bc.EventService.DeleteIncomingEvent(eventID);
        }
    }

    // Scene Swapping Logic
    public void GotoLoginScene(GameObject previousScene)
    {
        var newScene = Instantiate(Login);
        newScene.transform.parent = previousScene.transform.parent.transform.parent;
        GameScene[] scenes = newScene.GetComponentsInChildren<GameScene>();
        foreach(GameScene scene in scenes)
        {
            scene.App = this;
        }
        Destroy(previousScene.transform.parent.gameObject);
    }

    public void GotoMatchSelectScene(GameObject previousScene)
    {
        _localMatchSelect = null;
        var newScene = Instantiate(MatchSelect);
        _localMatchSelect = newScene.transform.GetChild(0).GetComponent<MatchSelect>();
        newScene.transform.parent = previousScene.transform.parent.transform.parent;
        GameScene[] scenes = newScene.GetComponentsInChildren<GameScene>();
        foreach (GameScene scene in scenes)
        {
            scene.App = this;
        }
        Destroy(previousScene.transform.parent.gameObject);
    }

    public void GotoLeaderboardScene(GameObject previousScene)
    {
        var newScene = Instantiate(Leaderboard);
        newScene.transform.parent = previousScene.transform.parent.transform.parent;
        GameScene[] scenes = newScene.GetComponentsInChildren<GameScene>();
        foreach (GameScene scene in scenes)
        {
            scene.App = this;
        }
        Destroy(previousScene.transform.parent.gameObject);
    }

    public void GotoAchievementsScene(GameObject previousScene)
    {
        var newScene = Instantiate(Achievements);
        newScene.transform.parent = previousScene.transform.parent.transform.parent;
        GameScene[] scenes = newScene.GetComponentsInChildren<GameScene>();
        foreach (GameScene scene in scenes)
        {
            scene.App = this;
        }
        Destroy(previousScene.transform.parent.gameObject);
    }

    public void GotoTicTacToeScene(GameObject previousScene)
    {
        _localTicTacToe = null;
        var newScene = Instantiate(CurrentMatch.yourToken == "X" ? TicTacToeX: TicTacToeO);
        _localTicTacToe = newScene.transform.GetChild(7).GetComponent<TicTacToe>();
        newScene.transform.parent = previousScene.transform.parent.transform.parent;
        GameScene[] scenes = newScene.GetComponentsInChildren<GameScene>();
        foreach (GameScene scene in scenes)
        {
            scene.App = this;
        }
        Destroy(previousScene.transform.parent.gameObject);
    }

    public void OnCompleteGame()
    {
        // However, we are using a custom FINISH_RANK_MATCH script which is set up on brainCloud. View the commented Cloud Code script below
        var matchResults = new JsonData { ["ownerId"] = OwnerId, ["matchId"] = MatchId };

        if (Winner < 0)
        {
            matchResults["isTie"] = true;
        }
        else
        {
            matchResults["isTie"] = false;
            matchResults["winnerId"] = WinnerInfo.ProfileId;
            matchResults["loserId"] = LoserInfo.ProfileId;
        }

        Bc.ScriptService.RunScript("RankGame_FinishMatch", matchResults.ToJson(), OnMatchCompleted,
            (status, code, error, cbObject) => { });
    }
    
    private void OnMatchCompleted(string responseData, object cbPostObject)
    {
        if (_localTicTacToe)
        {
            // Go back to game select scene
            GotoMatchSelectScene(_localTicTacToe.gameObject);   
        }
    }
}