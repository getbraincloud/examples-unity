using UnityEngine;
using UnityEngine.UI;

using BrainCloud.JsonFx.Json;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;

// BC Manager will hold a reference to this, which will do the accessing 
// and listening to lobby events
public class BCLobbyManager
{
    public static string ALL_LOBBY_TYPE = "eightVall";

    public string LobbyId = "";
    public string SearchingEntryId = "";

    private bool quickFindLobbyAfterEnable = false;
    public void QuickFindLobby()
    {
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
    public void FindLobby()
    {
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
            BCManager.Wrapper.LobbyService.CancelFindRequest(ALL_LOBBY_TYPE, SearchingEntryId);
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
            QuickFindLobby();
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
            ALL_LOBBY_TYPE,
            0,
            1,
            lobbyParams.algo,
            lobbyParams.filters,
            false,
            lobbyParams.extra,
            "all",
            lobbyParams.settings,
            null,
            OnLobbySuccess
        );

    }

    public void CreateLobby()
    {
        var lobbyParams = CreateLobbyParams();

        BCManager.Wrapper.LobbyService.CreateLobby(
            ALL_LOBBY_TYPE, 0, false,
            lobbyParams.extra,
            "all",
            lobbyParams.settings,
            null,
            OnLobbySuccess);
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

            // now join!
            _launcher.JoinOrCreateLobby();
        }

        if (data.ContainsKey("entryId"))
        {
            SearchingEntryId = data["entryId"] as string;
        }
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