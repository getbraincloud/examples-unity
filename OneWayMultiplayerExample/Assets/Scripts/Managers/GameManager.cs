using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        _currentUserInfo = new UserInfo();
    }
}
