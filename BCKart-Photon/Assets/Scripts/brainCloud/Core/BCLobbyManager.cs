using UnityEngine;
using UnityEngine.UI;

using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;

// BC Manager will hold a reference to this, which will do the accessing 
// and listening to lobby events
public class BCLobbyManager
{
    public static string ACTIVE_LOBBY_TYPE = "eight";

    public event Action<string> OnLobbyEventReceived;

    public string LobbyId = "";
    public string SearchingEntryId = "";

    private bool quickFindLobbyAfterEnable = false;
    public void QuickFindLobby(GameLauncher launcher)
    {
        _launcher = launcher;
        if (!ConfirmRTTEnabled())
        {
            quickFindLobbyAfterEnable = true;
        }
        else
        {
            FindOrCreateLobby();
        }        
    }

    private bool hostLobbyAfterEnable = false;
    private GameLauncher _launcher;
    public void HostLobby(GameLauncher launcher)
    {
        _launcher = launcher;
        if (!ConfirmRTTEnabled())
        {
            hostLobbyAfterEnable = true;
        }
        else
        {
            CreateLobby();
        }
    }

    private bool findLobbyAfterEnable = false;
    public void FindLobby(GameLauncher launcher)
    {
        _launcher = launcher;
        if (!ConfirmRTTEnabled())
        {
            findLobbyAfterEnable = true;
        }
        else
        {
            FindLobby();
        }
    }

    public void CancelFind()
    {
        if (SearchingEntryId != "")
        {
            BCManager.Wrapper.LobbyService.CancelFindRequest(ACTIVE_LOBBY_TYPE, SearchingEntryId);
        }
    }

    public void LeaveLobby()
    {
        if (LobbyId != "")
        {
            BCManager.Wrapper.LobbyService.LeaveLobby(LobbyId, OnLeaveLobbySuccess, OnLeaveLobbyError);
        }
    }

    private void OnLeaveLobbySuccess(string jsonResponse, object cbObject)
    {
        Debug.Log("BCLobbyManager OnLeaveLobbySuccess: " + jsonResponse);
        LobbyId = "";
    }

    private void OnLeaveLobbyError(int status, int reasonCode, string jsonError, object cbObject)
    {
        Debug.LogError($"BCLobbyManager OnEnableRTTError: {status}, {reasonCode}, {jsonError}");
        LobbyId = "";
    }

