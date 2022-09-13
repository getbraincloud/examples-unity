using System;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using BrainCloud.JsonFx.Json;
using UnityEngine;

public class BrainCloudManager : MonoBehaviour
{
    
    private BrainCloudWrapper _bcWrapper;
    private bool _dead = false;
    public bool LeavingGame;
    public BrainCloudWrapper Wrapper => _bcWrapper;
    public static BrainCloudManager Instance;
    private bool _isNewPlayer = false;
    public int _defaultRating = 1000;
    public long _findPlayersRange;
    public long _numberOfMatches;

    private string _playbackStreamId;
    private long _incrementRatingAmount = 100;
    private long _decrementRatingAmount = 50;
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
    
    // Uninitialize brainCloud
    void UninitializeBC()
    {
        if (_bcWrapper != null)
        {
            _bcWrapper.Client.ShutDown();
        }
    }

    private void OnApplicationQuit()
    {
        UninitializeBC();
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
        _bcWrapper.MatchMakingService.SetPlayerRating(_defaultRating);
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
        //ToDo: Create a seperate subscreen condition here to hide the lobby screen and show in text "no players found"
        //Or enable a text that says that with the list empty. 
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

        _dead = true;

        string message = cbObject as string;

        MenuManager.Instance.AbortToSignIn($"Message: {message} |||| JSON: {jsonError}");

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
        GameManager.Instance.CurrentUserInfo.ShieldTime = (int) data["shieldExpiry"];
        
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
        _bcWrapper.PlaybackStreamService.EndStream(_playbackStreamId);
        _bcWrapper.OneWayMatchService.CompleteMatch(_playbackStreamId);
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
        string eventData = CreateJsonSpawnEventData(in_spawnPoint, in_troop);
        string summaryData = CreateSummaryData();
        _bcWrapper.PlaybackStreamService.AddEvent(_playbackStreamId, eventData, summaryData, OnRecordSuccess, OnFailureCallback);
    }

    private void OnRecordSuccess(string in_jsonResponse, object cbObject)
    {
        //We did it.
    }

    public void StartMatch()
    {
        var opponentId = GameManager.Instance.OpponentUserInfo.ProfileId;
        _bcWrapper.OneWayMatchService.StartMatch(opponentId, 1000, OnStartMatchSuccess, OnFailureCallback);
        _bcWrapper.PlaybackStreamService.StartStream(opponentId,true, null, OnFailureCallback);
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
        PlayerPrefs.SetString("PlaybackKey", _playbackStreamId);
        GameManager.Instance.LoadToGame();
    }

    public void ReadStream()
    {
        _bcWrapper.PlaybackStreamService.ReadStream(_playbackStreamId, OnReadStreamSuccess, OnFailureCallback);
    }

    private void OnReadStreamSuccess(string in_jsonResponse, object cbObject)
    {
        //We did it
        //GameManager.Instance.SessionManager.LoadPlayback(in_jsonResponse);
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
