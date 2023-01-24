using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoadingMenuState : MenuState
{
    public TMP_Text LoadingMessage;
    public Button CancelButton;
    public bool CancelNextState;
    private MenuStates _gameStateToOpen;
    
    //Called from Unity Button within Connecting Canvas
    public void CancelNextStateButtonPress()
    {
        CancelNextState = true;
    }
    
    private void CloseWindow()
    {
        MenuStates nextState;
        if (CancelNextState)
        {
            if (MenuManager.Instance.CurrentMenuState == 0)
            {
                nextState = MenuStates.SignIn;
            }
            else
            {
                nextState = MenuManager.Instance.CurrentMenuState - 1;
            }
        }
        else
        {
            nextState = _gameStateToOpen;
        }
        
        MenuManager.Instance.ChangeState(nextState);
        CancelNextState = false;
        gameObject.SetActive(false);
    }
    
    public void ConnectStatesWithLoading(string loadingMessage, bool cancelButtonEnabled, MenuStates newGameState)
    {
        LoadingMessage.text = loadingMessage;
        CancelButton.gameObject.SetActive(cancelButtonEnabled);
        _gameStateToOpen = newGameState;
        gameObject.SetActive(true);
        StartCoroutine(WaitForResponse());
    }
    
    private IEnumerator WaitForResponse()
    {
        while (MenuManager.Instance.IsLoading)
        {
            if (CancelNextState)
            {
                MenuManager.Instance.IsLoading = false;
            }
            yield return new WaitForFixedUpdate();
        }
        CloseWindow();
    }
}