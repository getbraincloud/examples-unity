using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds all the information needed from a User
/// </summary>

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

    public UserInfo(Dictionary<string, object> userJson)
    {
        ID = userJson["profileId"] as string;
        Username = userJson["name"] as string;
        Dictionary<string, object> extra = userJson["extra"] as Dictionary<string, object>;
        int colorIndex = (int)extra["colorIndex"];
        UserGameColor = (GameColors) colorIndex;
    }
}