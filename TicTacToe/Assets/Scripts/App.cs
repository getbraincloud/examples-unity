#region

using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using UnityEngine;

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
    public string PlayerRating;
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
    private TicTacToe _localTicTacToe;
    private MatchSelect _localMatchSelect;

    public MatchSelect MyMatchSelect
    {
        get => _localMatchSelect;
        set => _localMatchSelect = value;
    }
    public bool IsAskingToRematch;
    private void Start()
    {
        var playerOneObject = new GameObject(WrapperName);

        Bc = gameObject.AddComponent<BrainCloudWrapper>(); // Create the brainCloud Wrapper
        DontDestroyOnLoad(this); // on an Object that won't be destroyed on Scene Changes

        Bc.WrapperName = WrapperName; // Optional: Add a WrapperName
        Bc.Init(); // Required: Initialize the Wrapper.
        //Bc.Client.EnableLogging(true);

        // Now that brainCloud is setup. Let's go to the Login Scene
        var loginObject = Instantiate(Login);
        loginObject.GetComponentInChildren<GameScene>().App = this;
        loginObject.transform.parent = playerOneObject.transform;
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
            Bc.EventService.DeleteIncomingEvent(eventID);

            //Enable ask to play again screen
            if (!IsAskingToRematch && AskedToRematch)
            {
                if (_localTicTacToe)
                {
                    _localTicTacToe.AskToRematchScreen.SetActive(true);    
                }
            }
            //Disable wait screen for asking user to rematch
            else if (IsAskingToRematch)
            {
                if (_localTicTacToe)
                {
                    _localTicTacToe.PleaseWaitScreen.SetActive(false);
                    GotoMatchSelectScene(_localTicTacToe.gameObject);    
                }
            }
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
}