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
using System.Linq;

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
    public bool isLobbyOwner
    {
        get => LocalUserInfo.ProfileID == CurrentLobby.OwnerID;
    }

    private string _roomAddress;
    private int _roomPort;

    public static BrainCloudManager Singleton { get; private set; }
    public List<UserScoreInfo> ListOfUsers = new List<UserScoreInfo>();
    private List<string> featuredUser = new List<string>();
    private bool foundTopUserInfo = false;
    private bool foundFeaturedUserInfo = false;

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
        Dictionary<string, object> lobby;
        Dictionary<string, object> settings;
        string[] replayUserIds;

        // If there is a lobby object present in the message, update our lobby
        // state with it.
        if (jsonData.ContainsKey("lobby") && (string)response["operation"] != "SETTINGS_UPDATE")
        {
            _currentLobby = new Lobby(jsonData["lobby"] as Dictionary<string, object>, jsonData["lobbyId"] as string);
                
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
                    if (LobbyControl.Singleton != null)
                    {
                        LobbyControl.Singleton.GenerateUserStatsForLobby();
                    }
                    lobby = jsonData["lobby"] as Dictionary<string, object>;
                    settings = lobby["settings"] as Dictionary<string, object>;
                    if (!settings.ContainsKey("replay_users")) break;
                    replayUserIds = settings["replay_users"] as string[];
                    StartCoroutine(DelayAddIdToList(replayUserIds));
                    break;
                case "MEMBER_UPDATE":
                    if (LobbyControl.Singleton != null) LobbyControl.Singleton.GenerateUserStatsForLobby();
                    break;
                case "DISBANDED":
                    var reason = jsonData["reason"] as Dictionary<string, object>;
                    if ((int)reason["code"] != ReasonCodes.RTT_ROOM_READY)
                    {
                        // Disbanded for any other reason than ROOM_READY, means we failed to launch the game.
                        LobbyControl.Singleton.SetupPopupPanel($"Received an error message while launching room: {reason["desc"]}");
                    }
                    break;
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
                        LobbyControl.Singleton.FetchPlaybacks();
                    }
                    SceneTransitionHandler.SwitchScene("Connecting");
                    _unityTransport.ConnectionData.Address = _roomAddress;
                    _unityTransport.ConnectionData.Port = (ushort)_roomPort;
                    _netManager.StartClient();
                    AddUserToList(_localUserInfo.Username, NetworkManager.Singleton.LocalClientId);
                    //open in game level and then connect to server
                    break;
                case "SETTINGS_UPDATE":
                    lobby = jsonData["lobby"] as Dictionary<string, object>;
                    settings = lobby["settings"] as Dictionary<string, object>;
                    replayUserIds = settings["replay_users"] as string[];
                    LobbyControl.Singleton.AddIdToList(replayUserIds[^1]);
                    break;
            }
        }
    }

    private IEnumerator DelayAddIdToList(string[] userIds)
    {
        yield return new WaitUntil(() => foundTopUserInfo && foundFeaturedUserInfo);
        foreach (string userId in userIds)
        {
            LobbyControl.Singleton.AddIdToList(userId);
        }
        yield break;
    }

    public void UpdateReady()
    {
        Dictionary<string, object> extra = new Dictionary<string, object>();
        _wrapper.LobbyService.UpdateReady(_currentLobby.LobbyID, true, extra);
    }
    
    public void SendNewIdSignal(string[] newIds)
    {
        Dictionary<string, object> replayUsers = new Dictionary<string, object> { { "replay_users",  newIds} };
        Dictionary<string, object> temp = new Dictionary<string, object>();
        _wrapper.LobbyService.UpdateSettings(CurrentLobby.LobbyID, replayUsers, null, OnFailureCallback);
    }

    public void StartGetFeaturedUser()
    {
        StartCoroutine(GetFeaturedUser());
    }

    private IEnumerator GetFeaturedUser()
    {
        _wrapper.GlobalEntityService.ReadEntity("219b77d3-340a-4c13-b700-8cd279c0dd60", OnFindFeaturedUser, OnFailureCallback);
        yield return new WaitUntil(() => featuredUser.Count == 1);
        _wrapper.LeaderboardService.GetPlayersSocialLeaderboard("InvaderHighScore", featuredUser, OnFeaturedUserInfoSuccess, OnFailureCallback);
        yield break;
    }

    private void OnFindFeaturedUser(string in_jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(in_jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
        Dictionary<string, object> entityData = data["data"] as Dictionary<string, object>;
        featuredUser.Add((string)entityData["user"]);
    }

    private void OnFeaturedUserInfoSuccess(string in_jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(in_jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
        Dictionary<string, object>[] leaderboard = data["leaderboard"] as Dictionary<string, object>[];
        Dictionary<string, object> userData = leaderboard[0];
        LobbyControl.Singleton.UpdateFeaturedSelector((string)userData["playerId"], (string)userData["playerName"], (int)userData["score"]);
        featuredUser.Clear();
        foundFeaturedUserInfo = true;
    }

    public void GetTopUsers(int amount)
    {
        BrainCloudSocialLeaderboard.SortOrder order = BrainCloudSocialLeaderboard.SortOrder.HIGH_TO_LOW;
        _wrapper.LeaderboardService.GetGlobalLeaderboardPage("InvaderHighScore", order, 0, amount - 1, OnTopUserInfoSuccess, OnFailureCallback);
    }

    private void OnTopUserInfoSuccess(string in_jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(in_jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
        Dictionary<string, object>[] leaderboard = data["leaderboard"] as Dictionary<string, object>[];
        
        foreach (Dictionary<string, object> userData in leaderboard)
        {
            LobbyControl.Singleton.UpdateLeaderBoardSelector((int)userData["rank"], (string)userData["playerId"], (string)userData["name"], (int)userData["score"]);
        }
        foundTopUserInfo = true;
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
