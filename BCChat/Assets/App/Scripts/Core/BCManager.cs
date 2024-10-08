using BrainCloud;
using BrainCloud.Entity;
using BrainCloud.JSONHelper;
using BrainCloud.Plugin;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to a GameObject in the Unity Editor to automatically create a bridge between
/// <see cref="BrainCloudWrapper"/> and scripts that require access to the various brainCloud services.
/// This also gives you more direct access to BrainCloudWrapper's properties.
///
/// <para>
/// Despite living as a GameObject, <see cref="BCManager"/> will set up its properties to work as
/// static singletons to allow Unity components and other C# classes to grab references to services that
/// live within <see cref="BrainCloudClient"/>, which is handled by BrainCloudWrapper. Do not delete the
/// GameObject BCManager is attached to or unintended behaviour might arise from BrainCloudClient's services.
/// </para>
///
/// <para>
/// While all of BrainCloudClient's services are accessible thorugh here, it is recommended to cache
/// references to whichever service you need in the script for faster accessing in the IL/CPP source.
/// </para>
/// 
/// <br><seealso cref="BrainCloudWrapper"/></br>
/// <br><seealso cref="BrainCloudClient"/></br>
/// <br><seealso cref="UserHandler"/></br>
/// </summary>
public class BCManager : MonoBehaviour
{
    private static bool _isInstanced = false; // To keep track if BCManager has been Instantiated or not

    [SerializeField] private bool InitFromBCSettings = true;

    private static bool _rememberMeSetting = false;

    public static bool RememberMeSetting
    {
        get
        {
            return _rememberMeSetting;
        }
        private set
        {
            _rememberMeSetting = value;
        }
    }

    #region Wrapper Properties

    /// <summary>
    /// The App's productName in the PlayerSettings.
    /// </summary>
    public static string AppName => Application.productName;

    /// <summary>
    /// Get the GameObject directly. Useful if other manager-type components are
    /// attached to this GameObject or if a reference to the GameObject is needed.
    /// </summary>
    public static GameObject GameObject { get; private set; }

    /// <summary>
    /// brainCloud's services and functionality lives in here, which is a wrapper around the C# libraries.
    /// </summary>
    public static BrainCloudWrapper Wrapper { get; private set; }

    /// <summary>
    /// Access brainCloud services directly. Useful if the Wrapper doesn't expose something in particular.
    /// </summary>
    public static BrainCloudClient Client => Wrapper != null ? Wrapper.Client : null;

    #endregion

    #region Client Services

    public static BrainCloudEntity EntityService => Wrapper != null ? Wrapper.EntityService : null;

    public static BCEntityFactory EntityFactory => Wrapper != null ? Wrapper.EntityFactory : null;

    public static BrainCloudGlobalEntity GlobalEntityService => Wrapper != null ? Wrapper.GlobalEntityService : null;

    public static BrainCloudGlobalApp GlobalAppService => Wrapper != null ? Wrapper.GlobalAppService : null;

    public static BrainCloudPresence PresenceService => Wrapper != null ? Wrapper.PresenceService : null;

    public static BrainCloudVirtualCurrency VirtualCurrencyService => Wrapper != null ? Wrapper.VirtualCurrencyService : null;

    public static BrainCloudAppStore AppStoreService => Wrapper != null ? Wrapper.AppStoreService : null;

    public static BrainCloudPlayerStatistics PlayerStatisticsService => Wrapper != null ? Wrapper.PlayerStatisticsService : null;

    public static BrainCloudGlobalStatistics GlobalStatisticsService => Wrapper != null ? Wrapper.GlobalStatisticsService : null;

    public static BrainCloudIdentity IdentityService => Wrapper != null ? Wrapper.IdentityService : null;

    public static BrainCloudItemCatalog ItemCatalogService => Wrapper != null ? Wrapper.ItemCatalogService : null;

    public static BrainCloudUserItems UserItemsService => Wrapper != null ? Wrapper.UserItemsService : null;

    public static BrainCloudScript ScriptService => Wrapper != null ? Wrapper.ScriptService : null;

    public static BrainCloudMatchMaking MatchMakingService => Wrapper != null ? Wrapper.MatchMakingService : null;

    public static BrainCloudOneWayMatch OneWayMatchService => Wrapper != null ? Wrapper.OneWayMatchService : null;

    public static BrainCloudPlaybackStream PlaybackStreamService => Wrapper != null ? Wrapper.PlaybackStreamService : null;

    public static BrainCloudGamification GamificationService => Wrapper != null ? Wrapper.GamificationService : null;

    public static BrainCloudPlayerState PlayerStateService => Wrapper != null ? Wrapper.PlayerStateService : null;

    public static BrainCloudFriend FriendService => Wrapper != null ? Wrapper.FriendService : null;

    public static BrainCloudEvent EventService => Wrapper != null ? Wrapper.EventService : null;

    public static BrainCloudSocialLeaderboard SocialLeaderboardService => Wrapper != null ? Wrapper.SocialLeaderboardService : null;

    public static BrainCloudSocialLeaderboard LeaderboardService => Wrapper != null ? Wrapper.LeaderboardService : null;

    public static BrainCloudAsyncMatch AsyncMatchService => Wrapper != null ? Wrapper.AsyncMatchService : null;

    public static BrainCloudTime TimeService => Wrapper != null ? Wrapper.TimeService : null;

    public static BrainCloudTournament TournamentService => Wrapper != null ? Wrapper.TournamentService : null;

    public static BrainCloudGlobalFile GlobalFileService => Wrapper != null ? Wrapper.GlobalFileService : null;

