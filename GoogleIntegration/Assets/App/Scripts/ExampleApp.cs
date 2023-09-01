using BrainCloud;
using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using Firebase;
using Firebase.Messaging;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using BrainCloud.Plugin;

/// <summary>
/// 
/// </summary>
[RequireComponent(typeof(BrainCloudWrapper))]
public class ExampleApp : MonoBehaviour
{
    #region App Defines

    public enum PanelState
    {
        None,
        Login,
        Main,
        Store
    }

    // Firebase Messaging
    private const string TOPIC_BRAINCLOUD_EXAMPLE_PUSHNOTIFICATION = "/topics/brainCloud/example/PushNotification";
    private const string PLAYERPREFS_FIREBASE_TOKEN_KEY = TOPIC_BRAINCLOUD_EXAMPLE_PUSHNOTIFICATION + "." + "FIREBASE_TOKEN_KEY";

    // String Formats
    private const string USER_INFO_FORMAT = "Profile ID: {0}\nAnonymous ID: {1}";
    private const string APP_INFO_FORMAT = "{0} ({1}) v{2}";
    private const string BC_VERSION_FORMAT = "brainCloud v{0}";

    #endregion

    [Header("DEBUG")]
    [SerializeField] int DEBUG_BUILD_NUMBER = 0;
    [Space]

    [Header("UI Elements")]
    [SerializeField] private CanvasGroup MainCG = default;
    [SerializeField] private TMP_Text UserInfoText = default;
    [SerializeField] private TMP_Text AppInfoLabel = default;
    [SerializeField] private TMP_Text VersionInfoLabel = default;

    [Header("Panels")]
    [SerializeField] private LoginPanel LoginPanel = default;
    [SerializeField] private MainPanel MainPanel = default;
    [SerializeField] private StorePanel StorePanel = default;

    [Header("Firebase Messaging")]
    [SerializeField] private string NotificationTitle = "Notification Title";
    [SerializeField] private string NotificationBody = "Hello World from brainCloud!";
    [SerializeField] private string NotificationImageURL = string.Empty;

    public bool IsInteractable
    {
        get => MainCG.interactable;
        set => MainCG.interactable = value;
    }

    private BrainCloudWrapper BC = null; // How we will interact with the brainCloud client
    private FirebaseApp Firebase = null;

    #region Unity Messages

    private void Awake()
    {
        // Init BrainCloudWrapper
        BC = gameObject.GetComponent<BrainCloudWrapper>();
        BC.WrapperName = Application.productName;
        BC.Init();

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        IsInteractable = false;

        // Initial UI
        ChangePanelState(PanelState.Login);

        GetStoredUserIDs();
        AppInfoLabel.text = string.Format(APP_INFO_FORMAT, BC.WrapperName, BC.Client.AppId, BC.Client.AppVersion);
        VersionInfoLabel.text = string.Format(BC_VERSION_FORMAT, BC.Client.BrainCloudClientVersion);

        StartCoroutine(InitializeApp());
    }

    private void OnDestroy()
    {
        FirebaseMessaging.MessageReceived -= OnFirebaseMessageReceived;
        FirebaseMessaging.TokenReceived -= OnFirebaseTokenReceived;

        Firebase?.Dispose();
        Firebase = null;

        BC = null;
    }

    #endregion

