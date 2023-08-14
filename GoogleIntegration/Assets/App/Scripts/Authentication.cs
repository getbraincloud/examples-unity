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
    private const string TOPIC_BRAINCLOUD_EXAMPLE_PUSHNOTIFICATION = "/topics/brainCloud/example/PushNotification";
    private const string PLAYERPREFS_FIREBASE_TOKEN_KEY = TOPIC_BRAINCLOUD_EXAMPLE_PUSHNOTIFICATION + "." + "FIREBASE_TOKEN_KEY";

    private const string IAP_INFO_FORMAT = "Energy: <b>{0}</b> | {1}: <b>{2}</b> | Has Special Item? <b>{3}</b>";
    private const string USER_INFO_FORMAT = "Profile ID: {0}\nAnonymous ID: {1}";
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
    [SerializeField] private Button OpenStoreButton = default;
    [SerializeField] private Button CloseStoreButton = default;
    [Space]
    [SerializeField] private Button BuyEnergyButton = default;
    [SerializeField] private Button BuyCurrencyButton = default;
    [SerializeField] private Button BuyItemButton = default;

    [Header("Info Labels")]
    [SerializeField] private TMP_Text IAPInfoText = default;
    [SerializeField] private TMP_Text UserInfoText = default;
    [SerializeField] private TMP_Text AppInfoLabel = default;
    [SerializeField] private TMP_Text VersionInfoLabel = default;

    [Header("Misc")]
    [SerializeField] private GameObject TopSeparator = default;
    [SerializeField] private GameObject[] StoreClosedElements = default;
    [SerializeField] private GameObject[] StoreOpenedElements = default;

    private bool IsStoreOpened = false;
    private BrainCloudWrapper BC = null;
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

    private void OnEnable()
    {
        LoginButton.onClick.AddListener(OnLoginButton);
        LogoutButton.onClick.AddListener(OnLogoutButton);
        SendPushButton.onClick.AddListener(OnSendPushButton);
        OpenStoreButton.onClick.AddListener(OnOpenStoreButton);
        CloseStoreButton.onClick.AddListener(OnCloseStoreButton);
        BuyEnergyButton.onClick.AddListener(OnBuyEnergyButton);
        BuyCurrencyButton.onClick.AddListener(OnBuyCurrencyButton);
        BuyItemButton.onClick.AddListener(OnBuyItemButton);
    }

    private void Start()
    {
        MainCG.interactable = false;

        // Setup buttons
        OnCloseStoreButton();
        LoginButton.gameObject.SetActive(true);
        TopSeparator.SetActive(false);
        LogoutButton.gameObject.SetActive(false);
        SendPushButton.gameObject.SetActive(false);
        OpenStoreButton.gameObject.SetActive(false);

        UpdateUserData();
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
        OpenStoreButton.onClick.RemoveAllListeners();
        CloseStoreButton.onClick.RemoveAllListeners();
        BuyEnergyButton.onClick.RemoveAllListeners();
        BuyCurrencyButton.onClick.RemoveAllListeners();
        BuyItemButton.onClick.RemoveAllListeners();
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

    #region UI

    public void UpdateUserData()
    {
        IAPInfoText.text = string.Format(IAP_INFO_FORMAT, UserData.EnergyAmount, "Gems", UserData.CurrencyAmount, UserData.HasSpecialItem ? "YES" : "NO");
    }

    public bool GetStoredUserIDs()
    {
        const string DEFAULT_TEXT = "---";

        string profileID = BC.GetStoredProfileId(), anonID = BC.GetStoredAnonymousId();

        UserInfoText.text = string.Format(USER_INFO_FORMAT,
                                          string.IsNullOrWhiteSpace(profileID) ? DEFAULT_TEXT : profileID,
                                          string.IsNullOrWhiteSpace(anonID) ? DEFAULT_TEXT : anonID);

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

            OnCloseStoreButton();
            LoginButton.gameObject.SetActive(true);
            TopSeparator.SetActive(false);
            LogoutButton.gameObject.SetActive(false);
            SendPushButton.gameObject.SetActive(false);
            OpenStoreButton.gameObject.SetActive(false);

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

        if (!HasFirebaseToken())
        {
            Debug.LogWarning("Have not received Firebase token for push notifications. Unable to send Push Notification yet.");
            return;
        }

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
            GetFirebaseToken(),
            onSuccess,
            OnBrainCloudError,
            this
        );
    }

    private void OnOpenStoreButton()
    {
        IsStoreOpened = true;

        foreach (var element in StoreClosedElements)
        {
            element.SetActive(true);
        }

        foreach (var element in StoreOpenedElements)
        {
            element.SetActive(false);
        }
    }

    private void OnCloseStoreButton()
    {
        IsStoreOpened = false;

        foreach (var element in StoreClosedElements)
        {
            element.SetActive(false);
        }

        foreach (var element in StoreOpenedElements)
        {
            element.SetActive(true);
        }
    }

    private void OnBuyEnergyButton()
    {
        Debug.Log("Buying ENERGY");
    }

    private void OnBuyCurrencyButton()
    {
        Debug.Log("Buying CURRENCY");
    }

    private void OnBuyItemButton()
    {
        Debug.Log("Buying ITEM");
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

        OnCloseStoreButton();
        LoginButton.gameObject.SetActive(false);
        TopSeparator.SetActive(true);
        LogoutButton.gameObject.SetActive(true);
        SendPushButton.gameObject.SetActive(true);
        OpenStoreButton.gameObject.SetActive(true);
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
        }

        yield return null;

        MainCG.interactable = true;
    }

    private bool HasFirebaseToken() => !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(PLAYERPREFS_FIREBASE_TOKEN_KEY));

    private string GetFirebaseToken() => PlayerPrefs.GetString(PLAYERPREFS_FIREBASE_TOKEN_KEY);

    private void SetFirebaseToken(string token) => PlayerPrefs.SetString(PLAYERPREFS_FIREBASE_TOKEN_KEY, token);

    private void ResetFirebaseToken() => PlayerPrefs.DeleteKey(PLAYERPREFS_FIREBASE_TOKEN_KEY);

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
