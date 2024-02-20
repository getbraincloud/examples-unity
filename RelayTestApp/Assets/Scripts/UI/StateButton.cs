using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateButton : MonoBehaviour
{
    //Called from Unity Button
    public void StateButtonChange() => StateManager.Instance.ButtonPressed_ChangeState();

    public void LookingForFFALobby()
    {
        GameManager.Instance.GameMode = GameMode.FreeForAll;
        StateManager.Instance.ButtonPressed_ChangeState();
    }
    
    //Called from Unity Button
    public void LookingForTeamLobby()
    {
        GameManager.Instance.GameMode = GameMode.Team;
        StateManager.Instance.ButtonPressed_ChangeState();
    }

    public void Reconnect() => StateManager.Instance.ReconnectToGame();
}
