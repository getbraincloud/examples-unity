using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// - Holds info needed for the current user and other connected users
/// - Handles getting UI element data made by user
/// - References Prefabs used for listing members in a list
/// - Handles Error window
/// 
/// </summary>

public enum GameColors{Black,Purple,Grey,Orange,Blue,Green,Yellow,Cyan,White}

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    public UserEntry UserEntryLobbyPrefab;
    public UserEntry UserEntryMatchPrefab;
    public UserCursor UserCursorPrefab;
    [Header("Parent Transforms")]
    public GameObject UserEntryLobbyParent;
    public GameObject UserEntryMatchParent;
    public GameObject UserCursorParent;
    [Header("UI References")]
    public TMP_InputField UsernameInputField;
    public TMP_InputField PasswordInputField;
    public TMP_Text LoggedInNameText;
    public DialogueMessage ErrorMessage;
    public UserMouseArea GameArea;  //for updating members list of shockwaves
    public GameObject StartGameBtn;
    
    private UserEntry _currentUserEntry;
    
    private UserInfo _currentUserInfo;

    
    private List<UserEntry> _matchEntries = new List<UserEntry>();
    private List<UserCursor> _userCursorsList = new List<UserCursor>();
    public List<UserCursor> CursorList => _userCursorsList;
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
        _matchEntries.Add(_currentUserEntry);
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
        
        //Apply in game color changes
        Settings.SetPlayerPrefColor(newColor);
        
        //Send update to BC
        var extra = new Dictionary<string, object>();
        extra["colorIndex"] = (int)_currentUserInfo.UserGameColor;
        BrainCloudManager.Instance.Wrapper.LobbyService.UpdateReady
        (
            StateManager.Instance.CurrentLobby.LobbyID,
            StateManager.Instance.isReady,
            extra
        );
        
    }
    public void UpdateLobbyList()
    {
        if (_matchEntries.Count > 0)
        {
            for(int i = _matchEntries.Count - 1; i > -1; i--)
            {
                Destroy(_matchEntries[i].gameObject);
            }
            _matchEntries.Clear();    
        }
        Lobby lobby = StateManager.Instance.CurrentLobby;
        for (int i = 0; i < lobby.Members.Count; i++)
        {
            var newEntry = Instantiate(UserEntryLobbyPrefab, Vector3.zero, Quaternion.identity,UserEntryLobbyParent.transform);
            newEntry.UsernameText.text = lobby.Members[i].Username;
            newEntry.UsernameText.color = lobby.Members[i].UserColor;
            _matchEntries.Add(newEntry);
        }
    }
    
    /// <summary>
    /// After list of users is generated for the current match, call this to display the connected users
    /// </summary>
    public void UpdateMatchList()
    {
        if (_matchEntries.Count > 0)
        {
            for(int i = _matchEntries.Count - 1; i > -1; i--)
            {
                Destroy(_matchEntries[i].gameObject);
            }
            _matchEntries.Clear();    
        }
        
        Lobby lobby = StateManager.Instance.CurrentLobby;
        for (int i = 0; i < lobby.Members.Count; i++)
        {
            var newEntry = Instantiate(UserEntryMatchPrefab, Vector3.zero, Quaternion.identity,UserEntryMatchParent.transform);
            newEntry.UsernameText.text = lobby.Members[i].Username;
            newEntry.UsernameText.color = lobby.Members[i].UserColor;
            _matchEntries.Add(newEntry);
        }
    }

    public void UpdateCursorList()
    {
        Lobby lobby = StateManager.Instance.CurrentLobby;
        if (_userCursorsList.Count > 0)
        {
            for (int i = _userCursorsList.Count - 1; i > -1; i--)
            {
                Destroy(_userCursorsList[i].gameObject);
            }
            _userCursorsList.Clear();
        }

        for (int i = 0; i < lobby.Members.Count; i++)
        {
            var newCursor = Instantiate(UserCursorPrefab, Vector3.zero, Quaternion.identity, UserCursorParent.transform);
            if (lobby.Members[i].Username == CurrentUserInfo.Username)
            {
                GameArea.LocalCursor = newCursor.Cursor;
            }
            newCursor.SetUpCursor(lobby.Members[i].UserColor,lobby.Members[i].Username);
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

    public void MemberLeft()
    {
        UpdateMatchList();
        UpdateCursorList();
    }
    
}

