using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Holds info needed for the current user
/// </summary>

public enum GameColors{Black,Purple,Grey,Orange,Blue,Green,Yellow,Cyan,White}

public class GameManager : MonoBehaviour
{
    public UserEntry UserEntryPrefab;
    public GameObject UserEntryLobbyParent;
    public GameObject UserEntryMatchParent;
    public TMP_InputField UsernameInputField;
    public TMP_InputField PasswordInputField;
    public TMP_Text LoggedInNameText;
    public DialogueMessage ErrorMessage;
    
    private GameColors _userColor = GameColors.White;
    private UserEntry _currentUserEntry;
    private UserInfo _currentUserInfo;
    private List<UserEntry> UserEntries = new List<UserEntry>();
    private List<UserEntry> MatchEntries = new List<UserEntry>();
    

    
    private static GameManager _instance;
    public static GameManager Instance => _instance;
    public UserInfo CurrentUserInfo
    {
        get => _currentUserInfo;
        set => _currentUserInfo = value;
    }

    private void Awake()
    {
        if (!_instance)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        PasswordInputField.inputType = TMP_InputField.InputType.Password;
        _currentUserEntry = Instantiate(UserEntryPrefab, Vector3.zero, Quaternion.identity, UserEntryLobbyParent.transform);
        UserEntries.Add(_currentUserEntry);
    }

    public void UpdateUsername(string name)
    {
        _currentUserEntry.UsernameText.text = name;
        _currentUserInfo.Username = name;
        LoggedInNameText.text = $"Logged in as {name}";
    }

    public void ChangeLobbyTextColor(GameColors newColor)
    {
        _currentUserInfo.UserColor = ReturnUserColor(newColor);
        _currentUserEntry.UsernameText.color = _currentUserInfo.UserColor;
        _currentUserEntry.UserDotImage.color = _currentUserInfo.UserColor;
    }
    /// <summary>
    /// After list of users is generated for the current match, call this to display the connected users.
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
    /// <summary>
    /// Main returns the current color the user has equipped or changes to new color and returns it
    /// </summary>
    /// <param name="newColor"> if the color needs to be changed</param>
    /// <returns></returns>
    public Color ReturnUserColor(GameColors newColor = GameColors.White)
    {
        if (newColor != GameColors.White)
        {
            CurrentUserInfo.UserGameColor = newColor;
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

