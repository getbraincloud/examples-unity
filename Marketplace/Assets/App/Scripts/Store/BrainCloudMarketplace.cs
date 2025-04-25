using BrainCloud;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

/// <summary>
/// Makes use of <see cref="BrainCloudWrapper"/> to act as a bridge between brainCloud's Marketplace features and Unity IAP.
///
/// <br><seealso cref="BrainCloudWrapper"/></br>
/// <br><seealso cref="BrainCloudAppStore"/></br>
/// </summary>
public class BrainCloudMarketplace : IDetailedStoreListener
{
    private const string APP_STORE =
#if UNITY_ANDROID
        "googlePlay";
#elif UNITY_IOS
        "itunes";
#else
        "";
#endif

    private static BrainCloudMarketplace instance = null;

    private static BrainCloudWrapper bc = null;
    private static IStoreController controller = null;
    private static IExtensionProvider extensions = null;
    private static Action<BCProduct[]> onProcessingFinished = null;
    private static BCProduct[] bcIventory = null;

    /// <summary>
    /// Check to see if BrainCloudMarketplace is initialized.
    /// </summary>
    public static bool IsInitialized => instance != null;

    /// <summary>
    /// Check to see if there was an error with the recently made function call.
    /// Will be set before your callbacks are returned.
    /// </summary>
    public static bool HasErrorOccurred { get; private set; } = false;

    private BrainCloudMarketplace() { }

