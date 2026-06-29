using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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

public enum GameMode { FreeForAll, Team }

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
    public TMP_Text AppIdText, LobbyIdText, AppVersionText, BCVersionText, ServerVersionText, EnvText;
    public Button ReconnectButton;
    public Toggle RememberMeToggle;

    [Header("Ping Region Data")]
    public Toggle UsePingDataToggle;
    public TMP_Text PingRegionQualityText;
    public TMP_Text RelayPingText;

    //for updating members list of splatters
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
    private readonly List<UserEntry> _liveMatchEntryList = new List<UserEntry>();
    private readonly List<UserInfo> _liveMatchUserList = new List<UserInfo>();

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

    private static List<Color> colours = new List<Color>();

    private void Awake()
    {
        stManager = StateManager.Instance;
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
        AppIdText.text = BrainCloud.Plugin.Interface.AppId;
        AppVersionText.text = Application.version;
        BCVersionText.text = BrainCloudManager.Instance.Wrapper.Client.BrainCloudClientVersion;
        BrainCloudManager.Instance.Wrapper.Client.GetAuthenticationService().getServerVersion(
            (string jsonResponse, object cbObj) =>
            {
                var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
                var data = response["data"] as Dictionary<string, object>;

                ServerVersionText.text = data["serverVersion"] as string;
            });

        string env = BrainCloud.Plugin.Interface.DispatcherURL.Split('.')[1];
        if (env == "braincloudservers") env = "prod";
        EnvText.text = env;
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
    public void UpdateColorList(List<Color> listOfColors)
    {
        colours.Clear();
        colours = listOfColors;
    }

    private void LoadPlayerSettings()
    {
        _currentUserInfo = Settings.LoadPlayerInfo();
        if (UsePingDataToggle != null)
        {
            UsePingDataToggle.isOn = Settings.GetUsePingData();
            UsePingDataToggle.onValueChanged.AddListener(OnUsePingDataToggleChanged);
        }
    }

    public void OnUsePingDataToggleChanged(bool value)
    {
        Settings.SetUsePingData(value);
    }

    public void UpdateMainMenuText()
    {
        PlayerPrefs.SetString(Settings.UsernameKey, _currentUserInfo.Username);
        LoggedInNameText.text = $"Logged in as {_currentUserInfo.Username}";
    }

    //Note: Lobby text color is changed within UpdateLobbyList() from Brain Cloud's callback OnLobbyEvent()
    public void UpdateLocalColorChange(int newColor)
    {
        _currentUserInfo.UserGameColor = newColor;
        //Apply in game color changes
        Settings.SetPlayerPrefColor(newColor);

        //Send update to BC
        Dictionary<string, object> extra = new Dictionary<string, object>();
        extra["colorIndex"] = (int)_currentUserInfo.UserGameColor;
        extra["presentSinceStart"] = _currentUserInfo.PresentSinceStart;
        if (IsLocalUserHost())
        {
            extra["relayCompressionType"] = (int)BrainCloudManager.Instance._relayCompressionType;
        }
        BrainCloudManager.Instance.Wrapper.LobbyService.UpdateReady
        (
            stManager.CurrentLobby.LobbyID,
            stManager.isReady,
            extra,
            null,
            BrainCloudManager.Instance.OnUpdateReadyFailure
        );
    }

    public void UpdatePresentSinceStart()
    {
        _currentUserInfo.PresentSinceStart = true;
        //Send update to BC
        Dictionary<string, object> extra = new Dictionary<string, object>();
        extra["colorIndex"] = (int)_currentUserInfo.UserGameColor;
        extra["presentSinceStart"] = _currentUserInfo.PresentSinceStart;
        if (IsLocalUserHost())
        {
            extra["relayCompressionType"] = (int)BrainCloudManager.Instance._relayCompressionType;
        }
        BrainCloudManager.Instance.Wrapper.LobbyService.UpdateReady
        (
            stManager.CurrentLobby.LobbyID,
            stManager.isReady,
            extra,
            null,
            BrainCloudManager.Instance.OnUpdateReadyFailure
        );
    }

    public void SendUpdateRelayCompressionType()
    {
        //Send update to BC
        Dictionary<string, object> extra = new Dictionary<string, object>();
        extra["colorIndex"] = (int)_currentUserInfo.UserGameColor;
        extra["presentSinceStart"] = _currentUserInfo.PresentSinceStart;
        if (IsLocalUserHost())
        {
            extra["relayCompressionType"] = (int)BrainCloudManager.Instance._relayCompressionType;
        }
        BrainCloudManager.Instance.Wrapper.LobbyService.UpdateReady
        (
            stManager.CurrentLobby.LobbyID,
            stManager.isReady,
            extra,
            null,
            BrainCloudManager.Instance.OnUpdateReadyFailure
        );
    }

    public void UpdateCursorList()
    {
        Lobby lobby = stManager.CurrentLobby;
        EmptyCursorList();
        Color newColor;
        Transform parent = UserCursorParent.transform;
        for (int i = 0; i < lobby.Members.Count; i++)
        {
            //Set up Cursor image
            UserCursor newCursor = Instantiate(UserCursorPrefab, new Vector3(9999, 9999, 0), Quaternion.identity, parent);
            newCursor.AdjustVisibility(false);
            newColor = ReturnUserColor(lobby.Members[i].UserGameColor);
            newCursor.SetUpCursor(newColor, lobby.Members[i].Username);

            //Set up Rect Transform settings to anchor image
            lobby.Members[i].UserCursor = newCursor;
            RectTransform UITransform = newCursor.GetComponent<RectTransform>();
            Vector2 minMax = new Vector2(0, 1);
            UITransform.anchorMin = minMax;
            UITransform.anchorMax = minMax;
            UITransform.pivot = new Vector2(0.5f, 0.5f); ;

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
                if (matchEntry != null && matchEntry.gameObject != null)
                {
                    Destroy(matchEntry.gameObject);
                }
            }
            _matchEntries.Clear();
        }
        _liveMatchEntryList.Clear();
        _liveMatchUserList.Clear();
    }

    public void UpdateLobbyState()
    {
        AdjustLobbyList();
        StartGameBtn.SetActive(IsLocalUserHost());
        EndGameBtn.SetActive(IsLocalUserHost());
        CompressionDropdown.interactable = IsLocalUserHost();
        LobbyIdText.text = stManager.CurrentLobby.LobbyID;
        if (!LobbyIdText.enabled)
        {
            LobbyIdText.enabled = true;
        }
        UpdatePingRegionQuality();
    }

    public void UpdatePingRegionQuality()
    {
        if (PingRegionQualityText == null) return;

        var pingData = BrainCloudManager.Instance.PingData;
        string lobbyId = stManager.CurrentLobby != null ? stManager.CurrentLobby.LobbyID ?? "" : "";
        int colonPos = lobbyId.IndexOf(':');
        bool regionIsNumeric = colonPos > 0 && int.TryParse(lobbyId.Substring(0, colonPos), out _);
        string lobbyRegion = (colonPos > 0 && !regionIsNumeric) ? lobbyId.Substring(0, colonPos) : "";

        if (pingData.Count > 0)
        {
            string lines = "";
            int bestPing = int.MaxValue;
            foreach (var ms in pingData.Values) if (ms < bestPing) bestPing = ms;
            foreach (var kv in pingData)
            {
                string marker = kv.Key == lobbyRegion ? " ◄" : "";
                lines += $"{kv.Key}: {kv.Value} ms{marker}\n";
            }
            if (lobbyRegion.Length > 0 && pingData.TryGetValue(lobbyRegion, out int lobbyPing))
            {
                bool isGood = (lobbyPing - bestPing) <= 30;
                PingRegionQualityText.color = isGood ? new Color(0.27f, 0.93f, 0.27f) : new Color(0.93f, 0.27f, 0.27f);
            }
            PingRegionQualityText.text = lines.TrimEnd();
            PingRegionQualityText.gameObject.SetActive(true);
        }
        else
        {
            PingRegionQualityText.gameObject.SetActive(false);
        }
    }

    public void RefreshMatchEntryPings()
    {
        for (int i = 0; i < _liveMatchEntryList.Count && i < _liveMatchUserList.Count; i++)
        {
            UserEntry entry = _liveMatchEntryList[i];
            UserInfo user = _liveMatchUserList[i];
            if (entry == null || entry.UsernameText == null) continue;

            string pingStr = user.activePing < 0 ? " ..." : user.activePing >= 999 ? " T/O" : $" {user.activePing} ms";
            string baseName = user.Username;
            if (!user.IsReady && !user.PresentSinceStart) baseName += " (In Lobby)";
            entry.UsernameText.text = baseName + pingStr;
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
            Lobby lobby = stManager.CurrentLobby;
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
            Lobby lobby = stManager.CurrentLobby;
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
        _liveMatchEntryList.Clear();
        _liveMatchUserList.Clear();

        if (_gameMode == GameMode.FreeForAll)
        {
            CleanUpChildrenOfParent(UserEntryMatchParentFFA.transform);
            Lobby lobby = stManager.CurrentLobby;
            for (int i = 0; i < lobby.Members.Count; i++)
            {
                if (lobby.Members[i].IsAlive)
                {
                    var newEntry = Instantiate(UserEntryMatchPrefab, Vector3.zero, Quaternion.identity, UserEntryMatchParentFFA.transform);
                    SetUpUserEntry(lobby.Members[i], newEntry, true);
                    _matchEntries.Add(newEntry);
                    _liveMatchEntryList.Add(newEntry);
                    _liveMatchUserList.Add(lobby.Members[i]);
                }
            }
        }
        else if (_gameMode == GameMode.Team)
        {
            CleanUpChildrenOfParent(UserEntryMatchParentTeamAlpha.transform);
            CleanUpChildrenOfParent(UserEntryMatchParentTeamBeta.transform);
            Lobby lobby = stManager.CurrentLobby;
            for (int i = 0; i < lobby.Members.Count; i++)
            {
                if (lobby.Members[i].IsAlive)
                {
                    Transform parent = lobby.Members[i].Team == TeamCodes.alpha
                        ? UserEntryMatchParentTeamAlpha.transform
                        : UserEntryMatchParentTeamBeta.transform;
                    var newEntry = Instantiate(UserEntryMatchPrefab, Vector3.zero, Quaternion.identity, parent);
                    SetUpUserEntry(lobby.Members[i], newEntry, true);
                    _matchEntries.Add(newEntry);
                    _liveMatchEntryList.Add(newEntry);
                    _liveMatchUserList.Add(lobby.Members[i]);
                }
            }
        }
    }

    private void SetUpUserEntry(UserInfo info, UserEntry entry, bool updateMatch)
    {
        entry.UsernameText.text = info.Username;

        if (updateMatch && !info.IsReady && !info.PresentSinceStart)
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

    public void AdjustUserSplatterMask(string username, bool isVisible)
    {
        //populate user entries based on members in lobby
        Lobby lobby = stManager.CurrentLobby;
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
    public static Color ReturnUserColor(int newColor = 0)
    {
        if (newColor >= 0 && newColor < colours.Count)
        {
            return colours[newColor];
        }
        else
        {
            return colours[0];
        }
    }

    public bool IsLocalUserHost()
    {
        Lobby currentLobby = stManager.CurrentLobby;
        return currentLobby.OwnerID == CurrentUserInfo.ProfileID;
    }


    StateManager stManager = StateManager.Instance;
    #endregion
}

