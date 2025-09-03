using BrainCloud;
using BrainCloud.JsonFx.Json;
using FishNet.Managing.Timing;
using FishNet.Transporting;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BCFishNet;

public class BCManager : MonoBehaviour
{
    private static BCManager _instance;
    public static BCManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Look for an existing instance
                _instance = FindObjectOfType<BCManager>();

                if (_instance == null)
                {
                    // Create new GameObject to attach the manager to
                    GameObject managerObject = new GameObject("BCManager");
                    _instance = managerObject.AddComponent<BCManager>();
                    DontDestroyOnLoad(managerObject);
                }
            }

            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }

    public static string LOBBY_ID = "CursorParty_V3";
    private BrainCloudWrapper _bc;
    public BrainCloudWrapper bc => _bc;


    public string RelayPasscode;
    public string CurrentLobbyId = "";

    public string RoomAddress;
    public ushort RoomPort;

    public string LobbyOwnerId;

    public List<LobbyMemberData> LobbyMembersData => new List<LobbyMemberData>(memberData);
    private List<LobbyMemberData> memberData = new List<LobbyMemberData>();

    public void AddMember(LobbyMemberData member)
    {
        int index = memberData.FindIndex(m => m.ProfileId == member.ProfileId);
        if (index >= 0)
        {
            memberData[index] = member;
        }
        else
        {
            memberData.Add(member);
        }
    }

    public void RemoveMember(LobbyMemberData member)
    {
        int removedCount = memberData.RemoveAll(m => m != null && m.ProfileId == member.ProfileId);
    }

    public void RemoveMember(string profileId)
    {
        int removedCount = memberData.RemoveAll(m => m != null && m.ProfileId == profileId);
    }

    public void ClearMembers()
    {
        memberData.Clear();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        Instance = this;

        _bc = gameObject.AddComponent<BrainCloudWrapper>();
        _bc.Init();

        Debug.Log("BrainCloud client version: " + _bc.Client.BrainCloudClientVersion);
    }

    private string _playerName;
    public string PlayerName
    {
        get => _playerName;
        set => _playerName = value;
    }
    private string _externalId;
    public string ExternalId
    {
        get => _externalId;
        set => _externalId = value;
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

        BCManager.Instance.AddMember(lobbyMemberData);
    }

    public void OnLobbyEvent(string json)
    {
        try
        {
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
                // try and always read it
                if (lobbyData.ContainsKey("members"))
                {
                    Dictionary<string, object>[] membersData = lobbyData["members"] as Dictionary<string, object>[];
                    FillMemberRows(membersData);
                }
                else if (jsonData.ContainsKey("members"))
                {
                    Dictionary<string, object>[] membersData2 = jsonData["members"] as Dictionary<string, object>[];
                    FillMemberRows(membersData2);
                }

                if (jsonData.ContainsKey("lobbyId"))
                {
                    BCManager.Instance.CurrentLobbyId = jsonData["lobbyId"] as string;
                }

                var operation = response["operation"] as string;

                switch (operation)
                {
                    case "MEMBER_JOIN":
                        {
                            var lobbyMemberData = new LobbyMemberData(
                                memberData["name"] as string,
                                (bool)memberData["isReady"],
                                memberData.ContainsKey("profileId") ? memberData["profileId"] as string : null,
                                memberData.ContainsKey("netId") ? System.Convert.ToInt16(memberData["netId"]) : (short)0,
                                memberData.ContainsKey("rating") ? System.Convert.ToInt32(memberData["rating"]) : 0,
                                memberData.ContainsKey("cxId") ? memberData["cxId"] as string : null,
                                memberData.ContainsKey("extraData") ? memberData["extraData"] as Dictionary<string, object> : null
                            );

                            AddMember(lobbyMemberData);
                        }
                        break;
                    case "MEMBER_LEFT":
                        {
                            Debug.Log("OnLobbyEvent : " + json);
                            RemoveMember(joiningMemberId);
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

    public void LeaveCurrentLobby()
    {
        bc.LobbyService.LeaveLobby(CurrentLobbyId);
        CurrentLobbyId = "";

        PlayerListItemManager.Instance.ClearAll();
    }

    private void OnAuthSuccess(string responseData, object cbObject, Action<bool> callback)
    {
        Debug.Log("Authentication successful");

        try
        {
            var response = JsonReader.Deserialize<Dictionary<string, object>>(responseData);

            if (response.TryGetValue("data", out object dataObj) &&
                dataObj is Dictionary<string, object> data &&
                data.TryGetValue("playerName", out object playerNameObj))
            {
                _playerName = playerNameObj?.ToString();
                Debug.Log($"Stored playerName: {_playerName}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse playerName from response: {ex.Message}");
        }

        callback?.Invoke(true);
    }

    private void OnAuthFailure(int error, int code, string statusMessage, object cbObject, Action<bool> callback)
    {
        Debug.LogWarning($"Authentication failed: {statusMessage} (code: {code}, error: {error})");
        callback?.Invoke(false);
    }

    public void AuthenticateAnonymous(Action<bool> callback)
    {
        SuccessCallback success = (responseData, cbObject) =>
        {
            OnAuthSuccess(responseData, cbObject, callback);
        };

        FailureCallback failure = (error, code, statusMessage, cbObject) =>
            OnAuthFailure(error, code, statusMessage, cbObject, callback);

        _bc.AuthenticateAnonymous(success, failure);
    }

    public void AuthenticateUser(string username, string password, Action<bool> callback)
    {
        SuccessCallback success = (responseData, cbObject) =>
            OnAuthSuccess(responseData, cbObject, callback);

        FailureCallback failure = (statusMessage, code, error, cbObject) =>
            OnAuthFailure(statusMessage, code, error, cbObject, callback);

        _bc.AuthenticateUniversal(username, password, true, success, failure);
    }

    public void EnableRTT(bool enabled, Action OnSuccess)
    {
        if (enabled)
        {
            SuccessCallback success = (responseData, cbObject) =>
            {
                OnSuccess();
            };
            _bc.RTTService.EnableRTT(success, OnRTTFailed);
        }
        else
        {
            _bc.RTTService.DisableRTT();
        }
    }

    private void OnRTTFailed(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Debug.LogWarning($"RTT disconnected: {statusMessage} (Status Code: {statusCode}, Reason Code: {reasonCode})");

        var activeScene = SceneManager.GetActiveScene().name;

        PlayerListItemManager.Instance.ClearAll();
        
        // TODO: Make Network Handling more robust
        // while in the main menu, if we are not connected show a display message and prompt to reconnect
        if (activeScene == "Main")
        {
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateState(UIManager.State.Login);
            }
        }
        else
        {
            SceneManager.LoadScene("Main");
        }
    }

    private List<string> listToJoinWith = new List<string>();
    public void OnRelayFailure(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Debug.LogWarning($"RelayService Connection Failure - statusMessage: {statusCode} code: {reasonCode} error: {statusMessage}");

        PlayerListItemManager.Instance.ClearAll();

        SceneManager.LoadScene("Main");
    }

    public void FindLobby(Action<string> OnEntryId)
    {
        var lobbyParams = CreateLobbyParams(OnEntryId);

        _bc.LobbyService.FindLobby(
            BCManager.LOBBY_ID,
            0,
            1,
            lobbyParams.algo,
            lobbyParams.filters,
            false,
            lobbyParams.extra,
            "all",
            null,
            lobbyParams.success
        );
    }

    public void QuickFindLobby(Action<string> OnEntryId)
    {
        var lobbyParams = CreateLobbyParams(OnEntryId);

        _bc.LobbyService.FindOrCreateLobby(
            BCManager.LOBBY_ID,
            0,
            1,
            lobbyParams.algo,
            lobbyParams.filters,
            false,
            lobbyParams.extra,
            "all",
            lobbyParams.settings,
            null,
            lobbyParams.success
        );
    }

    public void JoinLobby(string lobbyId, Action<string> OnEntryId)
    {
        var lobbyParams = CreateLobbyParams(OnEntryId);
    
        _bc.LobbyService.JoinLobby(lobbyId,
            true,
            lobbyParams.extra,
            "all",
            null,
            lobbyParams.success
        );
    }
    
    public void QuickFindLobbyWithPreviousMembers(Action<string> OnEntryId)
    {
        var lobbyParams = CreateLobbyParams(OnEntryId);

        _bc.LobbyService.FindOrCreateLobby(
            BCManager.LOBBY_ID,
            0,
            1,
            lobbyParams.algo,
            lobbyParams.filters,
            false,
            lobbyParams.extra,
            "all",
            lobbyParams.settings,
            listToJoinWith.ToArray(), // list of strings
            lobbyParams.success
        );
    }


    private LobbyParams CreateLobbyParams(Action<string> OnEntryId)
    {
        var algo = new Dictionary<string, object>
        {
            ["strategy"] = "ranged-absolute",
            ["alignment"] = "center",
            ["ranges"] = new List<int> { 1000 }
        };

        var filters = new Dictionary<string, object>();
        filters["appVersion"] = Application.version;

        var extra = new Dictionary<string, object>();

        var settings = new Dictionary<string, object>();
        settings["appVersion"] = Application.version;

        SuccessCallback success = (in_response, cbObject) =>
        {
            var response = JsonReader.Deserialize<Dictionary<string, object>>(in_response);
            var data = response["data"] as Dictionary<string, object>;
            if (data.ContainsKey("entryId"))
            {
                var entryId = data["entryId"] as string;
                OnEntryId?.Invoke(entryId);
            }
        };

        return new LobbyParams
        {
            algo = algo,
            filters = filters,
            extra = extra,
            success = success,
            settings = settings
        };
    }

    public void CreateLobby(Action<string> OnSuccess)
    {
        var lobbyParams = CreateLobbyParams(OnSuccess);

        _bc.LobbyService.CreateLobby(BCManager.LOBBY_ID, 0, false, lobbyParams.extra, "all", lobbyParams.settings, null, lobbyParams.success);
    }

    public void OnGameSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode loadMode)
    {
        StartCoroutine(DelayedConnect());

        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnGameSceneLoaded;
    }

    private IEnumerator DelayedConnect()
    {
        yield return new WaitForSeconds(0.05f);

        Debug.Log("[BCFishNet] DelayedConnect called");

        BCFishNetTransport BCFishNet = FindObjectOfType<BCFishNetTransport>();
        BCFishNet.Config(_bc, RoomAddress, RelayPasscode, CurrentLobbyId, RoomPort, OnRelayFailure, OnLobbyEvent);

        if (BCFishNet != null)
        {
            Debug.Log("Found BCFishNetTransport");
            bool isServer = LobbyOwnerId == bc.Client.ProfileId;

            if (BCFishNet.GetConnectionState(isServer) == LocalConnectionState.Stopped)
            {
                BCFishNet.SetServerBindAddress(RoomAddress, IPAddressType.IPv4);
            }

            BCFishNet.StartConnection(isServer);//Start Client
        }
    }
}

public class LobbyParams
{
    public Dictionary<string, object> algo;
    public Dictionary<string, object> filters;
    public Dictionary<string, object> extra;
    public SuccessCallback success;
    public Dictionary<string, object> settings;
}
