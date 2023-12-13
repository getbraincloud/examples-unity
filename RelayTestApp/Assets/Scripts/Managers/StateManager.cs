using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrainCloud;

/// <summary>
/// - Responsible for switching states from either button events or loading events
/// - If there's an error, it will give an error pop up and send you back to Sign In game state.
/// - Holding information such as
///     - What state I'm currently in
///     - Transition from State to State
///     - Clean up game objects when game is finished
///     - Info about Server and Lobby
/// </summary>

public enum GameStates{SignIn,MainMenu,Lobby,Match,Connecting}
public class StateManager : MonoBehaviour
{
    //Game States
    public List<GameState> ListOfStates = new List<GameState>();
    public GameStates CurrentGameState;
    public ConnectingGameState LoadingGameState;
    public DialogueMessage ErrorMessage;
    public GameObject LobbyFFAView;
    public GameObject LobbyTeamView;
    public GameObject MatchFFAView;
    public GameObject MatchTeamView;
    
    //Network info needed
    [SerializeField]
    public Lobby CurrentLobby;
    [SerializeField]
    public Server CurrentServer;
    internal RelayConnectionType Protocol { get; set; }
    
    //Specific for loading and waiting
    public bool isReady;
    public bool isLoading;
    
    //Used to clean up objects when game is finished
    public List<GameObject> Shockwaves = new List<GameObject>();
    
    //Messages for loading screen
    private const string LOGGING_IN_MESSAGE = "Logging in...";
    private const string LOOKING_FOR_LOBBY_MESSAGE = "Joining Lobby...";
    private const string JOINING_MATCH_MESSAGE = "Joining Match...";
    
    //Singleton
    private static StateManager _instance;
    public static StateManager Instance => _instance;
    
    protected virtual void Awake()
    {
        if (!_instance)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        ChangeState(GameStates.SignIn);
    }

    public void AbortToSignIn(string errorMessage)
    {
        ErrorMessage.SetUpPopUpMessage(errorMessage);
        LoadingGameState.CancelNextState = true;
        ChangeState(GameStates.SignIn);
    }

    public void LeaveToMainMenu()
    {
        StartCoroutine(DelayToDisconnect());
    }

    IEnumerator DelayToDisconnect()
    {
        yield return new WaitForSeconds(0.2f);
        GameManager.Instance.LobbyIdText.enabled = false;
        BrainCloudManager.Instance.CloseGame();
        ChangeState(GameStates.MainMenu);
        ResetData();
        yield return new WaitForFixedUpdate();
    }

    public void LeaveMatchBackToMenu()
    {
        GameManager.Instance.LobbyIdText.enabled = false;
        ResetData();
        ChangeState(GameStates.SignIn);
    }

    private void ResetData()
    {
        CurrentServer = null;
        isReady = false;
        
        foreach (GameObject shockwave in Shockwaves)
        {
            if (shockwave != null)
            {
                Destroy(shockwave);    
            }
        }
        Shockwaves = new List<GameObject>();
        GameManager.Instance.EmptyCursorList();
        GameManager.Instance.CurrentUserInfo.IsAlive = false;
        GameManager.Instance.CurrentUserInfo.MousePosition = Vector2.zero;
    }
    
    //Takes in the current Game state to then load into the next game state
    public void ButtonPressed_ChangeState(GameStates newState = GameStates.Connecting)
    {
        foreach (GameState state in ListOfStates)
        {
            state.gameObject.SetActive(false);
        }

        if (newState != GameStates.Connecting)
        {
            CurrentGameState = newState;
        }
        EnableCurrentGameModeScreen();
        isLoading = true;
        //User is in this state and moving onto the next
        switch (CurrentGameState)
        {
            //Logging In...
            case GameStates.SignIn:
                CurrentGameState = GameStates.MainMenu;
                CheckToEnableReconnectButton();
                BrainCloudManager.Instance.Login();
                LoadingGameState.ConnectStatesWithLoading(LOGGING_IN_MESSAGE,false,GameStates.MainMenu);
                break;
            //Looking for Lobby...
            case GameStates.MainMenu:
                CurrentGameState = GameStates.Lobby;
                BrainCloudManager.Instance.FindLobby(Protocol);
                LoadingGameState.ConnectStatesWithLoading(LOOKING_FOR_LOBBY_MESSAGE,true, CurrentGameState);
                break;
            //Setting up Match...
            case GameStates.Lobby:
                CurrentGameState = GameStates.Match;
                BrainCloudManager.Instance.StartGame();
                LoadingGameState.ConnectStatesWithLoading(JOINING_MATCH_MESSAGE,false, CurrentGameState);
                break;
        }
    }
    
    private void CheckToEnableReconnectButton()
    {
        if (CurrentLobby != null && CurrentLobby.LobbyID.Length > 0)
        {
            GameManager.Instance.ReconnectButton.gameObject.SetActive(true);
        }
        else
        {
            GameManager.Instance.ReconnectButton.gameObject.SetActive(false);
        }
    }

    private void EnableCurrentGameModeScreen()
    {
        
        if (CurrentGameState == GameStates.Lobby)
        {
            if (GameManager.Instance.GameMode == GameMode.FreeForAll)
            {
                LobbyTeamView.SetActive(false);
                LobbyFFAView.SetActive(true);
            }
            else
            {
                LobbyTeamView.SetActive(true);
                LobbyFFAView.SetActive(false);
            }
        }
        else if (CurrentGameState == GameStates.Match)
        {
            if (GameManager.Instance.GameMode == GameMode.FreeForAll)
            {
                MatchTeamView.SetActive(false);
                MatchFFAView.SetActive(true);
            }
            else
            {
                MatchTeamView.SetActive(true);
                MatchFFAView.SetActive(false);
            }
        }
    }

    public void ReconnectToGame()
    {
        foreach (GameState state in ListOfStates)
        {
            state.gameObject.SetActive(false);
        }
        
        CurrentGameState = GameStates.Match;
        isLoading = true;
        LoadingGameState.ConnectStatesWithLoading(JOINING_MATCH_MESSAGE,false,GameStates.Match);

        BrainCloudManager.Instance.ReconnectUser();
    }

    
    public void ChangeState(GameStates newGameState)
    {
        CurrentGameState = newGameState;
        EnableCurrentGameModeScreen();
        if(newGameState == GameStates.MainMenu)
        {
            CheckToEnableReconnectButton();
        }
        foreach (GameState currentState in ListOfStates)
        {
            currentState.gameObject.SetActive(currentState.CurrentGameState == newGameState);
        }
    }
}
