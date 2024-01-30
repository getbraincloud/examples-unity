using System;
using System.Collections.Generic;

[Serializable]
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
            if (user.ProfileID == GameManager.Instance.CurrentUserInfo.ProfileID)
            {
                GameManager.Instance.CurrentUserInfo = user;
            }
            user.IsAlive = true;
            if (user.ProfileID.Equals(OwnerID))
            {
                Dictionary<string, object> extra = jsonMember["extra"] as Dictionary<string, object>;
                if (extra.ContainsKey("relayCompressionType"))
                {
                    BrainCloudManager.Instance._relayCompressionType = (RelayCompressionTypes) extra["relayCompressionType"];
                    GameManager.Instance.CompressionDropdown.value = (int) extra["relayCompressionType"];   
                }

                if (extra.ContainsKey("presentSinceStart"))
                {
                    user.PresentSinceStart = (bool) extra["presentSinceStart"];
                }

                user.IsHost = true;
            }
            Members.Add(user);
            List<UserInfo> userList = StateManager.Instance.SessionPlayers;
            List<string> listOfIds = new List<string>();
            for (int j = 0; j < userList.Count; j++)
            {
                listOfIds.Add(userList[j].cxId);
            }
            
            if(!listOfIds.Contains(user.cxId))
            {
                StateManager.Instance.SessionPlayers.Add(user);
            }
        }
    }

    public string ReassignOwnerID(string id)
    {
        OwnerID = FormatOwnerID(id);

        return OwnerID;
    }

    public string FormatCxIdToProfileId(string id)
    {
        return FormatOwnerID(id);
    }

    private string FormatOwnerID(string id)
    {
        string[] splits = id.Split(':');
        return splits[1];
    }
}
