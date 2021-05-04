using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class StateManager : MonoBehaviour
{
    public List<State> ListOfStates = new List<State>();
    
    public ConnectingState LoadingState;
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
        ChangeState(StatesEnum.SignIn);
    }

    public void LeaveToLoggedIn()
    {
        ButtonPressed_ChangeState(StatesEnum.SignIn);
    }
    
    public void ButtonPressed_ChangeState(StatesEnum newState)
    {
        foreach (State state in ListOfStates)
        {
            state.gameObject.SetActive(false);
        }
        //User is in this state and moving onto the next
        switch (newState)
        {
            case StatesEnum.SignIn:
                LoadingState.ConnectStates(LoggingInMessage,false,StatesEnum.LoggedIn);
                break;
            case StatesEnum.LoggedIn:
                LoadingState.ConnectStates(LookingForLobbyMessage,true,StatesEnum.Lobby);
                break;
            case StatesEnum.Lobby:
                GameManager.Instance.SetUpMatchList();
                LoadingState.ConnectStates(JoiningMatchMessage,false,StatesEnum.Match);
                break;
        }
    }
    
    public void ChangeState(StatesEnum newState)
    {
        foreach (State currentState in ListOfStates)
        {
            currentState.gameObject.SetActive(currentState.CurrentState == newState);
        }
    }
}
