using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using BrainCloud.JsonFx.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// This class demonstrates how to communicate with BrainCloud services.
/// These services include:
///     - Login with switching user logic
///     - Retrieving User Entities from local or other users
///     - Match Making
///     - Creating & Modifying User Entities
///     - Adjusting User Ratings
///     - Recording Playback Stream events
///     - Reading a completed Playback Stream 
/// </summary>

public class NetworkManager : MonoBehaviour
{
    
    private BrainCloudWrapper _bcWrapper;
    public static NetworkManager Instance;
    private bool _isNewPlayer;
    private int _defaultRating = 1200;
    private long _findPlayersRange = 10000;
    private long _numberOfMatches = 20;
    private string _playbackStreamId;
    private long _incrementRatingAmount = 100;
    private long _decrementRatingAmount = 50;
    private bool _dead;
    private bool _shieldActive;
    //Summary info
    private int slayCount;
    public int SlayCount
    {
        get => slayCount;
    }
    private int defeatedTroops;
    public int DefeatedTroops
    {
        get => defeatedTroops;
    }
    private float timeLeft;
    public float TimeLeft
    {
        get => timeLeft;
    }

    public bool IsPlaybackIDValid() => !_playbackStreamId.IsNullOrEmpty();
    
    private void Awake()
    {
        _bcWrapper = GetComponent<BrainCloudWrapper>();
        if (!Instance)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
        LoadID();
    }

    public bool IsSessionValid()
    {
        return _bcWrapper.Client.Authenticated;
    }
    
    private void InitializeBC()
    {
        _bcWrapper.Init();

        _bcWrapper.Client.EnableLogging(true);
    }

    private void OnApplicationQuit()
    {
        if (_bcWrapper != null)
        {
            _bcWrapper.Client.ShutDown();
        }
    }

    //Called from Unity Button, attempting to login
    public void Login()
    {
        _isNewPlayer = false;
        string username = MenuManager.Instance.UsernameInputField.text;
        string password = MenuManager.Instance.PasswordInputField.text;
        if (username.IsNullOrEmpty())
        {   
            MenuManager.Instance.AbortToSignIn($"Please provide a username");
            return;
        }
        if (password.IsNullOrEmpty())
        {
            MenuManager.Instance.AbortToSignIn($"Please provide a password");
            return;
        }

        if (!username.Equals(GameManager.Instance.CurrentUserInfo.Username))
        {
            _isNewPlayer = true;
        }
        
        Settings.SaveLogin(username, password);
        InitializeBC();
        // Authenticate with brainCloud
        _bcWrapper.AuthenticateUniversal(username, password, true, HandlePlayerState, OnFailureCallback, "Login Failed");
    }

    public void SignOut()
    {
        _bcWrapper.PlayerStateService.Logout();
    }

    public void UpdateEntity()
    {
        _bcWrapper.EntityService.UpdateEntity
        (
            GameManager.Instance.CurrentUserInfo.EntityId,
            "vikings",
            CreateJsonEntityData(false),
            CreateACLJson(),
            -1,
            null,
            OnFailureCallback
        );
    }

    public void LookForPlayers()
    {
        _bcWrapper.MatchMakingService.EnableMatchMaking(OnEnableMatchMaking, OnFoundPlayersError);
    }

    void OnEnableMatchMaking(string jsonResponse, object cbObject)
    {
        _bcWrapper.MatchMakingService.FindPlayers
        (
            _findPlayersRange,
            _numberOfMatches,
            OnFoundPlayers,
            OnFoundPlayersError
        );
    }

    public void SetDefaultPlayerRating()
    {
        GameManager.Instance.CurrentUserInfo.Rating = _defaultRating;
        MenuManager.Instance.UpdateMatchMakingInfo();
    }

