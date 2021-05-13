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
    
    private UserInfo _currentUserInfo;
    private List<UserEntry> _matchEntries = new List<UserEntry>();
    private List<UserCursor> _userCursorsList = new List<UserCursor>();     //needed for cleanup
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
        Settings.LoadSettings();
    }

    public void UpdateLoggedInText(string name)
    {
        _currentUserInfo.Username = name;
        
        PlayerPrefs.SetString(Settings.UsernameKey,name);
        LoggedInNameText.text = $"Logged in as {name}";
    }
    
    //Note: Lobby text color is changed within UpdateLobbyList() from Brain Cloud's callbacks
    public void UpdateLocalColorChange(GameColors newColor)
    {
        _currentUserInfo.UserGameColor = newColor;
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
        AdjustEntryList(UserEntryLobbyParent.transform,UserEntryLobbyPrefab);
    }
    
    /// <summary>
    /// After list of users is generated for the current match, call this to display the connected users
    /// </summary>
    public void UpdateMatchList()
    {
        AdjustEntryList(UserEntryMatchParent.transform,UserEntryMatchPrefab);
    }

    private void AdjustEntryList(Transform parent, UserEntry prefab)
    {
        if (_matchEntries.Count > 0)
        {
            for(int i = _matchEntries.Count - 1; i > -1; i--)
            {
                Destroy(_matchEntries[i].gameObject);
            }
            _matchEntries.Clear();    
        }
        
        //populate user entries based on members in lobby
        Lobby lobby = StateManager.Instance.CurrentLobby;
        for (int i = 0; i < lobby.Members.Count; i++)
        {
            var newEntry = Instantiate(prefab, Vector3.zero, Quaternion.identity,parent);
            SetUpUserEntry(lobby.Members[i], newEntry);
            _matchEntries.Add(newEntry);
        }
    }

    private void SetUpUserEntry(UserInfo info,UserEntry entry)
    {
        entry.UsernameText.text = info.Username;
        var userColor = ReturnUserColor(info.UserGameColor);
        entry.UsernameText.color = userColor;
        if (entry.UserDotImage != null)
        {
            entry.UserDotImage.color = userColor;
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
        Color newColor;
        for (int i = 0; i < lobby.Members.Count; i++)
        {
            var newCursor = Instantiate(UserCursorPrefab, Vector3.zero, Quaternion.identity, UserCursorParent.transform);
            if (lobby.Members[i].Username == CurrentUserInfo.Username)
            {
                GameArea.LocalUserCursor = newCursor;
            }

            newColor = ReturnUserColor(lobby.Members[i].UserGameColor);
            newCursor.SetUpCursor(newColor,lobby.Members[i].Username);
            newCursor.AdjustVisibility(false);
            lobby.Members[i].UserCursor = newCursor;
            _userCursorsList.Add(newCursor);
        }
    }
    
    /// <summary>
    /// Main returns the current color the user has equipped or changes to new color and returns it
    /// </summary>
    /// <param name="newColor"> if the color needs to be changed</param>
    /// <returns></returns>
    public static Color ReturnUserColor(GameColors newColor = GameColors.White)
    {
        switch (newColor)
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

