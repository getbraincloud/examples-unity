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
    [SerializeField] private string _profileID;
    public string ProfileID
    {
        get => _profileID;
        set => _profileID = value;
    }
    //Used for displaying and identifying users
    [SerializeField] private string _username;
    public string Username
    {
        get => _username;
        set => _username = value;
    }
    //Is this user still connected
    [SerializeField] private bool _isAlive;
    public bool IsAlive
    {
        get => _isAlive;
        set => _isAlive = value;
    }
    [SerializeField]  private string _cxId;
    public string CxID
    {
        get => _cxId;
    }
    [SerializeField] private bool _isHost;
    public bool IsHost
    {
        get => _isHost;
        set => _isHost = value;
    }
    [SerializeField] private string NetID;
    //Used to determine if user is in lobby or in match.
    [SerializeField] private bool _isReady;

    [SerializeField] private string _passCode;
    public string PassCode
    {
        get => _passCode;
        set => _passCode = value;
    }
    public bool IsReady
    {
        get => _isReady;
    }
    
    public UserInfo() { }

    public UserInfo(Dictionary<string, object> userJson)
    {
        _cxId = userJson["cxId"] as string;
        _profileID = userJson["profileId"] as string;
        _username = userJson["name"] as string;
        _isReady = (bool)userJson["isReady"];
        if (userJson.ContainsKey("passcode"))
        {
            _passCode = userJson["passcode"] as string;
        }
    }
}
