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
    public string CurrentLobbyId;

    public string RoomAddress;
    public ushort RoomPort;

    public string LobbyOwnerId;

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
        Debug.LogError($"RTT disconnected: {statusMessage} (Status Code: {statusCode}, Reason Code: {reasonCode})");

        var activeScene = SceneManager.GetActiveScene().name;
        if (activeScene == "MainMenu")
        {
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateState(UIManager.State.Login);
            }
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
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


    private LobbyParams CreateLobbyParams(Action<string> OnEntryId)
    {
        var algo = new Dictionary<string, object>
        {
            ["strategy"] = "ranged-absolute",
            ["alignment"] = "center",
            ["ranges"] = new List<int> { 1000 }
        };

        var filters = new Dictionary<string, object>();
        var extra = new Dictionary<string, object>();
        var settings = new Dictionary<string, object>();

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
        BCFishNet.Config(_bc, RoomAddress, RelayPasscode, CurrentLobbyId, RoomPort);

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
