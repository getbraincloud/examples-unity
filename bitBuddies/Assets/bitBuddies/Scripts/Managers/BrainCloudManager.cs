using BrainCloud;
using BrainCloud.Entity;
using BrainCloud.JSONHelper;
using System;
using System.Collections.Generic;
using Gameframework;
using BrainCloud;
using BrainCloud.JsonFx.Json;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class AppChildrenInfo
{
    public string profileName { get; set; }
    public string profileId { get; set; }
    public Dictionary<string, object> summaryFriendData { get; set; }
    public int buddyBling { get; set; }
    public int buddyLove { get; set; }
}

public class BrainCloudManager : SingletonBehaviour<BrainCloudManager>
{
    public static BrainCloudClient Client => Wrapper != null ? Wrapper.Client : null;
    private bool _reconnectUser;
    private List<AppChildrenInfo> appChildrenInfos = new List<AppChildrenInfo>();
    public List<AppChildrenInfo> AppChildrenInfos
    {
        get { return appChildrenInfos; }
    }
    public static BrainCloudWrapper Wrapper { get; private set; } 
    [SerializeField] private UserInfo _userInfo;
    public UserInfo UserInfo
    {
        get => _userInfo;
        set {_userInfo = value;}
    }

    private bool _isProcessing;
    public bool IsProcessingRequest
    {
        get { return _isProcessing; }
    }

    public override void StartUp()
    {
        Wrapper = gameObject.AddComponent<BrainCloudWrapper>();
        Wrapper.Init();
    }
    
    public bool CanReconnectUser()
    {
        return Wrapper.CanReconnect();
    }
    
    public void ReconnectUser()
    {
        _isProcessing = true;
        _reconnectUser = true;
        Wrapper.Reconnect
        (
            HandleSuccess("Authenticate Success", OnAuthenticateSuccess), 
            HandleFailure("Authenticate Failed", OnFailureCallback)
        );
    }
    
    public void OnAuthenticateSuccess(string jsonResponse)
    {
        //Check if user manually logged in or reconnected,
        //if reconnected then assign the values..
        var data = jsonResponse.Deserialize("data");

        var username = data["playerName"] as string;
        if(username.IsNullOrEmpty() && !UserInfo.Username.IsNullOrEmpty())
        {
            Wrapper.PlayerStateService.UpdateName(UserInfo.Username);
        }
        else
        {
            UserInfo.UpdateUsername(username);
        }
            
        var email = data["emailAddress"] as string;
        if(email.IsNullOrEmpty() && !UserInfo.Email.IsNullOrEmpty())
        {
            Wrapper.PlayerStateService.UpdateContactEmail(UserInfo.Email);
        }
        else 
        {
            UserInfo.UpdateEmail(email);
        }
        Dictionary<string, object> scriptData = new Dictionary<string, object> {{"childAppId", BrainCloudConsts.APP_CHILD_ID}};
        Wrapper.ScriptService.RunScript
        (
            BrainCloudConsts.GET_CHILD_ACCOUNTS_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("Getting Child Accounts Success", OnGetChildAccounts),
            HandleFailure("Getting Child Accounts Failed", OnFailureCallback)
        );
    }
    
    private void OnGetChildAccounts(string jsonResponse)
    {
        /*
         * {"packetId":1,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"evaluateTime":6885,"scriptSize":3435},
         * "response":{"data":{"children":[{"profileName":"","profileId":"94ad75fc-bba7-4c34-a7be-083c07419a41","appId":"49162",
         * "summaryFriendData":null}]},"status":200},"success":true,"reasonCode":null},"status":200}]}
         */
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var firstData =  packet["data"] as Dictionary<string, object>;
        var response = firstData["response"] as Dictionary<string, object>;
        var secondData = response["data"] as Dictionary<string, object>;

        appChildrenInfos = new List<AppChildrenInfo>();
        var children = secondData["children"] as Dictionary<string, object>[];
        foreach (var child in children)
        {
            var childData = new AppChildrenInfo();
            childData.profileName = child["profileName"] as string;
            childData.profileId = child["profileId"] as string;
            childData.summaryFriendData = child["summaryFriendData"] as Dictionary<string, object>;
            appChildrenInfos.Add(childData);
        }

        if (appChildrenInfos.Count == 0 || appChildrenInfos[0].profileId.IsNullOrEmpty())
        {
            Debug.LogError("Child Profile ID is missing. Cant fetch data.");
            return;
        }
        
        Dictionary<string, object> scriptData = new Dictionary<string, object>
        {
            {"childAppId", BrainCloudConsts.APP_CHILD_ID},
            {"childProfileId", appChildrenInfos[0].profileId}
        };
        
        //Get data from cloud code scripts
        Wrapper.ScriptService.RunScript
        (
            BrainCloudConsts.GET_STATS_SCRIPT_NAME, 
            scriptData.Serialize(), 
            HandleSuccess("Stats Retrieved", OnGetStatsSuccess), 
            HandleFailure("Getting Stats Failed", OnFailureCallback)
        );
            
        Wrapper.ScriptService.RunScript
        (
            BrainCloudConsts.GET_CURRENCIES_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("Get Currencies Success", OnGetCurrenciesSuccess),
            HandleFailure("Getting Currencies Failed", OnFailureCallback)        
        );
    }
    