    void OnFoundPlayers(string jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string,object>;
        
        if (data == null)
        {
            Debug.LogWarning("Something went wrong, data is null");
            return;
        }

        Dictionary<string, object>[] matchesFound = data["matchesFound"] as Dictionary<string, object>[];
        List<UserInfo> users = new List<UserInfo>();

        if (matchesFound == null || matchesFound.Length == 0)
        {
            Debug.LogWarning("No Players Found.");
            MenuManager.Instance.errorPopUpMessageState.SetUpPopUpMessage("No Players Found");
            return;
        }
        
        for (int i = 0; i < matchesFound.Length; i++)
        {
            var newUser = new UserInfo();

            newUser.Username = matchesFound[i]["playerName"] as string;
            newUser.Rating = (int) matchesFound[i]["playerRating"];
            newUser.ProfileId = matchesFound[i]["playerId"] as string;
            
            users.Add(newUser);
        }
        
        MenuManager.Instance.UpdateLobbyList(users);
    }

    void OnFoundPlayersError(int status, int reasonCode, string jsonError, object cbObject)
    {
        List<UserInfo> emptyList = new List<UserInfo>();
        MenuManager.Instance.UpdateLobbyList(emptyList);
        MenuManager.Instance.errorPopUpMessageState.SetUpPopUpMessage("No Players Found");
    }
    
    // User authenticated, handle the result
    void HandlePlayerState(string jsonResponse, object cbObject)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = response["data"] as Dictionary<string, object>;
        var tempUsername = GameManager.Instance.CurrentUserInfo.Username;
        var userInfo = GameManager.Instance.CurrentUserInfo;
        
        userInfo = new UserInfo
        {
            ProfileId = data["profileId"] as string
        };
        
