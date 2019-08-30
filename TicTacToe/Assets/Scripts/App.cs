#region

using UnityEngine;
using System.Collections.Generic;

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

    string serverUrl = "https://internal.braincloudservers.com/dispatcherv2";//slightly changed
    string secret = "e21cd163-394a-48c9-aa9c-0f58305e9dcf";
    string appId = "22907";
    Dictionary<string, string> secretMap = new Dictionary<string, string>();
    string version = "1.0.0";


    // All Game Scenes
    [SerializeField] public GameObject Achievements;
    [SerializeField] public GameObject Leaderboard;
    [SerializeField] public GameObject Login;
    [SerializeField] public GameObject MatchSelect;
    [SerializeField] public GameObject TicTacToe;

    // Variables for handling local multiplayer
    [SerializeField] public int Offset;   
    [SerializeField] public Rect ViewportRect;
    [SerializeField] public int WindowId;
    [SerializeField] public string WrapperName;


    private void Start()
    {
        var playerOneObject = new GameObject(WrapperName);

        Bc = gameObject.AddComponent<BrainCloudWrapper>(); // Create the brainCloud Wrapper
        DontDestroyOnLoad(this); // on an Object that won't be destroyed on Scene Changes

        Bc.WrapperName = WrapperName; // Optional: Add a WrapperName
        secretMap.Add(appId, secret);
        Bc.InitWithApps(serverUrl, appId, secretMap, version);
        Bc.Client.EnableLogging(true);

        // Now that brainCloud is setup. Let's go to the Login Scene

        var loginObject = Instantiate(Login);
        loginObject.GetComponentInChildren<GameScene>().App = this;
        loginObject.transform.parent = playerOneObject.transform;
    }

    private void Update()
    {
        // If you aren't attaching brainCloud as a Component to a gameObject,
        // you must manually update it with this call.
        // _bc.Update();
        // 
        // Given we are using a game Object. Leave _bc.Update commented out.
    }
    
    
    
    // Scene Swapping Logic
    public void GotoLoginScene(GameObject previousScene)
    {
        var newScene = Instantiate(Login);
        newScene.transform.parent = previousScene.transform.parent.transform.parent;
        newScene.GetComponentInChildren<GameScene>().App = this;
        Destroy(previousScene.transform.parent.gameObject);
    }

    public void GotoMatchSelectScene(GameObject previousScene)
    {
        var newScene = Instantiate(MatchSelect);
        newScene.transform.parent = previousScene.transform.parent.transform.parent;
        newScene.GetComponentInChildren<GameScene>().App = this;
        Destroy(previousScene.transform.parent.gameObject);
    }

    public void GotoLeaderboardScene(GameObject previousScene)
    {
        var newScene = Instantiate(Leaderboard);
        newScene.transform.parent = previousScene.transform.parent.transform.parent;
        newScene.GetComponentInChildren<GameScene>().App = this;
        Destroy(previousScene.transform.parent.gameObject);
    }

    public void GotoAchievementsScene(GameObject previousScene)
    {
        var newScene = Instantiate(Achievements);
        newScene.transform.parent = previousScene.transform.parent.transform.parent;
        newScene.GetComponentInChildren<GameScene>().App = this;
        Destroy(previousScene.transform.parent.gameObject);
    }

    public void GotoTicTacToeScene(GameObject previousScene)
    {
        var newScene = Instantiate(TicTacToe);
        newScene.transform.parent = previousScene.transform.parent.transform.parent;
        newScene.GetComponentInChildren<GameScene>().App = this;
        Destroy(previousScene.transform.parent.gameObject);
    }
}