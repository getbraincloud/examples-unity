using UnityEngine;

public class App : MonoBehaviour
{
    public BrainCloudWrapper bc;


    // Setup a couple stuff into our TicTacToe scene
    public string boardState = "#########";

    [SerializeField] public GameObject login;
    public string matchId;
    [SerializeField] public GameObject matchSelect;
    public ulong matchVersion;

    [SerializeField] public int offset;
    public string ownerId;
    public PlayerInfo playerInfoO = new PlayerInfo();
    public PlayerInfo playerInfoX = new PlayerInfo();
    public string PlayerName;


    public string ProfileId;
    [SerializeField] public GameObject ticTacToe;
    [SerializeField] public Rect viewportRect;
    public PlayerInfo whosTurn;
    [SerializeField] public int windowId;

    [SerializeField] public string wrapperName;


    private void Start()
    {
        var playerOneObject = new GameObject(wrapperName);

        bc = gameObject.AddComponent<BrainCloudWrapper>(); // Create the brainCloud Wrapper
        DontDestroyOnLoad(this); // on an Object that won't be destroyed on Scene Changes

        bc.WrapperName = wrapperName; // Optional: Add a WrapperName
        bc.Init(); // Required: Initialize the Wrapper.

        // Now that brainCloud is setup. Let's go to the Login Scene

        var loginObject = Instantiate(login);
        loginObject.GetComponentInChildren<GameScene>().app = this;
        loginObject.transform.parent = playerOneObject.transform;
    }

    public void GotoLoginScene(GameObject previousScene)
    {
        var newScene = Instantiate(login);
        newScene.transform.parent = previousScene.transform.parent.transform.parent;
        newScene.GetComponentInChildren<GameScene>().app = this;
        Destroy(previousScene.transform.parent.gameObject);
    }

    public void GotoMatchSelectScene(GameObject previousScene)
    {
        var newScene = Instantiate(matchSelect);
        newScene.transform.parent = previousScene.transform.parent.transform.parent;
        newScene.GetComponentInChildren<GameScene>().app = this;
        Destroy(previousScene.transform.parent.gameObject);
    }

    public void GotoTicTacToeScene(GameObject previousScene)
    {
        var newScene = Instantiate(ticTacToe);
        newScene.transform.parent = previousScene.transform.parent.transform.parent;
        newScene.GetComponentInChildren<GameScene>().app = this;
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