        // If no username is set for this user, then update the name
        if (!data.ContainsKey("playerName"))
        {
            // Update name for display
            _bcWrapper.PlayerStateService.UpdateName(tempUsername, OnLoggedIn, OnFailureCallback,
                "Failed to update username to braincloud");
        }
        else
        {
            //Checking if playerName field has a real value to read in, if so we move on to checking the user entity
            userInfo.Username = data["playerName"] as string;
            if (userInfo.Username.IsNullOrEmpty())
            {
                userInfo.Username = tempUsername;
                _bcWrapper.PlayerStateService.UpdateName(userInfo.Username, OnLoggedIn, OnFailureCallback,
                    "Failed to update username to braincloud");
            }
            else
            {
                OnLoggedIn(null, null);
            }
        }
    }
    
    // Go back to login screen, with an error message
    void OnFailureCallback(int status, int reasonCode, string jsonError, object cbObject)
    {
        if (_dead) return;
        _bcWrapper.Client.ResetCommunication();
        _dead = true;

        string message = cbObject as string;

        if (!SceneManager.GetActiveScene().name.Contains("Game"))
        {
            MenuManager.Instance.AbortToSignIn($"Message: {message} |||| JSON: {jsonError}");   
        }
    }
    
    // User fully logged in. 
    void OnLoggedIn(string jsonResponse, object cbObject)
    {
        //Check if this is a new login, if so then check if this user has entities
        if (!_isNewPlayer)
        {
            if (GameManager.Instance.IsEntityIdValid())
            {
                _bcWrapper.EntityService.GetEntity
                (
                    GameManager.Instance.CurrentUserInfo.EntityId,
                    OnValidEntityResponse,
                    OnFailureCallback
                );    
            }
        }
        else
        {
            _bcWrapper.EntityService.GetEntitiesByType
            (
                "vikings",
                OnReadEntitiesByTypeResponse,
                OnFailureCallback
            );
        }
    }
    
    void OnValidEntityResponse(string jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string,object>;
        
        //Attempted to read entity but got no data
        if (data == null)
        {
            Debug.LogWarning("Invalid entity from response");
            //Attempt to get entities of the type we want
            _bcWrapper.EntityService.GetEntitiesByType
            (
                "vikings",
                OnReadEntitiesByTypeResponse,
                OnFailureCallback
            );
            return;
        }
        Dictionary<string, object> entityData = data["data"] as Dictionary<string,object>;
        
        int defenderSelection = (int) entityData["defenderSelection"];
        int invaderSelection = (int) entityData["invaderSelection"];
        
        GameManager.Instance.UpdateLocalArmySelection(defenderSelection, invaderSelection);
        MenuManager.Instance.UpdateMainMenu();
        GetUserRating();
    }

    void OnReadEntitiesByTypeResponse(string jsonResponse, object cbObject)
    {
        //Read in the entities, if list is empty than create a new entity.
        Dictionary<string, object> response = JsonReader.Deserialize(jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;

        var entities = data["entities"] as Dictionary<string, object>[];
        
        if (entities != null && entities.Length > 0)
        {
            Dictionary<string, object> entityData = entities[0]["data"] as Dictionary<string, object>;
            
            int defenderSelection = (int) entityData["defenderSelection"];
            int invaderSelection = (int) entityData["invaderSelection"];
            string entityId = entities[0]["entityId"] as string;
            
            GameManager.Instance.UpdateFromReadResponse(entityId, defenderSelection, invaderSelection);
            MenuManager.Instance.UpdateMainMenu();
        }
        else
        {
            _bcWrapper.EntityService.CreateEntity
            (
                "vikings",
                CreateJsonEntityData(true),
                CreateACLJson(),
                OnCreatedEntityResponse,
                OnFailureCallback
            );
        }
        GetUserRating();
    }

    private void GetUserRating()
    {
        _bcWrapper.MatchMakingService.Read(OnReadMatchMaking, OnFailureCallback);
    }

    private void OnReadMatchMaking(string jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;

        GameManager.Instance.CurrentUserInfo.Rating = (int) data["playerRating"];
        GameManager.Instance.CurrentUserInfo.MatchesPlayed = (int) data["matchesPlayed"];
        
        //Using try catch in case the shield expiry returns an int rather than a long
        try
        {
            DateTime shieldExpiryDateTime = DateTimeOffset.FromUnixTimeMilliseconds((long)data["shieldExpiry"]).DateTime;
            TimeSpan difference = shieldExpiryDateTime.Subtract(DateTime.UtcNow);

            _shieldActive = difference.Minutes > 0;

            GameManager.Instance.CurrentUserInfo.ShieldTime = difference.Minutes;
        }
        catch (Exception e)
        {
            GameManager.Instance.CurrentUserInfo.ShieldTime = 0;
            _shieldActive = false;
        }

        MenuManager.Instance.UpdateMatchMakingInfo();
        MenuManager.Instance.IsLoading = false;
    }

    void OnCreatedEntityResponse(string jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> jsonData = response["data"] as Dictionary<string, object>;
        string entityId = jsonData["entityId"] as string;
        
        GameManager.Instance.UpdateEntityId(entityId);
        GameManager.Instance.UpdateLocalArmySelection(0, 0);
        MenuManager.Instance.IsLoading = false;
        MenuManager.Instance.UpdateMainMenu();
        SetDefaultPlayerRating();
    }

    public void ReadLobbyUserSelected(string in_userId)
    {
        _bcWrapper.EntityService.GetSharedEntitiesForProfileId(in_userId, OnReadLobbyUserSelected, OnFailureCallback);
    }

    private void OnReadLobbyUserSelected(string jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> jsonData = response["data"] as Dictionary<string, object>;
        
        var entities = jsonData["entities"] as Dictionary<string, object>[];

        if (entities == null || entities.Length == 0)
        {
            Debug.LogWarning("This user has no user entities set up");
            return;
        }
        var entityData = entities[0]["data"] as Dictionary<string, object>;

        if (entityData == null || !entityData.ContainsKey("defenderSelection"))
        {
            Debug.LogWarning("This user has no user entities set up");
            return;
        }
        
        //Get what defender set is selected
        GameManager.Instance.UpdateOpponentInfo
        (
            (ArmyDivisionRank) entityData["defenderSelection"],
            entities[0]["entityId"] as string
        );
        
        //Set up pop up window for confirmation to invade user
        MenuManager.Instance.confirmPopUpMessageState.SetUpConfirmationForMatch();
    }

    public void GameCompleted(bool in_didPlayerWin)
    {
        if (in_didPlayerWin)
        {
            _bcWrapper.MatchMakingService.IncrementPlayerRating(_incrementRatingAmount, OnAdjustPlayerRating, OnFailureCallback);
        }
        else
        {
            _bcWrapper.MatchMakingService.DecrementPlayerRating(_decrementRatingAmount, OnAdjustPlayerRating, OnFailureCallback);
        }
        
        string eventData = CreateJsonIdsEventData();
        string summaryData = CreateSummaryData();
        _bcWrapper.PlaybackStreamService.AddEvent(_playbackStreamId, eventData, summaryData, null, OnFailureCallback);
        RecordDefenderSelected((int)GameManager.Instance.DefenderRank);
        PlayerPrefs.SetString("PlaybackKey", _playbackStreamId);
    }

    private void OnAdjustPlayerRating(string jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;

        if (data == null) return;
        GameManager.Instance.CurrentUserInfo.Rating = (int) data["playerRating"];
    }

    public void RecordTroopSpawn(Vector3 in_spawnPoint, TroopAI in_troop)
    {
        if (!GameManager.Instance.GameActive) return;
        
        string eventData = CreateJsonSpawnEventData(in_spawnPoint, in_troop);
        string summaryData = CreateSummaryData();
        _bcWrapper.PlaybackStreamService.AddEvent(_playbackStreamId, eventData, summaryData, null, OnFailureCallback);
    }

    public void RecordTargetSwitch(TroopAI in_troop, int in_targetID, int in_targetTeamID)
    {
        if (!GameManager.Instance.GameActive) return;
        
        string eventData = CreateJsonTargetEventData(in_troop, in_targetID, in_targetTeamID);
        string summaryData = CreateSummaryData();
        _bcWrapper.PlaybackStreamService.AddEvent(_playbackStreamId, eventData, summaryData, null, OnFailureCallback);
    }

    public void RecordTargetDestroyed(int in_entityID, int in_teamID)
    {
        if (!GameManager.Instance.GameActive) return;
        
        string eventData = CreateJsonDestroyEventData(in_entityID, in_teamID);
        string summaryData = CreateSummaryData();
        _bcWrapper.PlaybackStreamService.AddEvent(_playbackStreamId, eventData, summaryData, null, OnFailureCallback);
    }

    public void RecordDefenderSelected(int in_defenderRank)
    {
        Dictionary<string, object> eventData = new Dictionary<string, object>();
        eventData.Add("eventId", (int)EventId.Defender);
        eventData.Add("defenderRank", in_defenderRank);
        string value = JsonWriter.Serialize(eventData);
        string summaryData = CreateEndGameSummaryData();
        _bcWrapper.PlaybackStreamService.AddEvent(_playbackStreamId, value, summaryData, OnRecordSuccess, OnFailureCallback);
    }

    //Game flow for this callback, Game Completed -> Get All Ids -> Send record request -> OnRecordSuccess
    private void OnRecordSuccess(string in_jsonResponse, object cbObject)
    {
        _bcWrapper.PlaybackStreamService.EndStream(_playbackStreamId);
        _bcWrapper.OneWayMatchService.CompleteMatch(_playbackStreamId);
    }

    public void StartMatch()
    {
        var opponentId = GameManager.Instance.OpponentUserInfo.ProfileId;
        _bcWrapper.OneWayMatchService.StartMatch(opponentId, 1000, OnStartMatchSuccess, OnFailureCallback);
    }

    private void OnStartMatchSuccess(string in_jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(in_jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;

        if (data == null)
        {
            Debug.LogError("Response object doesn't have data. Something went wrong");
            return;
        }
        
        _playbackStreamId = data["playbackStreamId"] as string;
        GameManager.Instance.LoadToGame();
    }

    public void ReadStream()
    {
        if (_playbackStreamId.IsNullOrEmpty())
        {
            LoadID();
        }
        _bcWrapper.PlaybackStreamService.ReadStream(_playbackStreamId, OnReadStreamSuccess, OnFailureCallback);
    }

    private void OnReadStreamSuccess(string in_jsonResponse, object cbObject)
    {
        GameManager.Instance.ReplayRecords.Clear();
        //Extracting events from response...
        Dictionary<string, object> response = JsonReader.Deserialize(in_jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
        Dictionary<string, object>[] events = data["events"] as Dictionary<string, object>[];
        Dictionary<string, object> summary = data["summary"] as Dictionary<string, object>;
        if (events == null || events.Length == 0)
        {
            Debug.LogWarning("No events were retrieved...");
            return;
        }

        if (summary != null && summary.Count > 0)
        {
            slayCount = (int) summary["slayCount"];
            defeatedTroops = (int) summary["defeatedTroops"];
            double value = (double) summary["timeLeft"];
            timeLeft = (float) value;
        }

        for (int i = 0; i < events.Length; i++)
        {
            PlaybackStreamRecord record = new PlaybackStreamRecord();
            record.eventID = (EventId) events[i]["eventId"];
            
            if (events[i].ContainsKey("frameId"))
            {
                record.frameID = (int) events[i]["frameId"];    
            }

            if (events[i].ContainsKey("troopType"))
            {
                record.troopType = (EnemyTypes) events[i]["troopType"];    
            }
            
            if (events[i].ContainsKey("troopID"))
            {
                record.entityID = (int) events[i]["troopID"];
            }

            if (events[i].ContainsKey("targetTeamID"))
            {
                record.targetTeamID = (int) events[i]["targetTeamID"];
            }

            if (events[i].ContainsKey("targetID"))
            {
                record.targetID = (int) events[i]["targetID"];
            }

            if (events[i].ContainsKey("teamID"))
            {
                record.teamID = (int) events[i]["teamID"];
            }

            if (events[i].ContainsKey("spawnPointX"))
            {
                double pointX = (double) events[i]["spawnPointX"];
                double pointY = (double) events[i]["spawnPointY"];
                double pointZ = (double) events[i]["spawnPointZ"];
                record.position.x = (float) pointX;
                record.position.y = (float) pointY;
                record.position.z = (float) pointZ;    
            }

            if (record.eventID == EventId.Ids)
            {
                GameManager.Instance.ReadIDs(events[i]);
            }
            else if (record.eventID == EventId.Defender)
            {
                //Assign defender rank   
                GameManager.Instance.OnReadSetDefenderList((ArmyDivisionRank) events[i]["defenderRank"]); 
            }
            else
            {
                GameManager.Instance.ReplayRecords.Add(record); 
            }
        }

        if (SceneManager.GetActiveScene().name.Contains("Game"))
        {
            GameManager.Instance.ResetGameSceneForStream();
            //Loading things while in game
            PlaybackStreamManager.Instance.StartStream();
        }
        else
        {
            //Loading things while in menu
            GameManager.Instance.LoadToPlaybackScene();
        }
    }

    public void LoadID()
    {
        _playbackStreamId = PlayerPrefs.GetString("PlaybackKey");
        if (_playbackStreamId.IsNullOrEmpty())
        {
            Debug.LogWarning("There's no playback ID locally saved, complete a game to do a playback.");
        }
    }

    public void ReplayStream()
    {
        GameManager.Instance.UpdateSpawnInvaderList();
        LoadID();
        ReadStream();
    }

    public void TurnOnShield()
    {
        if (_shieldActive) return;
        GameManager.Instance.CurrentUserInfo.ShieldTime = 60;
        _bcWrapper.MatchMakingService.TurnShieldOnFor(60, OnTurnOnShieldSuccess);
    }

    private void OnTurnOnShieldSuccess(string jsonResponse, object cbObject)
    {
        MenuManager.Instance.UpdateMatchMakingInfo();
    }

    public void SummaryInfo(int in_slayCount, int in_defeatedTroops, float in_timeLeft)
    {
        slayCount = in_slayCount;
        defeatedTroops = in_defeatedTroops;
        timeLeft = in_timeLeft;
    }

    string CreateEndGameSummaryData()
    {
        Dictionary<string, object> summaryData = new Dictionary<string, object>();
        summaryData.Add("slayCount", slayCount);
        summaryData.Add("defeatedTroops", defeatedTroops);
        summaryData.Add("timeLeft", timeLeft);
        string value = JsonWriter.Serialize(summaryData);
        return value;
    }

    string CreateSummaryData()
    {
        int total = GameManager.Instance.RemainingStructures();
        Dictionary<string, object> summaryData = new Dictionary<string, object>();
        summaryData.Add("total", total);
        string value = JsonWriter.Serialize(summaryData);
        return value;
    }

    string CreateJsonSpawnEventData(Vector3 in_spawnPoint, TroopAI in_troop)
    {
        Dictionary<string, object> eventData = new Dictionary<string, object>();
        eventData.Add("eventId", (int)EventId.Spawn);
        eventData.Add("frameId", GameManager.Instance.SessionManager.FrameID);
        eventData.Add("spawnPointX", in_spawnPoint.x);
        eventData.Add("spawnPointY", in_spawnPoint.y);
        eventData.Add("spawnPointZ", in_spawnPoint.z);
        eventData.Add("troopType", (int)in_troop.EnemyType);
        eventData.Add("troopID", in_troop.EntityID);
        string value = JsonWriter.Serialize(eventData);
        return value;
    }

    string CreateJsonTargetEventData(TroopAI in_troop, int in_targetID, int in_targetTeamID)
    {
        Dictionary<string, object> eventData = new Dictionary<string, object>();
        eventData.Add("eventId", (int)EventId.Target);
        eventData.Add("frameId", GameManager.Instance.SessionManager.FrameID);
        eventData.Add("troopID", in_troop.EntityID);
        eventData.Add("teamID", in_troop.TeamID);
        eventData.Add("targetTeamID", in_targetTeamID);
        eventData.Add("targetID", in_targetID);
        string value = JsonWriter.Serialize(eventData);
        return value;
    }

    string CreateJsonDestroyEventData(int in_entityID, int in_teamID)
    {
        Dictionary<string, object> eventData = new Dictionary<string, object>();
        eventData.Add("eventId", (int)EventId.Destroy);
        eventData.Add("frameId", GameManager.Instance.SessionManager.FrameID);
        eventData.Add("troopID", in_entityID);
        eventData.Add("teamID", in_teamID);
        string value = JsonWriter.Serialize(eventData);
        return value;
    }

    string CreateJsonIdsEventData()
    {
        Dictionary<string, object> eventData = new Dictionary<string, object>();
        eventData.Add("eventId", (int)EventId.Ids);
        
        Dictionary<string, object> invadersList = new Dictionary<string, object>();
        List<int> invadersIDs = GameManager.Instance.InvaderIDs;
        for (int i = 0; i < invadersIDs.Count; i++)
        {
            invadersList.Add(i.ToString(), invadersIDs[i]);
        }
        eventData.Add("invadersList", invadersList);

        Dictionary<string, object> defendersList = new Dictionary<string, object>();
        List<int> defendersIDs = GameManager.Instance.DefenderIDs;
        for (int i = 0; i < defendersIDs.Count; i++)
        {
            defendersList.Add(i.ToString(), defendersIDs[i]);
        }
        eventData.Add("defendersList", defendersList);
        
        string value = JsonWriter.Serialize(eventData);
        return value;
    }

    string CreateJsonEntityData(bool in_isDataNew)
    {
        Dictionary<string, object> entityInfo = new Dictionary<string, object>();
        if (in_isDataNew)
        {
            entityInfo.Add("defenderSelection", 0);
            entityInfo.Add("invaderSelection", 0);    
        }
        else
        {
            UserInfo user = GameManager.Instance.CurrentUserInfo;
            entityInfo.Add("defenderSelection",(int) user.DefendersSelected);
            entityInfo.Add("invaderSelection",(int) user.InvaderSelected);
        }
        
        string value = JsonWriter.Serialize(entityInfo);
        return value;
    }

    string CreateACLJson()
    {
        Dictionary<string, object> aclInfo = new Dictionary<string, object>();
        aclInfo.Add("other", 2);
        string value = JsonWriter.Serialize(aclInfo);
        return value;
    }
}
