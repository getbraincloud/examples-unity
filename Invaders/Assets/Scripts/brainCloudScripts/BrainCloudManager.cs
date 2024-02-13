using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.JsonFx.Json;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class BrainCloudManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField UsernameField;
    [SerializeField] private TMP_InputField PasswordField;
    private BrainCloudWrapper _wrapper;
    private BrainCloudS2S _bcS2S = new BrainCloudS2S();
    private Lobby _currentLobby;
    public Lobby CurrentLobby
    {
        get => _currentLobby;
        set => _currentLobby = value;
    }
    
    public int LobbyMemberCount
    {
        get => _currentLobby.Members.Count;
    }
    public UnityTransport _unityTransport;

    private UserInfo _localUserInfo = new UserInfo();
    public UserInfo LocalUserInfo
    {
        get => _localUserInfo;
        set => _localUserInfo = value;
    }

    public static BrainCloudManager Singleton { get; private set; }
    void Awake()
    {
        if(Singleton == null)
        {
            Singleton = this;
        }
        else
        {
            Destroy(gameObject);
        }
    
        _wrapper = GetComponent<BrainCloudWrapper>();
        _wrapper.Init();
        _unityTransport = GetComponent<UnityTransport>();
        _bcS2S.Init(BrainCloud.Plugin.Interface.AppId, "Testing", BrainCloud.Plugin.Interface.AppSecret, true);
    }

    public void AuthenticateWithBrainCloud(string in_username, string in_password)
    {
        LocalUserInfo.Username = in_username;
        _wrapper.AuthenticateUniversal(in_username, in_password, true, OnAuthenticateSuccess, OnFailureCallback);
    }
    
    private void OnAuthenticateSuccess(string jsonResponse, object cbObject)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = response["data"] as Dictionary<string, object>;
        _localUserInfo.ProfileID = data["profileId"] as string;
        var playerName = data["playerName"] as string;
        // If no username is set for this user, ask for it
        if (playerName.IsNullOrEmpty())
        {
            // Update name for display
            _wrapper.PlayerStateService.UpdateName(_localUserInfo.Username, OnUpdateName, OnFailureCallback,
                "Failed to update username to braincloud");
        }
        else
        {
            MenuControl.Singleton.SwitchMenuButtons();            
        }
    }
    
    private void OnUpdateName(string jsonResponse, object cbObject)
    {
        MenuControl.Singleton.SwitchMenuButtons();
    }
    
    public void FindOrCreateLobby()
    {
        _wrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent); 
        _wrapper.RTTService.EnableRTT(RTTConnectionType.WEBSOCKET, OnRTTConnected, OnFailureCallback);
    }
    
    void OnRTTConnected(string jsonResponse, object cbObject)
    {
        // Find lobby
        var algo = new Dictionary<string, object>();
        algo["strategy"] = "ranged-absolute";
        algo["alignment"] = "center";
        List<int> ranges = new List<int>();
        ranges.Add(1000);
        algo["ranges"] = ranges;
        
        //
        var filters = new Dictionary<string, object>();

        //
        var settings = new Dictionary<string, object>();

        var extra = new Dictionary<string, object>();

        string teamCode = "all";
        string lobbyType = "InvaderParty";

        //
        _wrapper.LobbyService.FindOrCreateLobby
        (
            lobbyType,
            0, // rating
            1, // max steps
            algo, // algorithm
            filters, // filters
            0, // Timeout
            false, // ready
            extra, // extra
            teamCode, // team code
            settings, // settings
            null, // other users
            null, // Success of lobby found will be in the event onLobbyEvent
            OnFailureCallback, 
            "Failed to find lobby"
        );
    }
    
    void OnLobbyEvent(string jsonResponse)
    {
        Dictionary<string, object> response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        Dictionary<string, object> jsonData = response["data"] as Dictionary<string, object>;

        // If there is a lobby object present in the message, update our lobby
        // state with it.
        if (jsonData.ContainsKey("lobby"))
        {
            _currentLobby = new Lobby(jsonData["lobby"] as Dictionary<string, object>,
                jsonData["lobbyId"] as string);
                
            if (MenuControl.Singleton.IsLoading)
            {
                MenuControl.Singleton.IsLoading = false;
            }
            //GameManager.Instance.UpdateMatchAndLobbyState();
        }

        //Using the key "operation" to determine what state the lobby is in
        if (response.ContainsKey("operation"))
        {
            var operation = response["operation"] as string;
            switch (operation)
            {
                case "MEMBER_JOIN":
                case "MEMBER_UPDATE":
                    if(LobbyControl.Singleton != null)
                    {
                        LobbyControl.Singleton.GenerateUserStatsForLobby();                        
                    }
                    break;
                    
                case "DISBANDED":
                {
                    var reason = jsonData["reason"] as Dictionary<string, object>;
                    if ((int) reason["code"] != ReasonCodes.RTT_ROOM_READY)
                    {
                        // Disbanded for any other reason than ROOM_READY, means we failed to launch the game.
                        //CloseGame(true);
                    }
                    break;
                }
                case "STARTING":
                    
                    break;
                case "ROOM_READY":
                    
                    break;
            }
        }
    }
    
    public void UpdateReady()
    {
        Dictionary<string, object> extra = new Dictionary<string, object>();
        _wrapper.LobbyService.UpdateReady(_currentLobby.LobbyID, true, extra);
    }
    
    private void OnFailureCallback(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Debug.Log("Error: " + statusMessage);
    }

}
