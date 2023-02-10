using BrainCloud;
using BrainCloud.Entity;
using System;
using UnityEngine;

/// <summary>
/// <para>
/// Attach to a GameObject in the Unity Editor to automatically create a bridge between
/// <see cref="BrainCloudWrapper"/> and scripts that require access to the various brainCloud services.
/// This also gives you more direct access to BrainCloudWrapper's properties.
/// </para>
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
/// <seealso cref="BrainCloudWrapper"/><br></br>
/// <seealso cref="BrainCloudClient"/><br></br>
/// <seealso cref="UserHandler"/>
/// </summary>
public class BCManager : MonoBehaviour
{
    private static bool _isInstanced = false; // To keep track if BCManager has been Instantiated or not

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

    #region Callback Creation Helpers

    /// <summary>
    /// Creates a callback for various brainCloud API calls if they return as a success.
    /// This will also format a log into the console with all the relevant information.
    /// </summary>
    /// <param name="logMessage">Optional information to provide context on the success.</param>
    /// <param name="onSuccess">Optional callback to invoke from successful API calls.</param>
    /// <returns></returns>
    public static SuccessCallback CreateSuccessCallback(string logMessage = null, Action onSuccess = null)
    {
        logMessage = string.IsNullOrEmpty(logMessage) ? "Success" : logMessage;
        return (jsonResponse, _) =>
        {
            LogMessage(logMessage, jsonResponse);
            onSuccess?.Invoke();
        };
    }

    /// <summary>
    /// Creates a callback for various brainCloud API calls if they return as a failure.
    /// This will also format a log into the console with all the relevant information.
    /// </summary>
    /// <param name="errorMessage">Optional information to provide context on the failure.</param>
    /// <param name="onFailure">Optional callback to invoke from failed API calls.</param>
    /// <returns></returns>
    public static FailureCallback CreateFailureCallback(string errorMessage = null, Action onFailure = null)
    {
        errorMessage = string.IsNullOrEmpty(errorMessage) ? "Failure" : errorMessage;
        return (status, reasonCode, jsonError, _) =>
        {
            LogError(errorMessage, status, reasonCode, jsonError);
            onFailure?.Invoke();
        };
    }

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

        GameObject = gameObject;

        Wrapper = gameObject.AddComponent<BrainCloudWrapper>();
        Wrapper.WrapperName = AppName;
        Wrapper.Init(); // Init data is taken from the brainCloud Unity Plugin

        DontDestroyOnLoad(gameObject);

        _isInstanced = true;
    }

    private void OnApplicationQuit()
    {
        _isInstanced = false;
    }

    private void OnDestroy()
    {
        if (_isInstanced)
        {
            Debug.LogError($"{gameObject.name} (BCManager) has been destroyed during run time. brainCloud functions might become unstable. Was this intentional?");
            _isInstanced = false;
        }

        Wrapper = null;
        GameObject = null;
    }

    #endregion

    #region Additional Methods

    /// <summary>
    /// Logs brainCloud Messages in a consistent format.
    /// </summary>
    /// <param name="logMessage">Custom message to use as a header or to add additional information.</param>
    /// <param name="jsonResponse">The JSON response from the brainCloud server.</param>
    public static void LogMessage(string logMessage, string jsonResponse) =>
        Debug.Log($"{logMessage}\nJSON Response:\n{jsonResponse}");

    /// <summary>
    /// Logs brainCloud Errors in a consistent format.
    /// </summary>
    /// <param name="errorMessage">Custom error message to use as a header or to add additional information.</param>
    /// <param name="status">The status code returned from the brainCloud server.</param>
    /// <param name="reasonCode">The reason code returned from the brainCloud server.</param>
    /// <param name="jsonError">The JSON response from the brainCloud server.</param>
    public static void LogError(string errorMessage, int status, int reasonCode, string jsonError) =>
        Debug.LogError($"{errorMessage} - Status: {status} - Reason: {reasonCode}\nJSON Response:\n{jsonError}");

    #endregion
}
