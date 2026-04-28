using BrainCloud;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

/// <summary>
/// Singleton MonoBehaviour that bridges brainCloud's Marketplace with Unity IAP.
/// Add this component to a persistent GameObject in your scene (alongside BCManager).
/// <br><seealso cref="BrainCloudWrapper"/></br>
/// <br><seealso cref="BrainCloudAppStore"/></br>
/// </summary>
public class BrainCloudMarketplace : MonoBehaviour, IDetailedStoreListener
{
    private const string APP_STORE =
#if UNITY_ANDROID
        "googlePlay";
#elif UNITY_IOS || UNITY_STANDALONE_OSX
        "itunes";
#elif UNITY_STANDALONE_WIN
        "steam";
#else
        "";
#endif

    public static BrainCloudMarketplace Instance { get; private set; }

    private static BrainCloudWrapper bc = null;
    private static IStoreController controller = null;
    private static IExtensionProvider extensions = null;
    private static Action<BCProduct[]> onProcessingFinished = null;
    private static BCProduct[] bcIventory = null;
    private static string _pendingGooglePurchaseToken = null;
    private static string _pendingAppleReceipt = null;

    /// <summary>
    /// True once Unity IAP has successfully initialized with products from brainCloud.
    /// </summary>
    public static bool IsInitialized => Instance != null && controller != null;

    /// <summary>
    /// True if an error occurred during the most recent operation.
    /// </summary>
    public static bool HasErrorOccurred { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Initializes Unity IAP using a pre-fetched <see cref="BCProduct"/> array.
    /// Use this when you already have the store product data from your own cloud code call
    /// (e.g. via InventoryService.FetchStoreItems) and don't need a separate GetSalesInventory call.
    /// </summary>
    /// <param name="products">The store products to register with Unity IAP.</param>
    /// <param name="onInitialized">Optional callback fired once Unity IAP finishes initializing.</param>
    public static async void InitializeWithProducts(BCProduct[] products, Action<BCProduct[]> onInitialized = null)
    {
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
        if (Instance == null)
        {
            HasErrorOccurred = true;
            Debug.LogError("BrainCloudMarketplace: no Instance found in scene. Add it as a component on a GameObject.");
            onInitialized?.Invoke(null);
            return;
        }

        bc = BCManager.Instance.BCWrapper;
        bcIventory = products;

        InternalSetCallback(onInitialized);

        // Unity IAP 4.x requires Unity Gaming Services to be initialized first
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            try
            {
                await UnityServices.InitializeAsync();
                Debug.Log("[IAP] Unity Gaming Services initialized.");
            }
            catch (Exception e)
            {
                HasErrorOccurred = true;
                Debug.LogError($"[IAP] Failed to initialize Unity Gaming Services: {e.Message}");
                InternalInvokeCallback(null);
                return;
            }
        }

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        foreach (var product in bcIventory)
        {
            builder.AddProduct(product.GetProductID(), product.IAPProductType);
        }

        UnityPurchasing.Initialize(Instance, builder);
#else
        HasErrorOccurred = true;
        Debug.Log("BrainCloudMarketplace is not supported on this platform.");
        onInitialized?.Invoke(null);
#endif
    }

    /// <summary>
    /// Fetches products directly from brainCloud's GetSalesInventory and initializes Unity IAP.
    /// Use this if you are not already fetching store data via a cloud code script.
    /// </summary>
    /// <param name="onFetchFinished">Callback with the available <see cref="BCProduct"/> array,
    /// or null on error.</param>
    public static void FetchProducts(Action<BCProduct[]> onFetchFinished = null)
    {
        static void onFetchSuccess(string jsonResponse, object cbObject)
        {
            var data = (JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)["data"] as Dictionary<string, object>)["productInventory"];
            bcIventory = JsonReader.Deserialize<BCProduct[]>(JsonWriter.Serialize(data));

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (var product in bcIventory)
            {
                builder.AddProduct(product.GetProductID(), product.IAPProductType);
            }

            UnityPurchasing.Initialize(Instance, builder);
        };

        if (!IsInitialized)
        {
            InternalInitialize(onFetchFinished);
            return;
        }

