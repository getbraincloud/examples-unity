using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// - Holds info needed for the current user and other connected users
/// - Handles getting UI element data made by user
/// - References Prefabs used for listing members in a list
/// - Handles Error window
/// 
/// </summary>

public enum GameColors{Black,Purple,Grey,Orange,Blue,Green,Yellow,Cyan,White}
public enum GameMode {FreeForAll, Team}

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    public UserEntry UserEntryLobbyPrefab;
    public UserEntry UserEntryMatchPrefab;
    public UserCursor UserCursorPrefab;
    [Header("Parent Transforms")]
    public GameObject UserEntryLobbyParentFFA;
    public GameObject UserEntryMatchParentFFA;
    public GameObject UserEntryLobbyParentTeamAlpha;
    public GameObject UserEntryLobbyParentTeamBeta;
    public GameObject UserEntryMatchParentTeamAlpha;
    public GameObject UserEntryMatchParentTeamBeta;
    public GameObject UserCursorParent;
    [Header("UI References")]
    public TMP_InputField UsernameInputField;
    public TMP_InputField PasswordInputField;
    public TMP_Text LoggedInNameText;
    public TMP_Text AppIdText;
    public TMP_Text LobbyIdText;
    public Button ReconnectButton;
    //for updating members list of shockwaves
    public GameArea GameArea;
    public Button JoinInProgressButton;
    public TMP_Dropdown FFADropdown;
    public TMP_Dropdown TeamDropdown;
    //local user's start button for starting a match
    public GameObject StartGameBtn;
    public GameObject EndGameBtn;
    public TMP_Text LobbyLocalUserText;
    public TMP_Dropdown CompressionDropdown;
    private EventSystem _eventSystem;
    //List references for clean up when game closes
    private readonly List<UserEntry> _matchEntries = new List<UserEntry>();
    private readonly List<UserCursor> _userCursorsList = new List<UserCursor>();

    private GameMode _gameMode = GameMode.FreeForAll;
    public GameMode GameMode
    {
        get => _gameMode;
        set => _gameMode = value;
    }
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
        extra["presentSinceStart"] = _currentUserInfo.PresentSinceStart;
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

    public void UpdatePresentSinceStart()
    {
        _currentUserInfo.PresentSinceStart = true;
        //Send update to BC
        Dictionary<string,object> extra = new Dictionary<string, object>();
        extra["colorIndex"] = (int)_currentUserInfo.UserGameColor;
        extra["presentSinceStart"] = _currentUserInfo.PresentSinceStart;
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
        extra["presentSinceStart"] = _currentUserInfo.PresentSinceStart;
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
        Transform parent = UserCursorParent.transform;
        for (int i = 0; i < lobby.Members.Count; i++)
        {
            //Set up Cursor image
            UserCursor newCursor = Instantiate(UserCursorPrefab, new Vector3(9999, 9999, 0), Quaternion.identity, parent);
            newCursor.AdjustVisibility(false);
            newColor = ReturnUserColor(lobby.Members[i].UserGameColor);
            newCursor.SetUpCursor(newColor,lobby.Members[i].Username);
            
            //Set up Rect Transform settings to anchor image
            lobby.Members[i].UserCursor = newCursor;
            RectTransform UITransform = newCursor.GetComponent<RectTransform>();
            Vector2 minMax = new Vector2(0, 1);
            UITransform.anchorMin = minMax;
            UITransform.anchorMax = minMax;
            UITransform.pivot = new Vector2(0.5f, 0.5f);;
            
            //Save references for later..
            lobby.Members[i].CursorTransform = UITransform;
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
                if(matchEntry != null && matchEntry.gameObject != null)
                {
                    Destroy(matchEntry.gameObject);                    
                }
            }
            _matchEntries.Clear();    
        }
    }
    
    public void UpdateLobbyState()
    {   
        AdjustLobbyList();
        StartGameBtn.SetActive(IsLocalUserHost());
        EndGameBtn.SetActive(IsLocalUserHost());
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
        AdjustMatchList();
    }

    private void CleanUpChildrenOfParent(Transform parent)
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
    }

    private void AdjustLobbyList()
    {
        if (_gameMode == GameMode.FreeForAll)
        {
            CleanUpChildrenOfParent(UserEntryLobbyParentFFA.transform);
            //populate user entries based on members in lobby
            Lobby lobby = StateManager.Instance.CurrentLobby;
            for (int i = 0; i < lobby.Members.Count; i++)
            {
                if (lobby.Members[i].IsAlive)
                {
                    var newEntry = Instantiate(UserEntryLobbyPrefab, Vector3.zero, Quaternion.identity, UserEntryLobbyParentFFA.transform);
                    SetUpUserEntry(lobby.Members[i], newEntry, false);
                    _matchEntries.Add(newEntry);
                }    
            }    
        }
        else if (_gameMode == GameMode.Team)
        {
            CleanUpChildrenOfParent(UserEntryLobbyParentTeamAlpha.transform);
            CleanUpChildrenOfParent(UserEntryLobbyParentTeamBeta.transform);
            //populate user entries based on members in lobby
            Lobby lobby = StateManager.Instance.CurrentLobby;
            for (int i = 0; i < lobby.Members.Count; i++)
            {
                if (lobby.Members[i].IsAlive)
                {
                    Transform parent = null;
                    if (lobby.Members[i].Team == TeamCodes.alpha)
                    {
                        parent = UserEntryLobbyParentTeamAlpha.transform;
                    }
                    //Member should be on team beta
                    else
                    {
                        parent = UserEntryLobbyParentTeamBeta.transform;
                    }
                    var newEntry = Instantiate(UserEntryLobbyPrefab, Vector3.zero, Quaternion.identity, parent);
                    SetUpUserEntry(lobby.Members[i], newEntry, false);
                    _matchEntries.Add(newEntry);
                }    
            }   
        }
        

        LobbyLocalUserText.text = _currentUserInfo.Username;
        LobbyLocalUserText.color = ReturnUserColor(_currentUserInfo.UserGameColor);
    }

    private void AdjustMatchList()
    {
        if (_gameMode == GameMode.FreeForAll)
        {
            CleanUpChildrenOfParent(UserEntryMatchParentFFA.transform);
            //populate user entries based on members in lobby
            Lobby lobby = StateManager.Instance.CurrentLobby;
            for (int i = 0; i < lobby.Members.Count; i++)
            {
                if (lobby.Members[i].IsAlive)
                {
                    var newEntry = Instantiate(UserEntryMatchPrefab, Vector3.zero, Quaternion.identity, UserEntryMatchParentFFA.transform);
                    SetUpUserEntry(lobby.Members[i], newEntry, true);
                    _matchEntries.Add(newEntry);
                }    
            }    
        }
        else if(_gameMode == GameMode.Team)
        {
            CleanUpChildrenOfParent(UserEntryMatchParentTeamAlpha.transform);
            CleanUpChildrenOfParent(UserEntryMatchParentTeamBeta.transform);
            //populate user entries based on members in lobby
            Lobby lobby = StateManager.Instance.CurrentLobby;
            for (int i = 0; i < lobby.Members.Count; i++)
            {
                if (lobby.Members[i].IsAlive)
                {
                    Transform parent = null;
                    if (lobby.Members[i].Team == TeamCodes.alpha)
                    {
                        parent = UserEntryMatchParentTeamAlpha.transform;
                    }
                    //Member should be on team beta
                    else
                    {
                        parent = UserEntryMatchParentTeamBeta.transform;
                    }
                    var newEntry = Instantiate(UserEntryMatchPrefab, Vector3.zero, Quaternion.identity, parent);
                    SetUpUserEntry(lobby.Members[i], newEntry, true);
                    _matchEntries.Add(newEntry);
                }
            }
        }
    }
    
    private void SetUpUserEntry(UserInfo info,UserEntry entry, bool updateMatch)
    {
        entry.UsernameText.text = info.Username;
        
        if(updateMatch && !info.IsReady && !info.PresentSinceStart)
        {
            entry.UsernameText.text = info.Username + " (In Lobby)";
        }

        if (entry.HostImage)
        {
            entry.HostImage.enabled = info.IsHost;    
        }
         
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
    
    public void UpdateLobbyDropdowns(List<string> in_ffaList, List<string> in_teamList)
    {
        FFADropdown.options.Clear();
        TeamDropdown.options.Clear();
        for (int i = 0; i < in_ffaList.Count; i++)
        {
            TMP_Dropdown.OptionData entry = new TMP_Dropdown.OptionData(in_ffaList[i]);
            FFADropdown.options.Add(entry);            
        }

        for (int i = 0; i < in_teamList.Count; i++)
        {
            TMP_Dropdown.OptionData entry = new TMP_Dropdown.OptionData(in_teamList[i]);
            TeamDropdown.options.Add(entry);
        }
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
        return currentLobby.OwnerID == CurrentUserInfo.ProfileID;
    }

    #endregion
}

