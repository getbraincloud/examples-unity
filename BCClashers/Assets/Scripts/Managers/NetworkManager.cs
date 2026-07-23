using System;
using System.Collections.Generic;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using BrainCloud.JsonFx.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

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

    public BrainCloudWrapper Wrapper
    {
        get => _bcWrapper;
    }
    public static NetworkManager Instance;
    private bool _isNewPlayer;
    private int _defaultRating = 1200;
    private long _findPlayersRange = 10000;
    private long _numberOfMatches = 20;
    private string _playbackStreamId;
    private long _incrementRatingAmount = 100;
    private long _decrementRatingAmount = 50;
    //How many matches to pull for each dashboard history panel. The cards only show the first
    //few, but My Stats aggregates over the whole window - so this doubles as the sample size
    //behind the stats panel. See MatchHistoryStats: these are "last N", NOT lifetime totals.
    private int _recentMatchCount = 50;
    private const string UNKNOWN_OPPONENT_NAME = "Unknown";
    //Gold wagered on the current raid. Captured at StartMatch, which runs in the MENU scene:
    //the price list lives on MenuManager, and MenuManager (unlike GameManager/NetworkManager)
    //is not DontDestroyOnLoad, so it is gone by the time the match ends in the Game scene.
    private int _currentMatchStake;
    private bool _dead;
    private bool _shieldActive;
    private bool _didInvadersWin;
    private string _invadedPlaybackID;
    //Difficulty of defense to raid. ANY_DEFENDER_RANK matches every difficulty.
    public const int ANY_DEFENDER_RANK = -1;
    private int _defenderRankFilter = ANY_DEFENDER_RANK;

    private static string _currencyType = "gold";
    private static int _startingGold = 100000;

    //Fallback only. The real round length is GameSessionManager.RoundDuration (set in the
    //inspector), but that only exists while the Game scene is loaded - the menu reads streams
    //without it, so a match recorded before durationSecs was stamped still needs a denominator.
    private const float DEFAULT_MATCH_LENGTH_SECONDS = 180f;

    private float MatchLengthSeconds()
    {
        GameSessionManager session = GameManager.Instance.SessionManager;
        return session != null ? session.RoundDuration : DEFAULT_MATCH_LENGTH_SECONDS;
    }

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
            if (_bcWrapper.Client.Authenticated)
            {
                _bcWrapper.LogoutOnApplicationQuit(false);
            }
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

    public void Reconnect()
    {
        _bcWrapper.Reconnect(HandlePlayerState, OnFailureCallback);
    }

    public void SignOut()
    {
        PlayerPrefs.DeleteAll();
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

        PublishDefenseSummary();
    }

    //Summary friend data is the only player data the matchmaking filter sees, so publish our
    //defense there or we stay invisible to other players' searches.
    public void PublishDefenseSummary()
    {
        _bcWrapper.PlayerStateService.UpdateSummaryFriendData
        (
            CreateJsonDefenseSummaryData(),
            null,
            OnFailureCallback
        );
    }

    //Called from a Unity Dropdown to pick which defense difficulty to raid.
    public void SetDefenderRankFilter(int in_defenderRank)
    {
        _defenderRankFilter = in_defenderRank;
    }

    public void LookForPlayers()
    {
        _bcWrapper.MatchMakingService.EnableMatchMaking(OnEnableMatchMaking, OnFoundPlayersError);
    }

    private void OnEnableMatchMaking(string jsonResponse, object cbObject)
    {
        //FilterOneWayMultiplayer drops candidates with no raidable defense, server-side.
        _bcWrapper.MatchMakingService.FindPlayersUsingFilter
        (
            _findPlayersRange,
            _numberOfMatches,
            CreateJsonMatchFilterParms(),
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
            _bcWrapper.PlayerStateService.UpdateUserName(tempUsername, OnLoggedIn, OnFailureCallback,
                "Failed to update username to braincloud");
        }
        else
        {
            //Checking if playerName field has a real value to read in, if so we move on to checking the user entity
            userInfo.Username = data["playerName"] as string;
            if (userInfo.Username.IsNullOrEmpty())
            {
                userInfo.Username = tempUsername;
                _bcWrapper.PlayerStateService.UpdateUserName(userInfo.Username, OnLoggedIn, OnFailureCallback,
                    "Failed to update username to braincloud");
            }
            else
            {
                OnLoggedIn(null, null);
            }
        }

        if (!MenuManager.Instance.RememberMeToggle.isOn)
        {
            _bcWrapper.ResetStoredProfileId();
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

    //Price of the currently selected invader force. Only valid while the MainMenu scene is
    //loaded (see _currentMatchStake). Guarded because ArmyDivisionRank has None/Test entries
    //that have no matching price.
    private int GetSelectedInvaderPrice()
    {
        List<int> prices = MenuManager.Instance.PriceOfInvaders;
        int index = (int) GameManager.Instance.CurrentUserInfo.InvaderSelected;
        return index >= 0 && index < prices.Count ? prices[index] : 0;
    }

    public void DecreaseGoldAmountForInvaderSelection()
    {
        var decrementAmount = GetSelectedInvaderPrice();
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
                //Backfill for profiles created before the matchmaking filter existed.
                PublishDefenseSummary();
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
                    //Backfill for profiles created before the matchmaking filter existed.
                    PublishDefenseSummary();
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
            _recentMatchCount,
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

        //Same response, second reading: the full "Recent Invasions" list for the dashboard.
        GameManager.Instance.RecentInvasions = ParseRecentStreams(jsonResponse, false);

        //Chain the other half of the history ("My Recent Attacks"), which finishes the login flow.
        GetRecentAttacks();
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
        //A brand new player starts on the Easy defense, so they are raidable straight away.
        PublishDefenseSummary();
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
        RecordMatchResultToStats(in_didPlayerWin);
        PlayerPrefs.SetString("PlaybackKey", _playbackStreamId);
    }

    //Redesign: hand the finished raid to the RecordMatchResult cloud script, which writes the
    //user statistics for BOTH players (see the script header). Attacker-only path - GameCompleted
    //never runs in playback mode. No-ops harmlessly server-side until the clashers_* stats are
    //defined in the portal, so it is safe to ship ahead of that step.
    private void RecordMatchResultToStats(bool in_didAttackerWin)
    {
        int startingStructures = GameManager.Instance.StartingStructureCount;
        float damageFraction = startingStructures > 0
            ? Mathf.Clamp01((float) _structureKillCount / startingStructures)
            : 0f;

        UserInfo defender = GameManager.Instance.OpponentUserInfo;

        Dictionary<string, object> scriptData = new Dictionary<string, object>();
        scriptData.Add("damagePercent", Mathf.Clamp(Mathf.RoundToInt(damageFraction * 100f), 0, 100));
        scriptData.Add("damageGold", Mathf.RoundToInt(damageFraction * _currentMatchStake));
        scriptData.Add("durationSecs", MatchLengthSeconds() - timeLeft);
        scriptData.Add("didAttackerWin", in_didAttackerWin);
        scriptData.Add("attackerRating", GameManager.Instance.CurrentUserInfo.Rating);
        scriptData.Add("defenderProfileId", defender != null ? defender.ProfileId : "");
        scriptData.Add("defenderRank", (int) GameManager.Instance.DefenderRank);

        _bcWrapper.ScriptService.RunScript("RecordMatchResult", JsonWriter.Serialize(scriptData), null, OnFailureCallback);
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
        //Capture the wager now, while MenuManager still exists, for the end-of-match summary.
        _currentMatchStake = GetSelectedInvaderPrice();
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
        ReadStreamById(GameManager.Instance.InvadedStreamInfo.PlaybackStreamID);
    }

    //Replay any match by id - used by the "Watch" button on the dashboard's match-history cards.
    public void ReadStreamById(string in_playbackStreamId)
    {
        if (in_playbackStreamId.IsNullOrEmpty())
        {
            Debug.LogWarning("No playback stream id supplied, cannot replay this match.");
            return;
        }
        _bcWrapper.PlaybackStreamService.ReadStream(in_playbackStreamId, OnReadStreamSuccess, OnFailureCallback);
    }

    // -----------------------------
    // Redesign: dashboard match history
    // -----------------------------

    /// <summary>
    /// Re-read both history panels.
    ///
    /// The login chain reads the streams once (OnReadMatchMaking -> OnGetRecentStreams), but
    /// returning from a raid re-loads the MainMenu scene WITHOUT re-authenticating, so without
    /// this the dashboard would keep showing the history from before the match just played -
    /// the match you just finished would not appear until the next login.
    /// </summary>
    public void RefreshMatchHistory()
    {
        if (!IsSessionValid()) return;

        _bcWrapper.PlaybackStreamService.GetRecentStreamsForTargetPlayer
        (
            GameManager.Instance.CurrentUserInfo.ProfileId,
            _recentMatchCount,
            OnGetRecentStreams,
            OnFailureCallback
        );
    }

    //"My Recent Attacks" - the matches this player initiated.
    private void GetRecentAttacks()
    {
        _bcWrapper.PlaybackStreamService.GetRecentStreamsForInitiatingPlayer
        (
            GameManager.Instance.CurrentUserInfo.ProfileId,
            _recentMatchCount,
            OnGetRecentAttacks,
            OnFailureCallback
        );
    }

    private void OnGetRecentAttacks(string jsonResponse, object cbObject)
    {
        GameManager.Instance.RecentAttacks = ParseRecentStreams(jsonResponse, true);

        //Bind now with the stream-derived numbers, then read the real lifetime statistics -
        //when they arrive the panel re-binds and overlays them (see MatchHistoryStats).
        MenuManager.Instance.UpdateMatchHistoryPanels();
        MenuManager.Instance.UpdateMainMenu();
        MenuManager.Instance.IsLoading = false;

        ReadUserStatistics();
    }

    //"My Stats" lifetime totals, written server-side by RecordMatchResult.
    public void ReadUserStatistics()
    {
        _bcWrapper.PlayerStatisticsService.ReadAllUserStats(OnReadUserStatistics, OnFailureCallback);
    }

    private void OnReadUserStatistics(string jsonResponse, object cbObject)
    {
        GameManager.Instance.UserStatistics = null;
        if (JsonReader.Deserialize(jsonResponse) is Dictionary<string, object> response &&
            response["data"] is Dictionary<string, object> data &&
            data["statistics"] is Dictionary<string, object> stats)
        {
            GameManager.Instance.UserStatistics = stats;
        }

        //Re-bind the stats panel with the lifetime overlay applied (no-op if nothing is defined).
        MenuManager.Instance.UpdateMatchHistoryPanels();
    }

    /// <summary>
    /// Turns a GET_RECENT_STREAMS_* response into dashboard cards.
    ///
    /// Two things worth knowing:
    ///  - The profile ids are read off the STREAM (initiatingPlayerId/targetPlayerId) rather than
    ///    the summary, because they are always present - the summary fields are not.
    ///  - The summary is overwritten by every AddEvent during a match (see CreateSummaryData),
    ///    and only becomes the end-game blob on the final event. We therefore only surface
    ///    streams carrying "stake", which is stamped exclusively by CreateEndGameSummaryData.
    ///    That skips both in-progress/abandoned matches and matches recorded before this
    ///    redesign - neither can populate a card without inventing an outcome.
    /// </summary>
    private List<MatchSummary> ParseRecentStreams(string jsonResponse, bool in_isAttack)
    {
        var matches = new List<MatchSummary>();

        if (JsonReader.Deserialize(jsonResponse) is not Dictionary<string, object> response) return matches;
        if (response["data"] is not Dictionary<string, object> data) return matches;
        if (data["streams"] is not Dictionary<string, object>[] streams) return matches;

        foreach (Dictionary<string, object> stream in streams)
        {
            if (GetValue(stream, "summary") is not Dictionary<string, object> summary) continue;
            if (!summary.ContainsKey("stake")) continue;

            var match = new MatchSummary();
            match.IsAttack = in_isAttack;
            match.PlaybackStreamId = ToStr(GetValue(stream, "playbackStreamId"));
            match.OpponentProfileId = ToStr(GetValue(stream, in_isAttack ? "targetPlayerId" : "initiatingPlayerId"));
            match.OccurredAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(ToLong(GetValue(stream, "createdAt"))).UtcDateTime;

            //The opponent is whichever side of the match we are not.
            match.OpponentName = ToStr(GetValue(summary, in_isAttack ? "defenderName" : "attackerName"));
            if (match.OpponentName.IsNullOrEmpty())
            {
                match.OpponentName = UNKNOWN_OPPONENT_NAME;
            }
            match.OpponentRating = ToInt(GetValue(summary, in_isAttack ? "defenderRating" : "attackerRating"));
            //...and MY rating is whichever side of the summary we ARE.
            match.MyRating = ToInt(GetValue(summary, in_isAttack ? "attackerRating" : "defenderRating"));
            match.DidAttackerWin = GetValue(summary, "didInvadersWin") is bool won && won;
            //Absent must mean None, not Easy(0) - otherwise matches recorded before defenderRank
            //was stamped would all be counted against the Line layout in the breach breakdown.
            match.DefenderRank = summary.ContainsKey("defenderRank")
                ? (ArmyDivisionRank) ToInt(GetValue(summary, "defenderRank"))
                : ArmyDivisionRank.None;

            match.Stake = ToInt(GetValue(summary, "stake"));
            match.DamageGold = ToInt(GetValue(summary, "damageGold"));
            match.DurationSeconds = summary.ContainsKey("durationSecs")
                ? ToFloat(GetValue(summary, "durationSecs"))
                : MatchLengthSeconds() - ToFloat(GetValue(summary, "timeLeft"));

            matches.Add(match);
        }

        //Newest first, matching the design.
        matches.Sort((a, b) => b.OccurredAtUtc.CompareTo(a.OccurredAtUtc));
        return matches;
    }

    private static object GetValue(Dictionary<string, object> dict, string key)
        => dict != null && dict.ContainsKey(key) ? dict[key] : null;

    //JsonFx boxes numbers as int/long/double depending on magnitude (the same reason the shield
    //expiry read below is wrapped in a try/catch), so convert rather than hard-cast.
    private static string ToStr(object value) => value as string ?? "";
    private static int ToInt(object value) => value == null ? 0 : Convert.ToInt32(value);
    private static long ToLong(object value) => value == null ? 0L : Convert.ToInt64(value);
    private static float ToFloat(object value) => value == null ? 0f : Convert.ToSingle(value);

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

        //Redesign: the dashboard's match-history cards are built entirely from this blob, so
        //stamp BOTH sides of the match into it. The stream is always recorded by the attacker,
        //but it is read back from both ends (my attacks / invasions against me) - carrying both
        //identities means neither panel needs a follow-up per-opponent lookup.
        UserInfo attacker = GameManager.Instance.CurrentUserInfo;
        UserInfo defender = GameManager.Instance.OpponentUserInfo;

        int stake = _currentMatchStake;
        //Damage is "how much of the base fell", charged against the stake, so a levelled town
        //is exactly 100% of stake -> Total Victory / Town Destroyed on the badge legend.
        int startingStructures = GameManager.Instance.StartingStructureCount;
        float damageFraction = startingStructures > 0
            ? Mathf.Clamp01((float) _structureKillCount / startingStructures)
            : 0f;

        summaryData.Add("attackerName", attacker.Username);
        summaryData.Add("attackerRating", attacker.Rating);
        summaryData.Add("attackerProfileId", attacker.ProfileId);

        if (defender != null)
        {
            summaryData.Add("defenderName", defender.Username);
            summaryData.Add("defenderRating", defender.Rating);
            summaryData.Add("defenderProfileId", defender.ProfileId);
        }

        summaryData.Add("stake", stake);
        summaryData.Add("damageGold", Mathf.RoundToInt(damageFraction * stake));
        summaryData.Add("durationSecs", MatchLengthSeconds() - timeLeft);
        //Which layout was being defended. Drives the dashboard's "breaches by layout" breakdown.
        summaryData.Add("defenderRank", (int) GameManager.Instance.DefenderRank);

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

    private string CreateJsonDefenseSummaryData()
    {
        UserInfo user = GameManager.Instance.CurrentUserInfo;
        Dictionary<string, object> summaryInfo = new Dictionary<string, object>();
        summaryInfo.Add("hasDefense", user.DefendersSelected != ArmyDivisionRank.None);
        summaryInfo.Add("defenderRank", (int) user.DefendersSelected);

        return JsonWriter.Serialize(summaryInfo);
    }

    private string CreateJsonMatchFilterParms()
    {
        Dictionary<string, object> filterParms = new Dictionary<string, object>();
        filterParms.Add("requireDefense", true);
        filterParms.Add("defenderRank", _defenderRankFilter);

        return JsonWriter.Serialize(filterParms);
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