        InternalSetCallback(onFetchFinished);
        bc.AppStoreService.GetSalesInventory(APP_STORE,
                                             string.Empty,
                                             onFetchSuccess,
                                             OnBrainCloudFailure("Unable to fetch products from brainCloud!",
                                                                 () => InternalInvokeCallback(null)));
    }

    /// <summary>
    /// Returns fetched products that are available for purchase on the platform store.
    /// </summary>
    public static BCProduct[] GetInventory()
    {
        if (InternalCheckNotInitialized())
            return null;

        if (bcIventory == null || bcIventory.Length == 0)
        {
            Debug.LogWarning("BrainCloudMarketplace has no available products.");
            return null;
        }

        List<BCProduct> updated = new(bcIventory);
        for (int i = 0; i < bcIventory.Length; i++)
        {
            string id = bcIventory[i].GetProductID();
            if (controller.products.WithID(id) is not Product iapProduct || !iapProduct.availableToPurchase)
            {
                updated.Remove(bcIventory[i]);
            }
            else
            {
                bcIventory[i].SetUnityProduct(iapProduct);
            }
        }

        return updated.Count > 0 ? updated.ToArray() : null;
    }

    /// <summary>
    /// Initiates a platform store purchase for the given product.
    /// brainCloud receipt verification runs automatically on success.
    /// </summary>
    /// <param name="product">The product to purchase.</param>
    /// <param name="onPurchaseFinished">Callback with purchased <see cref="BCProduct"/>(s),
    /// or null on error or cancellation.</param>
    public static void PurchaseProduct(BCProduct product, Action<BCProduct[]> onPurchaseFinished = null)
    {
        if (InternalCheckNotInitialized())
        {
            onPurchaseFinished?.Invoke(null);
            return;
        }

        InternalSetCallback(onPurchaseFinished);
        string id = product.GetProductID(), payload = product.payload;
        var iapProduct = controller.products.WithID(id);

        if (iapProduct != null && iapProduct.availableToPurchase)
        {
            Debug.Log($"Purchasing: {product.title} (ID: {id} | Price: {product.GetLocalizedPrice()} | Type: {product.IAPProductType})");

            void onCacheSuccess(string jsonResponse, object cbObject)
            {
                controller.InitiatePurchase(iapProduct);
            }

            bc.AppStoreService.CachePurchasePayloadContext(APP_STORE,
                                                           id,
                                                           payload,
                                                           onCacheSuccess,
                                                           OnBrainCloudFailure("Unable to cache the purchase payload context on brainCloud!",
                                                                               () => InternalInvokeCallback(null)));
        }
        else
        {
            Debug.Log($"Product is not available! Cannot purchase: {product.title} (Exists? {iapProduct != null} | Available? {iapProduct?.availableToPurchase})");
            InternalInvokeCallback(null);
        }
    }

    /// <summary>
    /// Returns true if the user owns the given non-consumable product.
    /// </summary>
    public static bool OwnsNonconsumable(BCProduct product) => OwnsNonconsumable(product.GetProductID());

    /// <summary>
    /// Returns true if the user owns a non-consumable product by its store ID.
    /// </summary>
    public static bool OwnsNonconsumable(string id)
    {
        if (InternalCheckNotInitialized())
            return false;

#if !UNITY_EDITOR
        return controller.products.WithID(id) is Product nonconsumable &&
               nonconsumable.definition.type == ProductType.NonConsumable &&
               nonconsumable.hasReceipt;
#else
        return false;
#endif
    }

    /// <summary>
    /// Returns true if the user has an active subscription for the given product.
    /// </summary>
    public static bool HasSubscription(BCProduct product) => HasSubscription(product.GetProductID());

    /// <summary>
    /// Returns true if the user has an active subscription by its store ID.
    /// </summary>
    public static bool HasSubscription(string id)
    {
        if (InternalCheckNotInitialized())
            return false;

        // Unity IAP's SubscriptionManager is only supported on iOS and Google Play.
        // For macOS and Windows, use InventoryService.GetNoAdsSubscriptionStatus instead.
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
        if (controller.products.WithID(id) is Product subscription &&
            subscription.definition.type == ProductType.Subscription && subscription.hasReceipt)
        {
            var subscriptionManager = new SubscriptionManager(subscription, null);
            if (subscriptionManager.getSubscriptionInfo() is SubscriptionInfo info)
            {
                return info.isCancelled() != Result.True && info.isSubscribed() == Result.True;
            }
        }
#endif
        return false;
    }

    /// <summary>
    /// Calls the Cloud Code script <b>GetTransactionHistory</b> to retrieve the user's transaction history.
    /// </summary>
    public static void GetTransactionHistory(Action<BCTransactionPage> onGetHistory,
                                             int pageNumber = 1, int numPerPage = 50,
                                             Dictionary<string, object> sortCriteria = null)
    {
        const string SCRIPT_NAME = "GetTransactionHistory";

        sortCriteria ??= new Dictionary<string, object>()
        {
            { "createdAt", -1 }
        };

        void onSuccess(string jsonResponse, object _)
        {
            var data = (JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)["data"] as Dictionary<string, object>)
                ["response"] as Dictionary<string, object>;

            if (data.ContainsKey("success") && data["success"] is bool success && success)
            {
                var history = JsonReader.Deserialize<BCTransactionPage>(JsonWriter.Serialize(data["transactionPage"]));

                if (history.count <= 0)
                    Debug.Log("User has no transaction history.");

                HasErrorOccurred = false;
                onGetHistory(history);
                return;
            }

            Debug.Log("Was unable to retrieve transaction history for user.");
            HasErrorOccurred = true;
            onGetHistory(null);
        }

        bc.ScriptService
          .RunScript(SCRIPT_NAME,
                     JsonWriter.Serialize(new Dictionary<string, object>()
                     {
                         { "pagination",     new Dictionary<string, object>() {{ "rowsPerPage", numPerPage }, { "pageNumber", pageNumber }}},
                         { "searchCriteria", new Dictionary<string, object>() {{ "type", APP_STORE }}},
                         { "sortCriteria",   sortCriteria }
                     }),
                     onSuccess,
                     OnBrainCloudFailure("Unable to get transaction history from brainCloud!",
                                         () => { HasErrorOccurred = true; onGetHistory(null); }));
    }

    /// <summary>
    /// Gets a platform store extension (e.g. IGooglePlayStoreExtensions).
    /// </summary>
    public static T GetExtension<T>() where T : IStoreExtension
    {
        if (InternalCheckNotInitialized())
            return default;

        return extensions.GetExtension<T>();
    }

    #region IDetailedStoreListener

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        BrainCloudMarketplace.controller = controller;
        BrainCloudMarketplace.extensions = extensions;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("[IAP] Unity IAP initialized. Product states:");
        foreach (Product p in controller.products.all)
        {
            sb.AppendLine($"  {p.definition.id} | type={p.definition.type} | availableToPurchase={p.availableToPurchase} | price={p.metadata.localizedPriceString}");
        }
        Debug.Log(sb.ToString());

        InternalInvokeCallback(GetInventory());
    }

    public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        HasErrorOccurred = true;
        var errorMessage = $"Unity IAP failed to initialize. Reason: {error}.";
        if (!string.IsNullOrWhiteSpace(message))
            errorMessage += $"\nDetails: {message}";

        Debug.LogError(errorMessage);
        Debug.LogError("BrainCloudMarketplace cannot initialize.");

        InternalInvokeCallback(null);
        InternalDispose();
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        var product = args.purchasedProduct;
        Debug.Log($"Purchase Complete: {product.definition.id}; Receipt:\n{product.receipt}");

        var json = JsonReader.Deserialize<Dictionary<string, object>>(product.receipt);

