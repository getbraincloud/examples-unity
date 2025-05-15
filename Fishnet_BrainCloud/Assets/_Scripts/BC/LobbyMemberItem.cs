using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMemberItem : MonoBehaviour
{
    [SerializeField]
    private TMP_Text playerName, readyState;

    public void SetPlayerName(string name)
    {
        playerName.text = name;
    }

    public void UpdateReady(bool ready)
    {
        readyState.text = ready ? "Ready" : "Not Ready";
    }
}
