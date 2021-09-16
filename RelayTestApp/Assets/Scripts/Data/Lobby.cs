using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lobby
{
    public string LobbyID;
    public string OwnerID;
    public List<UserInfo> Members = new List<UserInfo>();

    public Lobby(Dictionary<string, object> lobbyJson, string in_lobbyId)
    {
        LobbyID = in_lobbyId;
        OwnerID = lobbyJson["ownerCxId"] as string;
        //OwnerID = OwnerID.Trim(Br)
        var jsonMembers = lobbyJson["members"] as Dictionary<string, object>[];
        for (int i = 0; i < jsonMembers.Length; ++i)
        {
            Dictionary<string,object> jsonMember = jsonMembers[i];
            var user = new UserInfo(jsonMember);
            if (user.ID == GameManager.Instance.CurrentUserInfo.ID) user.AllowSendTo = false;
            Members.Add(user);
        }
    }
}
