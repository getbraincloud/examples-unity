
using BrainCloud;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// BC Manager will hold a reference to this, which will do the accessing 
// and listening to lobby events
public class BCLobbyManager
{
    public static string ACTIVE_LOBBY_TYPE = "eight";

    public event Action<string> OnLobbyEventReceived;

    public event Action<LobbyMember> PlayerJoined;
	public event Action<LobbyMember> PlayerLeft;
    public event Action<LobbyMember> PlayerChanged;
    public event Action<BCLobbyManager> OnLobbyDetailsUpdated;

    public string LobbyId = "";
    public string SearchingEntryId = "";
    public string TrackName = "Cavern Cove";
    public int TrackId = 0;
    public string ModeName = "Race";
    public int GameTypeId = 0;

    public int KeepAliveRateSeconds = 1440;

    public LobbyMember Local
    {
        get
        {
            string localProfileId = BCManager.Wrapper.Client.ProfileId;
            if (LobbyMembers.TryGetValue(localProfileId, out LobbyMember member))
            {
                return member;
            }
            return null; // or throw exception if you prefer
        }
    }

    // Strong-typed lobby member list
    public Dictionary<string, LobbyMember> LobbyMembers = new Dictionary<string, LobbyMember>();
    private string _lobbyOwnerProfileId = null;

    public LobbyState LobbyState
    {
        get
        {
            return _lobbyState;
        }
    }
    private LobbyState _lobbyState = LobbyState.NotInLobby;
    
