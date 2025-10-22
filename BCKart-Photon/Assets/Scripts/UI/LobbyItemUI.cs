using System;
using UnityEngine;
using UnityEngine.UI;

public class LobbyItemUI : MonoBehaviour {

    public Text username;
    public Image ready;
    public Image leader;

    private RoomPlayer _player;

    public void SetPlayer(RoomPlayer player) {
        _player = player;
    }

    private void Update() {
        if (_player.Object != null && _player.Object.IsValid)
        {
            username.text = _player.Username.Value;
            ready.gameObject.SetActive(_player.IsReady);
        }
    }
}