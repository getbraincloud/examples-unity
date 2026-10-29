using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateButton : MonoBehaviour
{
    //Called from Unity Button
    public void StateButtonChange() => StateManager.Instance.ButtonPressed_ChangeState();

    //Called from Unity Button — GameMode is already set by the Lobby Type dropdown selection (see DropdownMenuCallback/BrainCloudManager.SetLobbyType)
    public void LookingForLobby()
    {
        StateManager.Instance.ButtonPressed_ChangeState();
    }

    public void Reconnect() => StateManager.Instance.ReconnectToGame();
}