    private bool quickFindLobbyAfterEnable = false;
    public void QuickFindLobby(GameLauncher launcher)
    {
        lobbyMemberSearch = MAX_MEMBERS_SEARCH;
        ACTIVE_LOBBY_TYPE = GetLobbyString(lobbyMemberSearch); // back to eight
        //recursiveOnError = true;
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
        //recursiveOnError = true;
        _launcher = launcher;
        lobbyMemberSearch = MAX_MEMBERS_SEARCH;
        ACTIVE_LOBBY_TYPE = GetLobbyString(lobbyMemberSearch); // back to eight
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
        //recursiveOnError = false;
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

    public void LeavePhotonGameSession()
    {
        _launcher.LeavePhotonGameSession();
    }

    private void OnLeaveLobbySuccess(string jsonResponse, object cbObject)
    {
        Debug.Log("BCLobbyManager OnLeaveLobbySuccess: " + jsonResponse);
        LobbyId = "";
        LobbyMembers.Clear();
    }

    private void OnLeaveLobbyError(int status, int reasonCode, string jsonError, object cbObject)
    {
        Debug.LogError($"BCLobbyManager OnEnableRTTError: {status}, {reasonCode}, {jsonError}");
        LobbyId = "";
        LobbyMembers.Clear();
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

    public string GetLobbyString(int inLobbyInt)
    {
        string lobbyType = inLobbyInt switch
        {
            1 => "one",
            2 => "two",
            3 => "three",
            4 => "four",
            5 => "five",
            6 => "six",
            7 => "seven",
            8 => "eight",
            _ => "unknown"
        };
        return lobbyType;
    }

    public int GetLobbyInt(string inLobbyString)
    {
        return inLobbyString.ToLower() switch
        {
            "one"   => 1,
            "two"   => 2,
            "three" => 3,
            "four"  => 4,
            "five"  => 5,
            "six"   => 6,
            "seven" => 7,
            "eight" => 8,
            _       => -1 // unknown
        };
    }
    
    public void SendKeepAlive()
    {
	    var requestData = new KeepAliveRequest { lobbyId = LobbyId };
	    string jsonPayload = JsonUtility.ToJson(requestData);
    
        Debug.Log("[LobbyKeepAlive] Sending keep-alive signal... " + jsonPayload);
        BCManager.ScriptService.RunScript(
            "KeepLobbyAlive.ccjs",
            jsonPayload);
    }

    public void SendCompleteGame()
    {
        _lobbyState = LobbyState.InLobby;

        // force them all to false locally, and not connected anymore
        foreach (var member in LobbyMembers.Values)
        {
            member.isReady = false;
            member.isConnected = false;
        }

        // send signal to complete the game and allow the lobby to restart
        var requestData = new KeepAliveRequest { lobbyId = LobbyId };
        string jsonPayload = JsonUtility.ToJson(requestData);
        Debug.Log("[LobbyKeepAlive] Sending Complete signal... " + jsonPayload);
        // Call the script cleanly using RunScript with a serialized payload
        BCManager.ScriptService.RunScript(
            "CompleteLobby.ccjs",
               jsonPayload
        );
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

            LobbyId = data.ContainsKey("lobbyId") ? data["lobbyId"] as string : LobbyId;

            // Update lobby ID and owner
            Dictionary<string, object> lobby = null;
            if (data.TryGetValue("lobby", out object lobbyObj))
            {
                lobby = lobbyObj as Dictionary<string, object>;
                if (lobby != null)
                {
                    //ServerInfo.MaxUsers 
                    LobbyId = lobby.ContainsKey("lobbyId") ? lobby["lobbyId"] as string : LobbyId;

                    KeepAliveRateSeconds = lobby.ContainsKey("keepAliveRateSeconds") ? (int)lobby["keepAliveRateSeconds"] : KeepAliveRateSeconds;

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
                    // Parse full members array (current lobby snapshot)
                    if (lobby.TryGetValue("members", out object membersObj))
                    {
                        var membersList = membersObj as IList;
                        if (membersList != null)
                        {
                            foreach (var m in membersList)
                            {
                                var memberData = m as Dictionary<string, object>;
                                if (memberData != null && memberData.TryGetValue("profileId", out object pidObj))
                                {
                                    string profileId = pidObj as string;
                                    if (!string.IsNullOrEmpty(profileId))
                                    {
                                        if (LobbyMembers.TryGetValue(profileId, out LobbyMember existing))
                                        {
                                            existing.UpdateFromData(memberData, _lobbyOwnerProfileId);
                                            Debug.Log($"MEMBER_JOIN (already exists, updated): {profileId}, ${existing.isHost}");
                                            PlayerChanged?.Invoke(existing);
                                        }
                                        else
                                        {
                                            var member = new LobbyMember(memberData)
                                            {
                                                isHost = profileId == _lobbyOwnerProfileId
                                            };
                                            LobbyMembers[profileId] = member;
                                            PlayerJoined?.Invoke(member);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // handle host migration ? I don't believe it works
            // since we are not the Relay service, photon 
            // and the MEMBER_LEFT events are needed to know when a host leaves
            // disabled this at the moment, since we removed the max players 
            if (operation == "JOIN_FAIL" && recursiveOnError)
            {
                --lobbyMemberSearch;
                if (quickFindRecursive)
                {
                    FindOrCreateLobby();
                }
                else
                {
                    FindLobby();
                }
            }

            // handle starting. do not disband on start
            if (operation == "STARTING")
            {
                if (lobby != null)
                {
                    // Update lobby ID if present
                    LobbyId = lobby.ContainsKey("lobbyId") ? lobby["lobbyId"] as string : LobbyId;

                    // Optionally update owner
                    if (lobby.TryGetValue("ownerCxId", out object ownerCxIdObj))
                    {
                        string ownerCxId = ownerCxIdObj as string;
                        if (!string.IsNullOrEmpty(ownerCxId))
                        {
                            string[] parts = ownerCxId.Split(':');
                            if (parts.Length >= 2)
                                _lobbyOwnerProfileId = parts[1];
                        }
                    }

                    // Mark the lobby as starting
                    _lobbyState = LobbyState.Starting;
                    Debug.Log("[Lobby] Lobby is starting with " + LobbyMembers.Count + " member(s).");
                }
                
            }

            // handle lobby signals like updating the track id and game type
            if (operation == "SIGNAL")
            {
                if (data.TryGetValue("signalData", out object signalDataObj))
                {
                    var signalData = signalDataObj as Dictionary<string, object>;
                    // regular lobby updates and connected events
                    if (signalData != null && signalData.ContainsKey("KartId"))
                    {
                        // Update the member that sent this signal
                        if (data.TryGetValue("from", out object fromObj))
                        {
                            var fromDict = fromObj as Dictionary<string, object>;
                            if (fromDict != null && fromDict.TryGetValue("id", out object fromIdObj))
                            {
                                string profileId = fromIdObj as string;
                                if (!string.IsNullOrEmpty(profileId) && LobbyMembers.TryGetValue(profileId, out LobbyMember member))
                                {
                                    member.kartId = Convert.ToInt32(signalData["KartId"]);
                                    Debug.Log($"Member {member.displayName} KartId={member.kartId}");
                                    // and update the rest
                                    if (member.profileId == Local.profileId)
                                    {
                                        ClientInfo.KartId = member.kartId;
                                    }
                                }
                            }
                        }
                    }
                    else if (signalData != null && signalData.ContainsKey("TrackId"))
                    {
                        if (signalData.ContainsKey("TrackId"))
                        {
                            TrackId = Convert.ToInt32(signalData["TrackId"]);
                        }

                        if (signalData.ContainsKey("GameTypeId"))
                        {
                            GameTypeId = Convert.ToInt32(signalData["GameTypeId"]);
                        }

                        if (signalData.ContainsKey("lobbyId"))
                        {
                            LobbyId = Convert.ToString(signalData["lobbyId"]);
                        }

                        bool connected = signalData.ContainsKey("Connected") ? Convert.ToBoolean(signalData["Connected"]) : false;

                        Debug.Log($"Received SIGNAL: TrackId={TrackId}, GameTypeId={GameTypeId}");

                        // Update the member that sent this signal
                        if (data.TryGetValue("from", out object fromObj))
                        {
                            var fromDict = fromObj as Dictionary<string, object>;
                            if (fromDict != null && fromDict.TryGetValue("id", out object fromIdObj))
                            {
                                string profileId = fromIdObj as string;
                                if (!string.IsNullOrEmpty(profileId) && LobbyMembers.TryGetValue(profileId, out LobbyMember member))
                                {
                                    member.isConnected = connected;
                                    Debug.Log($"Member {member.displayName} Connected={connected}");
                                    // host connected, and not self
                                    if (member.isConnected && member.isHost && member.profileId != Local.profileId)
                                    {
                                        Debug.Log($"CONNECTED: {member.profileId}, Host={member.isHost}");

                                        // now we can as well
                                        JoinOrCreateLobby();
                                    }
                                }
                            }
                        }
                    }
                    else if (signalData != null && signalData.ContainsKey("continueLobbyId"))
                    {
                        // if this lobby id matches ours 
                        if (signalData.ContainsKey("continueLobbyId") && 
                            Convert.ToString(signalData["continueLobbyId"]) == LobbyId)
                        {
                            // the leader needs to also tell all the others to do this same thing 
                            LeavePhotonSessionToLobbyScreen(false);
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
                            string localProfileId = BCManager.Wrapper.Client.ProfileId;
                            if (operation == "MEMBER_JOIN")
                            {
                                if (localProfileId == profileId)
                                {
                                    _lobbyState = LobbyState.InLobby;
                                }
                                if (!LobbyMembers.ContainsKey(profileId))
                                {
                                    // Only create new member if not already present
                                    var member = new LobbyMember(memberData)
                                    {
                                        isHost = profileId == _lobbyOwnerProfileId
                                    };
                                    LobbyMembers[profileId] = member;
                                    PlayerJoined?.Invoke(member);
                                }
                                else
                                {
                                    // Already exists, just update fields
                                    LobbyMembers[profileId].UpdateFromData(memberData, _lobbyOwnerProfileId);
                                    PlayerChanged?.Invoke(LobbyMembers[profileId]);
                                }
                            }
                            else if (operation == "MEMBER_UPDATE")
                            {
                                if (LobbyMembers.TryGetValue(profileId, out LobbyMember existingMember))
                                {
                                    existingMember.UpdateFromData(memberData, _lobbyOwnerProfileId);
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
                                    PlayerJoined?.Invoke(member);
                                }
                            }
                            else if (operation == "MEMBER_LEFT")
                            {
                                if (localProfileId == profileId)
                                {
                                    _lobbyState = LobbyState.NotInLobby;
                                }

                                if (LobbyMembers.ContainsKey(profileId))
                                {
                                    LobbyMembers.TryGetValue(profileId, out LobbyMember existingMember);

                                    // we don't try and do a migration since we don't get the message
                                    // if we leave we don't want to change this
                                    if (existingMember.isHost && Local.profileId != profileId)
                                    {
                                        LeavePhotonSessionToLobbyScreen(true);
                                    }
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
            OnLobbyDetailsUpdated?.Invoke(this);
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

    private void LeavePhotonSessionToLobbyScreen(bool showHostMessage)
    {
        
        // the leader needs to also tell all the others to do this same thing 
		LeavePhotonGameSession();

		SendCompleteGame();

        // clear the rest and go back to the lobby of players
        SceneManager.LoadScene(LevelManager.LOBBY_SCENE);
		UIScreen.BackToInitial();

		// .show the lobby UI 
		LobbyUI lobbyUI = Resources.FindObjectsOfTypeAll<LobbyUI>().FirstOrDefault();
        UIScreen.Focus(lobbyUI.GetComponent<UIScreen>());
        if (showHostMessage) lobbyUI.ShowLobbyDisplayMessage("Host Disconnected From Photon Session");
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

    public bool IsQuickFind
    {
        get
        {
            return quickFindRecursive;
        }
    }

    private bool quickFindRecursive = true;

    private bool recursiveOnError = false;
    private void FindOrCreateLobby()
    {
        //quickFindRecursive = true;
        var lobbyParams = CreateLobbyParams();
        lobbyMemberSearch = MAX_MEMBERS_SEARCH;
        ACTIVE_LOBBY_TYPE = GetLobbyString(lobbyMemberSearch);

        //if (lobbyMemberSearch == 0)
        {
            lobbyMemberSearch = MAX_MEMBERS_SEARCH;
            ACTIVE_LOBBY_TYPE = GetLobbyString(lobbyMemberSearch); // back to eight
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
        /*
        else
        {
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
        */
    }
    
	public void JoinOrCreateLobby()
    {
        // set the client side joining mechanism
        if (Local.isHost)
        {
            // just force it to 8 no matter the lobby type
            ServerInfo.MaxUsers = MAX_MEMBERS_SEARCH;
            _launcher.SetCreateLobby();
        }
        else
            _launcher.SetJoinLobby();
                                    
                                    
        _launcher.JoinOrCreateLobby();
    }

    public void CreateLobby()
    {
        var lobbyParams = CreateLobbyParams();

        lobbyMemberSearch = MAX_MEMBERS_SEARCH;
        ACTIVE_LOBBY_TYPE = GetLobbyString(lobbyMemberSearch);

        BCManager.Wrapper.LobbyService.CreateLobby(
            ACTIVE_LOBBY_TYPE, 0, false,
            lobbyParams.extra,
            "all",
            lobbyParams.settings,
            null,
            OnLobbySuccess,
            OnLobbyError);
    }

    private static int MAX_MEMBERS_SEARCH = 8;
    private int lobbyMemberSearch = MAX_MEMBERS_SEARCH;
    private void FindLobby()
    {
        //quickFindRecursive = false;
        var lobbyParams = CreateLobbyParams();
        //if (lobbyMemberSearch != 0)
        {
            lobbyMemberSearch = MAX_MEMBERS_SEARCH;
        
            ACTIVE_LOBBY_TYPE = GetLobbyString(lobbyMemberSearch);
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
        /*
        else
        {
            lobbyMemberSearch = MAX_MEMBERS_SEARCH;
            ACTIVE_LOBBY_TYPE = GetLobbyString(lobbyMemberSearch);
            var jsonMessage = "{\"service\":\"lobby\",\"operation\":\"JOIN_FAIL\",\"data\":{\"lobbyType\":\"one\",\"reason\":{\"code\":80200,\"desc\":\"No lobbies found matching criteria\"}}}";

            OnLobbyEventReceived?.Invoke(jsonMessage);
        }
        */
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
    public bool isConnected;

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
        isConnected = false;

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

public enum LobbyState
{
    NotInLobby,   // player not in lobby
    InLobby,      // Player is in the lobby, waiting
    Starting,     // Lobby is starting soon
    InGame,       // Game is currently in progress
    FinishedGame  // Game has finished
}

[System.Serializable]
public class KeepAliveRequest
{
    public string lobbyId;
}