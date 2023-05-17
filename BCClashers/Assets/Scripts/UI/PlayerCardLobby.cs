using System.Collections;
using System.Collections.Generic;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using UnityEngine;
using TMPro;

public class PlayerCardLobby : MonoBehaviour
{
    public TMP_Text PlayerNameText;
    public TMP_Text PlayerRatingText;

    public UserInfo UserInfo;

    public void PlayerSelected()
    {
        if (UserInfo.ProfileId.IsNullOrEmpty())
        {
            Debug.LogWarning("UserId is empty for this player");
            return;
        }
        
        NetworkManager.Instance.ReadLobbyUserSelected(UserInfo.ProfileId);
        GameManager.Instance.OpponentUserInfo = UserInfo;
    }
}
