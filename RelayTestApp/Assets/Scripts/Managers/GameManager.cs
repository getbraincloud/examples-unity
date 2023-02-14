using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    public TMP_Text AppIdText;
    public TMP_Text LobbyIdText;
    public Button ReconnectButton;
    public Button JoinInProgressButton;
    //for updating members list of shockwaves
    public GameArea GameArea;  
    //local user's start button for starting a match
    public GameObject StartGameBtn;
    public GameObject EndGameBtn;
    public TMP_Text LobbyLocalUserText;
    public TMP_Dropdown CompressionDropdown;
    private EventSystem _eventSystem;
    //List references for clean up when game closes
    private readonly List<UserEntry> _matchEntries = new List<UserEntry>();
    private readonly List<UserCursor> _userCursorsList = new List<UserCursor>();     
    
    //Singleton Pattern
    private static GameManager _instance;
    public static GameManager Instance => _instance;
    
    //Local User Info
    [SerializeField]
    private UserInfo _currentUserInfo;
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
        ReconnectButton.gameObject.SetActive(false);
        JoinInProgressButton.gameObject.SetActive(false);
        _eventSystem = EventSystem.current;
        PasswordInputField.inputType = TMP_InputField.InputType.Password;
        LoadPlayerSettings();
        LobbyIdText.enabled = false;
        AppIdText.text = $"App ID: {BrainCloud.Plugin.Interface.AppId}";
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Selectable next = _eventSystem.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
         
            if (next != null)
            {
                InputField inputfield = next.GetComponent<InputField>();
                if (inputfield != null)
                {
                    //if it's an input field, also set the text caret
                    inputfield.OnPointerClick(new PointerEventData(_eventSystem));
                }
                _eventSystem.SetSelectedGameObject(next.gameObject, new BaseEventData(_eventSystem));
            }
        }
    }
    
