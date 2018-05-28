#region

using UnityEngine;

#endregion

public class App : MonoBehaviour
{
    [SerializeField] public GameObject Achievements;
    public BrainCloudWrapper Bc;


    // Setup a couple stuff into our TicTacToe scene
    public string BoardState = "#########";
    [SerializeField] public GameObject Leaderboard;
    public string MatchId;
    public ulong MatchVersion;
    public string OwnerId;
    public PlayerInfo PlayerInfoO = new PlayerInfo();
    public PlayerInfo PlayerInfoX = new PlayerInfo();
    public PlayerInfo WhosTurn;
    public string PlayerName;
    public string PlayerRating;
    public string ProfileId;

    
    // All Game Scenes
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
        Bc.Init(); // Required: Initialize the Wrapper.

        // Now that brainCloud is setup. Let's go to the Login Scene

        var loginObject = Instantiate(Login);
        loginObject.GetComponentInChildren<GameScene>().App = this;
        loginObject.transform.parent = playerOneObject.transform;
    }

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

    private void Update()
    {
        // If you aren't attaching brainCloud as a Component to a gameObject,
        // you must manually update it with this call.
        // _bc.Update();
        // 
        // Given we are using a game Object. Leave _bc.Update commented out.
    }
}