    private void OnGetStatsSuccess(string jsonResponse, object cbObject)
    {
        Dictionary<string, object> packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        Dictionary<string, object> data = packet["data"] as Dictionary<string, object>;
        Dictionary<string, object> response = data["response"] as Dictionary<string, object>;
        
        var parentStats = response["parentStats"] as Dictionary<string, object>;
        var statistics = parentStats["statistics"] as Dictionary<string, object>; 
        UserInfo.UpdateLevel((int) statistics["Level"]);
        if(UserInfo.Coins > 0)
        {
            CompletedGettingCurrencies();
        }
    }
    
    private void OnGetCurrenciesSuccess(string jsonResponse, object cbObject)
    {
        Dictionary<string, object> packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        Dictionary<string, object> data = packet["data"] as Dictionary<string, object>;
        Dictionary<string, object> response = data["response"] as Dictionary<string, object>;
        
        /*
         * {"packetId":1,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"evaluateTime":18707,"scriptSize":4017},
         * "response":{"parentStats":{"statistics":{"Level":3}}},"success":true,"reasonCode":null},"status":200},
         * {"data":{"runTimeData":{"hasIncludes":true,"evaluateTime":13287,"scriptSize":3708},"response":{},
         * "success":true,"reasonCode":null},"status":200}]}
         */
        
        var gemsInfo = response["Gems"] as Dictionary<string, object>;
        UserInfo.UpdateGems((int) gemsInfo["balance"]);
        var coinsInfo = response["Coins"] as Dictionary<string, object>;
        UserInfo.UpdateCoins((int) coinsInfo["balance"]);
        if(UserInfo.Level > 0)
        {
            CompletedGettingCurrencies();
        }
    }
    
    private void CompletedGettingCurrencies()
    {
        _isProcessing = false;
        StateManager.Instance.RefreshScreen();
    }
    
