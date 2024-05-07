using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.JsonFx.Json;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using System;
using UnityEngine.UI;

public class BrainCloudManager : MonoBehaviour
{
    [SerializeField] private SceneTransitionHandler _sceneTransitionHandler;
    private NetworkManager _netManager;

    private BrainCloudWrapper _wrapper;
    public BrainCloudWrapper BCWrapper
    {
        get => _wrapper;
    }
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
    
    public BrainCloudS2S S2SWrapper
    {
        get => _bcS2S;
    }

    public SceneTransitionHandler SceneTransitionHandler
    {
        get => _sceneTransitionHandler;
    }

    public NetworkManager NetworkManager
    {
        get => _netManager;
    }

    private UnityTransport _unityTransport;

    public UnityTransport UnityTransport
    {
        get => _unityTransport;
    }

    public bool IsDedicatedServer;

    private UserInfo _localUserInfo = new UserInfo();
    public UserInfo LocalUserInfo
    {
        get => _localUserInfo;
        set => _localUserInfo = value;
    }

    private string _roomAddress;
    private int _roomPort;

    public static BrainCloudManager Singleton { get; private set; }
    public List<UserScoreInfo> ListOfUsers = new List<UserScoreInfo>();
    void Awake()
    {
        IsDedicatedServer = Application.isBatchMode && !Application.isEditor;

        _netManager = GetComponent<NetworkManager>();
        _unityTransport = GetComponent<UnityTransport>();
        if (Singleton == null)
        {
            Singleton = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (IsDedicatedServer)
        {       
            Debug.Log("Initializing S2S on server");
            _bcS2S = new BrainCloudS2S();
            string appId = Environment.GetEnvironmentVariable("APP_ID");
            string serverName = Environment.GetEnvironmentVariable("SERVER_NAME");
            string serverSecret = Environment.GetEnvironmentVariable("SERVER_SECRET");
            _bcS2S.Init(appId, serverName, serverSecret, true, "https://api.internal.braincloudservers.com/s2sdispatcher");
            //_bcS2S.Authenticate();
            _bcS2S.LoggingEnabled = true;
        }
        else
        {
            _wrapper = GetComponent<BrainCloudWrapper>();
            _wrapper.Init();
        }
    }

    private void Start()
    {
        if (IsDedicatedServer)
        {
            Debug.Log("Starting NetCode Server");
            _unityTransport.SetConnectionData("0.0.0.0", 7777);
            _netManager.StartServer();
        }
        else if(_wrapper.CanReconnect())
        {
            _wrapper.Reconnect(OnAuthenticateSuccess, OnFailureCallback);            
        }
    }

    private void Update()
    {
        if (IsDedicatedServer)
        {
            _bcS2S.RunCallbacks();
        }
    }

    private void OnApplicationQuit()
    {
        if(_wrapper.Client.Authenticated)
        {
            _wrapper.LogoutOnApplicationQuit(false);
        }
    }
    
    public void Logout()
    {
        _wrapper.LogoutOnApplicationQuit(true);
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
        if(!MenuControl.Singleton.RememberMeToggle.isOn)
        {
            _wrapper.ResetStoredProfileId();
            _wrapper.Client.AuthenticationService.ProfileId = _localUserInfo.ProfileID;
        }
    }
    
    [ServerRpc]
    public void AddUserToList(string in_username, ulong in_clientID)
    {
        if(IsDedicatedServer)
        {
            UserScoreInfo user = new UserScoreInfo();
            user.clientName = in_username;
            user.clientID = in_clientID;
            ListOfUsers.Add(user);
        }
    }

    private void OnUpdateName(string jsonResponse, object cbObject)
    {
        MenuControl.Singleton.SwitchMenuButtons();
    }
    
    public void FindOrCreateLobby()
    {
        _wrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent); 
        _wrapper.RTTService.EnableRTT(OnRTTConnected, OnFailureCallback);
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
        }

        if (jsonData.ContainsKey("passcode"))
        {
            _localUserInfo.PassCode = jsonData["passcode"] as string;
        }

        
        //Using the key "operation" to determine what state the lobby is in
        if (response.ContainsKey("operation"))
        {
            var operation = response["operation"] as string;
            switch (operation)
            {
                case "MEMBER_JOIN":
                case "MEMBER_UPDATE":
                    if (LobbyControl.Singleton != null)
                    {
                        LobbyControl.Singleton.GenerateUserStatsForLobby();
                    }
                    break;

                case "DISBANDED":
                    {
                        var reason = jsonData["reason"] as Dictionary<string, object>;
                        if ((int)reason["code"] != ReasonCodes.RTT_ROOM_READY)
                        {
                            // Disbanded for any other reason than ROOM_READY, means we failed to launch the game.
                            LobbyControl.Singleton.SetupPopupPanel($"Received an error message while launching room: {reason["desc"]}");
                        }
                        break;
                    }
                case "STARTING":
                    break;
                case "ROOM_ASSIGNED":
                    if(LobbyControl.Singleton != null)
                    {
                        LobbyControl.Singleton.LoadingIndicatorMessage = "Server room is assigned";
                    }
                    Dictionary<string, object> connectData = jsonData["connectData"] as Dictionary<string, object>;
                    _roomAddress = connectData["address"] as string;
                    Dictionary<string, object> ports = connectData["ports"] as Dictionary<string, object>;
                    _roomPort = (int)ports["7777/tcp"];
                    break;
                case "ROOM_READY":
                    if(LobbyControl.Singleton != null)
                    {
                        LobbyControl.Singleton.LoadingIndicatorMessage = "Room is ready";
                        LobbyControl.Singleton.IsLoading = false;
                    }
                    SceneTransitionHandler.SwitchScene("Connecting");
                    _unityTransport.ConnectionData.Address = _roomAddress;
                    _unityTransport.ConnectionData.Port = (ushort)_roomPort;
                    _netManager.StartClient();
                    AddUserToList(_localUserInfo.Username, NetworkManager.Singleton.LocalClientId);
                    //open in game level and then connect to server
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
        if(LobbyControl.Singleton != null)
        {
            LobbyControl.Singleton.SetupPopupPanel($"Failure Callback received, error message: {statusMessage}");
        }
        Debug.Log("Error: " + statusMessage);
    }

}
