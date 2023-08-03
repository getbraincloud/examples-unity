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
    private const string PROFILE_ID_FORMAT = "<align=left>Profile ID:</align>\n{0}";
    private const string ANONYMOUS_ID_FORMAT = "<align=left>Anonymous ID:</align>\n{0}";
    private const string APP_INFO_FORMAT = "{0} ({1}) v{2}";
    private const string BC_VERSION_FORMAT = "brainCloud v{0}";

    [SerializeField] private CanvasGroup MainCG = default;

    [Header("Buttons")]
    [SerializeField] private Button LoginButton = default;
    [SerializeField] private Button LogoutButton = default;
    [SerializeField] private Button SendPushButton = default;
    [SerializeField] private Button ShowStoreButton = default;

    [Header("User Info")]
    [SerializeField] private TMP_Text ProfileIDLabel = default;
    [SerializeField] private TMP_Text AnonIDLabel = default;

    [Header("App Info")]
    [SerializeField] private TMP_Text AppInfoLabel = default;
    [SerializeField] private TMP_Text VersionInfoLabel = default;

    private BrainCloudWrapper BC = null;
    private FirebaseApp FireBase = null;
    private string FirebaseToken = string.Empty;
    private readonly string NotificationMessage = "Testing Google Notification with brainCloud";

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

        StartCoroutine(WaitToEnableApp());
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
        bool canDoReconnect = false;

        // Set Profile ID info
        if (!string.IsNullOrWhiteSpace(BC.GetStoredProfileId()))
        {
            ProfileIDLabel.text = string.Format(PROFILE_ID_FORMAT, BC.GetStoredProfileId());
            canDoReconnect = true;
        }
        else
        {
            ProfileIDLabel.text = string.Format(PROFILE_ID_FORMAT, "---");
        }

        // Set Anonymous ID info
        if (!string.IsNullOrWhiteSpace(BC.GetStoredAnonymousId()))
        {
            AnonIDLabel.text = string.Format(ANONYMOUS_ID_FORMAT, BC.GetStoredAnonymousId());
        }
        else
        {
            AnonIDLabel.text = string.Format(ANONYMOUS_ID_FORMAT, "---");
            canDoReconnect = false;
        }

        return canDoReconnect;
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
        MainCG.interactable = false;

        SuccessCallback onSuccess = (_, _) =>
        {
            Debug.Log($"Registered Device for Push Notifications! Sending one now...");

            string content = "{\"notification\":{\"body\":\"" + NotificationMessage + "\",\"title\":\"message title\"},\"data\":{\"customfield1\":\"customValue1\",\"customfield2\":\"customValue2\"},\"priority\":\"normal\"}";

            BC.PushNotificationService.SendRawPushNotification
            (
                BC.GetStoredProfileId(),
                content,
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

    private IEnumerator WaitToEnableApp()
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
            FirebaseMessaging.MessageReceived += OnMessageReceived;
            FirebaseMessaging.TokenReceived += OnTokenReceived;

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

    public virtual void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Debug.Log("Received a new message");
        var notification = e.Message.Notification;
        if (notification != null)
        {
            Debug.Log("title: " + notification.Title);
            Debug.Log("body: " + notification.Body);
            var android = notification.Android;
            if (android != null)
            {
                Debug.Log("android channel_id: " + android.ChannelId);
            }

            if (notification.Body.Contains(NotificationMessage))
            {
                Debug.Log($"Notification received: {NotificationMessage}");
            }
        }
        if (e.Message.From.Length > 0)
            Debug.Log("from: " + e.Message.From);
        if (e.Message.Link != null)
        {
            Debug.Log("link: " + e.Message.Link.ToString());
        }
        if (e.Message.Data.Count > 0)
        {
            Debug.Log("data:");
            foreach (KeyValuePair<string, string> iter in e.Message.Data)
            {
                Debug.Log("  " + iter.Key + ": " + iter.Value);
            }
        }
    }

    public virtual void OnTokenReceived(object sender, TokenReceivedEventArgs token)
    {
        Debug.Log($"Received Firebase Registration Token: {token.Token}");
        FirebaseToken = token.Token;
    }

    private Func<Task, bool> LogTaskCompletion(string operation) => task =>
    {
        bool complete = false;
        if (task.IsCanceled)
        {
            Debug.Log(operation + " canceled.");
        }
        else if (task.IsFaulted)
        {
            Debug.Log(operation + " encounted an error.");
            if (task.Exception == null) return false;
            foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
            {
                string errorCode = "";
                FirebaseException firebaseEx = exception as Firebase.FirebaseException;
                if (firebaseEx != null)
                {
                    errorCode = String.Format("Error.{0}: ",
                        ((Error)firebaseEx.ErrorCode).ToString());
                }
                Debug.Log(errorCode + exception.ToString());
            }
        }
        else if (task.IsCompleted)
        {
            Debug.Log(operation + " completed");
            complete = true;
        }

        return complete;
    };

    #endregion
}
