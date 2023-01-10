using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using UnityEngine;

public class Lobby
{
    public string LobbyID;
    public string OwnerID;
    public List<UserInfo> Members = new List<UserInfo>();

    public Lobby(Dictionary<string, object> lobbyJson, string in_lobbyId)
    {
        LobbyID = in_lobbyId;
        OwnerID = FormatOwnerID(lobbyJson["ownerCxId"] as string);
        
        var jsonMembers = lobbyJson["members"] as Dictionary<string, object>[];
        if (jsonMembers == null)
        {
            return;
        }
        for (int i = 0; i < jsonMembers.Length; ++i)
        {
            Dictionary<string,object> jsonMember = jsonMembers[i];
            var user = new UserInfo(jsonMember);
            if (user.ID == GameManager.Instance.CurrentUserInfo.ID)
            {
                user.AllowSendTo = false;
            }

            if (user.ID.Equals(OwnerID))
            {
                Dictionary<string, object> extra = jsonMember["extra"] as Dictionary<string, object>;
                if (extra.ContainsKey("relayCompressionType"))
                {
                    BrainCloudManager.Instance._relayCompressionType = (RelayCompressionTypes) extra["relayCompressionType"];
                    GameManager.Instance.CompressionDropdown.value = (int) extra["relayCompressionType"];   
                }
            }
            Members.Add(user);
        }
    }

    private string FormatOwnerID(string id)
    {
        string[] splits = id.Split(':');
        return splits[1];
    }
}
