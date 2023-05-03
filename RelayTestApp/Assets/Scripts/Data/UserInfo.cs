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
    //Used to determine if user is in lobby or in match.
    public bool IsReady;
    public bool PresentSinceStart;
    public RectTransform CursorTransform;
    public UserInfo(Dictionary<string, object> userJson)
    {
        cxId = userJson["cxId"] as string;
        ID = userJson["profileId"] as string;
        Username = userJson["name"] as string;
        Dictionary<string, object> extra = userJson["extra"] as Dictionary<string, object>;
        int colorIndex = (int)extra["colorIndex"];
        UserGameColor = (GameColors) colorIndex;
        IsReady = (bool)userJson["isReady"];
        if (userJson.ContainsKey("presentSinceStart"))
        {
            PresentSinceStart = (bool)userJson["presentSinceStart"];    
        }
    }
}
