using BrainCloud;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class BrainCloudMarketplace : IDetailedStoreListener
{
    private static BrainCloudMarketplace instance = null;

    private static BrainCloudWrapper bc = null;
    private static IStoreController controller = null;
    private static IExtensionProvider extensions = null;
    private static Action<BCProduct[]> onProcessingFinished = null;
    private static BCProduct[] bcIventory = null;

    public static bool IsInitialized => instance != null;
    public static bool ErrorOccurred { get; private set; } = false;

    private BrainCloudMarketplace() { }

    public static void Initialize(BrainCloudWrapper bcWrapper, Action<BCProduct[]> onInitialized = null)
    {
#if UNITY_EDITOR || UNITY_ANDROID
        if (IsInitialized)
        {
            Debug.Log("BrainCloudMarketplace has already been initialized.");
            onInitialized?.Invoke(GetInventory());
            return;
        }

        instance = new();
        bc = bcWrapper;

        FetchProducts(onInitialized);
#else
        ErrorOccurred = true;
        Debug.Log("BrainCloudMarketplace is not supported on this platform.");
        onInitialized?.Invoke(null);
#endif
    }

    public static void FetchProducts(Action<BCProduct[]> onFetchFinished = null)
    {
        if (InternalInitializedCheck())
        {
            onFetchFinished?.Invoke(null);
            return;
        }

        static void onSuccess(string jsonResponse, object cbObject)
        {
            // Products created in brainCloud's Marketplace portal get stored as an array under data > productInventory
            var data = (JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)["data"] as Dictionary<string, object>)["productInventory"];
            bcIventory = JsonReader.Deserialize<BCProduct[]>(JsonWriter.Serialize(data));

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (var product in bcIventory)
            {
                builder.AddProduct(product.GetGooglePlayPriceData().id, product.IAPProductType);
            }

            UnityPurchasing.Initialize(instance, builder);
        };

        InternalSetCallback(onFetchFinished);
        bc.AppStoreService.GetSalesInventory("googlePlay",
                                             "{\"userCurrency\":\"CAD\"}",
                                             onSuccess,
                                             OnBrainCloudFailure("Unable to fetch products from brainCloud!",
                                                                 () => InternalInvokeCallback(null)));
    }

    public static BCProduct[] GetInventory()
    {
        if (InternalInitializedCheck())
        {
            return null;
        }

        List<BCProduct> updated = new(bcIventory);
        for (int i = 0; i < bcIventory.Length; i++)
        {
            string id = bcIventory[i].GetGooglePlayPriceData().id;
            if (controller.products.WithID(id) is not Product iapProduct || !iapProduct.availableToPurchase)
            {
                updated.Remove(bcIventory[i]);
            }
        }

        bcIventory = updated.Count > 0 ? new BCProduct[updated.Count] : null;
        for(int i = 0; i < updated.Count; i++)
        {
            bcIventory[i] = updated[i];
        }

        return bcIventory;
    }

    public static void PurchaseProduct(BCProduct product, Action<BCProduct[]> onPurchaseFinished = null)
    {
        if (InternalInitializedCheck())
        {
            onPurchaseFinished?.Invoke(null);
            return;
        }

        InternalSetCallback(onPurchaseFinished);
        string id = product.GetGooglePlayPriceData().id;
        var iapProduct = controller.products.WithID(id);

        if (iapProduct != null && iapProduct.availableToPurchase)
        {
            Debug.Log($"Purchasing: {product.title} (ID: {id} | Price: {product.GetGooglePlayPriceData().GetIAPPrice()} | Type: {product.IAPProductType})");

            controller.InitiatePurchase(iapProduct);
        }
        else
        {
            Debug.Log($"Product is not available! Cannot purchse: {product.title} (Product exists: {iapProduct != null} | Available? {iapProduct.availableToPurchase})");

            InternalInvokeCallback(null);
        }
    }

    public static bool HasSubscription(string productId)
    {
        if (InternalInitializedCheck())
        {
            return false;
        }
        else if (controller.products.WithID(productId) is Product subscription &&
                 subscription.definition.type == ProductType.Subscription && subscription.hasReceipt)
        {
            var subscriptionManager = new SubscriptionManager(subscription, null);
            if (subscriptionManager.getSubscriptionInfo() is SubscriptionInfo info)
            {
                return info.isCancelled() != Result.True && info.isSubscribed() == Result.True;
            }
        }

        return false;
    }

    public static T GetExtension<T>() where T : IStoreExtension
    {
        if (InternalInitializedCheck())
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
        ErrorOccurred = true;
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
#if !UNITY_EDITOR
        json = JsonReader.Deserialize<Dictionary<string, object>>(json["Payload"].ToString());
        json = JsonReader.Deserialize<Dictionary<string, object>>(json["json"].ToString());

        bc.AppStoreService.VerifyPurchase("googlePlay",
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
#else
        if (controller.products.WithID(product.definition.id) is Product purchased && purchased.hasReceipt)
        {
            foreach (var item in bcIventory)
            {
                if (product.definition.id == item.GetGooglePlayPriceData().id)
                {
                    Debug.Log($"Purchase Transaction: {json["TransactionID"]}");
                    InternalInvokeCallback(new BCProduct[] { item });
                    return PurchaseProcessingResult.Complete;
                }
            }
        }

        ErrorOccurred = true;
        InternalInvokeCallback(null);
        Debug.LogError("An unknown error occurred with fake store.");

        return PurchaseProcessingResult.Complete;
#endif
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        ErrorOccurred = true;
        Debug.LogError($"Purchase Failed. Product: {product.definition.id}. Reason: {failureReason}");

        InternalInvokeCallback(null);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        ErrorOccurred = true;
        Debug.LogError($"Purchase Failed. Product: {product.definition.id}. Reason: {failureDescription.reason}" +
                       (!string.IsNullOrWhiteSpace(failureDescription.message) ? $"\nDetails: {failureDescription.message}" : string.Empty));

        InternalInvokeCallback(null);
    }

#endregion

    #region brainCloud

    private static void OnVerifyPurchasesSuccess(string jsonResponse, object cbObject)
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
            string productId = transaction["productId"].ToString();

            if (transaction.ContainsKey("errorMessage") &&
                !string.IsNullOrWhiteSpace(transaction["errorMessage"].ToString()))
            {
                status = transaction["errorMessage"].ToString();
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
                    if (productId == item.GetGooglePlayPriceData().id)
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
            ErrorOccurred = true;
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
            ErrorOccurred = true;
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

    private static bool InternalInitializedCheck()
    {
        if (!IsInitialized)
        {
            Debug.LogError("BrainCloudMarketplace has not been initialized! Call BrainCloudMarketplace.Initialize() first.");
            return true;
        }

        ErrorOccurred = false;
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
        bc.StopCoroutine(InternalConfirmingPurchase());

        bc = null;
        controller = null;
        extensions = null;
        processingProduct = null;
        onProcessingFinished = null;
        bcIventory = null;

        instance = null;
    }
}
