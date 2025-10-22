using System;
using UnityEngine;
using UnityEngine.UI;

public class RoomPlayerItemUI : MonoBehaviour {

    public Text username;
    public Image ready;
    public Image leader;

    private RoomPlayer _player;

    public void SetPlayer(RoomPlayer player) {
        _player = player;
    }

    private void Update() {
        username.text = _player.Username.Value;
        ready.gameObject.SetActive(_player.IsReady);
    }
}