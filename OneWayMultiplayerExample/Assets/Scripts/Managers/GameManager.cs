using System;
using System.Collections;
using System.Collections.Generic;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum InvaderRanks{Easy,Medium,Pillager}
public enum DefenderRanks{Easy,Medium,ForValhalla}

public class GameManager : MonoBehaviour
{
    //Local User Info
    private UserInfo _currentUserInfo;
    public UserInfo CurrentUserInfo
    {
        get => _currentUserInfo;
        set => _currentUserInfo = value;
    }
    //Singleton Pattern
    private static GameManager _instance;
    public static GameManager Instance => _instance;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (!_instance)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        _currentUserInfo = Settings.LoadPlayerInfo();
    }

    public bool IsEntityIdValid()
    {
        return !_currentUserInfo.EntityId.IsNullOrEmpty();
    }

    public void UpdateEntityId(string in_id)
    {
        _currentUserInfo.EntityId = in_id;
        Settings.SaveEntityId(in_id);
    }
}
