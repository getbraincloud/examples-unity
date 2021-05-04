using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectingGameState : GameState
{
    public TMP_Text LoadingMessage;
    public Button CancelButton;
    public bool CancelNextState;
    private GameStatesEnum _gameStateToOpen;
    private float _waitTime = 0.5f;
    public void ConnectStates(string loadingMessage, bool cancelButtonEnabled, GameStatesEnum newGameState)
    {
        LoadingMessage.text = loadingMessage;
        CancelButton.gameObject.SetActive(cancelButtonEnabled);
        _gameStateToOpen = newGameState;
        gameObject.SetActive(true);
        StartCoroutine(DelayToOpenState());
    }

    IEnumerator DelayToOpenState()
    {
        yield return new WaitForSeconds(_waitTime);
        if (!CancelNextState)
        {
            StateManager.Instance.ChangeState(_gameStateToOpen);    
        }

        CancelNextState = false;
        gameObject.SetActive(false);
    }

}