    public void RewardCoinsToParent(int in_coins)
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object> {{"increaseAmount", in_coins}};
        Wrapper.ScriptService.RunScript
        (
            BrainCloudConsts.AWARD_COINS_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("RewardCoinsToParent Success", OnRewardCoinsToParent),
            HandleFailure("RewardCoinsToParent Failed", OnFailureCallback)
        );
    }
    
    private void OnRewardCoinsToParent(string jsonResponse, object cbObject)
    {
        /*
         * {"packetId":4,"responses":[{"data":{"runTimeData":{"hasIncludes":false,"evaluateTime":16716,"scriptSize":284,"renderTime":3},
         * "response":{"getResult":{"data":{"currencyMap":{"Gems":{"consumed":0,"balance":160,"purchased":0,"awarded":160,"revoked":0},
         * "Coins":{"consumed":0,"balance":200,"purchased":0,"awarded":200,"revoked":0}}},"status":200}},"success":true,"reasonCode":null},
         * "status":200}]}
         */
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var firstData =  packet["data"] as Dictionary<string, object>;
        var response = firstData["response"] as Dictionary<string, object>;
        var getResult = response["getResult"] as Dictionary<string, object>;
        var secondData = getResult["data"] as Dictionary<string, object>;
        var currencyMap = secondData["currencyMap"] as Dictionary<string, object>;
        var coins = currencyMap["Coins"] as Dictionary<string, object>;
        UserInfo.UpdateCoins((int) coins["balance"]);
        StateManager.Instance.RefreshScreen();
    }
    
    public void RewardGemsToParent(int in_gems)
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object> {{"increaseAmount", in_gems}};
        Wrapper.ScriptService.RunScript
        (
            BrainCloudConsts.AWARD_GEMS_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("RewardGemsToParent Success", OnRewardGemsToParent),
            HandleFailure("RewardGemsToParent Failed", OnFailureCallback)
        );
    }
    
    private void OnRewardGemsToParent(string jsonResponse, object cbObject)
    {
        /*
         * {"packetId":3,"responses":[{"data":{"runTimeData":{"hasIncludes":false,"evaluateTime":13247,"scriptSize":283,"renderTime":4},
         * "response":{"getResult":{"data":{"currencyMap":{"Gems":{"consumed":0,"balance":160,"purchased":0,"awarded":160,"revoked":0},
         * "Coins":{"consumed":0,"balance":100,"purchased":0,"awarded":100,"revoked":0}}},"status":200}},"success":true,"reasonCode":null},
         * "status":200}]}
         */
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var firstData =  packet["data"] as Dictionary<string, object>;
        var response = firstData["response"] as Dictionary<string, object>;
        var getResult = response["getResult"] as Dictionary<string, object>;
        var secondData = getResult["data"] as Dictionary<string, object>;
        var currencyMap = secondData["currencyMap"] as Dictionary<string, object>;
        var gems = currencyMap["Gems"] as Dictionary<string, object>;
        UserInfo.UpdateGems((int) gems["balance"]);
        StateManager.Instance.RefreshScreen();
    }
    
    public void LevelUpParent()
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object> {{"Level", 1}};
        Wrapper.PlayerStatisticsService.IncrementUserStats
        (
            scriptData.Serialize(), 
            HandleSuccess("LevelUpParent Success", OnLevelUpParent),
            HandleFailure("LevelUpParent Failed", OnFailureCallback)
        );
    }
    
    private void OnLevelUpParent(string jsonResponse, object cbObject)
    {
        /*
         * {"packetId":2,"responses":[{"data":{"rewardDetails":{},"currency":{},"rewards":{},"statistics":{"Level":3}},"status":200}]}
         */
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var firstData =  packet["data"] as Dictionary<string, object>;
        var statistics = firstData["statistics"] as Dictionary<string, object>;
        UserInfo.UpdateLevel((int) statistics["Level"]);
        StateManager.Instance.RefreshScreen();
    }
    
    public void AddBuddy()
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object> {{"childAppId", BrainCloudConsts.APP_CHILD_ID}};
        Wrapper.ScriptService.RunScript
        (
            BrainCloudConsts.ADD_CHILD_ACCOUNT_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("Add Buddy Success", OnAddBuddy),
            HandleFailure("Add Buddy Failed", OnFailureCallback)
        );
    }
    
    private void OnAddBuddy(string jsonResponse)
    {
        
    }
    
    private void OnFailureCallback()
    {
    
    }

    #region Callback Creation Helpers

    /// <summary>
    /// Creates a callback used for various brainCloud API calls for when they return as a success.
    /// This will also format a log into the console with all the relevant information.
    /// </summary>
    /// <param name="logMessage">Optional information to provide context on the success.</param>
    /// <param name="onSuccess">Optional callback to invoke after successful API calls.</param>
    public static SuccessCallback HandleSuccess(string logMessage = "", Action onSuccess = null) =>
        InternalHandleSuccess(logMessage, onSuccess?.Target, onSuccess != null ? (_, _) => onSuccess.Invoke() : null);

    /// <summary>
    /// Creates a callback used for various brainCloud API calls for when they return as a success.
    /// This will also format a log into the console with all the relevant information and as
    /// well as invoke the onSuccess Action with the JSON response.
    /// </summary>
    /// <param name="logMessage">Optional information to provide context on the success.</param>
    /// <param name="onSuccessS">Optional callback to invoke after successful API calls which passes the JSON response.</param>
    public static SuccessCallback HandleSuccess(string logMessage = "", Action<string> onSuccessS = null) =>
        InternalHandleSuccess(logMessage, onSuccessS?.Target, onSuccessS != null ? (jsonResponse, _) => onSuccessS.Invoke(jsonResponse) : null);

    /// <summary>
    /// Creates a callback for various brainCloud API calls for when they return as a success.
    /// This will also format a log into the console with all the relevant information and as
    /// well as invoke the onSuccess Action with the JSON response and the callback object.
    /// </summary>
    /// <param name="logMessage">Optional information to provide context on the success.</param>
    /// <param name="onSuccessSO">Optional callback to invoke after successful API calls which passes the JSON response and the callback object.</param>
    public static SuccessCallback HandleSuccess(string logMessage = "", Action<string, object> onSuccessSO = null) =>
        InternalHandleSuccess(logMessage, onSuccessSO?.Target, onSuccessSO);

    /// <summary>
    /// Creates a callback for various brainCloud API calls for when they return as a failure.
    /// This will also format a log into the console with all the relevant information.
    /// </summary>
    /// <param name="errorMessage">Optional information to provide context on the failure.</param>
    /// <param name="onFailure">Optional callback to invoke after failed API calls.</param>
    public static FailureCallback HandleFailure(string errorMessage = "", Action onFailure = null) =>
        InternalHandleFailure(errorMessage, onFailure?.Target, onFailure != null ? (_, _) => onFailure.Invoke() : null);

    /// <summary>
    /// Creates a callback for various brainCloud API calls for when they return as a failure.
    /// This will also format a log into the console with all the relevant information and as
    /// well as invoke the onFailure Action with an <see cref="ErrorResponse"/>.
    /// </summary>
    /// <param name="errorMessage">Optional information to provide context on the failure.</param>
    /// <param name="onFailureER">Optional callback to invoke after failed API calls which contains the JSON error.</param>
    public static FailureCallback HandleFailure(string errorMessage, Action<ErrorResponse> onFailureER = null) =>
        InternalHandleFailure(errorMessage, onFailureER?.Target, onFailureER != null ? (jsonError, _) => onFailureER.Invoke(jsonError) : null);

    /// <summary>
    /// Creates a callback for various brainCloud API calls for when they return as a failure.
    /// This will also format a log into the console with all the relevant information and as
    /// well as invoke the onFailure Action with an <see cref="ErrorResponse"/> and the callback object.
    /// </summary>
    /// <param name="errorMessage">Optional information to provide context on the failure.</param>
    /// <param name="onFailureERO">Optional callback to invoke after failed API calls which passes the JSON error and the callback object.</param>
    public static FailureCallback HandleFailure(string errorMessage, Action<ErrorResponse, object> onFailureERO = null) =>
        InternalHandleFailure(errorMessage, onFailureERO?.Target, onFailureERO);

    private static SuccessCallback InternalHandleSuccess(string logMessage, object targetObject, Action<string, object> onSuccess)
    {
        logMessage = string.IsNullOrWhiteSpace(logMessage) ? "Success" : logMessage;
        return (jsonResponse, cbObject) =>
        {
            cbObject ??= targetObject;
            string cbObjectName = cbObject != null ? cbObject.GetType().Name : string.Empty;
            if (cbObjectName.Contains("DisplayClass")) // Generated Class
            {
                cbObject = null;
            }
            else if (!string.IsNullOrWhiteSpace(cbObjectName))
            {
                logMessage = $"{cbObjectName}: {logMessage}";
            }

#if UNITY_EDITOR
            logMessage = $"{logMessage}\nJSON Response:\n{jsonResponse}";
            if (cbObject is MonoBehaviour mbObject)
            {
                Debug.Log(logMessage, mbObject);
            }
            else
            {
                Debug.Log(logMessage);
            }
#else
            Debug.Log($"{logMessage}\nJSON Response:\n{jsonResponse}");
#endif

            onSuccess?.Invoke(jsonResponse, cbObject);
        };
    }

    private static FailureCallback InternalHandleFailure(string errorMessage, object targetObject, Action<ErrorResponse, object> onFailure = null)
    {
        errorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Failure" : errorMessage;
        return (status, reasonCode, jsonError, cbObject) =>
        {
            cbObject ??= targetObject;
            string cbObjectName = cbObject != null ? cbObject.GetType().Name : string.Empty;
            if (cbObjectName.Contains("DisplayClass")) // Generated Class
            {
                cbObject = null;
            }
            else if (!string.IsNullOrWhiteSpace(cbObjectName))
            {
                errorMessage = $"{cbObjectName}: {errorMessage}";
            }

#if UNITY_EDITOR
            errorMessage = $"{errorMessage} - Status: {status} - Reason: {reasonCode}\nJSON Response:\n{jsonError}";
            if (cbObject is MonoBehaviour mbObject)
            {
                Debug.LogError(errorMessage, mbObject);
            }
            else
            {
                Debug.LogError(errorMessage);
            }
#else
            Debug.Log($"{errorMessage} - Status: {status} - Reason: {reasonCode}\nJSON Response:\n{jsonError}");
#endif

            onFailure?.Invoke(jsonError.Deserialize<ErrorResponse>(), cbObject);
        };
    }

#endregion
}
