using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using UnityEngine;
/// <summary>
/// This class will have a combination of User.cs and State.cs from the example app
/// </summary>
public class UserInfo
{
    //Local info needed
    public string ID;
    public string Username;
    public bool AllowSendTo;
    public bool IsAlive;
    
    
    public Color UserColor;
    public GameColors UserGameColor;
    public Vector2 MousePosition;
    public UserInfo() { }

    public UserInfo(Dictionary<string, object> userJson)
    {
        ID = userJson["profileId"] as string;
        Username = userJson["name"] as string;
        var extra = userJson["extra"] as Dictionary<string, object>;
        var stringColor = (string)extra["colorIndex"];
        UserGameColor = GameManager.Instance.ReturnUserColor(stringColor);
        UserColor = GameManager.Instance.ReturnUserColor(UserGameColor);
    }
}
