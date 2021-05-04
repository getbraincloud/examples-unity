using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsible for switching states from either button events or loading events
/// </summary>

public class StateManager : MonoBehaviour
{
    public List<GameState> ListOfStates = new List<GameState>();
    
    public ConnectingGameState loadingGameState;
    private static StateManager _instance;
    public static StateManager Instance => _instance;

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
        ChangeState(GameStatesEnum.SignIn);
    }

    public void AbortToSignIn()
    {
        loadingGameState.CancelNextState = true;
        ChangeState(GameStatesEnum.SignIn);
    }

    public void LeaveToLoggedIn()
    {
        ChangeState(GameStatesEnum.LoggedIn);
    }
    
    public void ButtonPressed_ChangeState(GameStatesEnum newGameState)
    {
        foreach (GameState state in ListOfStates)
        {
            state.gameObject.SetActive(false);
        }
        //User is in this state and moving onto the next
        switch (newGameState)
        {
            //Logging In...
            case GameStatesEnum.SignIn:
                
                BrainCloudManager.Instance.Login();
                loadingGameState.ConnectStates(LoggingInMessage,false,GameStatesEnum.LoggedIn);
                break;
            //Looking for Lobby...
            case GameStatesEnum.LoggedIn:
                loadingGameState.ConnectStates(LookingForLobbyMessage,true,GameStatesEnum.Lobby);
                break;
            //Setting up Match...
            case GameStatesEnum.Lobby:
                GameManager.Instance.SetUpMatchList();
                loadingGameState.ConnectStates(JoiningMatchMessage,false,GameStatesEnum.Match);
                break;
        }
    }
    
    public void ChangeState(GameStatesEnum newGameState)
    {
        foreach (GameState currentState in ListOfStates)
        {
            currentState.gameObject.SetActive(currentState.currentGameState == newGameState);
        }
    }
}
