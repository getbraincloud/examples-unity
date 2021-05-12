using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateButton : MonoBehaviour
{
    public void StateButtonChange()
    {
        GameStates newGameState = transform.parent.GetComponent<GameState>().currentGameState;
        StateManager.Instance.ButtonPressed_ChangeState(newGameState);
    }
}