    /// <summary>
    /// Fetches the products that are available on brainCloud and matches them with your available products on <b>Google Play Store</b>.
    /// </summary>
    /// <param name="onFetchFinished">Your callback that will process the fetched <see cref="BCProduct"/>(s).
    /// Returns <b>null</b> if there is an error or if your brainCloud products are not configured.</param>
    public static void FetchProducts(Action<BCProduct[]> onFetchFinished = null)
    {
        static void onFetchSuccess(string jsonResponse, object cbObject)
        {
            // Products created in brainCloud's Marketplace portal get stored as an array under data > productInventory
            var data = (JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)["data"] as Dictionary<string, object>)["productInventory"];
            bcIventory = JsonReader.Deserialize<BCProduct[]>(JsonWriter.Serialize(data));

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (var product in bcIventory)
            {
                builder.AddProduct(product.GetProductID(), product.IAPProductType);
            }

            UnityPurchasing.Initialize(instance, builder);
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
    /// Gets the fetched inventory products and matches them with the products available from <b>Google Play Store</b>.
    /// </summary>
    public static BCProduct[] GetInventory()
    {
        if (InternalCheckNotInitialized())
        {
            return null;
        }
        else if (bcIventory == null || bcIventory.Length == 0)
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
    /// Initiates the purchase process for the user.
    /// </summary>
    /// <param name="product">The product that is being purchased.</param>
    /// <param name="onPurchaseFinished">Your callback that will process the purchased <see cref="BCProduct"/>(s).
    /// Returns <b>null</b> if there is an error or if the user cancels the purchase.</param>
    public static void PurchaseProduct(BCProduct product, Action<BCProduct[]> onPurchaseFinished = null)
    {
        if (InternalCheckNotInitialized())
        {
            onPurchaseFinished?.Invoke(null);
            return;
        }

        InternalSetCallback(onPurchaseFinished);
        string id = product.GetProductID();
        var iapProduct = controller.products.WithID(id);

        if (iapProduct != null && iapProduct.availableToPurchase)
        {
            Debug.Log($"Purchasing: {product.title} (ID: {id} | Price: {product.GetLocalizedPrice()} | Type: {product.IAPProductType})");

            controller.InitiatePurchase(iapProduct);
        }
        else
        {
            Debug.Log($"Product is not available! Cannot purchse: {product.title} (Exists? {iapProduct != null} | Available? {iapProduct.availableToPurchase})");

            InternalInvokeCallback(null);
        }
    }

    /// <summary>
    /// Check to see if the user owns this nonconsumable product.
    /// 
    /// <para>
    /// <b>Note:</b> This checks against the <b>Google Play Store</b>! Make sure your user has
    /// their Google Play account associated with their brainCloud account to avoid any issues.
    /// </para>
    /// </summary>
    /// <returns><b>True</b> if the user already owns this nonconsumable product. <b>False</b> otherwise.</returns>
    public static bool OwnsNonconsumable(BCProduct product) => OwnsNonconsumable(product.GetProductID());

    /// <summary>
    /// Check to see if the user owns this nonconsumable product via its <b>Google Play Store</b> ID.
    /// 
    /// <para>
    /// <b>Note:</b> This checks against the <b>Google Play Store</b>! Make sure your user has
    /// their Google Play account associated with their brainCloud account to avoid any issues.
    /// </para>
    /// </summary>
    /// <returns><b>True</b> if the user already owns this nonconsumable product. <b>False</b> otherwise.</returns>
    public static bool OwnsNonconsumable(string id)
    {
        if (InternalCheckNotInitialized())
        {
            return false;
        }
#if !UNITY_EDITOR
        return controller.products.WithID(id) is Product nonconsumable &&
               nonconsumable.definition.type == ProductType.NonConsumable &&
               nonconsumable.hasReceipt;
#else
        return false;
#endif
    }

    /// <summary>
    /// Check to see if the user is currently subscribed to this product.
    /// 
    /// <para>
    /// <b>Note:</b> This checks against the <b>Google Play Store</b>! Make sure your user has
    /// their Google Play account associated with their brainCloud account to avoid any issues.
    /// </para>
    /// </summary>
    /// <returns><b>True</b> if the user is already subscribed to this product. <b>False</b> otherwise.</returns>
    public static bool HasSubscription(BCProduct product) => HasSubscription(product.GetProductID());

    /// <summary>
    /// Check to see if the user is currently subscribed to this product via its <b>Google Play Store</b> ID.
    /// 
    /// <para>
    /// <b>Note:</b> This checks against the <b>Google Play Store</b>! Make sure your user has
    /// their Google Play account associated with their brainCloud account to avoid any issues.
    /// </para>
    /// </summary>
    /// <returns><b>True</b> if the user is already subscribed to this product. <b>False</b> otherwise.</returns>
    public static bool HasSubscription(string id)
    {
        if (InternalCheckNotInitialized())
        {
            return false;
        }
#if !UNITY_EDITOR
        else if (controller.products.WithID(id) is Product subscription &&
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
    /// 
    /// </summary>
    /// <param name="onGetHistory"></param>
    /// <param name="pageNumber"></param>
    /// <param name="numPerPage"></param>
    /// <param name="sortCriteria"></param>
    public static void GetTransactionHistory(Action<bool, BCTransactionPage> onGetHistory,
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
                {
                    Debug.Log("User has no transaction history.");
                }

                onGetHistory(true, history);
                return;
            }

            Debug.Log("Was unable to retreive transaction history for user.");
            onGetHistory(false, null);
        }

        bc.ScriptService
          .RunScript(SCRIPT_NAME,
                     JsonWriter.Serialize(new Dictionary<string, object>()
                     {
                         { "pagination",     new Dictionary<string, object>() {{ "rowsPerPage", numPerPage }, { "pageNumber",  pageNumber }}},
                         { "searchCriteria", new Dictionary<string, object>() {{ "type", APP_STORE }}},
                         { "sortCriteria",   sortCriteria }
                     }),
                     onSuccess,
                     OnBrainCloudFailure("Unable to get transaction history from brainCloud!",
                                         () => onGetHistory(false, null)));
    }

    /// <summary>
    /// Get any store extensions that are associated with the <b>Google Play Store</b>.
    /// </summary>
    public static T GetExtension<T>() where T : IStoreExtension
    {
        if (InternalCheckNotInitialized())
        {
            return default;
        }

        return extensions.GetExtension<T>();
    }

    #region IDetailedStoreListener

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        BrainCloudMarketplace.controller = controller;
        BrainCloudMarketplace.extensions = extensions;

        Debug.Log("Unity IAP & BrainCloudMarketplace initialized/updated.");

        InternalInvokeCallback(GetInventory());
    }

    public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        HasErrorOccurred = true;
        var errorMessage = $"Unity IAP failed to initialize. Reason: {error}.";
        if (string.IsNullOrWhiteSpace(message))
        {
            errorMessage += $"\nDetails: {message}";
        }

        Debug.LogError(errorMessage);
        Debug.LogError("BrainCloudMarketplace cannot initialize.");

        InternalInvokeCallback(null);
        InternalDispose();
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        // Retrieve the purchased product
        var product = args.purchasedProduct;
        Debug.Log($"Purchase Complete: {product.definition.id}; Receipt:\n{product.receipt}");

        var json = JsonReader.Deserialize<Dictionary<string, object>>(product.receipt);
#if !UNITY_EDITOR && UNITY_ANDROID
        json = JsonReader.Deserialize<Dictionary<string, object>>(json["Payload"].ToString());
        json = JsonReader.Deserialize<Dictionary<string, object>>(json["json"].ToString());

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
#elif !UNITY_EDITOR && UNITY_IOS
        bc.AppStoreService.VerifyPurchase(APP_STORE,
                                          JsonWriter.Serialize(new Dictionary<string, object>
                                          {
                                              { "receipt",                json["Payload"] },
                                              { "excludeOldTransactions", false           }
                                          }),
                                          OnVerifyPurchasesSuccess,
                                          OnBrainCloudFailure("Unable to verify purchase(s) with brainCloud!",
                                                              () => InternalInvokeCallback(null)));

        return PurchaseProcessingResult.Pending;
#else
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
        Debug.LogError("An unknown error occurred with fake store.");

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
            string productId = transaction.ContainsKey("productId") ? transaction["productId"].ToString()   // googlePlay
                             : transaction.ContainsKey("product_id") ? transaction["product_id"].ToString() // itunes
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
            else if (controller.products.WithID(productId) is Product product && product.hasReceipt)
            {
                status = "Could not confirm pruchase!";
                foreach (var item in bcIventory)
                {
                    if (productId == item.GetProductID())
                    {
                        controller.ConfirmPendingPurchase(product);
                        paidProducts.Add(item);
                        status = string.Empty;
                        break;
                    }
                }
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
            HasErrorOccurred = true;
            string failedMessage = "One or more purchases were unable to be fully processed:";
            for (int i = 0; i < failedTransactions.Count; i++)
            {
                failedMessage += $"\n{failedTransactions[i]}";
            }

            Debug.Log(failedMessage);
        }
        else
        {
            Debug.Log($"Purchase(s) verified with brainCloud!");
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
            {
                Debug.LogError(logError);
            }

            failCallback?.Invoke();
        };
    }

    #endregion

    private static void InternalInitialize(Action<BCProduct[]> onInitialized = null)
    {
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
        instance = new();
        bc = UnityEngine.Object.FindObjectOfType<BrainCloudWrapper>();

        if (bc == null || bc.Client == null || !bc.Client.IsInitialized())
        {
            HasErrorOccurred = true;
            Debug.LogError("BrainCloudMarketplace requires BrainCloudWrapper to be loaded properly before being used!");
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

    private static void InternalDispose()
    {
        bc = null;
        controller = null;
        extensions = null;
        onProcessingFinished = null;
        bcIventory = null;

        instance = null;
    }
}
