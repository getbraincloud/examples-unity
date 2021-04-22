using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectingState : State
{
    public TMP_Text LoadingMessage;
    public Button CancelButton;
    
    private StatesEnum _stateToOpen;
    private float _waitTime = 1;
    public void ConnectStates(string loadingMessage, bool cancelButtonEnabled, StatesEnum newState)
    {
        LoadingMessage.text = loadingMessage;
        CancelButton.gameObject.SetActive(cancelButtonEnabled);
        _stateToOpen = newState;
        gameObject.SetActive(true);
        StartCoroutine(DelayToOpenState());
    }

    IEnumerator DelayToOpenState()
    {
        yield return new WaitForSeconds(_waitTime);
        StateManager.Instance.ChangeState(_stateToOpen);
        gameObject.SetActive(false);
    }

}
