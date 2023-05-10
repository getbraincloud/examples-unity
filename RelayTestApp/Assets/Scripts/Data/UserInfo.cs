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
    public string ID;
    //Used for displaying and identifying users
    public string Username;
    //if this user should show shockwaves locally
    public bool AllowSendTo = true;     
    //Is this user still connected
    public bool IsAlive;
    //Current user color to display
    public GameColors UserGameColor;
    //Current Mouse Position to display
    public Vector2 MousePosition;
    //Shockwaves are created based on each location given from list
    public List<Vector2> ShockwavePositions = new List<Vector2>();
    //Class to handle each user's cursor
    public UserCursor UserCursor;
    public UserInfo() { }
    public string cxId;

    public string NetID;
    //Used to determine if user is in lobby or in match.
    public bool IsReady;
    public bool PresentSinceStart;
    public RectTransform CursorTransform;
    public TeamCodes Team;
    public UserInfo(Dictionary<string, object> userJson)
    {
        cxId = userJson["cxId"] as string;
        ID = userJson["profileId"] as string;
        Username = userJson["name"] as string;
        IsReady = (bool)userJson["isReady"];
        string teamValue = userJson["team"] as string;
        Enum.TryParse(teamValue, out Team);
        if (GameManager.Instance.GameMode == GameMode.FreeForAll)
        {
            Dictionary<string, object> extra = userJson["extra"] as Dictionary<string, object>;
            int colorIndex = (int)extra["colorIndex"];
            UserGameColor = (GameColors) colorIndex;    
        }
        else if(GameManager.Instance.GameMode == GameMode.Team)
        {
            if (Team == TeamCodes.alpha)
            {
                UserGameColor = GameColors.Blue;
            }
            else
            {
                UserGameColor = GameColors.Orange;
            }
        }
        if (userJson.ContainsKey("presentSinceStart"))
        {
            PresentSinceStart = (bool)userJson["presentSinceStart"];    
        }
    }
}