#if !UNITY_EDITOR && UNITY_ANDROID
        json = JsonReader.Deserialize<Dictionary<string, object>>(json["Payload"].ToString());
        json = JsonReader.Deserialize<Dictionary<string, object>>(json["json"].ToString());

        if (json["productId"].ToString() == "no_ads" && json.ContainsKey("purchaseToken"))
            _pendingGooglePurchaseToken = json["purchaseToken"].ToString();

        bc.AppStoreService.VerifyPurchase(APP_STORE,
                                          JsonWriter.Serialize(new Dictionary<string, object>
                                          {
                                              { "productId", json["productId"]     },
                                              { "orderId",   json["orderId"]       },
                                              { "token",     json["purchaseToken"] },
                                              { "includeSubscriptionCheck", product.definition.type == ProductType.Subscription }
                                          }),
                                          OnVerifyPurchasesSuccess,
                                          OnBrainCloudFailure("Unable to verify purchase(s) with brainCloud!",
                                                              () => InternalInvokeCallback(null)));

        return PurchaseProcessingResult.Pending;
#elif !UNITY_EDITOR && (UNITY_IOS || UNITY_STANDALONE_OSX)
        string appleReceipt = json["Payload"].ToString();
        string iapProductId  = product.definition.id;

        if (iapProductId == "no_ads")
            _pendingAppleReceipt = appleReceipt;

        // CachePurchasePayloadContext must be called before VerifyPurchase on Apple platforms.
        // PurchaseProduct handles this for user-initiated purchases, but Unity IAP can also
        // replay pending transactions at startup (ones that were never confirmed), which
        // arrive here directly without going through PurchaseProduct first. Calling it here
        // every time covers both cases — for a normal purchase it's a harmless second write
        // of the same data; for a replayed transaction it's the step that was missing.
        BCProduct bcProduct  = FindInInventoryByProductId(iapProductId);
        string    payload    = bcProduct?.payload ?? string.Empty;

        bc.AppStoreService.CachePurchasePayloadContext(
            APP_STORE,
            iapProductId,
            payload,
            (string _, object __) =>
            {
                bc.AppStoreService.VerifyPurchase(APP_STORE,
                                                  JsonWriter.Serialize(new Dictionary<string, object>
                                                  {
                                                      { "receipt",                appleReceipt },
                                                      { "excludeOldTransactions", false        }
                                                  }),
                                                  OnVerifyPurchasesSuccess,
                                                  OnBrainCloudFailure("Unable to verify purchase(s) with brainCloud!",
                                                                      () => InternalInvokeCallback(null)));
            },
            OnBrainCloudFailure("Unable to cache purchase payload context on brainCloud!",
                                () => InternalInvokeCallback(null)));

        return PurchaseProcessingResult.Pending;
