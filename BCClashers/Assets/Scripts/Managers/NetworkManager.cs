using System;
using System.Collections.Generic;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using BrainCloud.JsonFx.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private bool _didInvadersWin;
    private string _invadedPlaybackID;

    private static string _currencyType = "gold";
    private static int _startingGold = 100000;

    public bool DidInvadersWin
    {
        get => _didInvadersWin;
    }
    //Summary info
    private int _invaderKillCount;
    public int SlayCount
    {
        get => _invaderKillCount;
    }

    private int _defenderKillCount;
    public int DefeatedTroops
    {
        get => _defenderKillCount;
    }

    private int _structureKillCount;

    public int StructureKillCount
    {
        get => _structureKillCount;
        set => _structureKillCount = value;
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

        //Initialize brainCloud by grabbing plugin info that is set up using brainCloud->Settings
        _bcWrapper.Init();
    }

    public bool IsSessionValid()
    {
        return _bcWrapper.Client.Authenticated;
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

        //If a new player is logging in, delete previous player data
        if (!username.Equals(GameManager.Instance.CurrentUserInfo.Username))
        {
            _isNewPlayer = true;
            _playbackStreamId = "";
            PlayerPrefs.DeleteAll();
        }
        
        Settings.SaveLogin(username, password);
        // Authenticate with brainCloud
        _bcWrapper.AuthenticateUniversal(username, password, true, HandlePlayerState, OnFailureCallback);
    }

    public void SignOut()
    {
        _bcWrapper.Logout(true);
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

    private void OnEnableMatchMaking(string jsonResponse, object cbObject)
    {
        _bcWrapper.MatchMakingService.FindPlayers
        (
            _findPlayersRange,
            _numberOfMatches,
            OnFoundPlayers,
            OnFoundPlayersError
        );
    }

    private void SetDefaultPlayerRating()
    {
        GameManager.Instance.CurrentUserInfo.Rating = _defaultRating;
        MenuManager.Instance.UpdateMatchMakingInfo();
    }

    private void OnFoundPlayers(string jsonResponse, object cbObject)
    {
        if (JsonReader.Deserialize(jsonResponse) is Dictionary<string, object> response)
        {
            if (response["data"] is not Dictionary<string, object> data)
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

            for (int i = 0; i < matchesFound.Length; ++i)
            {
                var newUser = new UserInfo();

                newUser.Username = matchesFound[i]["playerName"] as string;
                newUser.Rating = (int) matchesFound[i]["playerRating"];
                newUser.ProfileId = matchesFound[i]["playerId"] as string;

                users.Add(newUser);
            }

            MenuManager.Instance.UpdateLobbyList(users);
        }
    }

    private void OnFoundPlayersError(int status, int reasonCode, string jsonError, object cbObject)
    {
        List<UserInfo> emptyList = new List<UserInfo>();
        MenuManager.Instance.UpdateLobbyList(emptyList);
        MenuManager.Instance.errorPopUpMessageState.SetUpPopUpMessage("No Players Found");
    }
    
    // User authenticated, handle the result
    private void HandlePlayerState(string jsonResponse, object cbObject)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = response["data"] as Dictionary<string, object>;
        
        var userInfo = GameManager.Instance.CurrentUserInfo;
        if (data is not null)
        {
            userInfo.ProfileId = data["profileId"] as string;
        }

        var tempUsername = GameManager.Instance.CurrentUserInfo.Username;
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

        _bcWrapper.EntityService.GetSingleton(_currencyType, OnGetSingleton, OnFailureCallback);
    }

    private void OnGetSingleton(string jsonResponse, object cbObject)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = response["data"] as Dictionary<string, object>;
        if (data == null)
        {
            GameManager.Instance.CurrentUserInfo.GoldAmount = _startingGold;
            MenuManager.Instance.UpdateGoldAmount();
            string jsonEntityData = CreateJsonCurrencyEntityData();
            _bcWrapper.EntityService.UpdateSingleton(_currencyType, jsonEntityData, CreateACLJson(0), -1);
            return;
        }

        var entityData = data["data"] as Dictionary<string, object>;
        var gold = (int) entityData["gold"];
        GameManager.Instance.CurrentUserInfo.GoldAmount = gold;
        MenuManager.Instance.UpdateGoldAmount();
    }

    public void IncreaseGoldAmount()
    {
        GameManager.Instance.CurrentUserInfo.PreviousGoldAmount = GameManager.Instance.CurrentUserInfo.GoldAmount;
        GameManager.Instance.CurrentUserInfo.GoldAmount += 100000;
        MenuManager.Instance.UpdateGoldAmount();
        MenuManager.Instance.ValidateInvaderSelection();
        _bcWrapper.EntityService.UpdateSingleton(_currencyType, CreateJsonCurrencyEntityData(), CreateACLJson(0), -1);
    }

    public void IncreaseGoldFromGameStats(int slayCount, int troopsSurvived)
    {
        int goldGained = (slayCount * 10000) + (troopsSurvived * 10000) + (_structureKillCount * 10000);
        GameManager.Instance.CurrentUserInfo.GoldAmount += goldGained;
        _bcWrapper.EntityService.UpdateSingleton(_currencyType, CreateJsonCurrencyEntityData(), CreateACLJson(0), -1);
    }

    public void DecreaseGoldAmountForShield()
    {
        GameManager.Instance.CurrentUserInfo.GoldAmount -= 100000;
        MenuManager.Instance.UpdateGoldAmount();
        MenuManager.Instance.ValidateInvaderSelection();
        _bcWrapper.EntityService.UpdateSingleton(_currencyType, CreateJsonCurrencyEntityData(), CreateACLJson(0), -1);
    }

    public void DecreaseGoldAmountForInvaderSelection()
    {
        var invaderSelection = (int) GameManager.Instance.CurrentUserInfo.InvaderSelected;
        var decrementAmount = MenuManager.Instance.PriceOfInvaders[invaderSelection];
        GameManager.Instance.CurrentUserInfo.GoldAmount -= decrementAmount;
        MenuManager.Instance.UpdateGoldAmount();
        MenuManager.Instance.ValidateInvaderSelection();
        _bcWrapper.EntityService.UpdateSingleton(_currencyType, CreateJsonCurrencyEntityData(), CreateACLJson(0), -1);
    }
    
    
    // Go back to login screen, with an error message
    private void OnFailureCallback(int status, int reasonCode, string jsonError, object cbObject)
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
    private void OnLoggedIn(string jsonResponse, object cbObject)
    {
        //Check if this is a new login, if so then check if this user has entities
        if (!_isNewPlayer && GameManager.Instance.IsEntityIdValid())
        {
            _bcWrapper.EntityService.GetEntity
            (
                GameManager.Instance.CurrentUserInfo.EntityId,
                OnValidEntityResponse,
                OnFailureCallback
            );
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
    
    private void OnValidEntityResponse(string jsonResponse, object cbObject)
    {
        if (JsonReader.Deserialize(jsonResponse) is Dictionary<string, object> response)
        {
            //Attempted to read entity but got no data
            if (response["data"] is not Dictionary<string, object> data)
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

            if (data["data"] is Dictionary<string, object> entityData)
            {
                int defenderSelection = (int) entityData["defenderSelection"];
                int invaderSelection = (int) entityData["invaderSelection"];

                GameManager.Instance.UpdateLocalArmySelection(defenderSelection, invaderSelection);
            }
        }

        MenuManager.Instance.UpdateMainMenu();
        GetUserRating();
    }

    private void OnReadEntitiesByTypeResponse(string jsonResponse, object cbObject)
    {
        //Read in the entities, if list is empty than create a new entity.
        if (JsonReader.Deserialize(jsonResponse) is Dictionary<string, object> response)
        {
            if (response["data"] is Dictionary<string, object> data && data["entities"] is Dictionary<string, object>[] {Length: > 0 } entities)
            {
                if (entities[0]["data"] is Dictionary<string, object> entityData)
                {
                    int defenderSelection = (int) entityData["defenderSelection"];
                    int invaderSelection = (int) entityData["invaderSelection"];
                    string entityId = entities[0]["entityId"] as string;

                    GameManager.Instance.UpdateFromReadResponse(entityId, defenderSelection, invaderSelection);
                }

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
        }

        GetUserRating();
    }

    private void GetUserRating()
    {
        _bcWrapper.MatchMakingService.Read(OnReadMatchMaking, OnFailureCallback);
    }

    private void OnReadMatchMaking(string jsonResponse, object cbObject)
    {
        if (JsonReader.Deserialize(jsonResponse) is Dictionary<string, object> response)
        {
            if (response["data"] is Dictionary<string, object> data)
            {
                GameManager.Instance.CurrentUserInfo.Rating = (int) data["playerRating"];
                GameManager.Instance.CurrentUserInfo.MatchesPlayed = (int) data["matchesPlayed"];

                //Using try catch in case the shield expiry returns an int rather than a long
                try
                {
                    DateTime shieldExpiryDateTime = DateTimeOffset.FromUnixTimeMilliseconds((long) data["shieldExpiry"]).DateTime;
                    TimeSpan difference = shieldExpiryDateTime.Subtract(DateTime.UtcNow);

                    _shieldActive = difference.Minutes > 0;

                    GameManager.Instance.CurrentUserInfo.ShieldTime = difference.Minutes;
                }
                catch
                {
                    GameManager.Instance.CurrentUserInfo.ShieldTime = 0;
                    _shieldActive = false;
                }
            }
        }

        _bcWrapper.PlaybackStreamService.GetRecentStreamsForTargetPlayer
        (
            GameManager.Instance.CurrentUserInfo.ProfileId,
            10,
            OnGetRecentStreams,
            OnFailureCallback
        );
    }

    private void OnGetRecentStreams(string jsonResponse, object cbObject)
    {
        if (JsonReader.Deserialize(jsonResponse) is Dictionary<string, object> response)
        {
            Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
            StreamInfo streamInfo = new StreamInfo();
            if (data != null)
            {
                if (data["streams"] is Dictionary<string, object>[] {Length: > 0 } streams)
                {
                    streamInfo.PlaybackStreamID = streams[0]["playbackStreamId"] as string;
                    if (streams[0]["summary"] is Dictionary<string, object> summary)
                    {
                        if (summary.ContainsKey("defenderKillCount"))
                        {
                            streamInfo.SlayCount = (int) summary["defenderKillCount"];
                        }

                        if (summary.ContainsKey("invaderKillCount"))
                        {
                            streamInfo.DefeatedTroops = (int) summary["invaderKillCount"];
                        }

                        if (summary.ContainsKey("didInvadersWin"))
                        {
                            streamInfo.DidInvadersWin = (bool) summary["didInvadersWin"];
                        }

                        if (summary.ContainsKey("timeLeft"))
                        {
                            var timeLeft = (double) summary["timeLeft"];
                            streamInfo.DurationOfInvasion = (float) (180f - timeLeft);
                        }
                    }
                }
            }

            GameManager.Instance.InvadedStreamInfo = streamInfo;
        }

        MenuManager.Instance.UpdateMatchMakingInfo();
        MenuManager.Instance.IsLoading = false;
    }

    private void OnCreatedEntityResponse(string jsonResponse, object cbObject)
    {
        if (JsonReader.Deserialize(jsonResponse) is Dictionary<string, object> response)
        {
            if (response["data"] is Dictionary<string, object> jsonData)
            {
                string entityId = jsonData["entityId"] as string;

                GameManager.Instance.UpdateEntityId(entityId);
            }
        }

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

        if (jsonData["entities"] is not Dictionary<string, object>[] entities || entities.Length == 0)
        {
            Debug.LogWarning("This user has no user entities set up");
            return;
        }

        if (entities[0]["data"] is not Dictionary<string, object> entityData || !entityData.ContainsKey("defenderSelection"))
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

        MenuManager.Instance.UpdateSelectedPlayerDefense((int) GameManager.Instance.OpponentUserInfo.DefendersSelected);

        MenuManager.Instance.ValidateInvaderSelection();
    }

    public void GameCompleted(bool in_didPlayerWin)
    {
        _didInvadersWin = in_didPlayerWin;
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
        DecreaseGoldAmountForInvaderSelection();
        if (_shieldActive)
        {
            _bcWrapper.MatchMakingService.TurnShieldOff();
            _shieldActive = false;
            GameManager.Instance.CurrentUserInfo.ShieldTime = 0;
        }
        var opponentId = GameManager.Instance.OpponentUserInfo.ProfileId;
        _bcWrapper.OneWayMatchService.StartMatch(opponentId, _findPlayersRange, OnStartMatchSuccess, OnFailureCallback);
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

    public void ReadInvasionStream()
    {
        _bcWrapper.PlaybackStreamService.ReadStream(GameManager.Instance.InvadedStreamInfo.PlaybackStreamID, OnReadStreamSuccess, OnFailureCallback);
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
            _invaderKillCount = (int) summary["invaderKillCount"];
            _defenderKillCount = (int) summary["defenderKillCount"];
            timeLeft = (float)(double)summary["timeLeft"];
            _didInvadersWin = (bool) summary["didInvadersWin"];
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
        DecreaseGoldAmountForShield();
        GameManager.Instance.CurrentUserInfo.ShieldTime = 60;
        _bcWrapper.MatchMakingService.TurnShieldOnFor(60, OnTurnOnShieldSuccess);
    }

    private void OnTurnOnShieldSuccess(string jsonResponse, object cbObject)
    {
        MenuManager.Instance.UpdateMatchMakingInfo();
    }

    public void SummaryInfo(int in_slayCount, int in_defeatedTroops, float in_timeLeft)
    {
        _invaderKillCount = in_slayCount;
        _defenderKillCount = in_defeatedTroops;
        timeLeft = in_timeLeft;
    }

    private string CreateEndGameSummaryData()
    {
        Dictionary<string, object> summaryData = new Dictionary<string, object>();
        summaryData.Add("invaderKillCount", _invaderKillCount);
        summaryData.Add("defenderKillCount", _defenderKillCount);
        summaryData.Add("timeLeft", timeLeft);
        summaryData.Add("didInvadersWin", _didInvadersWin);
        string value = JsonWriter.Serialize(summaryData);
        return value;
    }

    private string CreateSummaryData()
    {
        int total = GameManager.Instance.RemainingStructures();
        Dictionary<string, object> summaryData = new Dictionary<string, object>();
        summaryData.Add("total", total);
        string value = JsonWriter.Serialize(summaryData);
        return value;
    }

    private string CreateJsonSpawnEventData(Vector3 in_spawnPoint, TroopAI in_troop)
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

    private string CreateJsonTargetEventData(TroopAI in_troop, int in_targetID, int in_targetTeamID)
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

    private string CreateJsonDestroyEventData(int in_entityID, int in_teamID)
    {
        Dictionary<string, object> eventData = new Dictionary<string, object>();
        eventData.Add("eventId", (int)EventId.Destroy);
        eventData.Add("frameId", GameManager.Instance.SessionManager.FrameID);
        eventData.Add("troopID", in_entityID);
        eventData.Add("teamID", in_teamID);
        string value = JsonWriter.Serialize(eventData);
        return value;
    }

    private string CreateJsonIdsEventData()
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

    private string CreateJsonEntityData(bool in_isDataNew)
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

    private string CreateJsonCurrencyEntityData()
    {
        Dictionary<string, object> entityInfo = new Dictionary<string, object>();
        entityInfo.Add("gold", GameManager.Instance.CurrentUserInfo.GoldAmount);
        string value = JsonWriter.Serialize(entityInfo);
        return value;
    }

    private string CreateACLJson(int aclLevel = 2)
    {
        Dictionary<string, object> aclInfo = new Dictionary<string, object>();
        aclInfo.Add("other", aclLevel);
        string value = JsonWriter.Serialize(aclInfo);
        return value;
    }
}
