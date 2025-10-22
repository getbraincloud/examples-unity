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

    public event Action<LobbyMember> PlayerJoined;
	public event Action<LobbyMember> PlayerLeft;
	public event Action<LobbyMember> PlayerChanged;

    public string LobbyId = "";
    public string SearchingEntryId = "";

    public LobbyMember Local
{
    get
    {
        string localProfileId = BCManager.Wrapper.Client.ProfileId;
            if (LobbyMembers.TryGetValue(localProfileId, out LobbyMember member))
            {
                return member;
            }
        
        	Debug.Log("LobbyUI LOCAL IS NULL: ");
        return null; // or throw exception if you prefer
    }
}
    // Strong-typed lobby member list
    public Dictionary<string, LobbyMember> LobbyMembers = new Dictionary<string, LobbyMember>();
    private string _lobbyOwnerProfileId = null;

    
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

            var message = JsonReader.Deserialize<Dictionary<string, object>>(jsonMessage);

            if (!message.TryGetValue("service", out object serviceObj) || (string)serviceObj != "lobby")
                return;

            if (!message.TryGetValue("operation", out object operationObj))
                return;

            string operation = operationObj as string;

            if (!message.TryGetValue("data", out object dataObj))
                return;

            var data = dataObj as Dictionary<string, object>;

            // Update lobby ID and owner
            if (data.TryGetValue("lobby", out object lobbyObj))
            {
                var lobby = lobbyObj as Dictionary<string, object>;
                if (lobby != null)
                {
                    LobbyId = lobby.ContainsKey("lobbyId") ? lobby["lobbyId"] as string : LobbyId;

                    if (lobby.TryGetValue("ownerCxId", out object ownerCxIdObj))
                    {
                        string ownerCxId = ownerCxIdObj as string;
                        if (!string.IsNullOrEmpty(ownerCxId))
                        {
                            string[] parts = ownerCxId.Split(':');
                            if (parts.Length >= 2)
                            {
                                _lobbyOwnerProfileId = parts[1];
                            }
                        }
                    }
                }
            }

            // Handle member operations
            if (operation == "MEMBER_JOIN" || operation == "MEMBER_UPDATE" || operation == "MEMBER_LEFT")
            {
                if (data.TryGetValue("member", out object memberObj))
                {
                    var memberData = memberObj as Dictionary<string, object>;
                    if (memberData != null)
                    {
                        string profileId = memberData.ContainsKey("profileId") ? memberData["profileId"] as string : null;
                        if (!string.IsNullOrEmpty(profileId))
                        {
                            if (operation == "MEMBER_JOIN")
                            {
                                if (!LobbyMembers.ContainsKey(profileId))
                                {
                                    // Only create new member if not already present
                                    var member = new LobbyMember(memberData)
                                    {
                                        isHost = profileId == _lobbyOwnerProfileId
                                    };
                                    LobbyMembers[profileId] = member;
                                    Debug.Log($"MEMBER_JOIN: {member.profileId}, Host={member.isHost}");
                                    PlayerJoined?.Invoke(member);
                                }
                                else
                                {
                                    // Already exists, just update fields
                                    LobbyMembers[profileId].UpdateFromData(memberData, _lobbyOwnerProfileId);
                                    Debug.Log($"MEMBER_JOIN (already exists, updated): {profileId}");
                                    PlayerChanged?.Invoke(LobbyMembers[profileId]);
                                }
                            }
                            else if (operation == "MEMBER_UPDATE")
                            {
                                if (LobbyMembers.TryGetValue(profileId, out LobbyMember existingMember))
                                {
                                    existingMember.UpdateFromData(memberData, _lobbyOwnerProfileId);
                                    Debug.Log($"MEMBER_UPDATE: {existingMember.profileId}");
                                    PlayerChanged?.Invoke(existingMember);
                                }
                                else
                                {
                                    // If somehow update arrives before join, create the member
                                    var member = new LobbyMember(memberData)
                                    {
                                        isHost = profileId == _lobbyOwnerProfileId
                                    };
                                    LobbyMembers[profileId] = member;
                                    Debug.Log($"MEMBER_UPDATE (created new): {profileId}");
                                    PlayerJoined?.Invoke(member);
                                }
                            }
                            else if (operation == "MEMBER_LEFT")
                            {
                                if (LobbyMembers.ContainsKey(profileId))
                                {
                                    PlayerLeft?.Invoke(LobbyMembers[profileId]);
                                    LobbyMembers.Remove(profileId);
                                    Debug.Log($"MEMBER_LEFT: {profileId}");
                                }
                            }
                        }
                    }
                }
            }

            OnLobbyEventReceived?.Invoke(jsonMessage);
        }
        catch (Exception ex)
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
    
	public void JoinOrCreateLobby()
    {
        if (Local.isHost)
            _launcher.SetCreateLobby();
        else
            _launcher.SetJoinLobby();
                                    
        _launcher.JoinOrCreateLobby();
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

[Serializable]
public class LobbyMember
{
    public string profileId;
    public string displayName;
    public int kartId;
    public bool isHost;
    public bool isReady;

    public LobbyMember(Dictionary<string, object> data)
    {
        // profileId
        if (data.TryGetValue("profileId", out object pid))
            profileId = pid as string;

        // display name
        if (data.TryGetValue("name", out object name))
            displayName = name as string;

        // isReady
        if (data.TryGetValue("isReady", out object ready))
        {
            if (ready is bool b)
                isReady = b;
            else if (ready is int i)
                isReady = i != 0;
        }

        // kartId (nested inside "extra")
        if (data.TryGetValue("extra", out object extraObj))
        {
            var extra = extraObj as Dictionary<string, object>;
            if (extra != null && extra.TryGetValue("kartId", out object kid))
            {
                if (kid is int k)
                    kartId = k;
                else if (kid is long l) // sometimes JSON numbers are long
                    kartId = (int)l;
            }
        }

        // default: not host
        isHost = false;

        Debug.Log($"LobbyMember created: profileId={profileId}, name={displayName}, kartId={kartId}, isReady={isReady}");
    }

    public void UpdateFromData(Dictionary<string, object> data, string lobbyOwnerProfileId)
    {
        if (data.TryGetValue("name", out object name))
            displayName = name as string;

        if (data.TryGetValue("isReady", out object ready))
        {
            if (ready is bool b)
                isReady = b;
            else if (ready is int i)
                isReady = i != 0;
        }

        if (data.TryGetValue("extra", out object extraObj))
        {
            var extra = extraObj as Dictionary<string, object>;
            if (extra != null && extra.TryGetValue("kartId", out object kid))
            {
                if (kid is int k)
                    kartId = k;
                else if (kid is long l)
                    kartId = (int)l;
            }
        }

        // Update host status
        isHost = profileId == lobbyOwnerProfileId;
    }
}