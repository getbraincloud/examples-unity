using System.Collections;
using System.Collections.Generic;
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

    public Vector3 Position;
    public List<Vector3> ShockwavePositions = new List<Vector3>();
    public Color UserColor;
    public GameColors UserGameColor;
    
    
    //Network info needed
    public Lobby CurrentLobby;
    public Server CurrentServer;
    public List<UserInfo> ConnectedUsers;
    
    
    public UserInfo() { }

    public UserInfo(Dictionary<string, object> userJson)
    {
        ID = userJson["profileId"] as string;
        Username = userJson["name"] as string;

        var extra = userJson["extra"] as Dictionary<string, object>;
        UserGameColor = (GameColors)extra["colorIndex"];
        UserColor = GameManager.Instance.ReturnUserColor(UserGameColor);
    }
}
