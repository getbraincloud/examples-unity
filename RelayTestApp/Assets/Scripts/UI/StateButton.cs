using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateButton : MonoBehaviour
{
    //Called from Unity Button
    public void StateButtonChange() => StateManager.Instance.ButtonPressed_ChangeState();
    
    //Called from Unity Button
    public void LookingForTeamLobby()
    {
        GameManager.Instance.GameMode = GameMode.Team;
        BrainCloudManager.Instance.LobbyType = RelayLobbyTypes.TeamCursorPartyV2;
        StateManager.Instance.ButtonPressed_ChangeState();
    }

    public void Reconnect() => StateManager.Instance.ReconnectToGame();
}
