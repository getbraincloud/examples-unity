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

/// <summary>
/// 
/// </summary>
public class ExampleApp : MonoBehaviour, IDetailedStoreListener
{
    #region App Defines

    // Firebase Messaging
    private const string TOPIC_BRAINCLOUD_EXAMPLE_PUSHNOTIFICATION = "/topics/brainCloud/example/PushNotification";
    private const string PLAYERPREFS_FIREBASE_TOKEN_KEY = TOPIC_BRAINCLOUD_EXAMPLE_PUSHNOTIFICATION + "." + "FIREBASE_TOKEN_KEY";

    // String Formats
    private const string IAP_INFO_FORMAT = "Energy: <b>{0}</b> | {1}: <b>{2}</b>";
    private const string USER_INFO_FORMAT = "Profile ID: {0}\nAnonymous ID: {1}";
    private const string APP_INFO_FORMAT = "{0} ({1}) v{2}";
    private const string BC_VERSION_FORMAT = "brainCloud v{0}";

    #endregion

    [Header("Firebase Messaging")]
    [SerializeField] private string NotificationTitle = "Notification Title";
    [SerializeField] private string NotificationBody = "Hello World from brainCloud!";
    [SerializeField] private string NotificationImageURL = string.Empty;

    [Header("UI Elements")]
    [SerializeField] private CanvasGroup MainCG = default;
    [SerializeField] private TMP_Text UserInfoText = default;
    [SerializeField] private TMP_Text AppInfoLabel = default;
    [SerializeField] private TMP_Text VersionInfoLabel = default;

    [Header("Login Panel")]
    [SerializeField] private GameObject LoginPanelGO = default;
    [SerializeField] private Button LoginButton = default;

    [Header("Main Panel")]
    [SerializeField] private GameObject MainPanelGO = default;
    [SerializeField] private Button LogoutButton = default;
    [SerializeField] private Button SendPushButton = default;
    [SerializeField] private Button OpenStoreButton = default;

    [Header("Store Panel")]
    [SerializeField] private GameObject StorePanelGO = default;
    [SerializeField] private TMP_Text IAPInfoText = default;
    [SerializeField] private Button CloseStoreButton = default;
    [Space]
    [SerializeField] private IAPButton IAPButtonTemplate = default;
    [SerializeField] private Transform IAPItemsContent = default;

