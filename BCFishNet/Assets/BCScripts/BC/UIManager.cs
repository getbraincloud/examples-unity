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
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _loginView, _mainView, _lobbyView;

    #region LoginVars
    [Header("Login Vars")]
    [SerializeField]
    private Button _loginButton;
    [SerializeField]
    private TMP_InputField _usernameInput, _passwordInput, _displayNameInput;
    #endregion

    #region MainMenuVars
    [Header("MainMenu Vars")]
    [SerializeField]
    private TMP_Text _mainStatus, _displayNameText;
    [SerializeField]
    private Button _createLobbyButton, _findLobbyButton, _cancelMatchmakingButton, _findCreateLobbyButton;

    private string _currentLobbyId;
    private string _currentEntryId;
    #endregion

    #region LobbyVars
    [Header("Lobby Vars")]
    [SerializeField]
    private TMP_Text _lobbyIdText;
    [SerializeField]
    private TMP_Text _lobbyStatusText, _displayNameTextLobby;
    [SerializeField]
    private Button _readyUpButton, _leaveLobbyButon;
    [SerializeField]
    private LobbyMemberItem _lobbyMemberRowPrefab;
    [SerializeField]
    private Transform _lobbyMembersContainer;

    private Dictionary<string, LobbyMemberItem> _members = new Dictionary<string, LobbyMemberItem>();
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
        //main menu
        _mainStatus.text = string.Empty;
        _createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
        _findLobbyButton.onClick.AddListener(OnFindLobbyClicked);
        _cancelMatchmakingButton.onClick.AddListener(OnCancelMatchmakingClicked);
        _findCreateLobbyButton.onClick.AddListener(OnQuickFind);

        //lobby
        _readyUpButton.onClick.AddListener(OnReadyUpClicked);
        _leaveLobbyButon.onClick.AddListener(OnLeaveLobbyClicked);

        
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
        //login
        //check if authenticated
        var bc = BCManager.Instance.bc;
        var storedId = bc.GetStoredProfileId();
        var storedAnonymousId = bc.GetStoredAnonymousId();

        if (storedId != null && storedAnonymousId != null && 
        storedId != string.Empty && storedAnonymousId != string.Empty)
        {
            //we have a stored profile id, so we can authenticate
            bc.AuthenticateUser(storedId, storedAnonymousId, (success) =>
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
        else    
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
#endif

        }
    }

    void OnEnable()
    {
        OnStateChanged();
    }

    void OnDestroy()
    {
        BCManager.Instance.bc.RTTService.DeregisterRTTLobbyCallback();

        //main menu
        _createLobbyButton.onClick.RemoveListener(OnCreateLobbyClicked);
        _findLobbyButton.onClick.RemoveListener(OnFindLobbyClicked);
        _cancelMatchmakingButton.onClick.RemoveListener(OnCancelMatchmakingClicked);
        _findCreateLobbyButton.onClick.RemoveListener(OnQuickFind);

        //lobby
        _readyUpButton.onClick.RemoveListener(OnReadyUpClicked);
        _leaveLobbyButon.onClick.RemoveListener(OnLeaveLobbyClicked);
    }

    private void OnLeaveLobbyClicked()
    {
        SuccessCallback success = (response, cbObject) =>
        {
            UpdateState(State.Main);
        };

        BCManager.Instance.bc.LobbyService.LeaveLobby(_currentLobbyId, success);
    }

    private void OnReadyUpClicked()
    {
        Dictionary<string, object> extra = new Dictionary<string, object>();

        BCManager.Instance.bc.LobbyService.UpdateReady(_currentLobbyId, true, extra);
    }

    private void OnCancelMatchmakingClicked()
    {
        BCManager.Instance.bc.LobbyService.CancelFindRequest(BCManager.LOBBY_ID, _currentEntryId);
        _findLobbyButton.gameObject.SetActive(true);
        _createLobbyButton.gameObject.SetActive(true);
        _findCreateLobbyButton.gameObject.SetActive(true);

        _mainStatus.text = string.Empty;
    }

    private void OnQuickFind()
    {
        _mainStatus.text = "Quick Finding lobby...";

        BCManager.Instance.QuickFindLobby((entryId) =>
        {
            _currentEntryId = entryId;
            //temp disable lobby buttons
            _findLobbyButton.gameObject.SetActive(false);
            _createLobbyButton.gameObject.SetActive(false);
            _findCreateLobbyButton.gameObject.SetActive(false);
            _cancelMatchmakingButton.gameObject.SetActive(true);

        });
    }
    private void OnFindLobbyClicked()
    {
        _mainStatus.text = "Finding lobby...";

        BCManager.Instance.FindLobby((entryId) =>
        {
            _currentEntryId = entryId;
            //temp disable lobby buttons
            _findLobbyButton.gameObject.SetActive(false);
            _createLobbyButton.gameObject.SetActive(false);
            _findCreateLobbyButton.gameObject.SetActive(false);
            _cancelMatchmakingButton.gameObject.SetActive(true);
        });
    }

    private void OnCreateLobbyClicked()
    {
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

    private void AddMemberRow(Dictionary<string, object> memberData)
    {
        string memberId = memberData["profileId"] as string;
        if (_members.ContainsKey(memberId))
        {
            return;
        }

        LobbyMemberItem lobbyMember = Instantiate(_lobbyMemberRowPrefab, _lobbyMembersContainer);
        lobbyMember.Config(
            memberData["name"] as string,
            (bool)memberData["isReady"],
            memberData.ContainsKey("profileId") ? memberData["profileId"] as string : null,
            memberData.ContainsKey("netId") ? System.Convert.ToInt16(memberData["netId"]) : (short)0,
            memberData.ContainsKey("rating") ? System.Convert.ToInt32(memberData["rating"]) : 0,
            memberData.ContainsKey("cxId") ? memberData["cxId"] as string : null,
            memberData.ContainsKey("extraData") ? memberData["extraData"] as Dictionary<string, object> : null
        );

        _members.Add(memberId, lobbyMember);
    }

    private void UpdateMemberReady(string id, bool ready)
    {
        if (_members.ContainsKey(id))
        {
            _members[id].UpdateReady(ready);
        }
    }

    private void RemoveMember(string id)
    {
        if (_members.ContainsKey(id))
        {
            if (_members[id] != null) Destroy(_members[id].gameObject);
            _members.Remove(id);
        }
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

                if (_mainStatus != null) _mainStatus.text = "Lobby Operation: " + operation;
                if (_lobbyStatusText != null) _lobbyStatusText.text = "Lobby Operation: " + operation;
                switch (operation)
                {
                    case "MEMBER_JOIN":
                        {
                            if (!string.IsNullOrEmpty(joiningMemberId) && joiningMemberId == BCManager.Instance.bc.Client.ProfileId)
                            {
                                //we just joined this lobby
                                UpdateState(State.Lobby);
                            }

                            Dictionary<string, object>[] membersData = lobbyData["members"] as Dictionary<string, object>[];
                            FillMemberRows(membersData);

                            _currentLobbyId = jsonData["lobbyId"] as string;
                            BCManager.Instance.CurrentLobbyId = _currentLobbyId;

                            _lobbyIdText.text = _currentLobbyId;

                            // let's echo all the lobby member item colors to the lobby
                            foreach (var member in _members)
                            {
                                if (member.Value != null)
                                {
                                    // Send a signal to all other members in the lobby with the new color
                                    member.Value.SendCurrentColourSignal();
                                }
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
                                        // Apply the receivedColor to the correct object
                                        _members[fromMemberId].ApplyColorUpdate(receivedColor);

                                        Debug.Log($"Received color: {receivedColor}, for member: {fromMemberId}  ");
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
        UpdateState(State.Main);
        BCManager.Instance.AuthenticateUser(str, pwd, (success) =>
        {
            if (success)
            {
                OnAuthSuccess();
            }
            else
            {
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
        _wrapper.LogoutOnApplicationQuit(false);
        _wrapper.Client.ResetCommunication();
        _wrapper.RTTService.DisableRTT();
        _wrapper.RTTService.DeregisterAllRTTCallbacks();

        UpdateState(State.Login);
    }

    public void OnLoginClicked()
    {
        if (string.IsNullOrEmpty(_usernameInput.text) || string.IsNullOrEmpty(_passwordInput.text))
        {
            // Show error message
            _mainStatus.text = "Username or password is empty, please enter correct value";
            // display the username and pwd
            
            Debug.LogError("Username: " + _usernameInput.text + ", Password: " + _passwordInput.text, this);
            return;
        }

        BCManager.Instance.AuthenticateUser(_usernameInput.text, _passwordInput.text, (success) =>
        {
            if (success)
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

                BCManager.Instance.PlayerName = displayName;
                // Update the display name in BrainCloud
                BCManager.Instance.bc.PlayerStateService.UpdateUserName(displayName, successCallback, failureCallback);

                OnAuthSuccess();
            }
            else
            {
                Debug.LogError("There was an error authenticating");
            }
        });
    }

    private void OnAuthSuccess()
    {
        if (!BCManager.Instance.bc.RTTService.IsRTTEnabled())
        {
            BCManager.Instance.EnableRTT(true, () =>
            {
                BCManager.Instance.bc.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
                UpdateState(State.Main);
            });

            _displayNameText.text = BCManager.Instance.PlayerName;
            _displayNameTextLobby.text = BCManager.Instance.PlayerName;
        }
        else
        {
            BCManager.Instance.bc.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
            UpdateState(State.Main);
        }
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
                }
                break;

            case State.Login:
                {
                    _mainView.SetActive(false);
                    _lobbyView.SetActive(false);
                    _loginView.SetActive(true);
                }
                break;

            case State.Main:
                {
                    _mainView.SetActive(true);
                    _lobbyView.SetActive(false);
                    _loginView.SetActive(false);

                    _mainStatus.text = string.Empty;
                }
                break;

            case State.Lobby:
                {
                    _mainView.SetActive(false);
                    _lobbyView.SetActive(true);
                    _loginView.SetActive(false);

                    _findLobbyButton.gameObject.SetActive(true);
                    _createLobbyButton.gameObject.SetActive(true);
                    _findCreateLobbyButton.gameObject.SetActive(true);
                    _cancelMatchmakingButton.gameObject.SetActive(false);

                    _lobbyMembersContainer.DestroyChildren();
                    _members.Clear();
                }
                break;

            case State.InGame:
                {
                    //load game scene
                }
                break;
        }

    }
}