#elif !UNITY_EDITOR && UNITY_STANDALONE_WIN
        json = JsonReader.Deserialize<Dictionary<string, object>>(json["Payload"].ToString());

        bc.AppStoreService.VerifyPurchase(APP_STORE,
                                          JsonWriter.Serialize(new Dictionary<string, object>
                                          {
                                              { "orderId", json["orderId"] },
                                              { "token",   json["token"]   }
                                          }),
                                          OnVerifyPurchasesSuccess,
                                          OnBrainCloudFailure("Unable to verify purchase(s) with brainCloud!",
                                                              () => InternalInvokeCallback(null)));

        return PurchaseProcessingResult.Pending;
#else
        // Unity Editor fake store
        if (controller.products.WithID(product.definition.id) is Product purchased && purchased.hasReceipt)
        {
            foreach (var item in bcIventory)
            {
                if (product.definition.id == item.GetProductID())
                {
                    Debug.Log($"Purchase Transaction: {json["TransactionID"]}");
                    InternalInvokeCallback(new BCProduct[] { item });
                    return PurchaseProcessingResult.Complete;
                }
            }
        }

        HasErrorOccurred = true;
        InternalInvokeCallback(null);
        Debug.LogError("An unknown error occurred with the fake store.");

        return PurchaseProcessingResult.Complete;