    private BrainCloudWrapper BC = null; // How we will interact with the brainCloud client
    private FirebaseApp Firebase = null;
    private IStoreController StoreController; // Used for the Unity IAP system
    private IExtensionProvider ExtensionProvider;

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
    }

    private void Start()
    {
        MainCG.interactable = false;

        // Initial UI
        LoginPanelGO.SetActive(true);
        MainPanelGO.SetActive(false);
        StorePanelGO.SetActive(false);

        UpdateUserData();
        GetStoredUserIDs();
        AppInfoLabel.text = string.Format(APP_INFO_FORMAT, BC.WrapperName, BC.Client.AppId, BC.Client.AppVersion);
        VersionInfoLabel.text = string.Format(BC_VERSION_FORMAT, BC.Client.BrainCloudClientVersion);

        StartCoroutine(InitializeApp());
    }

    private void OnDisable()
    {
        LoginButton.onClick.RemoveAllListeners();
        LogoutButton.onClick.RemoveAllListeners();
        SendPushButton.onClick.RemoveAllListeners();
        OpenStoreButton.onClick.RemoveAllListeners();
        CloseStoreButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        StoreController = null;
        ExtensionProvider = null;

        FirebaseMessaging.MessageReceived -= OnFirebaseMessageReceived;
        FirebaseMessaging.TokenReceived -= OnFirebaseTokenReceived;

        Firebase?.Dispose();
        Firebase = null;

        BC = null;
    }

    #endregion

    private IEnumerator InitializeApp()
    {
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
        MainCG.interactable = true;
    }

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

    private void HandleAutomaticLogin()
    {
        MainCG.interactable = false;

        Debug.Log($"Logging in with previous credentials...");

        BC.Reconnect(OnAuthenticationSuccess,
                     OnAuthenticationFailure,
                     this);
    }

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

        return !string.IsNullOrWhiteSpace(profileID) && !string.IsNullOrWhiteSpace(anonID);
    }

    private void OnLoginButton()
    {
        if (GetStoredUserIDs())
        {
            HandleAutomaticLogin();
        }
        else
        {
            BC.AuthenticateAnonymous(OnAuthenticationSuccess,
                                     OnAuthenticationFailure,
                                     this);
        }
    }

    private void OnLogoutButton()
    {
        MainCG.interactable = false;

        void onSuccess(string jsonResponse, object cbObject)
        {
            Debug.Log($"Logout success!");

            LoginPanelGO.SetActive(true);
            MainPanelGO.SetActive(false);

            GetStoredUserIDs();

            MainCG.interactable = true;
        };

        void onFailure (int status, int reason, string jsonError, object cbObject)
        {
            OnBrainCloudError(status, reason, jsonError, cbObject);

            Debug.LogError($"Logout failed!");
            Debug.LogError($"Try restarting the app...");

            GetStoredUserIDs();
        }

        BC.PlayerStateService.Logout(onSuccess, onFailure, this);
    }

    private void OnSendPushButton()
    {
        // https://github.com/firebase/firebase-admin-dotnet/blob/db55e58ee591dab1f90a399336670ae84bab915b/FirebaseAdmin/FirebaseAdmin.Snippets/FirebaseMessagingSnippets.cs

        if (!HasFirebaseToken())
        {
            Debug.LogWarning("Have not received Firebase token for push notifications. Unable to send push notification yet.");
            return;
        }

        MainCG.interactable = false;

        void onRegisterSuccess(string jsonResponse, object cbObject)
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

            void onSendSuccess(string jsonResponse, object cbObject)
            {
                MainCG.interactable = true;
                SendPushButton.interactable = false;

                Debug.Log($"Push notification request sent!");
                Debug.Log($"Push notifications are expensive to send so the button will remain disabled for this login.");
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

    private void OnOpenStoreButton()
    {
        MainCG.interactable = false;

        void onSuccess(string jsonResponse, object cbObject)
        {
            MainPanelGO.SetActive(false);
            StorePanelGO.SetActive(true);

            // Products created in brainCloud's Marketplace portal get stored as an array under data > productInventory
            var data = (JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)["data"] as Dictionary<string, object>)["productInventory"];
            var inventory = JsonReader.Deserialize<BCProduct[]>(JsonWriter.Serialize(data));

            // Enable Unity IAP and add the products
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (var product in inventory)
            {
                var iapButton = Instantiate(IAPButtonTemplate, IAPItemsContent, false);
                iapButton.SetProductDetails(product);
                iapButton.OnButtonAction += OnPurchaseBCProduct;
                iapButton.gameObject.SetActive(false);

                builder.AddProduct(product.itemId, product.IAPProductType);
            }

            UnityPurchasing.Initialize(this, builder);
        };

        BC.AppStoreService.GetSalesInventory("googlePlay",
                                             "{\"userCurrency\":\"CAD\"}",
                                             onSuccess,
                                             OnBrainCloudError,
                                             this);
    }

    private void OnCloseStoreButton()
    {
        for (int i = 0; i < IAPItemsContent.childCount; i++)
        {
            Destroy(IAPItemsContent.GetChild(i).gameObject);
        }

        MainPanelGO.SetActive(true);
        StorePanelGO.SetActive(false);
    }

    private void OnPurchaseBCProduct(BCProduct bcProduct)
    {
#if !UNITY_EDITOR
        MainCG.interactable = false;

        if (StoreController != null)
        {
            var iapProduct = StoreController.products.WithID(bcProduct.itemId);

            if (iapProduct != null && iapProduct.availableToPurchase)
            {
                StoreController.InitiatePurchase(iapProduct);

                Debug.Log($"Purchasing: {bcProduct.title} (ID: {bcProduct.itemId} | Price: {bcProduct.GetGooglePlayPriceData().GetIAPPrice()} | Type: {bcProduct.IAPProductType})");
            }
            else
            {
                Debug.Log($"Product is not available! Cannot purchse: {bcProduct.title} (Product exists: {iapProduct != null} | Available: {iapProduct.availableToPurchase})");
            }
        }
        else
        {
            Debug.LogError($"Store is not available! Cannot purchase: {bcProduct.title}");
        }
#else
        Debug.Log($"Purchasing: {bcProduct.title} (ID: {bcProduct.itemId} | Price: {bcProduct.GetGooglePlayPriceData().GetIAPPrice()} | Type: {bcProduct.IAPProductType})");
#endif
    }

    private void OnRedeemBCItem(BCItem item)
    {
        MainCG.interactable = false;

        // TODO: Be able to redeem Gems for items

        Debug.Log($"Redeeming {item.defId} x{item.quantity}");
    }

#endregion

    #region brainCloud

    private void OnAuthenticationSuccess(string jsonResponse, object cbObject)
    {
        BC.SetStoredAuthenticationType(AuthenticationType.Anonymous.ToString());

        LoginPanelGO.SetActive(false);
        MainPanelGO.SetActive(true);
        MainCG.interactable = true;

        GetStoredUserIDs();
        Debug.Log($"User Profile ID: {BC.GetStoredProfileId()}");
        Debug.Log($"User Anonymous ID: {BC.GetStoredAnonymousId()}");

        Debug.Log("Authentication success! You are now logged into your app on brainCloud.");
    }

    private void OnAuthenticationFailure(int status, int reason, string jsonError, object cbObject)
    {
        BC.ResetStoredAuthenticationType();
        GetStoredUserIDs();

        OnBrainCloudError(status, reason, jsonError, cbObject);

        Debug.LogError($"Authentication failed! Please try again.");
    }

    private void OnVerifyPurchasesSuccess(string jsonResponse, object cbObject)
    {
        var data = ((JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)
            ["data"] as Dictionary<string, object>)
            ["transactionSummary"] as Dictionary<string, object>)
            ["transactionDetails"];
        var details = JsonReader.Deserialize<Dictionary<string, object>[]>(JsonWriter.Serialize(data));

        List<string> failedTransactions = new();
        foreach(var transaction in details)
        {
            string status = string.Empty;
            string productId = transaction["productId"].ToString();

            Debug.Log($"Does {productId} contain error message? {transaction.ContainsKey("errorMessage")}");

            if (transaction.ContainsValue("errorMessage"))
            {
                status = transaction["errorMessage"].ToString();
            }
            else if ((bool)transaction["processed"] == false)
            {
                status = "Could not process.";
            }
            else if (StoreController.products.WithID(productId) is Product product && product.hasReceipt)
            {
                StoreController.ConfirmPendingPurchase(product);
            }
            else
            {
                status = "Unknown Error";
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                failedTransactions.Add($"{productId} - {status}");
            }
        }

        if (failedTransactions.Count > 0)
        {
            Debug.Log($"One or more purchases were unable to be fully processed:\n{JsonWriter.Serialize(data)}");
        }
        else
        {
            Debug.Log($"Purchase(s) verified with brainCloud!");
        }

        MainCG.interactable = true;
    }

    private void OnBrainCloudSuccess(string jsonResponse, object cbObject)
    {
        //
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

    #region Firebase Messaging

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

    #region Unity IAP

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        MainCG.interactable = true;

        StoreController = controller;
        ExtensionProvider = extensions;

        for (int i = 0; i < IAPItemsContent.childCount; i++)
        {
            var product = StoreController.products.WithID(IAPItemsContent.GetChild(i).GetComponent<IAPButton>().ProductData.itemId);
            if (product == null || !product.availableToPurchase || !product.definition.enabled)
            {
                IAPItemsContent.GetChild(i).gameObject.SetActive(false);
            }
            else
            {
                IAPItemsContent.GetChild(i).gameObject.SetActive(true);
            }
        }

        Debug.Log("Unity IAP updated.");
    }

    public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        MainCG.interactable = true;

        var errorMessage = $"Unity IAP failed to initialize. Reason: {error}.";
        if (string.IsNullOrWhiteSpace(message))
        {
            errorMessage += $"\nDetails: {message}";
        }

        StoreController = null;
        ExtensionProvider = null;

        Debug.LogError(errorMessage);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        MainCG.interactable = true;

        // Retrieve the purchased product
        var product = args.purchasedProduct;
        Debug.Log($"Purchase Complete! Product: {product.definition.id}; Receipt:\n{LoggerUI.FormatJSON(product.receipt)}");

        string payload = JsonReader.Deserialize<Dictionary<string, object>>(product.receipt)["Payload"].ToString();
        string json = JsonReader.Deserialize<Dictionary<string, object>>(payload)["json"].ToString();
        var details = JsonReader.Deserialize<Dictionary<string, object>>(json);

        void onFailure(int status, int reason, string jsonError, object cbObject)
        {
            OnBrainCloudError(status, reason, jsonError, cbObject);

            Debug.LogError($"Unable to verify purchase(s) with brainCloud!");
        }

        BC.AppStoreService.VerifyPurchase("googlePlay",
                                          JsonWriter.Serialize(new Dictionary<string, object>
                                          {
                                              { "productId", details["productId"] },     { "orderId",          details["orderId"] },
                                              { "token",     details["purchaseToken"] }, { "developerPayload", json }
                                          }),
                                          OnVerifyPurchasesSuccess,
                                          onFailure,
                                          this);

        return PurchaseProcessingResult.Pending;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        MainCG.interactable = true;

        Debug.LogError($"Purchase Failed. Product: {product.definition.id}. Reason: {failureReason}");
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        MainCG.interactable = true;

        Debug.LogError($"Purchase Failed. Product: {product.definition.id}. Reason: {failureDescription.reason}" +
                       (!string.IsNullOrWhiteSpace(failureDescription.message) ? $"\nDetails: {failureDescription.message}" : string.Empty));
    }

    #endregion
}
