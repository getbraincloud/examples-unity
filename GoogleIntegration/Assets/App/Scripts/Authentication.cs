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
using UnityEngine;
using UnityEngine.UI;

public class Authentication : MonoBehaviour
{
    private const string USER_INFO_FORMAT = "Profile ID: {0}\nAnonymous ID: {1}\nFirebase Token: {2}";
    private const string APP_INFO_FORMAT = "{0} ({1}) v{2}";
    private const string BC_VERSION_FORMAT = "brainCloud v{0}";

    [Header("Message Data")]
    [SerializeField] private string NotificationTitle = "Notification Title";
    [SerializeField] private string NotificationBody = "Hello World from brainCloud!";
    [SerializeField] private string NotificationImageURL = string.Empty;

    [Header("UI Elements")]
    [SerializeField] private CanvasGroup MainCG = default;

    [Header("Buttons")]
    [SerializeField] private Button LoginButton = default;
    [SerializeField] private Button LogoutButton = default;
    [SerializeField] private Button SendPushButton = default;
    [SerializeField] private Button ShowStoreButton = default;

    [Header("User Info")]
    [SerializeField] private TMP_Text UserInfoText = default;

    [Header("App Info")]
    [SerializeField] private TMP_Text AppInfoLabel = default;
    [SerializeField] private TMP_Text VersionInfoLabel = default;

    private BrainCloudWrapper BC = null;
    private FirebaseApp FireBase = null;
    private string FirebaseToken = string.Empty;

    #region Unity Messages

    private void Awake()
    {
        // Init BrainCloudWrapper
        BC = gameObject.GetComponent<BrainCloudWrapper>();
        BC.WrapperName = Application.productName;
        BC.Init();

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        LoginButton.onClick.AddListener(OnLoginButton);
        LogoutButton.onClick.AddListener(OnLogoutButton);
        SendPushButton.onClick.AddListener(OnSendPushButton);
        ShowStoreButton.onClick.AddListener(OnShowStoreButton);
    }

    private void Start()
    {
        MainCG.interactable = false;

        // Setup buttons
        LoginButton.gameObject.SetActive(true);
        LogoutButton.gameObject.SetActive(false);
        SendPushButton.interactable = false;
        ShowStoreButton.interactable = false;

        GetStoredUserIDs();
        AppInfoLabel.text = string.Format(APP_INFO_FORMAT, BC.WrapperName, BC.Client.AppId, BC.Client.AppVersion);
        VersionInfoLabel.text = string.Format(BC_VERSION_FORMAT, BC.Client.BrainCloudClientVersion);

        StartCoroutine(InitializeFirebase());
    }

    private void OnDisable()
    {
        LoginButton.onClick.RemoveAllListeners();
        LogoutButton.onClick.RemoveAllListeners();
        SendPushButton.onClick.RemoveAllListeners();
        ShowStoreButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        BC = null;
        FireBase?.Dispose();
        FireBase = null;
    }

    #endregion

    #region UI

    public bool GetStoredUserIDs()
    {
        const string DEFAULT_TEXT = "---";

        string profileID = BC.GetStoredProfileId(), anonID = BC.GetStoredAnonymousId(), token = DEFAULT_TEXT;

        UserInfoText.text = string.Format(USER_INFO_FORMAT, string.IsNullOrWhiteSpace(profileID) ? DEFAULT_TEXT : profileID,
                            string.IsNullOrWhiteSpace(anonID) ? DEFAULT_TEXT : anonID,
                            token);

        return !string.IsNullOrEmpty(profileID + anonID);
    }

    private void OnLoginButton()
    {
        if (GetStoredUserIDs())
        {
            HandleAutomaticLogin();
        }
        else
        {
            BC.AuthenticateAnonymous(OnAuthenticationSuccess, OnAuthenticationFailure, this);
        }
    }

    private void OnLogoutButton()
    {
        MainCG.interactable = false;

        SuccessCallback onSuccess = (_, _) =>
        {
            Debug.Log($"Logout success!");

            LoginButton.gameObject.SetActive(true);
            LogoutButton.gameObject.SetActive(false);
            SendPushButton.interactable = false;

            GetStoredUserIDs();

            MainCG.interactable = true;
        };

        FailureCallback onFailure = (_, _, _, _) =>
        {
            Debug.LogError($"Logout failed!");
            Debug.LogError($"Try restarting the app...");

            GetStoredUserIDs();
        };

        BC.PlayerStateService.Logout(onSuccess, onFailure, this);
    }

