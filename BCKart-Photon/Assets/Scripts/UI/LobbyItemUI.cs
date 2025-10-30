using System;
using UnityEngine;
using UnityEngine.UI;

public class LobbyItemUI : MonoBehaviour {

    public Text username;
    public Image ready;
    public Image leader;
    public Image kartDisplay;

    public Texture2D[] kartDisplayReferences;

    private LobbyMember _player;
    private int _lastKartId = -1;

    public void SetPlayer(LobbyMember player) 
    {
        _player = player;
        _lastKartId = -1; // reset so sprite updates immediately
    }

    public void Update() 
    {
        if (_player != null)
        {
            // UI flags
            username.text = _player.displayName;
            ready.gameObject.SetActive(_player.isReady);
            leader.gameObject.SetActive(_player.isHost);

            // Update kart display only if changed
            if (_player.kartId != _lastKartId && 
                _player.kartId >= 0 && 
                _player.kartId < kartDisplayReferences.Length)
            {
                Texture2D tex = kartDisplayReferences[_player.kartId];
                if (tex != null)
                {
                    kartDisplay.sprite = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f)
                    );
                }

                _lastKartId = _player.kartId;
            }
        }
    }
}
