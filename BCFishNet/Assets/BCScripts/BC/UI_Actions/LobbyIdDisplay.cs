using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyIdDisplay : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Text _lobbyIdText;
    // Start is called before the first frame update
    void Start()
    {
        _lobbyIdText.text = "Lobby Name: " + BCManager.Instance.CurrentLobbyId;
    }
}
