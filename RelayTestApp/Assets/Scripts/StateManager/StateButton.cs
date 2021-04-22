using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateButton : MonoBehaviour
{
    public void StateButtonChange()
    {
        StatesEnum newState = transform.parent.GetComponent<State>().CurrentState;
        StateManager.Instance.ButtonPressed_ChangeState(newState);
    }
}
