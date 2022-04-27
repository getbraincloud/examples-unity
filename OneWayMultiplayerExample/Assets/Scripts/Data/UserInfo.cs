using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserInfo
{
    //Used to know if local user is hosting
    public string ID;
    //Used for displaying and identifying users
    public string Username;
    //if this user should show shockwaves locally
    public bool AllowSendTo = true;     
    //Is this user still connected
    public bool IsAlive;

    public ArmyDivisionRank InvaderSelected = 0;

    public ArmyDivisionRank DefendersSelected = 0;

    public string EntityId;
    public UserInfo() { }
    public string cxId;
    public UserInfo(Dictionary<string, object> userJson)
    {
        cxId = userJson["cxId"] as string;
        ID = userJson["profileId"] as string;
        Username = userJson["name"] as string;
        Dictionary<string, object> extra = userJson["extra"] as Dictionary<string, object>;
        //ToDo: use extra for defender/invader settings...maybe?
    }
}