#region Update Components
    private void LoadPlayerSettings()
    {
        _currentUserInfo = Settings.LoadPlayerInfo();
        UsernameInputField.text = _currentUserInfo.Username;
        PasswordInputField.text = PlayerPrefs.GetString(Settings.PasswordKey);
    }
    
    public void UpdateMainMenuText()
    {
        PlayerPrefs.SetString(Settings.UsernameKey, _currentUserInfo.Username);
        LoggedInNameText.text = $"Logged in as {_currentUserInfo.Username}";
    }
    
    //Note: Lobby text color is changed within UpdateLobbyList() from Brain Cloud's callback OnLobbyEvent()
    public void UpdateLocalColorChange(GameColors newColor)
    {
        _currentUserInfo.UserGameColor = newColor;
        //Apply in game color changes
        Settings.SetPlayerPrefColor(newColor);
        
        //Send update to BC
        Dictionary<string,object> extra = new Dictionary<string, object>();
        extra["colorIndex"] = (int)_currentUserInfo.UserGameColor;
        if (IsLocalUserHost())
        {
            extra["relayCompressionType"] = (int) BrainCloudManager.Instance._relayCompressionType;
        }
        BrainCloudManager.Instance.Wrapper.LobbyService.UpdateReady
        (
            StateManager.Instance.CurrentLobby.LobbyID,
            StateManager.Instance.isReady,
            extra
        );
    }

    public void SendUpdateRelayCompressionType()
    {
        //Send update to BC
        Dictionary<string,object> extra = new Dictionary<string, object>();
        extra["colorIndex"] = (int)_currentUserInfo.UserGameColor;
        if (IsLocalUserHost())
        {
            extra["relayCompressionType"] = (int) BrainCloudManager.Instance._relayCompressionType;
        }
        BrainCloudManager.Instance.Wrapper.LobbyService.UpdateReady
        (
            StateManager.Instance.CurrentLobby.LobbyID,
            StateManager.Instance.isReady,
            extra
        );
    }
    
    public void UpdateCursorList()
    {
        Lobby lobby = StateManager.Instance.CurrentLobby;
        EmptyCursorList();
        Color newColor;
        for (int i = 0; i < lobby.Members.Count; i++)
        {
            UserCursor newCursor = Instantiate(UserCursorPrefab, Vector3.zero, Quaternion.identity, UserCursorParent.transform);
            lobby.Members[i].MousePosition = new Vector2(9999, 9999);
            newCursor.AdjustVisibility(false);
            newColor = ReturnUserColor(lobby.Members[i].UserGameColor);
            newCursor.SetUpCursor(newColor,lobby.Members[i].Username);
            lobby.Members[i].UserCursor = newCursor;
            _userCursorsList.Add(newCursor);
            if (lobby.Members[i].Username == CurrentUserInfo.Username)
            {
                GameArea.LocalUserCursor = newCursor;
            }
        }
    }

    public void ClearMatchEntries()
    { 
        if (_matchEntries.Count > 0)
        {
            foreach (UserEntry matchEntry in _matchEntries)
            {
                Destroy(matchEntry.gameObject);
            }
            _matchEntries.Clear();    
        }
    }
    
    public void UpdateLobbyState()
    {   
        AdjustEntryList(UserEntryLobbyParent.transform,UserEntryLobbyPrefab);
        StartGameBtn.SetActive(IsLocalUserHost());
        EndGameBtn.SetActive(IsLocalUserHost());
        if (!StartGameBtn.gameObject.activeSelf &&
            !BrainCloudManager.Instance.PresentWhileStarted)
        {
            JoinInProgressButton.gameObject.SetActive(true);
        }
        else
        {
            JoinInProgressButton.gameObject.SetActive(false);
        }
        CompressionDropdown.interactable = IsLocalUserHost();
        LobbyIdText.text = $"Lobby ID: {StateManager.Instance.CurrentLobby.LobbyID}";
        if (!LobbyIdText.enabled)
        {
            LobbyIdText.enabled = true;
        }
    }

    public void UpdateMatchAndLobbyState()
    {
        UpdateLobbyState();
        UpdateMatchState();
    }
    
    /// <summary>
    /// After list of users is generated for the current match, call this to display the connected users
    /// </summary>
    public void UpdateMatchState()
    {
        AdjustEntryList(UserEntryMatchParent.transform,UserEntryMatchPrefab);
    }

    private void AdjustEntryList(Transform parent, UserEntry prefab)
    {
        //Clean up any child objects in parent
        if (parent.childCount > 0)
        {
            for (int i = 0; i < parent.childCount; ++i)
            {
                Transform child = parent.GetChild(i);
                Destroy(child.gameObject);
            }
        }
        
        //populate user entries based on members in lobby
        Lobby lobby = StateManager.Instance.CurrentLobby;
        for (int i = 0; i < lobby.Members.Count; i++)
        {
            if (lobby.Members[i].IsAlive)
            {
                var newEntry = Instantiate(prefab, Vector3.zero, Quaternion.identity,parent);
                SetUpUserEntry(lobby.Members[i], newEntry);
                _matchEntries.Add(newEntry);
            }    
        }

        LobbyLocalUserText.text = _currentUserInfo.Username;
        LobbyLocalUserText.color = ReturnUserColor(_currentUserInfo.UserGameColor); 
    }
    
    private void SetUpUserEntry(UserInfo info,UserEntry entry)
    {
        entry.UsernameText.text = info.Username;
        Color userColor = ReturnUserColor(info.UserGameColor);
        entry.UsernameText.color = userColor;
        if (entry.UserDotImage != null)
        {
            entry.UserDotImage.color = userColor;
        }
    }

    public void AdjustUserShockwaveMask(string username,bool isVisible)
    {
        //populate user entries based on members in lobby
        Lobby lobby = StateManager.Instance.CurrentLobby;
        for (int i = 0; i < lobby.Members.Count; i++)
        {
            if (lobby.Members[i].Username.Equals(username))
            {
                lobby.Members[i].AllowSendTo = isVisible;
            }
        }
        if (CurrentUserInfo.Username.Equals(username))
        {
            CurrentUserInfo.AllowSendTo = isVisible;
        }
    }

    public void EmptyCursorList()
    {
        if (_userCursorsList.Count <= 0) return;

        foreach (UserCursor userCursor in _userCursorsList)
        {
            Destroy(userCursor.gameObject);   
        }
        _userCursorsList.Clear();
    }
#endregion Update Components
    
#region Helper Functions

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
                return new Color(0.33f,0.25f,0.37f);
            case GameColors.Grey:
                return new Color(0.4f,0.4f,0.4f);
            case GameColors.Orange:
                return new Color(0.85f, 0.4f, 0.04f);
            case GameColors.Blue:
                return new Color(0.31f,0.54f,0.84f);
            case GameColors.Green:
                return new Color(0.39f,0.72f,0.39f);
            case GameColors.Yellow:
                return new Color(0.9f,0.78f,0.43f);
            case GameColors.Cyan:
                return new Color(0.86f,0.96f,1);
        }
            
        return Color.white;
    }

    public bool IsLocalUserHost()
    {
        Lobby currentLobby = StateManager.Instance.CurrentLobby;
        return currentLobby.OwnerID == CurrentUserInfo.ID;
    }

#endregion
}

