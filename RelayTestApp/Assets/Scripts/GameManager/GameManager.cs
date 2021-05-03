using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum GameColors{Black,Purple,Grey,Orange,Blue,Green,Yellow,Cyan,White}

public class GameManager : MonoBehaviour
{
    public UserEntry UserEntryPrefab;
    public GameObject UserEntryLobbyParent;
    public GameObject UserEntryMatchParent;
    
    private GameColors _userColor = GameColors.White;
    
    private UserEntry _currentUser;
    private List<UserEntry> UserEntries = new List<UserEntry>();
    private List<UserEntry> MatchEntries = new List<UserEntry>();
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

        
        _currentUser = Instantiate(UserEntryPrefab, Vector3.zero, Quaternion.identity, UserEntryLobbyParent.transform);
        UserEntries.Add(_currentUser);
    }

    public void ChangeLobbyTextColor(GameColors newColor)
    {
        _currentUser.UsernameText.color = ReturnUserColor(newColor);
        _currentUser.UserDotImage.color = ReturnUserColor(newColor);
    }
    /// <summary>
    /// After list of users is generated for the current match, call this to display the other users.
    /// ToDo: Retrieve color from other users and applying the prefab to their color
    /// </summary>
    public void SetUpMatchList()
    {
        foreach (UserEntry matchEntry in MatchEntries)
        {
            Destroy(matchEntry);
        }
        MatchEntries.Clear();   
        
        foreach (UserEntry entry in UserEntries)
        {
            var newEntry = Instantiate(entry, Vector3.zero, Quaternion.identity, UserEntryMatchParent.transform);
            MatchEntries.Add(entry);   
        }
    }

    public Color ReturnUserColor(GameColors newColor=GameColors.White)
    {
        if (newColor != GameColors.White)
        {
            _userColor = newColor;
        }

        switch (_userColor)
        {
            case GameColors.Black:
                return Color.black;
            case GameColors.Purple:
                return Color.magenta;
            case GameColors.Grey:
                return Color.grey;
            case GameColors.Orange:
                return new Color(0.85f, 0.4f, 0.04f);
            case GameColors.Blue:
                return Color.blue;
            case GameColors.Green:
                return Color.green;
            case GameColors.Yellow:
                return Color.yellow;
            case GameColors.Cyan:
                return Color.cyan;
        }
        
        return Color.white;
    }
    
}

