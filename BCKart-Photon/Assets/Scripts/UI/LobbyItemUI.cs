using System;
using UnityEngine;
using UnityEngine.UI;

public class LobbyItemUI : MonoBehaviour {

    public Text username;
    public Image ready;
    public Image leader;

    private LobbyMember _player;

    public void SetPlayer(LobbyMember player) 
    {
        _player = player;
    }

    public void Update() 
    {
        if (_player != null)
        {
            username.text = _player.displayName;
            ready.gameObject.SetActive(_player.isReady);
            //leader.gameObject.SetActive(_player.isHost);
        }
    }
}