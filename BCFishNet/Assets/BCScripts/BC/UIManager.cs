using BrainCloud;
using BrainCloud.JsonFx.Json;
using FishNet.Managing.Scened;
using FishNet.Managing.Timing;
using FishNet.Serializing;
using BCFishNet;
using GameKit.Dependencies.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject _loginView, _mainView, _lobbyView, _topBarView, _loadingView;
    [SerializeField] private TMP_Text _mainStatus;
    #region LoginVars
    [Header("Login Vars")]
    [SerializeField] private TMP_InputField _usernameInput, _passwordInput, _displayNameInput;
    [SerializeField] private TMP_Text _authErrorText;
    [SerializeField] private Button _loginButton;
    #endregion

    #region MainMenuVars
    [Header("MainMenu Vars")]
    [SerializeField] private GameObject _displayNamePanel;
    [SerializeField] private TMP_Text _displayNameText, _accountNameText;
    [SerializeField] private Button _createLobbyButton, _findLobbyButton, _cancelMatchmakingButton, _findCreateLobbyButton;
    [SerializeField] private Image _playerColourImage;

    private string _currentEntryId;
    #endregion

    #region LobbyVars
    [Header("Lobby Vars")]
    [SerializeField] private TMP_Text _lobbyIdText, _loadingNumMembersText;
    [SerializeField] private Button _readyUpButton, _leaveLobbyButton;
    [SerializeField] private LobbyMemberItem _lobbyMemberRowPrefab;
    [SerializeField] private Transform _lobbyMembersContainer;

    #endregion

    #region LoadingVars
    [Header("Loading Vars")]
    [SerializeField] private TMP_Text _loadingStatusText;

    #endregion

    public enum State
    {
        None,
        Loading,
        Login,
        Main,
        Lobby,
        InGame
    }

    private State _curState = State.Login;
    // Start is called before the first frame update
    void Start()
    {
        // hide the top bar view
        _topBarView.SetActive(false);
        _loadingView.SetActive(false);
        _mainStatus.transform.parent.gameObject.SetActive(false);

        // login
        _authErrorText.text = string.Empty;

        //main menu
        _mainStatus.text = "Select an option to continue";

        if (BCManager.Instance.bc.Client.IsAuthenticated())
        {
            OnAuthSuccess();
            
        }
        else
        {
#if P1
            LoginP1();

#elif P2
            LoginP2();

#elif P3
            LoginP3();
#else
            /*
                        //login
                        //check if authenticated
                        var bc = BCManager.Instance.bc;
                    var storedId = bc.GetStoredProfileId();
                    var storedAnonymousId = bc.GetStoredAnonymousId();

                    if (storedId != null && storedAnonymousId != null && storedId != string.Empty && storedAnonymousId != string.Empty)
                    {
                        BCManager.Instance.AuthenticateAnonymous((success) =>
                        {
                            if (success)
                            {
                                OnAuthSuccess();
                            }
                            else
                            {
                                UpdateState(State.Login);
                            }
                        });
                    }
                    */
            UpdateState(State.Login);
#endif

        }
    }

    void OnEnable()
    {
        OnStateChanged();
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SelectNextField();
        }
    }

    void SelectNextField()
    {
        var current = EventSystem.current.currentSelectedGameObject;
        if (current != null)
        {
            var selectable = current.GetComponent<Selectable>();
            if (selectable != null)
            {
                var next = selectable.FindSelectableOnDown();
                if (next != null)
                {
                    EventSystem.current.SetSelectedGameObject(next.gameObject);
                    return;
                }
            }
        }
        // fallback: select the first input field
        if (_usernameInput != null && _usernameInput.gameObject.activeInHierarchy)
            _usernameInput.Select();
        else if (_passwordInput != null && _passwordInput.gameObject.activeInHierarchy)
            _passwordInput.Select();
        else if (_displayNameInput != null && _displayNameInput.gameObject.activeInHierarchy)
            _displayNameInput.Select();
    }

    void OnDestroy()
    {
        BCManager.Instance.bc.RTTService.DeregisterRTTLobbyCallback();
    }

    public void OnLeaveLobbyClicked()
    {
        SuccessCallback success = (response, cbObject) =>
        {
            UpdateState(State.Main);
        };

        BCManager.Instance.bc.LobbyService.LeaveLobby(BCManager.Instance.CurrentLobbyId, success);

    }

    public void OnReadyUpClicked()
    {
        Dictionary<string, object> extra = new Dictionary<string, object>();
        Debug.Log($"[Lobby] OnReadyUpClicked -> Setting Ready = true | LobbyId = {BCManager.Instance.CurrentLobbyId}");

        BCManager.Instance.bc.LobbyService.UpdateReady(BCManager.Instance.CurrentLobbyId, true, extra);
    }

    public void OnCancelMatchmakingClicked()
    {
        BCManager.Instance.bc.LobbyService.CancelFindRequest(BCManager.LOBBY_ID, _currentEntryId);
        _findLobbyButton.gameObject.SetActive(true);
        _createLobbyButton.gameObject.SetActive(true);
        _findCreateLobbyButton.gameObject.SetActive(true);
        _displayNamePanel.SetActive(true);

        _cancelMatchmakingButton.gameObject.SetActive(false);
        _loadingView.SetActive(false);
        _mainStatus.text = "Select an option to continue";
    }

    public void OnQuickFind()
    {
        _mainStatus.text = "Quick Finding lobby...";
        _loadingView.SetActive(true);
        _loadingStatusText.text = "Finding lobby...";

        BCManager.Instance.QuickFindLobby((entryId) =>
        {
            _currentEntryId = entryId;
            //temp disable lobby buttons
            _findLobbyButton.gameObject.SetActive(false);
            _createLobbyButton.gameObject.SetActive(false);
            _findCreateLobbyButton.gameObject.SetActive(false);
            _displayNamePanel.SetActive(false);
            _cancelMatchmakingButton.gameObject.SetActive(true);
        });
    }

    public void OnFindLobbyClicked()
    {
        _mainStatus.text = "Finding lobby...";
        _loadingView.SetActive(true);
        _loadingStatusText.text = "Finding lobby...";

        BCManager.Instance.FindLobby((entryId) =>
        {
            _currentEntryId = entryId;
            //temp disable lobby buttons
            _findLobbyButton.gameObject.SetActive(false);
            _createLobbyButton.gameObject.SetActive(false);
            _findCreateLobbyButton.gameObject.SetActive(false);

            _displayNamePanel.SetActive(false);
            _cancelMatchmakingButton.gameObject.SetActive(true);
        });
    }

    public void OnFindLobbyFromPrevious()
    {
        _mainStatus.text = "Finding lobby...";
        _loadingView.SetActive(true);
        _loadingStatusText.text = "Finding lobby...";

        BCManager.Instance.QuickFindLobbyWithPreviousMembers((entryId) =>
        {
            _mainStatus.text = "Found Lobby with Previous Members";
            _loadingStatusText.text = "Found Lobby with Previous Members";
            _currentEntryId = entryId;
            //temp disable lobby buttons
            _findLobbyButton.gameObject.SetActive(false);
            _createLobbyButton.gameObject.SetActive(false);
            _findCreateLobbyButton.gameObject.SetActive(false);

            _displayNamePanel.SetActive(false);
            _cancelMatchmakingButton.gameObject.SetActive(true);
        });
    }

    public void OnCreateLobbyClicked()
    {
        _loadingView.SetActive(true);
        _loadingStatusText.text = "Creating lobby...";
        BCManager.Instance.CreateLobby((json) =>
        {
        });
    }

    private void FillMemberRows(Dictionary<string, object>[] data)
    {
        foreach (Dictionary<string, object> row in data)
        {
            AddMemberRow(row);
        }
    }

    private void AddMemberRow(LobbyMemberData lobbyMemberData)
    {
        // Store the data in the manager
        BCManager.Instance.AddMember(lobbyMemberData);
        Debug.Log("AddMemberRow " + lobbyMemberData.ProfileId);

        _loadingNumMembersText.text = $"Members: {BCManager.Instance.LobbyMembersData.Count}";

        // if we can't find it create it
        if (!GetLobbyMemberItem(lobbyMemberData.ProfileId))
        {
            LobbyMemberItem lobbyMember = Instantiate(_lobbyMemberRowPrefab, _lobbyMembersContainer);
            lobbyMember.Config(lobbyMemberData);
        }
    }

    private LobbyMemberItem GetLobbyMemberItem(string profileId)
    {
        foreach (Transform child in _lobbyMembersContainer)
        {
            LobbyMemberItem item = child.GetComponent<LobbyMemberItem>();
            if (item != null)
            {
                Debug.Log($"[Lobby] Found child with LobbyMemberItem. ProfileId = {item.ProfileId}");

                if (item.ProfileId == profileId)
                {
                    Debug.Log($"[Lobby] Match found! Returning LobbyMemberItem for ProfileId: {profileId}");
                    return item;
                }
            }
        }
        return null;
    }

    private void AddMemberRow(Dictionary<string, object> memberData)
    {
        // Parse member data into LobbyMemberData
        var lobbyMemberData = new LobbyMemberData(
            memberData["name"] as string,
            (bool)memberData["isReady"],
            memberData.ContainsKey("profileId") ? memberData["profileId"] as string : null,
            memberData.ContainsKey("netId") ? System.Convert.ToInt16(memberData["netId"]) : (short)0,
            memberData.ContainsKey("rating") ? System.Convert.ToInt32(memberData["rating"]) : 0,
            memberData.ContainsKey("cxId") ? memberData["cxId"] as string : null,
            memberData.ContainsKey("extraData") ? memberData["extraData"] as Dictionary<string, object> : null
        );

        AddMemberRow(lobbyMemberData);
    }

    private void UpdateMemberReady(string id, bool ready)
    {
        LobbyMemberItem item = GetLobbyMemberItem(id);
        if (item)
        {
            item.UpdateReady(ready);
        }
    }

    private void RemoveMember(string id)
    {
        LobbyMemberItem item = GetLobbyMemberItem(id);

        Debug.Log("RemoveMember " + id);
        if (item)
        {
            Destroy(item.gameObject);
        }

        BCManager.Instance.RemoveMember(item.Data);
    }

    private void OnLobbyEvent(string json)
    {
        try
        {
            Debug.Log("OnLobbyEvent : " + json);

            Dictionary<string, object> response = JsonReader.Deserialize<Dictionary<string, object>>(json);
            Dictionary<string, object> jsonData = response["data"] as Dictionary<string, object>;

            Dictionary<string, object> lobbyData = new Dictionary<string, object>();
            Dictionary<string, object> memberData = new Dictionary<string, object>();

            string joiningMemberId = string.Empty;
            string fromMemberId = string.Empty;

            if (jsonData.ContainsKey("lobby"))
            {
                lobbyData = jsonData["lobby"] as Dictionary<string, object>;

                string ownerCxId = lobbyData["ownerCxId"] as string;
                if (!string.IsNullOrEmpty(ownerCxId))
                {
                    string[] parts = ownerCxId.Split(':');
                    if (parts.Length >= 3)
                    {
                        BCManager.Instance.LobbyOwnerId = parts[1]; // This is the profileID of the owner
                    }
                }
            }

            if (jsonData.ContainsKey("member"))
            {
                memberData = jsonData["member"] as Dictionary<string, object>;
                joiningMemberId = memberData["profileId"] as string;
            }
            if (jsonData.ContainsKey("from"))
            {
                var fromData = jsonData["from"] as Dictionary<string, object>;
                fromMemberId = fromData["id"] as string;
            }

            if (response.ContainsKey("operation"))
            {
                var operation = response["operation"] as string;
                var service = response["service"] as string;

                var data = response["data"] as Dictionary<string, object>;
                if (data.ContainsKey("reason") && data["reason"] is Dictionary<string, object> reasonData)
                {
                    if (reasonData.ContainsKey("desc"))
                    {
                        var description = reasonData["desc"] as string;
                        if (_mainStatus != null) _mainStatus.text = description;
                    }
                    else
                    {
                        if (_mainStatus != null) _mainStatus.text = "OP:" + operation;
                    }
                }
                else if (data.ContainsKey("desc")) // Fallback for older responses
                {
                    var description = data["desc"] as string;
                    if (_mainStatus != null) _mainStatus.text = description;
                }
                else
                {
                    if (_mainStatus != null) _mainStatus.text = "Service:" + service + " Operation:" + operation;
                }


                switch (operation)
                {
                    case "STATUS_UPDATE":
                    case "MEMBER_JOIN":
                        {
                            if (!string.IsNullOrEmpty(joiningMemberId) && joiningMemberId == BCManager.Instance.bc.Client.ProfileId)
                            {
                                //we just joined this lobby
                                UpdateState(State.Lobby);
                            }

                            Dictionary<string, object>[] membersData = lobbyData["members"] as Dictionary<string, object>[];
                            FillMemberRows(membersData);

                            BCManager.Instance.CurrentLobbyId = jsonData["lobbyId"] as string;

                            _lobbyIdText.text = BCManager.Instance.CurrentLobbyId;

                            LobbyMemberItem tempItem = GetLobbyMemberItem(BCManager.Instance.bc.Client.ProfileId);
                            if (tempItem != null)
                            {
                                tempItem.SendCurrentColourSignal();
                            }
                        }
                        break;
                    case "MEMBER_UPDATE":
                        {
                            bool memberReady = (bool)memberData["isReady"];

                            UpdateMemberReady(joiningMemberId, memberReady);
                        }
                        break;
                    case "MEMBER_LEFT":
                        {
                            RemoveMember(joiningMemberId);
                        }
                        break;
                    case "DISBANDED":
                        {
                            var reason = jsonData["reason"] as Dictionary<string, object>;
                            if ((int)reason["code"] != ReasonCodes.RTT_ROOM_READY)
                            {
                                // Disbanded for any other reason than ROOM_READY, means we failed to launch the game.
                                UpdateState(State.Main);
                            }
                        }
                        break;
                    case "STARTING":
                        {
                            // Save our picked color index
                            _loadingView.SetActive(true);
                            _loadingStatusText.text = "Launching...";
                        }
                        break;
                    case "ROOM_READY":
                        {
                            //get pass code
                            if (jsonData.ContainsKey("passcode"))
                            {
                                string passCode = jsonData["passcode"] as string;
                                BCManager.Instance.RelayPasscode = passCode;
                            }
                            Dictionary<string, object> roomData = jsonData["connectData"] as Dictionary<string, object>;
                            BCManager.Instance.RoomAddress = roomData["address"] as string;
                            Dictionary<string, object> portsData = roomData["ports"] as Dictionary<string, object>;
                            BCManager.Instance.RoomPort = (ushort)int.Parse(portsData["udp"].ToString());

                            //load game scene
                            UnityEngine.SceneManagement.SceneManager.sceneLoaded += BCManager.Instance.OnGameSceneLoaded;

                            UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
                        }
                        break;
                    case "JOIN_FAIL":
                        {
                            _findLobbyButton.gameObject.SetActive(true);
                            _createLobbyButton.gameObject.SetActive(true);
                            _findCreateLobbyButton.gameObject.SetActive(true);
                            _cancelMatchmakingButton.gameObject.SetActive(false);

                            _displayNamePanel.SetActive(true);
                            _loadingView.SetActive(false);
                        }
                        break;

                    case "SIGNAL":
                        {
                            if (jsonData.ContainsKey("signalData") && jsonData["signalData"] is Dictionary<string, object> signalData)
                            {
                                if (signalData.TryGetValue("color", out object colorObj) && colorObj is string hexColor)
                                {
                                    if (ColorUtility.TryParseHtmlString("#" + hexColor, out Color receivedColor))
                                    {
                                        LobbyMemberItem tempItem = GetLobbyMemberItem(fromMemberId);
                                        if (tempItem != null)
                                        {
                                            // Apply the receivedColor to the correct object
                                            tempItem.ApplyColorUpdate(receivedColor);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BrainCloud] OnLobbyEvent - Exception occurred: {ex.Message}");
        }
    }

    private const string P_PRE_FIX = "SMRJ_";
    private const string P1_STR = P_PRE_FIX + "player1";
    private const string P2_STR = P_PRE_FIX + "player2";
    private const string P3_STR = P_PRE_FIX + "player3";
    private const string P4_STR = P_PRE_FIX + "player4";
    
    private void LoginHelper(string str, string pwd)
    {
        UpdateState(State.Loading);
        BCManager.Instance.AuthenticateUser(str, pwd, (success) =>
        {
            if (success)
            {
                UpdateState(State.Main);
                OnAuthSuccess();
            }
            else
            {
                UpdateState(State.Login);
                _authErrorText.text = "Username or Password is incorrect, please try again.";
                Debug.LogError("There was an error authenticating");
            }
        });
    }
    public void LoginP1()
    {
        LoginHelper(P1_STR, P1_STR);
    }

    public void LoginP2()
    {
        LoginHelper(P2_STR, P2_STR);
    }

    public void LoginP3()
    {
        LoginHelper(P3_STR, P3_STR);
    }

    public void LoginP4()
    {
        LoginHelper(P4_STR, P4_STR);
    }

    public void Logout()
    {
        var _wrapper = BCManager.Instance.bc;
        _wrapper.Logout(true);
        _wrapper.Client.ResetCommunication();
        _wrapper.RTTService.DisableRTT();
        _wrapper.RTTService.DeregisterAllRTTCallbacks();

        UpdateState(State.Login);
    }

    public void OnDisplayNameUpdated()
    {
        string displayName = _displayNameInput.text;

        SuccessCallback successCallback = (response, cbObject) =>
        {
            Debug.Log(string.Format("Success | {0}", response));
        };
        FailureCallback failureCallback = (status, code, error, cbObject) =>
        {
            Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error));
        };

        string profileId = BCManager.Instance.bc.Client.ProfileId;
        Color playerColor = PlayerListItemManager.Instance.GetPlayerDataByProfileId(profileId).Color;
        Debug.Log($"Updating display name to: {displayName} with color: {playerColor}, ProfileId: {profileId}");
    
        _displayNameInput.gameObject.GetComponent<Image>().color = playerColor;
        _playerColourImage.color = playerColor;

        BCManager.Instance.PlayerName = displayName;
        _displayNameText.text = displayName;
        _accountNameText.text = BCManager.Instance.ExternalId;

        // Update the display name in BrainCloud
        BCManager.Instance.bc.PlayerStateService.UpdateName(displayName, successCallback, failureCallback);
    }

    public void OnLoginClicked()
    {
        if (string.IsNullOrEmpty(_usernameInput.text) || string.IsNullOrEmpty(_passwordInput.text))
        {
            // Show error message
            _authErrorText.text = "Username or password is empty, please enter correct value.";

            // display the username and pwd
            Debug.LogError("Username: " + _usernameInput.text + ", Password: " + _passwordInput.text, this);
            return;
        }

        UpdateState(State.Loading);
        BCManager.Instance.AuthenticateUser(_usernameInput.text, _passwordInput.text, (success) =>
        {
            if (success)
            {
                UpdateState(State.Main);
                OnAuthSuccess();
            }
            else
            {
                UpdateState(State.Login);
                _authErrorText.text = "There was an error authenticating. Try again.";
                Debug.LogError("There was an error authenticating");
            }

            // clear the input fields
            _usernameInput.text = string.Empty;
            _passwordInput.text = string.Empty;
        });
    }

    private void OnRTTEnabled()
    {
        // clear the game stuff
        PlayerListItemManager.Instance.ClearAll();

        BCManager bcm = BCManager.Instance;
        bcm.bc.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);

        UpdateState(State.Main);

        if (!string.IsNullOrEmpty(bcm.CurrentLobbyId))
        {
            UpdateState(State.Loading);

            Invoke("CreateOldMembers", 0.15f);
        }
    }

    private void CreateOldMembers()
    {

        BCManager bcm = BCManager.Instance;
        UpdateState(State.Lobby);

        //iterate and dispaly each of them
        foreach (var memberData in bcm.LobbyMembersData)
        {
            AddMemberRow(memberData);
        }
    }

    private void OnAuthSuccess()
    {
        BCManager bcm = BCManager.Instance;

        if (!bcm.bc.RTTService.IsRTTEnabled())
        {
            bcm.EnableRTT(true, () =>
            {
                OnRTTEnabled();
            });
        }
        else
        {
            OnRTTEnabled();
        }

        _displayNameInput.text = bcm.PlayerName;
        _displayNameText.text = bcm.PlayerName;
        _authErrorText.text = string.Empty;

        SuccessCallback success = (in_response, cbObject) =>
        {
            var response = JsonReader.Deserialize<Dictionary<string, object>>(in_response);

            if (response.TryGetValue("data", out var dataObj) && dataObj is Dictionary<string, object> dataDict)
            {
                if (dataDict.TryGetValue("identities", out var identitiesObj) && identitiesObj is Dictionary<string, object> identitiesDict)
                {
                    if (identitiesDict.TryGetValue("Universal", out var universalId))
                    {
                        bcm.ExternalId = universalId?.ToString();
                        _accountNameText.text = bcm.ExternalId;
                    }
                }
            }
        };

        bcm.bc.Client.IdentityService.GetIdentities(success);

    }

    public void UpdateState(State state)
    {
        Debug.Log("update State " + state);

        if (_curState != state)
        {
            _curState = state;
            OnStateChanged();
        }
    }

    private void OnStateChanged()
    {
        Debug.Log("OnStateChanged " + _curState);
        switch (_curState)
        {
            case State.Loading:
                {
                    _mainView.SetActive(false);
                    _lobbyView.SetActive(false);
                    _loginView.SetActive(false);
                    _loadingView.SetActive(true);
                    _loadingStatusText.text = "Loading ...";
                }
                break;

            case State.Login:
                {
                    _mainView.SetActive(false);
                    _lobbyView.SetActive(false);
                    _loginView.SetActive(true);
                    _loadingView.SetActive(false);

                    _topBarView.SetActive(false);

                    _mainStatus.transform.parent.gameObject.SetActive(false);
                }
                break;

            case State.Main:
                {
                    _mainView.SetActive(true);
                    _lobbyView.SetActive(false);
                    _loginView.SetActive(false);
                    _loadingView.SetActive(false);

                    _mainStatus.text = "Select an option to continue";

                    _topBarView.SetActive(true);
                    _mainStatus.transform.parent.gameObject.SetActive(true);
                }
                break;

            case State.Lobby:
                {
                    _mainView.SetActive(false);
                    _lobbyView.SetActive(true);
                    _loginView.SetActive(false);
                    _loadingView.SetActive(false);

                    _findLobbyButton.gameObject.SetActive(true);
                    _createLobbyButton.gameObject.SetActive(true);
                    _findCreateLobbyButton.gameObject.SetActive(true);
                    _cancelMatchmakingButton.gameObject.SetActive(false);

                    _displayNamePanel.SetActive(true);

                    _lobbyMembersContainer.DestroyChildren();

                    // add from lobby member data
                }
                break;

            case State.InGame:
                {
                    //load game scene
                    _loadingView.SetActive(true);
                    _loadingStatusText.text = "Launching...";
                }
                break;
        }

    }
}
