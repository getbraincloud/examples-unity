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
            if (user.ProfileID == BrainCloudManager.Singleton.LocalUserInfo.ProfileID)
            {
                BrainCloudManager.Singleton.LocalUserInfo = user;
            }
            user.IsAlive = true;
            if (user.ProfileID.Equals(OwnerID))
            {
                Dictionary<string, object> extra = jsonMember["extra"] as Dictionary<string, object>;

                user.IsHost = true;
            }
            Members.Add(user);
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

    public UserInfo FindMemberByPlayerId(string playerId)
    {
        UserInfo memberInfo = null;

        foreach(UserInfo member in Members)
        {
            if (member.ProfileID.Equals(playerId))
            {
                memberInfo = member;
                break;
            }
        }

        return memberInfo;
    }
}