    private void OnSendPushButton()
    {
        // https://github.com/firebase/firebase-admin-dotnet/blob/db55e58ee591dab1f90a399336670ae84bab915b/FirebaseAdmin/FirebaseAdmin.Snippets/FirebaseMessagingSnippets.cs

        MainCG.interactable = false;

        SuccessCallback onSuccess = (_, _) =>
        {
            Debug.Log($"Registered Device for Push Notifications! Sending one now...");

            // Basic message structure for notifications; see the FirebaseMessagingSnippets for more on how this can be customized
            var message = JsonWriter.Serialize(new Dictionary<string, object>
            {
                { "notification", new Dictionary<string, object>
                    {
                        { "title", NotificationTitle },
                        { "body", NotificationBody },
                        { "image", NotificationImageURL }
                    }
                },
                { "data", new Dictionary<string, object> // Pass whichever data the app can use
                    {
                        //{ "customfield1", "value1" },
                        //{ "customfield2", "value2" },
                        //{ "customfield3", "value3" }
                    }
                },
                { "priority", "normal" } // Can only be normal or high
            });

            BC.PushNotificationService.SendRawPushNotification
            (
                BC.GetStoredProfileId(),
                message,
                string.Empty,
                string.Empty,
                SendRawPushNotificationSuccess,
                OnBrainCloudError,
                this
            );
        };

        BC.PushNotificationService.RegisterPushNotificationDeviceToken
        (
            Platform.GooglePlayAndroid,
            FirebaseToken,
            onSuccess,
            OnBrainCloudError,
            this
        );
    }

    private void OnShowStoreButton()
    {
        Debug.Log("Showing the IAP Store...");
    }

    #endregion

    #region brainCloud

    public void HandleAutomaticLogin()
    {
        MainCG.interactable = false;

        Debug.Log($"Logging in with previous credentials...");

        BC.Reconnect(OnAuthenticationSuccess, OnAuthenticationFailure, this);
    }

    private void OnAuthenticationSuccess(string jsonResponse, object cbObject)
    {
        BC.SetStoredAuthenticationType(AuthenticationType.Anonymous.ToString());

        LoginButton.gameObject.SetActive(false);
        LogoutButton.gameObject.SetActive(true);
        SendPushButton.interactable = true;
        ShowStoreButton.interactable = true;
        MainCG.interactable = true;

        GetStoredUserIDs();
        Debug.Log($"User Profile ID: {BC.GetStoredProfileId()}");
        Debug.Log($"User Anonymous ID: {BC.GetStoredAnonymousId()}");

        Debug.Log($"Authentication success! You are now logged into your app on brainCloud.");
    }

    private void OnAuthenticationFailure(int status, int reason, string jsonError, object cbObject)
    {
        BC.ResetStoredAuthenticationType();
        GetStoredUserIDs();

        OnBrainCloudError(status, reason, jsonError, cbObject);

        Debug.LogError($"Authentication failed! Please try again.");
    }

    private void SendRawPushNotificationSuccess(string jsonResponse, object cbObject)
    {
        MainCG.interactable = true;
        SendPushButton.interactable = false;

        Debug.Log($"Push notification request sent!");
        Debug.Log($"Push notifications are expensive to send so the button will remain disabled for this login.");
    }

    private void OnBrainCloudError(int status, int reason, string jsonError, object cbObject)
    {
        // Deserialize jsonError
        var error = JsonReader.Deserialize<Dictionary<string, object>>(jsonError);
        var message = (string)error["status_message"];

        Debug.LogError($"Status: {status} | Reason: {reason} | Message:\n  {message}");

        MainCG.interactable = true;
    }

    #endregion

    #region Firebase

    private IEnumerator InitializeFirebase()
    {
        DependencyStatus status = (DependencyStatus)(-1);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            status = task.Result;
        });

        yield return new WaitUntil(() => (int)status >= 0);

        if (status == DependencyStatus.Available)
        {
            //FireBase = FirebaseApp.DefaultInstance;
            FirebaseMessaging.MessageReceived += OnFirebaseMessageReceived;
            FirebaseMessaging.TokenReceived += OnFirebaseTokenReceived;

            FirebaseMessaging.SubscribeAsync("brainCloudExampleTopic").ContinueWith(LogTaskCompletion("SubscribeAsync"));
            FirebaseMessaging.RequestPermissionAsync().ContinueWith(LogTaskCompletion("RequestPermissionAsync"));

            Debug.Log($"Firebase is ready for use. Status: {status}");
        }
        else
        {
            Debug.LogError($"Could not resolve all Firebase dependencies: {status}");
        }

        yield return null;

        MainCG.interactable = true;
    }

    public virtual void OnFirebaseMessageReceived(object sender, MessageReceivedEventArgs args)
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
            json.Add("link", message.Link);
        }

        if (message.Data != null && message.Data.Count > 0)
        {
            json.Add("data", message.Data);
        }

        Debug.Log($"Message received from Firebase:\n{LoggerUI.FormatJSON(JsonWriter.Serialize(json))}");
    }

    public virtual void OnFirebaseTokenReceived(object sender, TokenReceivedEventArgs token)
    {
        Debug.Log($"Received Firebase Registration Token: {token.Token}");
        FirebaseToken = token.Token;
    }

    #endregion

    private Func<Task, bool> LogTaskCompletion(string operation) => task =>
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
}