    private void OnEnableRTTSuccess(string jsonResponse, object cbObject)
    {
        Debug.Log("BCLobbyManager OnEnableRTTSuccess: " + jsonResponse);
        if (quickFindLobbyAfterEnable)
        {
            FindOrCreateLobby();
        }

        if (hostLobbyAfterEnable)
        {
            CreateLobby();
        }

        if (findLobbyAfterEnable)
        {
            FindLobby();
        }

        quickFindLobbyAfterEnable = false;
        hostLobbyAfterEnable = false;
        findLobbyAfterEnable = false;

        BCManager.Wrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
    }
    public void OnLobbyEvent(string jsonMessage)
    {
        try
        {
            Debug.Log("BCLobbyManager OnLobbyEvent: " + jsonMessage);

            // Parse top-level dictionary
            var message = JsonReader.Deserialize<Dictionary<string, object>>(jsonMessage);

            // Extract the service and operation type
            string service = message.ContainsKey("service") ? message["service"] as string : null;
            string operation = message.ContainsKey("operation") ? message["operation"] as string : null;

            if (service != "lobby" || string.IsNullOrEmpty(operation))
                return;

            // Extract data payload
            if (!message.TryGetValue("data", out object dataObj))
                return;

            var data = dataObj as Dictionary<string, object>;

            // parse the ownerId
            if (data.TryGetValue("lobby", out object lobbyObj))
            {
                LobbyId = data["lobbyId"] as string;
                // confirm if we are the server or just the client
                var lobby = lobbyObj as Dictionary<string, object>;
                if (lobby != null && lobby.TryGetValue("ownerCxId", out object ownerCxIdObj))
                {
                    string ownerCxId = ownerCxIdObj as string;

                    if (!string.IsNullOrEmpty(ownerCxId))
                    {
                        string[] parts = ownerCxId.Split(':');
                        if (parts.Length >= 2)
                        {
                            string ownerProfileId = parts[1]; // second segment
                            bool isHost = ownerProfileId == ClientInfo.LoginData.profileId;

                            Debug.Log($"Lobby owner profileId = {ownerProfileId}, Our profileId = {ClientInfo.LoginData.profileId} → Host = {isHost}");

                            if (_launcher != null)
                            {
                                if (isHost)
                                    _launcher.SetCreateLobby();
                                else
                                    _launcher.SetJoinLobby();
                            }
                        }
                    }
                }
            }

            if (operation == "MEMBER_JOIN" && data != null)
            {
                if (data.TryGetValue("member", out object memberObj))
                {
                    var member = memberObj as Dictionary<string, object>;
                    if (member != null && member.TryGetValue("profileId", out object profileIdObj))
                    {
                        string joinedProfileId = profileIdObj as string;

                        // Compare with our own player’s ID
                        if (!string.IsNullOrEmpty(joinedProfileId) && joinedProfileId == ClientInfo.LoginData.profileId)
                        {
                            _launcher.JoinOrCreateLobby();
                        }
                        else
                        {
                            Debug.Log($"Other player joined lobby: {joinedProfileId}");
                        }
                    }
                }
            }

            // Fire the event for listeners
            OnLobbyEventReceived?.Invoke(jsonMessage);
        }
        
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to parse lobby event: " + ex.Message);
        }
        
    }

    private void OnEnableRTTError(int status, int reasonCode, string jsonError, object cbObject)
    {
        Debug.LogError($"BCLobbyManager OnEnableRTTError: {status}, {reasonCode}, {jsonError}");
    }

    private void EnableRTT()
    {
        BCManager.Wrapper.RTTService.EnableRTT(OnEnableRTTSuccess, OnEnableRTTError);
    }

    private bool ConfirmRTTEnabled()
    {
        bool isEnabled = BCManager.Wrapper.RTTService.IsRTTEnabled();
        if (!isEnabled)
        {
            EnableRTT();
        }
        return isEnabled;
    }

    private void FindOrCreateLobby()
    {
        var lobbyParams = CreateLobbyParams();
        BCManager.Wrapper.LobbyService.FindOrCreateLobby(
            ACTIVE_LOBBY_TYPE,
            0,
            1,
            lobbyParams.algo,
            lobbyParams.filters,
            false,
            lobbyParams.extra,
            "all",
            lobbyParams.settings,
            null,
            OnLobbySuccess,
            OnLobbyError
        );
    }

    public void CreateLobby()
    {
        var lobbyParams = CreateLobbyParams();

        BCManager.Wrapper.LobbyService.CreateLobby(
            ACTIVE_LOBBY_TYPE, 0, false,
            lobbyParams.extra,
            "all",
            lobbyParams.settings,
            null,
            OnLobbySuccess,
            OnLobbyError);
    }
    
    private void FindLobby()
    {
        var lobbyParams = CreateLobbyParams();
        BCManager.Wrapper.LobbyService.FindLobby(
            ACTIVE_LOBBY_TYPE,
            0,
            1,
            lobbyParams.algo,
            lobbyParams.filters,
            false,
            lobbyParams.extra,
            "all",
            null,
            OnLobbySuccess,
            OnLobbyError
        );
    }

    private LobbyParams CreateLobbyParams()
    {
        var algo = new Dictionary<string, object>
        {
            ["strategy"] = "ranged-absolute",
            ["alignment"] = "center",
            ["ranges"] = new List<int> { 1000 }
        };

        var filters = new Dictionary<string, object>();
        filters["appVersion"] = Application.version;

        Dictionary<string, object> extra = GetLobbyExtraData();

        var settings = new Dictionary<string, object>();
        settings["appVersion"] = Application.version;

        return new LobbyParams
        {
            algo = algo,
            filters = filters,
            extra = extra,
            settings = settings
        };
    }

    private void OnLobbySuccess(string in_response, object cbObject)
    {
        Debug.Log("BCLobbyManager OnLobbySuccess: " + in_response);
        var response = JsonReader.Deserialize<Dictionary<string, object>>(in_response);
        var data = response["data"] as Dictionary<string, object>;
        if (data.ContainsKey("lobbyId"))
        {
            LobbyId = data["lobbyId"] as string;
            _launcher.SetCreateLobby();
        }

        if (data.ContainsKey("entryId"))
        {
            SearchingEntryId = data["entryId"] as string;
        }
    }

    private void OnLobbyError(int status, int reasonCode, string jsonError, object cbObject)
    {
        Debug.LogError($"BCLobbyManager OnLobbyError: {status}, {reasonCode}, {jsonError}");
        LobbyId = "";
    }

    private Dictionary<string, object> GetLobbyExtraData()
    {
        var extra = new Dictionary<string, object>();
        int kartId = ClientInfo.KartId;
        extra["kartId"] = kartId;
        return extra;
    }
}

public class LobbyParams
{
    public Dictionary<string, object> algo;
    public Dictionary<string, object> filters;
    public Dictionary<string, object> extra;
    public Dictionary<string, object> settings;
}