    public static BrainCloudCustomEntity CustomEntityService => Wrapper != null ? Wrapper.CustomEntityService : null;

    public static BrainCloudPushNotification PushNotificationService => Wrapper != null ? Wrapper.PushNotificationService : null;

    public static BrainCloudPlayerStatisticsEvent PlayerStatisticsEventService => Wrapper != null ? Wrapper.PlayerStatisticsEventService : null;

    public static BrainCloudS3Handling S3HandlingService => Wrapper != null ? Wrapper.S3HandlingService : null;

    public static BrainCloudRedemptionCode RedemptionCodeService => Wrapper != null ? Wrapper.RedemptionCodeService : null;

    public static BrainCloudDataStream DataStreamService => Wrapper != null ? Wrapper.DataStreamService : null;

    public static BrainCloudProfanity ProfanityService => Wrapper != null ? Wrapper.ProfanityService : null;

    public static BrainCloudFile FileService => Wrapper != null ? Wrapper.FileService : null;

    public static BrainCloudGroup GroupService => Wrapper != null ? Wrapper.GroupService : null;

    public static BrainCloudMail MailService => Wrapper != null ? Wrapper.MailService : null;

    public static BrainCloudRTT RTTService => Wrapper != null ? Wrapper.RTTService : null;

    public static BrainCloudLobby LobbyService => Wrapper != null ? Wrapper.LobbyService : null;

    public static BrainCloudChat ChatService => Wrapper != null ? Wrapper.ChatService : null;

    public static BrainCloudMessaging MessagingService => Wrapper != null ? Wrapper.MessagingService : null;

    public static BrainCloudRelay RelayService => Wrapper != null ? Wrapper.RelayService : null;

    public static BrainCloudBlockchain BlockchainService => Wrapper != null ? Client.Blockchain : null;

    #endregion

    #region Unity Messages

    private void Awake()
    {
        if (_isInstanced)
        {
            Debug.LogError($"BCManager already exists!");
            Destroy(this);
            return;
        }

        if (InitFromBCSettings)
        {
            Wrapper = gameObject.AddComponent<BrainCloudWrapper>();
            Wrapper.WrapperName = AppName;
            Wrapper.Init();
        }

        GameObject = gameObject;
        DontDestroyOnLoad(GameObject);

        _isInstanced = true;
    }

    private void OnApplicationQuit()
    {
        _isInstanced = false;
        //if logged in, log out and delete profile ID and anonymous ID if remember me was selected
        if (Client.Authenticated)
        {
            Wrapper.LogoutOnApplicationQuit(!RememberMeSetting);
        }
    }

    private void OnDestroy()
    {
        if (_isInstanced)
        {
            Debug.LogError($"{gameObject.name} (BCManager) has been destroyed during run time. brainCloud functions might become unstable. Was this intentional?");
            _isInstanced = false;
        }

        InternalDismantleClient();
        GameObject = null;
    }

    #endregion

    public static void UpdateRememberMeSetting(bool value)
    {
        RememberMeSetting = value;
    }
    public static void SetupClient(string serverURL, string appID, string appSecret, string version, Dictionary<string, string> childApps = null)
    {
        if (!_isInstanced)
        {
            Debug.LogError($"BCManager does not exist! Cannot setup a new brainCloud Client.");
            return;
        }

        GameObject.GetComponent<BCManager>().InternalSetupClient(serverURL, appID, appSecret, version, childApps);
    }

    public static void DismantleClient()
        => GameObject.GetComponent<BCManager>().InternalDismantleClient();

    private void InternalSetupClient(string serverURL, string appID, string appSecret, string version, Dictionary<string, string> childApps = null)
    {
        Wrapper = gameObject.AddComponent<BrainCloudWrapper>();
        Wrapper.WrapperName = AppName;

        // Make sure to use the right braincloudservers.com URL
        if (serverURL.Contains("braincloudservers.com"))
        {
            serverURL = "https://" +
                         serverURL[serverURL.IndexOf("api.")..(serverURL.IndexOf("braincloudservers.com") + 21)] +
                         "/dispatcherv2";
        }

        if (!childApps.IsNullOrEmpty())
        {
            childApps.Add(appID, appSecret);
            Wrapper.InitWithApps(serverURL, appID, childApps, version);
        }
        else
        {
            Wrapper.Init(serverURL, appSecret, appID, version);
        }
    }

    private void InternalDismantleClient()
    {
        Wrapper?.Client?.FlushCachedMessages(false);
        Wrapper?.Client?.ResetCommunication();
        Wrapper?.Client?.DeregisterEventCallback();
        Wrapper?.Client?.DeregisterRewardCallback();
        Wrapper?.Client?.DeregisterFileUploadCallback();
        Wrapper?.Client?.DeregisterGlobalErrorCallback();
        Wrapper?.Client?.DeregisterNetworkErrorCallback();

        if (Wrapper != null)
        {
            Destroy(Wrapper);
        }

        Wrapper = null;
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

            ErrorResponse response = jsonError.Deserialize<ErrorResponse>();
#if UNITY_EDITOR
            errorMessage = $"{errorMessage} - {response.Message}\nJSON Response:\n{jsonError}";
            if (cbObject is MonoBehaviour mbObject)
            {
                Debug.LogError(errorMessage, mbObject);
            }
            else
            {
                Debug.LogError(errorMessage);
            }
#else
            Debug.LogError($"{errorMessage} - {response.Message}\nJSON Response:\n{jsonError}");
#endif

            onFailure?.Invoke(response, cbObject);
        };
    }

#endregion
}
