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
    public TMP_Dropdown LobbyTypeDropdown;

    //local user's start button for starting a match
    public GameObject StartGameBtn;
    public GameObject EndGameBtn;
    public MatchSummaryController MatchSummaryScreen;
    public TMP_Text LiveScoreboardText;
    public TMP_Text MatchPingText;
    public Image OwnColorSwatchImage;
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

    // Shared "extra" payload for every UpdateReady call — colour, presence, own rank, and
    // relay compression type if host.
    public Dictionary<string, object> BuildExtraJson()
    {
        var extra = new Dictionary<string, object>();
        extra["colorIndex"] = (int)_currentUserInfo.UserGameColor;
        extra["presentSinceStart"] = _currentUserInfo.PresentSinceStart;
        extra["rank"] = _currentUserInfo.WorldwideRank;
        if (IsLocalUserHost())
        {
            extra["relayCompressionType"] = (int)BrainCloudManager.Instance._relayCompressionType;
        }
        return extra;
    }

    private void SendUpdateReady()
    {
        BrainCloudManager.Instance.Wrapper.LobbyService.UpdateReady
        (
            stManager.CurrentLobby.LobbyID,
            stManager.isReady,
            BuildExtraJson(),
            null,
            BrainCloudManager.Instance.OnUpdateReadyFailure
        );
    }

    //Note: Lobby text color is changed within UpdateLobbyList() from Brain Cloud's callback OnLobbyEvent()
    public void UpdateLocalColorChange(int newColor)
    {
        _currentUserInfo.UserGameColor = newColor;
        Settings.SetPlayerPrefColor(newColor);
        SendUpdateReady();
    }

    public void UpdatePresentSinceStart()
    {
        _currentUserInfo.PresentSinceStart = true;
        SendUpdateReady();
    }

    public void SendUpdateRelayCompressionType()
    {
        SendUpdateReady();
    }

    // Called once rank is known — pushes it to lobby-mates right away if already in a lobby.
    public void PushWorldwideRankIfInLobby()
    {
        if (stManager.CurrentLobby != null && !string.IsNullOrEmpty(stManager.CurrentLobby.LobbyID))
            SendUpdateReady();
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
        // "Exit Match", not host-only "End Match" — everyone can leave individually; the match
        // itself only ends via the 90s timer.
        EndGameBtn.SetActive(true);
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

    // Called by BrainCloudManager whenever match_result/lb_result data arrives or changes.
    public void RefreshMatchSummary()
    {
        if (MatchSummaryScreen != null)
            MatchSummaryScreen.RefreshResults();
    }

    // Live "RANK / PLAYER / COVERAGE" sidebar — rebuilt on every live_coverage snapshot.
    // Exact hex values from the cpp reference client's rankColorFor() (game.cpp), reused verbatim
    // there across the leaderboard panel, in-match sidebar, and match summary.
    private static string RankMedalColor(int rank)
    {
        switch (rank)
        {
            case 1: return "#FFD700"; // gold
            case 2: return "#BFBFBF"; // silver
            case 3: return "#CC8033"; // bronze
            default: return "#FFFFFF";
        }
    }

    public void RefreshLiveScoreboard()
    {
        if (LiveScoreboardText == null) return;

        var sb = new System.Text.StringBuilder();
        foreach (var entry in stManager.LiveCoverage)
        {
            string name = "?";
            bool isMe = false;
            foreach (var m in stManager.CurrentLobby.Members)
            {
                if (m.cxId == entry.CxId)
                {
                    name = m.Username;
                    isMe = m.ProfileID == CurrentUserInfo.ProfileID;
                    break;
                }
            }
            Color dotColour = ReturnUserColor(entry.ColourIndex);
            string hex = ColorUtility.ToHtmlStringRGB(dotColour);
            // Rank medal colours + "me" name colour match the cpp reference client's HUD sidebar exactly.
            string rankColor = RankMedalColor(entry.Rank);
            string nameDisplay = isMe ? $"<color=#59ff73>{name}</color> <mark=#ffffff22>YOU</mark>" : name;
            string row = $"<color={rankColor}>#{entry.Rank}</color>  <color=#{hex}>●</color>  {nameDisplay}<pos=90%><align=right>{entry.CoveragePct:F0}%</align>";
            sb.AppendLine(isMe ? $"<mark=#4caf5022>{row}</mark>" : row);
        }
        LiveScoreboardText.text = sb.ToString();
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
        if (OwnColorSwatchImage != null)
            OwnColorSwatchImage.color = ReturnUserColor(_currentUserInfo.UserGameColor);
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

        if (entry.HostBadgeRoot)
        {
            entry.HostBadgeRoot.SetActive(info.IsHost);
        }

        if (entry.YouBadgeRoot)
        {
            entry.YouBadgeRoot.SetActive(info.ProfileID == CurrentUserInfo.ProfileID);
        }

        if (entry.StatusText)
        {
            entry.StatusText.text = info.IsReady ? "Ready" : "Not ready";
        }

        if (entry.PingText)
        {
            entry.PingText.text = info.activePing >= 999 || info.activePing < 0 ? "..." : $"{info.activePing}ms";
        }

        if (entry.RankText)
        {
            entry.RankText.text = info.WorldwideRank > 0 ? $"#{info.WorldwideRank}" : "Unranked";
        }

        Color userColor = ReturnUserColor(info.UserGameColor);
        entry.UsernameText.color = userColor;
        if (entry.UserDotImage != null)
        {
            entry.UserDotImage.color = userColor;
        }

        if (entry.UserDotButton != null)
        {
            bool isLocal = info.ProfileID == CurrentUserInfo.ProfileID;
            entry.UserDotButton.onClick.RemoveAllListeners();
            entry.UserDotButton.interactable = isLocal;
            if (isLocal)
                entry.UserDotButton.onClick.AddListener(() => ColorPopupController.Instance?.Show());
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

    public void UpdateLobbyDropdown(List<string> in_lobbyList)
    {
        LobbyTypeDropdown.options.Clear();
        for (int i = 0; i < in_lobbyList.Count; i++)
        {
            TMP_Dropdown.OptionData entry = new TMP_Dropdown.OptionData(in_lobbyList[i]);
            LobbyTypeDropdown.options.Add(entry);
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

