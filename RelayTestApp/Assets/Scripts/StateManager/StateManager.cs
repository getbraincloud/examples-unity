using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrainCloud;
/// <summary>
/// - Responsible for switching states from either button events or loading events
/// - Holding information like 
/// </summary>

public class StateManager : MonoBehaviour
{
    public List<GameState> ListOfStates = new List<GameState>();
    public GameStates CurrentGameState;
    public ConnectingGameState LoadingGameState;
    
    
    private static StateManager _instance;
    public static StateManager Instance => _instance;
    
    //Network info needed
    public Lobby CurrentLobby;
    public Server CurrentServer;
    public List<GameObject> Shockwaves = new List<GameObject>();
    public List<GameObject> UserCursors = new List<GameObject>();
    public RelayConnectionType protocol = RelayConnectionType.WEBSOCKET;
    public bool isReady;
    public bool isLoading;
    private const string LoggingInMessage = "Logging in...";
    private const string LookingForLobbyMessage = "Joining Lobby...";
    private const string JoiningMatchMessage = "Joining Match...";
    
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

    public void LeaveToLoggedIn()
    {
        ChangeState(GameStates.LoggedIn);
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

        foreach (var cursor in UserCursors)
        {
            Destroy(cursor);
        }
        Shockwaves = new List<GameObject>();
        UserCursors = new List<GameObject>();
        GameManager.Instance.CurrentUserInfo.MousePosition = Vector2.zero;
        ChangeState(GameStates.SignIn);
    }
    
    //Takes in the current Game state to then load into the next game state
    public void ButtonPressed_ChangeState(GameStates currentGameState)
    {
        foreach (GameState state in ListOfStates)
        {
            state.gameObject.SetActive(false);
        }
        //User is in this state and moving onto the next
        switch (currentGameState)
        {
            //Logging In...
            case GameStates.SignIn:
                
                BrainCloudManager.Instance.Login();
                LoadingGameState.ConnectStates(LoggingInMessage,false,GameStates.LoggedIn);
                break;
            //Looking for Lobby...
            case GameStates.LoggedIn:
                CurrentGameState = GameStates.Lobby;
                BrainCloudManager.Instance.FindLobby(protocol);
                isLoading = true;
                LoadingGameState.ConnectStatesWithLoading(LookingForLobbyMessage,true,GameStates.Lobby);
                break;
            //Setting up Match...
            case GameStates.Lobby:
                CurrentGameState = GameStates.Match;
                BrainCloudManager.Instance.StartGame();
                isLoading = true;
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
    }
}
