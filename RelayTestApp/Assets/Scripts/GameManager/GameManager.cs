using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    
    private Color _userColor = Color.white;

    protected static GameManager _instance;
    public static GameManager Instance => _instance;

    protected virtual void Awake()
    {
        if (!_instance)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public Color UserColor
    {
        get => _userColor;
        set => _userColor = value;
    }

}
