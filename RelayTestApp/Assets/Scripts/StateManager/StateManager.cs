using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrainCloud;
/// <summary>
/// - Responsible for switching states from either button events or loading events
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
    
    //Network info needed
    public Lobby CurrentLobby;
    public Server CurrentServer;
    public RelayConnectionType protocol = RelayConnectionType.WEBSOCKET;
    
    //Specific for loading and waiting
    public bool isReady;
    public bool isLoading;
    
    //Used to clean up objects when game is finished
    public List<GameObject> Shockwaves = new List<GameObject>();
    
    //Messages for loading screen
    private const string LoggingInMessage = "Logging in...";
    private const string LookingForLobbyMessage = "Joining Lobby...";
    private const string JoiningMatchMessage = "Joining Match...";
    
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

    public void AbortToSignIn()
    {
        LoadingGameState.CancelNextState = true;
        ChangeState(GameStates.SignIn);
    }

    public void LeaveToMainMenu()
    {
        BrainCloudManager.Instance.CloseGame();
        ChangeState(GameStates.MainMenu);
    }

    public void LeaveMatchBackToMenu()
    {
        CurrentLobby = null;
        CurrentServer = null;
        foreach (GameObject shockwave in Shockwaves)
        {
            if (shockwave != null)
            {
                Destroy(shockwave);    
            }
        }
        //ToDo Clean up user cursors
        BrainCloudManager.Instance.CloseGame();
        Shockwaves = new List<GameObject>();
        
        GameManager.Instance.CurrentUserInfo.MousePosition = Vector2.zero;
        ChangeState(GameStates.SignIn);
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

        isLoading = true;
        //User is in this state and moving onto the next
        switch (CurrentGameState)
        {
            //Logging In...
            case GameStates.SignIn:
                CurrentGameState = GameStates.MainMenu;
                BrainCloudManager.Instance.Login();
                LoadingGameState.ConnectStatesWithLoading(LoggingInMessage,false,GameStates.MainMenu);
                break;
            //Looking for Lobby...
            case GameStates.MainMenu:
                CurrentGameState = GameStates.Lobby;
                BrainCloudManager.Instance.FindLobby(protocol);
                LoadingGameState.ConnectStatesWithLoading(LookingForLobbyMessage,true,GameStates.Lobby);
                break;
            //Setting up Match...
            case GameStates.Lobby:
                CurrentGameState = GameStates.Match;
                BrainCloudManager.Instance.StartGame();
                LoadingGameState.ConnectStatesWithLoading(JoiningMatchMessage,false,GameStates.Match);
                break;
        }
    }
    
    public void ChangeState(GameStates newGameState)
    {
        foreach (GameState currentState in ListOfStates)
        {
            currentState.gameObject.SetActive(currentState.currentGameState == newGameState);
        }

        CurrentGameState = newGameState;
    }
}
