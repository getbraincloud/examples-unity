using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using Oculus.Platform;
using Oculus.Platform.Models;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PurchaseHandler : MonoBehaviour
{
    private const bool CONSUME_ON_BRAINCLOUD_VERIFY = true;

    private const string INFO_LOG_INTO_BC = "Log In to\nbrainCloud\nto start IAP";
    private const string INFO_LOADING_PURCHASING = "Loading IAP\nPurchasing...";
    private const string INFO_IAP_AVAILABILITY = "IAP is only available on\nQuest Devices";

    [SerializeField] private CanvasGroup MainCanvas = null;
    [SerializeField] private TMP_Text ErrorMessage = null;
    [SerializeField] private GameObject WaitContent = null;

    [Header("Purchase Content")]
    [SerializeField] private TMP_Text InfoLabel = null;
    [SerializeField] private GameObject PurchaseContent = null;
    [SerializeField] private Transform IAPContent = null;
    [SerializeField] private IAPItem IAPItemTemplate = null;

    private BrainCloudWrapper BC = null;
    private List<IAPItem> IAPItems = null;

    // For User purchases management
    private const string ENTITY_TYPE = "purchases";
    private string UserPurchasesEntityID = string.Empty;
    private Dictionary<string, object> UserPurchases = null;

    private void Awake()
    {
        MainCanvas.interactable = false;

        ErrorMessage.text = string.Empty;
        ErrorMessage.gameObject.SetActive(false);

        WaitContent.SetActive(true);
        PurchaseContent.SetActive(false);
        IAPItemTemplate.gameObject.SetActive(false);

        UpdateInfoLabel(INFO_LOG_INTO_BC);
    }

    private void OnEnable()
    {
#if UNITY_EDITOR || !UNITY_ANDROID
        UpdateInfoLabel(INFO_IAP_AVAILABILITY);
#else
        if (FindFirstObjectByType<BrainCloudWrapper>() is var bc && bc != null)
        {
            BC = bc;
        }

        if (BC != null && BC.Client.IsAuthenticated())
        {
            StartCoroutine(GetIAPItems());
        }
#endif
    }

    private void Start()
    {
        IAPItems = new();
        UserPurchases = new();

        enabled = false;
    }

    private void OnDisable()
    {
        if (WaitContent != null && PurchaseContent != null)
        {
            WaitContent.SetActive(true);
            PurchaseContent.SetActive(false);

            UpdateInfoLabel(INFO_LOG_INTO_BC);
        }
    }

    private void OnDestroy()
    {
        BC = null;

        if (IAPItems != null && IAPItems.Count > 0)
        {
            for (int i = 0; i < IAPItems.Count; i++)
            {
                if (IAPItems[i] != null && IAPItems[i].gameObject != null)
                {
                    Destroy(IAPItems[i].gameObject);
                }
                IAPItems[i] = null;
            }

            IAPItems.Clear();
            IAPItems = null;
        }

        UserPurchases?.Clear();
        UserPurchases = null;
    }

    private void UpdateInfoLabel(string info)
    {
        InfoLabel.text = info;
    }

    private void AddIAPItem(BCProduct product)
    {
        var item = Instantiate(IAPItemTemplate, IAPContent, false);
        item.InitializeIAPItem(product,
                               (sku) => StartCoroutine(HandlePurchaseFlow(sku)));
        item.gameObject.SetActive(true);

        IAPItems.Add(item);
    }

    private Product GetMetaProduct(string sku)
    {
        foreach (var item in IAPItems)
        {
            if (sku == item.IAPSku)
            {
                return item.Product.MetaProduct;
            }
        }

        return null;
    }

    private bool UserPurchasesContainsDurable(string sku)
    {
        if (UserPurchases.ContainsKey("durables") &&
            UserPurchases["durables"] is object[] durables &&
            durables != null && durables.Length > 0)
        {
            foreach (object durable in durables)
            {
                if (durable.ToString() == sku)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool UserPurchasesContainsSubscription(string sku)
    {
        if (UserPurchases.ContainsKey("subscriptions") &&
            UserPurchases["subscriptions"] is object[] subscriptions &&
            subscriptions != null && subscriptions.Length > 0)
        {
            foreach (object subscription in subscriptions)
            {
                if (subscription.ToString() == sku)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void UpdateUserPurchases(string sku, ProductType type)
    {
        // Lets get the type
        string key = string.Empty;
        switch (type)
        {
            case ProductType.DURABLE:
                key = "durables";
                break;
            case ProductType.SUBSCRIPTION:
                key = "subscriptions";
                break;
            default:
                Debug.LogError($"This ProductType isn't stored for the user: {type}");
                return;
        }

        // We'll go through our purchases to see if there's a matching sku in that type list
        if (UserPurchases.ContainsKey(key) && UserPurchases[key] is object[] skus)
        {
            List<string> updatedSkus = new();
            foreach (var product in skus)
            {
                if (product.ToString() == sku)
                {
                    Debug.LogError($"User already owns SKU: {sku} ({type})");
                    updatedSkus.Clear();
                    updatedSkus = null;
                    return;
                }

                updatedSkus.Add(product.ToString());
            }

            updatedSkus.Add(sku);
            UserPurchases[key] = updatedSkus.ToArray();
        }
        else // If the type isn't in the purchases then it's a new purchase
        {
            UserPurchases.Add(key, new string[] { sku });
        }

        BC.EntityService.UpdateEntity(UserPurchasesEntityID,
                                      ENTITY_TYPE,
                                      JsonWriter.Serialize(UserPurchases),
                                      ACL.None().ToJsonString(),
                                      -1,
                                      null,
                                      OnBrainCloudFailure);
    }

    #region Flows

    private IEnumerator GetIAPItems()
    {
        MainCanvas.interactable = false;

        UpdateInfoLabel(INFO_LOADING_PURCHASING);

        yield return new WaitForFixedUpdate();

        // Clear IAPItems if we are calling this again
        if (IAPItems != null && IAPItems.Count > 0)
        {
            for (int i = 0; i < IAPItems.Count; i++)
            {
                Destroy(IAPItems[i].gameObject);
                IAPItems[i] = null;
            }

            IAPItems.Clear();

            yield return new WaitForFixedUpdate();
        }

        // First let's get the user's current items
        yield return GetUserInventory();

        // Lets get the IAP products from brainCloud
        BC.AppStoreService.GetSalesInventory("metaHorizon",
                                             string.Empty,
                                             OnGetSalesInventorySuccess,
                                             OnBrainCloudFailure);

        yield return new WaitUntil(() => IAPItems != null && IAPItems.Count > 0);
        yield return new WaitForFixedUpdate();

        // Then sync up with the IAP from Meta Horizon
        string[] metaSkus = new string[IAPItems.Count];
        for (int i = 0; i < metaSkus.Length; i++)
        {
            metaSkus[i] = IAPItems[i].Product.GetProductID();
        }

        bool isSuccess = false;
        IAP.GetProductsBySKU(metaSkus).OnComplete((msg) =>
        {
            if (msg.IsError)
            {
                DisplayError("IAP.GetProductsBySKU Error:");
                DisplayError(msg.GetError().Message);
            }
            else
            {
                // Sync BC Products with Meta Products
                foreach (Product iap in msg.GetProductList())
                {
                    foreach (var item in IAPItems)
                    {
                        if (item.Product.GetProductID() == iap.Sku)
                        {
                            item.Product.SetOculusProduct(iap);
                            item.UpdatePrice();
                        }
                    }
                }

                // Remove any that aren't synced
                List<IAPItem> toRemove = new(IAPItems.Count);
                foreach (var item in IAPItems)
                {
                    if (string.IsNullOrWhiteSpace(item.IAPSku))
                    {
                        toRemove.Add(item);
                    }
                }

                foreach (var item in toRemove)
                {
                    IAPItems.Remove(item);
                }

                toRemove.Clear();
                isSuccess = true;
            }
        });

        yield return new WaitUntil(() => isSuccess);
        yield return new WaitForFixedUpdate();

        // Next we need to get our confirmed Meta Horizon purchases and queue them up for brainCloud
        isSuccess = false;
        Dictionary<BCProduct, string> cachedPurchases = new();
        IAP.GetViewerPurchases().OnComplete((msg) =>
        {
            if (msg.IsError)
            {
                DisplayError("IAP.GetViewerPurchases Error:");
                DisplayError(msg.GetError().Message);
                return;
            }
            else if (msg.GetPurchaseList() == null ||
                     msg.GetPurchaseList().Count <= 0)
            {
                isSuccess = true;
            }
            else
            {
                foreach (Purchase purchase in msg.GetPurchaseList())
                {
                    foreach (IAPItem item in IAPItems)
                    {
                        if (purchase.Sku == item.Product.IAPSku)
                        {
                            Debug.Log("Got User Purchase:\n" +
                                     $"Sku: {purchase.Sku}\n" +   // Required for AppStore.VerifyPurchase
                                     $"Type: {purchase.Type}\n" +
                                     $"ID: {purchase.ID}\n" +     // Required for AppStore.VerifyPurchase
                                     $"ReportingId: {purchase.ReportingId}\n" +
                                     $"GrantTime: {(ulong)BrainCloud.TimeUtil.UTCDateTimeToUTCMillis(purchase.GrantTime)}\n" +
                                     $"ExpirationTime: {(ulong)BrainCloud.TimeUtil.UTCDateTimeToUTCMillis(purchase.ExpirationTime)}\n" +
                                     $"DeveloperPayload:\n{purchase.DeveloperPayload}");

                            if (UserPurchasesContainsDurable(purchase.Sku) ||
                                UserPurchasesContainsSubscription(purchase.Sku))
                            {
                                item.SetToPurchased(); // So the user can't purcahse again
                            }
                            else
                            {
                                // Cache this info to be used once we have all of our purchases
                                cachedPurchases.Add(item.Product,
                                                    JsonWriter.Serialize(new Dictionary<string, object>
                                                    {
                                                        { "userId",          LoginHandler.MetaUserID      },
                                                        { "sku",             purchase.Sku                 },
                                                        { "transactionId",   purchase.ID                  },
                                                        { "consumeOnVerify", CONSUME_ON_BRAINCLOUD_VERIFY }
                                                    }));
                            }

                            break;
                        }
                    }
                }

                isSuccess = true;
            }
        });

        yield return new WaitUntil(() => isSuccess);
        yield return new WaitForFixedUpdate();

        // Finally we will go through our purchases, cache the payloads, then verify purchases
        bool verifyLingering = false;
        foreach (BCProduct product in cachedPurchases.Keys)
        {
            isSuccess = false;
            verifyLingering = verifyLingering || product.IAPProductType == ProductType.DURABLE
                                              || product.IAPProductType == ProductType.SUBSCRIPTION;

            BC.AppStoreService
              .CachePurchasePayloadContext("metaHorizon",
                                           product.IAPSku,
                                           product.payload,
                                           (_, __) => isSuccess = true,
                                           OnBrainCloudFailure);

            yield return new WaitUntil(() => isSuccess);
            yield return new WaitForFixedUpdate();

            isSuccess = false;
            BC.AppStoreService
              .VerifyPurchase("metaHorizon",
                              cachedPurchases[product],
                              (jsonResponse, cbObject) =>
                              {
                                  OnVerifyPurchaseSuccess(jsonResponse, cbObject);
                                  isSuccess = true;
                              },
                              OnBrainCloudFailure);

            yield return new WaitUntil(() => isSuccess);
            yield return new WaitForFixedUpdate();
        }

        // If there are durables or subscriptions in our verify then we
        // will restart this process to update our UserPurchases list properly
        if (verifyLingering)
        {
            yield return GetIAPItems();
            yield break;
        }

        if (string.IsNullOrWhiteSpace(ErrorMessage.text))
        {
            WaitContent.SetActive(false);
            PurchaseContent.SetActive(true);

            MainCanvas.interactable = true;
        }
    }

    private IEnumerator GetUserInventory()
    {
        bool isSuccess = false;

        void onCreateUserInventory(string jsonResponse, object cbObject)
        {
            BC.EntityService.GetEntitiesByType(ENTITY_TYPE,
                                               onGetEntitiesByType,
                                               OnBrainCloudFailure);
        }

        void onGetEntitiesByType(string jsonResponse, object cbObject)
        {
            if (jsonResponse.Contains("purchases"))
            {
                var entity = ((JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)
                    ["data"] as Dictionary<string, object>)
                    ["entities"] as Dictionary<string, object>[])[0]; // We only want one of these entities tied to the user in this example

                // Get the current purchases object
                if (entity.ContainsKey("data") &&
                    entity["data"] is Dictionary<string, object> data &&
                    data != null)
                {
                    UserPurchases.Clear();
                    UserPurchases = null;
                    UserPurchases = data;
                }

                // Let's store the entityId for easy updating
                if (entity.ContainsKey("entityId") && entity["entityId"] is string id)
                {
                    UserPurchasesEntityID = id;
                }

                isSuccess = true;
            }
            else // If the user is new, we will create the entity before calling this again
            {
                BC.EntityService.CreateEntity(ENTITY_TYPE,
                                              JsonWriter.Serialize(UserPurchases),
                                              ACL.None().ToJsonString(),
                                              onCreateUserInventory,
                                              OnBrainCloudFailure);
            }
        }

        BC.EntityService.GetEntitiesByType(ENTITY_TYPE,
                                           onGetEntitiesByType,
                                           OnBrainCloudFailure);

        yield return new WaitUntil(() => isSuccess);
        yield return new WaitForFixedUpdate();
    }

    private IEnumerator HandlePurchaseFlow(string sku)
    {
        MainCanvas.interactable = false;

        yield return new WaitForFixedUpdate();

        // Launch the Meta Horizon purchase flow and wait for its OnComplete
        bool isSuccess = false;
        IAP.LaunchCheckoutFlow(sku).OnComplete((msg) =>
        {
            if (msg.IsError)
            {
                if (msg.GetError().Message.Contains("user_canceled"))
                {
                    WaitContent.SetActive(true);
                    PurchaseContent.SetActive(false);

                    UpdateInfoLabel($"Purchase Cancelled");

                    isSuccess = true;
                }
                else
                {
                    DisplayError("IAP.LaunchCheckoutFlow Error:");
                    DisplayError(msg.GetError().Message);
                }
            }
            else
            {
                WaitContent.SetActive(true);
                PurchaseContent.SetActive(false);

                Purchase purchase = msg.GetPurchase();

                UpdateInfoLabel($"You Purchased:\n{purchase.Sku}");

                Debug.Log("Purchased Product:\n" +
                         $"Sku: {purchase.Sku}\n" +   // Required for AppStore.VerifyPurchase
                         $"Type: {purchase.Type}\n" +
                         $"ID: {purchase.ID}\n" +     // Required for AppStore.VerifyPurchase
                         $"ReportingId: {purchase.ReportingId}\n" +
                         $"GrantTime: {(ulong)BrainCloud.TimeUtil.UTCDateTimeToUTCMillis(purchase.GrantTime)}\n" +
                         $"ExpirationTime: {(ulong)BrainCloud.TimeUtil.UTCDateTimeToUTCMillis(purchase.ExpirationTime)}\n" +
                         $"DeveloperPayload:\n{purchase.DeveloperPayload}");

                isSuccess = true;
            }
        });

        yield return new WaitUntil(() => isSuccess);
        yield return new WaitForSecondsRealtime(5.0f);

        yield return GetIAPItems();
    }

    #endregion

    #region brainCloud

    private void OnGetSalesInventorySuccess(string jsonResponse, object cbObject)
    {
        var data = (JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)["data"] as Dictionary<string, object>)["productInventory"];
        var inventory = JsonReader.Deserialize<BCProduct[]>(JsonWriter.Serialize(data));

        foreach (var item in inventory)
        {
            AddIAPItem(item);
        }
    }

    private void OnVerifyPurchaseSuccess(string jsonResponse, object cbObject)
    {
        // First let's get the transaction details from the call
        var data = ((JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)
            ["data"] as Dictionary<string, object>)
            ["transactionSummary"] as Dictionary<string, object>)
            ["transactionDetails"];
        var details = JsonReader.Deserialize<Dictionary<string, object>[]>(JsonWriter.Serialize(data));

        // We'll go through our transaction details and solve for all the potential scenarios
        List<string> failedTransactions = new();
        List<BCProduct> paidProducts = new();
        foreach (var transaction in details)
        {
            string status = string.Empty;
            string productId = transaction["sku"].ToString();
            
            if (transaction.ContainsKey("errorMessage") &&
                !string.IsNullOrWhiteSpace(transaction["errorMessage"].ToString()))
            {
                status = transaction["errorMessage"].ToString();

                // If the item's already been processed by brainCloud we still need to process it on our end
                if (status.ToLower().Contains("already") && status.ToLower().Contains("processed") &&
                    GetMetaProduct(productId) is Product product)
                {
                    foreach (var item in IAPItems)
                    {
                        if (productId == item.IAPSku)
                        {
                            paidProducts.Add(item.Product);
                            status = string.Empty;
                            break;
                        }
                    }
                }
            }
            else if ((bool)transaction["processed"] == false)
            {
                status = "Could not process.";
            }
            else if (GetMetaProduct(productId) is Product product)
            {
                status = "Could not confirm pruchase!";
                foreach (var item in IAPItems)
                {
                    if (productId == item.IAPSku)
                    {
                        paidProducts.Add(item.Product);
                        status = string.Empty;
                        break;
                    }
                }
            }
            else
            {
                status = "Unknown VerifyPurchase Error";
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                failedTransactions.Add($"{productId} - {status}");
            }
        }

        if (failedTransactions.Count > 0)
        {
            string failedMessage = "One or more purchases were unable to be fully processed:";
            for (int i = 0; i < failedTransactions.Count; i++)
            {
                failedMessage += $"\n{failedTransactions[i]}";
            }

            DisplayError(failedMessage);
        }
        else
        {
            Debug.Log($"Purchase(s) verified with brainCloud!");
        }

        foreach (var iap in paidProducts)
        {
            /*
             * brainCloud will consume CONSUMABLEs if
             * CONSUME_ON_BRAINCLOUD_VERIFY is set to true;
             * if set to false we will do it here
            */
            if (!CONSUME_ON_BRAINCLOUD_VERIFY &&
                iap.IAPProductType == ProductType.CONSUMABLE)
            {
                IAP.ConsumePurchase(iap.IAPSku);
            }

            /*
             * We'll add DURABLEs & SUBSCRIPTIONs to our
             * UserPurchases object for quick referencing
            */
            if (iap.IAPProductType == ProductType.DURABLE ||
                iap.IAPProductType == ProductType.SUBSCRIPTION)
            {
                UpdateUserPurchases(iap.IAPSku, iap.IAPProductType);
            }
        }
    }

    private void OnBrainCloudFailure(int status, int reasonCode, string jsonError, object cbObject)
    {
        var error = JsonReader.Deserialize(jsonError) as Dictionary<string, object>;

        DisplayError($"Error Received - Status: {status} || Reason {reasonCode} || Message:\n{error["status_message"]}");
    }

    #endregion

    private void DisplayError(string msg)
    {
        StopAllCoroutines();

        Debug.LogError(msg);

        WaitContent.SetActive(false);
        PurchaseContent.SetActive(false);
        ErrorMessage.gameObject.SetActive(true);

        ErrorMessage.text = string.IsNullOrWhiteSpace(ErrorMessage.text) ? msg
                          : ErrorMessage.text + $"\n{msg}";
    }
}