#endif
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        HasErrorOccurred = true;
        Debug.LogError($"Purchase Failed. Product: {product.definition.id}. Reason: {failureReason}");
        InternalInvokeCallback(null);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        HasErrorOccurred = true;
        Debug.LogError($"Purchase Failed. Product: {product.definition.id}. Reason: {failureDescription.reason}" +
                       (!string.IsNullOrWhiteSpace(failureDescription.message) ? $"\nDetails: {failureDescription.message}" : string.Empty));
        InternalInvokeCallback(null);
    }

    #endregion

    #region brainCloud

    public static void OnVerifyPurchasesSuccess(string jsonResponse, object _)
    {
        var data = ((JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)
            ["data"] as Dictionary<string, object>)
            ["transactionSummary"] as Dictionary<string, object>)
            ["transactionDetails"];
        var details = JsonReader.Deserialize<Dictionary<string, object>[]>(JsonWriter.Serialize(data));

        List<string> failedTransactions = new();
        List<BCProduct> paidProducts = new();

        foreach (var transaction in details)
        {
            string status = string.Empty;
            string productId = transaction.ContainsKey("productId")  ? transaction["productId"].ToString()
                             : transaction.ContainsKey("product_id") ? transaction["product_id"].ToString()
                             : "UnknownProduct";

            if (transaction.ContainsKey("errorMessage") &&
                !string.IsNullOrWhiteSpace(transaction["errorMessage"].ToString()))
            {
                status = transaction["errorMessage"].ToString();
                if (status.ToLower().Contains("already") && status.ToLower().Contains("processed") &&
                    controller.products.WithID(productId) is Product product && product.hasReceipt)
                {
                    controller.ConfirmPendingPurchase(product);
                }
            }
            else if ((bool)transaction["processed"] == false)
            {
                status = "Could not process.";
            }
            else if (controller.products.WithID(productId) is Product confirmed && confirmed.hasReceipt)
            {
                status = "Could not confirm purchase!";
                foreach (var item in bcIventory)
                {
                    if (productId == item.GetProductID())
                    {
                        controller.ConfirmPendingPurchase(confirmed);
                        paidProducts.Add(item);
                        status = string.Empty;
#if UNITY_ANDROID
                        if (productId == "no_ads" && !string.IsNullOrEmpty(_pendingGooglePurchaseToken))
                        {
                            string token = _pendingGooglePurchaseToken;
                            _pendingGooglePurchaseToken = null;
                            bc.PlayerStateService.UpdateAttributes(
                                JsonWriter.Serialize(new Dictionary<string, object> { { "googlePurchaseToken_no_ads", token } }),
                                false,
                                (_, __) => Debug.Log("Saved googlePurchaseToken_no_ads to user attributes."),
                                (_, __, jsonError, ___) => Debug.LogError($"Failed to save googlePurchaseToken_no_ads: {jsonError}"));
                        }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
                        if (productId == "no_ads" && !string.IsNullOrEmpty(_pendingAppleReceipt))
                        {
                            string receipt = _pendingAppleReceipt;
                            _pendingAppleReceipt = null;
                            bc.PlayerStateService.UpdateAttributes(
                                JsonWriter.Serialize(new Dictionary<string, object> { { "appleReceipt_no_ads", receipt } }),
                                false,
                                (_, __) => Debug.Log("Saved appleReceipt_no_ads to user attributes."),
                                (_, __, jsonError, ___) => Debug.LogError($"Failed to save appleReceipt_no_ads: {jsonError}"));
                        }
#endif
                        break;
                    }
                }
            }
            else
            {
                status = "Unknown Error";
            }

            if (!string.IsNullOrWhiteSpace(status))
                failedTransactions.Add($"{productId} - {status}");
        }

        if (failedTransactions.Count > 0)
        {
            HasErrorOccurred = true;
            string failedMessage = "One or more purchases were unable to be fully processed:";
            foreach (var t in failedTransactions)
                failedMessage += $"\n{t}";
            Debug.Log(failedMessage);
        }
        else
        {
            Debug.Log("Purchase(s) verified with brainCloud!");
        }

        InternalInvokeCallback(paidProducts.Count > 0 ? paidProducts.ToArray() : null);
    }

    private static FailureCallback OnBrainCloudFailure(string logError = "", Action failCallback = null)
    {
        return (int status, int reason, string jsonError, object _) =>
        {
            HasErrorOccurred = true;
            var error = JsonReader.Deserialize<Dictionary<string, object>>(jsonError);
            var message = (string)error["status_message"];

            Debug.LogError($"Status: {status} | Reason: {reason} | Message:\n{message}");

            if (!string.IsNullOrWhiteSpace(logError))
                Debug.LogError(logError);

            failCallback?.Invoke();
        };
    }

    #endregion

    private static void InternalInitialize(Action<BCProduct[]> onInitialized = null)
    {
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
        if (Instance == null)
        {
            HasErrorOccurred = true;
            Debug.LogError("BrainCloudMarketplace: no Instance found in scene. Add it as a component on a GameObject.");
            onInitialized?.Invoke(null);
            return;
        }

        bc = BCManager.Instance.BCWrapper;

        if (bc == null || bc.Client == null || !bc.Client.IsInitialized())
        {
            HasErrorOccurred = true;
            Debug.LogError("BrainCloudMarketplace requires BCManager to be initialized before calling FetchProducts!");
            onInitialized?.Invoke(null);
            return;
        }

        FetchProducts(onInitialized);
#else
        HasErrorOccurred = true;
        Debug.Log("BrainCloudMarketplace is not supported on this platform.");
        onInitialized?.Invoke(null);
#endif
    }

    private static bool InternalCheckNotInitialized()
    {
        if (!IsInitialized)
        {
            Debug.LogError("BrainCloudMarketplace has not been initialized! Call FetchProducts() first.");
            return true;
        }

        HasErrorOccurred = false;
        return false;
    }

    private static void InternalSetCallback(Action<BCProduct[]> cbAction)
    {
        onProcessingFinished = cbAction;
    }

    private static void InternalInvokeCallback(BCProduct[] cbResult)
    {
        onProcessingFinished?.Invoke(cbResult);
        onProcessingFinished = null;
    }

    private static BCProduct FindInInventoryByProductId(string iapProductId)
    {
        if (bcIventory == null) return null;
        foreach (var p in bcIventory)
        {
            try { if (p.GetProductID() == iapProductId) return p; }
            catch { /* skip malformed product entries */ }
        }
        return null;
    }

    private static void InternalDispose()
    {
        bc = null;
        controller = null;
        extensions = null;
        onProcessingFinished = null;
        bcIventory = null;
    }
}
