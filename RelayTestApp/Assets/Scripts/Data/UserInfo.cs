using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds all the information needed from a User
/// </summary>

[Serializable]
public class UserInfo
{
    //Used to know if local user is hosting
    public string ProfileID;
    //Used for displaying and identifying users
    public string Username;
    //if this user should show splatters locally
    public bool AllowSendTo = true;
    //Is this user still connected
    public bool IsAlive;
    //Current user color to display
    public int UserGameColor;
    //Current Mouse Position to display
    public Vector2 MousePosition;
    //Splatters are created based on each location given from list
    public List<Vector2> SplatterPositions = new List<Vector2>();
    //Splatters that take different shapes base on team code
    public List<TeamCodes> SplatterTeamCodes = new List<TeamCodes>();
    public List<TeamCodes> InstigatorTeamCodes = new List<TeamCodes>();
    //Class to handle each user's cursor
    public UserCursor UserCursor;
    public UserInfo() { }
    public string cxId;
    public bool IsHost;
    public string NetID;
    //Used to determine if user is in lobby or in match.
    public bool IsReady;
    public bool PresentSinceStart;
    public RectTransform CursorTransform;
    public TeamCodes Team;
    public UserInfo(Dictionary<string, object> userJson)
    {
        cxId = userJson["cxId"] as string;
        ProfileID = userJson["profileId"] as string;
        Username = userJson["name"] as string;
        IsReady = (bool)userJson["isReady"];
        string teamValue = userJson["team"] as string;
        Enum.TryParse(teamValue, out Team);

        if (GameManager.Instance.GameMode == GameMode.FreeForAll)
        {
            Dictionary<string, object> extra = userJson["extra"] as Dictionary<string, object>;
            int colorIndex = 0;
            if (extra != null && extra.ContainsKey("colorIndex"))
            {
                colorIndex = (int)extra["colorIndex"];
            }
            UserGameColor = colorIndex;
        }
        else if(GameManager.Instance.GameMode == GameMode.Team)
        {
            UserGameColor = (Team == TeamCodes.alpha) ? 4 : 3;
        }

        if (userJson.ContainsKey("presentSinceStart"))
        {
            PresentSinceStart = (bool)userJson["presentSinceStart"];
        }
    }
}