    private IEnumerator InitializeApp()
    {
        Debug.Log($"<b>App Build Number: {DEBUG_BUILD_NUMBER}</b>");
        Debug.Log("Initializing app plugins and subsystems, please wait...");

        // Wait for brainCloud to be initialized
        yield return new WaitUntil(() => BC.Client != null && BC.Client.IsInitialized());

        // Initialize Firebase Messaging
        DependencyStatus status = (DependencyStatus)(-1);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            status = task.Result;
        });

        yield return new WaitUntil(() => (int)status >= 0);

        if (status == DependencyStatus.Available)
        {
            Firebase = FirebaseApp.DefaultInstance;
            FirebaseMessaging.MessageReceived += OnFirebaseMessageReceived;
            FirebaseMessaging.TokenReceived += OnFirebaseTokenReceived;

            if (!HasFirebaseToken())
            {
                FirebaseMessaging.SubscribeAsync(TOPIC_BRAINCLOUD_EXAMPLE_PUSHNOTIFICATION).ContinueWith(LogTaskCompletion("SubscribeAsync"));
                FirebaseMessaging.RequestPermissionAsync().ContinueWith(LogTaskCompletion("RequestPermissionAsync"));
            }

            Debug.Log($"Firebase is ready for use. Status: {status}");
        }
        else
        {
            Debug.LogError($"Could not resolve all Firebase dependencies: {status}");
            Debug.LogError("App cannot start. Please close the app and try again.");
        }

        yield return new WaitUntil(() => Firebase != null);

        // Enable Unity Gaming Services
        bool ugsEnabled = false;
        try
        {
            var options = new InitializationOptions();
            UnityServices.InitializeAsync(options).ContinueWith(task => ugsEnabled = true);
        }
        catch (Exception e)
        {
            Debug.LogError($"Unity Gaming Services failed to initialize.\nError: {e.Message}.");
            Debug.LogError("App cannot start. Please close the app and try again.");
        }

        yield return new WaitUntil(() => ugsEnabled);

        Debug.Log("Unity Gaming Services has been successfully initialized.");

        yield return null;

        // Enable App
        IsInteractable = true;
    }

    public Func<Task, bool> LogTaskCompletion(string operation) => task =>
    {
        if (task.IsCompleted)
        {
            Debug.Log($"{operation} task is completed.");

            return true;
        }
        else if (task.IsCanceled)
        {
            Debug.LogWarning($"{operation} task is canceled.");
        }
        else
        {
            Debug.LogError($"{operation} task encounted an error.");

            if (task.Exception != null)
            {
                foreach (Exception e in task.Exception.Flatten().InnerExceptions)
                {
                    if (e is FirebaseException fbe)
                    {
                        Debug.LogError($"Firebase Error {(Error)fbe.ErrorCode}: {e}");
                    }

                    Debug.LogError(e);
                }
            }
        }

        return false;
    };

    public void ChangePanelState(PanelState state)
    {
        Debug.Log($"Changing panel state to <b>{state}</b>.");

        switch (state)
        {
            case PanelState.Login:
                LoginPanel.gameObject.SetActive(true);
                MainPanel.gameObject.SetActive(false);
                StorePanel.gameObject.SetActive(false);
                break;
            case PanelState.Main:
                LoginPanel.gameObject.SetActive(false);
                MainPanel.gameObject.SetActive(true);
                StorePanel.gameObject.SetActive(false);
                break;
            case PanelState.Store:
                LoginPanel.gameObject.SetActive(false);
                MainPanel.gameObject.SetActive(false);
                StorePanel.gameObject.SetActive(true);
                break;
            case PanelState.None:
            default:
                LoginPanel.gameObject.SetActive(false);
                MainPanel.gameObject.SetActive(false);
                StorePanel.gameObject.SetActive(false);
                break;
        }
    }

    public bool GetStoredUserIDs()
    {
        const string DEFAULT_TEXT = "---";

        string profileID = BC.GetStoredProfileId(), anonID = BC.GetStoredAnonymousId();

        UserInfoText.text = string.Format(USER_INFO_FORMAT,
                                          string.IsNullOrWhiteSpace(profileID) ? DEFAULT_TEXT : profileID,
                                          string.IsNullOrWhiteSpace(anonID) ? DEFAULT_TEXT : anonID);

        return !string.IsNullOrWhiteSpace(profileID) && !string.IsNullOrWhiteSpace(anonID);
    }

    public void SendPushNotification(Action onPushNotificationSent = null)
    {
        if (!HasFirebaseToken())
        {
            Debug.LogWarning("Have not received Firebase token for push notifications. Unable to send push notification yet.");
            return;
        }

        IsInteractable = false;

        void onRegisterSuccess(string jsonResponse, object cbObject)
        {
            Debug.Log($"Registered Device for Push Notifications! Sending one now...");

            // Basic message structure for notifications; see the FirebaseMessagingSnippets for more on how this can be customized
            var message = JsonWriter.Serialize(new Dictionary<string, object>
            {
                { "notification", new Dictionary<string, object>
                    {
                        { "title", NotificationTitle },
                        { "body",  NotificationBody },
                        { "image", NotificationImageURL }
                    }
                },
                { "data", new Dictionary<string, object> // Pass data that the app can use
                    {
                        //{ "customfield1", "value1" },
                        //{ "customfield2", "value2" },
                        //{ "customfield3", "value3" }
                    }
                },
                { "priority", "normal" } // Can only be normal or high
            });

            void onSendSuccess(string jsonResponse, object cbObject)
            {
                IsInteractable = true;
                onPushNotificationSent?.Invoke();
            }

            BC.PushNotificationService.SendRawPushNotification
            (
                BC.GetStoredProfileId(),
                message,
                string.Empty,
                string.Empty,
                onSendSuccess,
                OnBrainCloudError,
                this
            );
        }

        BC.PushNotificationService.RegisterPushNotificationDeviceToken
        (
            Platform.GooglePlayAndroid,
            GetFirebaseToken(),
            onRegisterSuccess,
            OnBrainCloudError,
            this
        );
    }

    public void OnBrainCloudSuccess(string jsonResponse, object cbObject)
    {
        //
    }

    public void OnBrainCloudError(int status, int reason, string jsonError, object cbObject)
    {
        // Deserialize jsonError
        var error = JsonReader.Deserialize<Dictionary<string, object>>(jsonError);
        var message = (string)error["status_message"];

        Debug.LogError($"Status: {status} | Reason: {reason} | Message:\n  {message}");

        IsInteractable = true;
    }

    #region Firebase Messaging

    public bool HasFirebaseToken() => !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(PLAYERPREFS_FIREBASE_TOKEN_KEY));

    public string GetFirebaseToken() => PlayerPrefs.GetString(PLAYERPREFS_FIREBASE_TOKEN_KEY);

    public void SetFirebaseToken(string token) => PlayerPrefs.SetString(PLAYERPREFS_FIREBASE_TOKEN_KEY, token);

    public void ResetFirebaseToken() => PlayerPrefs.DeleteKey(PLAYERPREFS_FIREBASE_TOKEN_KEY);

    private void OnFirebaseMessageReceived(object sender, MessageReceivedEventArgs args)
    {
        if (args == null || args.Message == null)
        {
            Debug.LogWarning("Received an unknown message from Firebase!");
            return;
        }

        FirebaseMessage message = args.Message;
        Dictionary<string, object> json = new();

        if (message.Notification is FirebaseNotification notification)
        {
            json.Add("title", notification.Title);
            json.Add("body", notification.Body);
            json.Add("image", notification.Icon);

            if (notification.Android is AndroidNotificationParams anp)
            {
                json.Add("androidConfig", new Dictionary<string, string> {{ "channelId", anp.ChannelId }});
            }
        }

        if (!string.IsNullOrWhiteSpace(message.From))
        {
            json.Add("from", message.From);
        }

        if (message.Link != null)
        {
            json.Add("link", message.Link.ToString());
        }

        if (message.Data != null && message.Data.Count > 0)
        {
            var data = new Dictionary<string, string>();

            foreach (string key in message.Data.Keys)
            {
                data.Add(key, message.Data[key].ToString());
            }

            json.Add("data", data);
        }

        Debug.Log($"Message received from Firebase:\n{LoggerUI.FormatJSON(JsonWriter.Serialize(json))}");
    }

    private void OnFirebaseTokenReceived(object sender, TokenReceivedEventArgs token)
    {
        Debug.Log($"Received Firebase Registration Token: {token.Token}");
        SetFirebaseToken(token.Token);
    }

    #endregion
}
