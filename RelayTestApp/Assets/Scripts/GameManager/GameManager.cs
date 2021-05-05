using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Holds info needed for the current user and other connected users
/// </summary>

public enum GameColors{Black,Purple,Grey,Orange,Blue,Green,Yellow,Cyan,White}

public class GameManager : MonoBehaviour
{
    public UserEntry UserEntryLobbyPrefab;
    public UserEntry UserEntryMatchPrefab;
    public GameObject UserEntryLobbyParent;
    public GameObject UserEntryMatchParent;
    public TMP_InputField UsernameInputField;
    public TMP_InputField PasswordInputField;
    public TMP_Text LoggedInNameText;
    public DialogueMessage ErrorMessage;
    
    private UserEntry _currentUserEntry;
    private UserInfo _currentUserInfo;
    
    //List to reference specifically for listing
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
        _currentUserEntry = Instantiate(UserEntryLobbyPrefab, Vector3.zero, Quaternion.identity, UserEntryLobbyParent.transform);
        Settings.LoadSettings();
    }

    public void UpdateUsername(string name)
    {
        _currentUserEntry.UsernameText.text = name;
        _currentUserInfo.Username = name;
        PlayerPrefs.SetString(Settings.UsernameKey,name);
        LoggedInNameText.text = $"Logged in as {name}";
    }

    public void ChangeLobbyTextColor(GameColors newColor)
    {
        _currentUserInfo.UserColor = ReturnUserColor(newColor);
        Settings.SetPlayerPrefColor(newColor);
        _currentUserEntry.UsernameText.color = _currentUserInfo.UserColor;
        _currentUserEntry.UserDotImage.color = _currentUserInfo.UserColor;
        CheckColorOnUsers();
    }
    /// <summary>
    /// After list of users is generated for the current match, call this to display the connected users
    /// </summary>
    public void SetUpMatchList()
    {
        if (MatchEntries.Count > 0)
        {
            for(int i = MatchEntries.Count - 1; i > -1; i--)
            {
                Destroy(MatchEntries[i].gameObject);
            }
            MatchEntries.Clear();    
        }
        Lobby lobby = StateManager.Instance.CurrentLobby;
        for (int i = 0; i < lobby.Members.Count; i++)
        {
            var newEntry = Instantiate(UserEntryMatchPrefab, Vector3.zero, Quaternion.identity,UserEntryMatchParent.transform);
            newEntry.UsernameText.color = lobby.Members[i].UserColor;
            MatchEntries.Add(newEntry);
        }
        
    }

    private void CheckColorOnUsers()
    {
        Lobby lobby = StateManager.Instance.CurrentLobby;
        for (int i = 0; i < MatchEntries.Count; i++)
        {
            var newEntry = Instantiate(UserEntryMatchPrefab, Vector3.zero, Quaternion.identity,UserEntryMatchParent.transform);
            newEntry.UsernameText.color = lobby.Members[i].UserColor;
            newEntry.UserDotImage.color = lobby.Members[i].UserColor;
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

        switch (CurrentUserInfo.UserGameColor)
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

    public GameColors ReturnUserColor(string colorToReturn)
    {
        switch (colorToReturn)
        {
            case "Black":
                return GameColors.Black;
            case "Purple":
                return GameColors.Purple;
            case "Grey":
                return GameColors.Grey;
            case "Orange":
                return GameColors.Orange;
            case "Blue":
                return GameColors.Blue;
            case "Green":
                return GameColors.Green;
            case "Yellow":
                return GameColors.Yellow;
            case "Cyan":
                return GameColors.Cyan;
        }
        return GameColors.White;
    }
    
}

