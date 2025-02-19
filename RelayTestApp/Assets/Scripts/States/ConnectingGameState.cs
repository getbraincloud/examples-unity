using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// - Waits until boolean StateManager.Instance.isLoading is set to false to open next game state
/// - If there's an error, CancelNextState will be turned true and will go back a previous game state
/// - If User clicks Cancel button, CancelNextState will turn true and will go back a previous state
///  
/// </summary>

public class ConnectingGameState : GameState
{
    public TMP_Text LoadingMessage;
    public Button CancelButton;
    public bool CancelNextState;
    private GameStates _gameStateToOpen;
    //Called from Unity Button within Connecting Canvas
    public void CancelNextStateButtonPress()
    {
        CancelNextState = true;
    }
    private void CloseWindow()
    {
        GameStates currentState = StateManager.Instance.CurrentGameState;
        if (currentState != GameStates.SignIn)
        {
            StateManager.Instance.ChangeState(CancelNextState ? StateManager.Instance.CurrentGameState - 1  : _gameStateToOpen);    
            if(CancelNextState)
            {
                BrainCloudManager.Instance.CancelFindRequest();
            }
        }
        else
        {
            StateManager.Instance.ChangeState(GameStates.SignIn);
        }
        CancelNextState = false;
        gameObject.SetActive(false);
    }
    public void ConnectStatesWithLoading(string loadingMessage, bool cancelButtonEnabled, GameStates newGameState)
    {
        LoadingMessage.text = loadingMessage;
        CancelButton.gameObject.SetActive(cancelButtonEnabled);
        _gameStateToOpen = newGameState;
        gameObject.SetActive(true);
        StartCoroutine(WaitForResponse());
    }
    IEnumerator WaitForResponse()
    {
        while (StateManager.Instance.isLoading)
        {
            if (CancelNextState)
            {
                StateManager.Instance.isLoading = false;
            }
            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForFixedUpdate();
        CloseWindow();
    }
}
