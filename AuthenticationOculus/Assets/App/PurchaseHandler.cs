using BrainCloud.JsonFx.Json;
using Oculus.Platform;
using Oculus.Platform.Models;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PurchaseHandler : MonoBehaviour
{
    private const string INFO_LOG_INTO_BC = "Log In to\nbrainCloud\nto start IAP";
    private const string INFO_LOADING_PURCHASING = "Loading IAP\nPurchasing...";

    //private static string[] IAP_ITEMS = new string[]
    //{
    //    "gems_up_100",   // Consumable
    //    "nonconsumable", // Durable
    //    "game_pass"      // Subscription
    //};

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
        if (FindFirstObjectByType<BrainCloudWrapper>() is var bc && bc != null)
        {
            BC = bc;
        }

        if (BC != null && BC.Client.IsAuthenticated())
        {
            StartCoroutine(GetIAPItems());
        }
    }

    private void Start()
    {
        IAPItems = new();

        enabled = false;
    }

    private void OnDisable()
    {
        WaitContent.SetActive(true);
        PurchaseContent.SetActive(false);

        UpdateInfoLabel(INFO_LOG_INTO_BC);
    }

    private void OnDestroy()
    {
        BC = null;

        if (IAPItems != null && IAPItems.Count > 0)
        {
            for (int i = 0; i < IAPItems.Count; i++)
            {
                Destroy(IAPItems[i].gameObject);
                IAPItems[i] = null;
            }

            IAPItems.Clear();
            IAPItems = null;
        }
    }

    private void UpdateInfoLabel(string info)
    {
        InfoLabel.text = info;
    }

    private IEnumerator GetIAPItems()
    {
        MainCanvas.interactable = false;

        UpdateInfoLabel(INFO_LOADING_PURCHASING);

        yield return new WaitForFixedUpdate();

        // Clear IAP if we are calling this again
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

        // First lets get the IAP from brainCloud
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
                        }
                    }
                }

                // Remove any that aren't synced

                isSuccess = true;
            }
        });

        yield return new WaitUntil(() => isSuccess);
        yield return new WaitForFixedUpdate();

        // Verify purchases; this will consume consumables and set durables to unpurchasable
        isSuccess = false;
        IAP.GetViewerPurchases().OnComplete((msg) =>
        {
            if (msg.IsError)
            {
                DisplayError(msg.GetError().Message);
                return;
            }
            else
            {
                foreach (Purchase iap in msg.GetPurchaseList())
                {
                    foreach (IAPItem item in IAPItems)
                    {
                        if (iap.Sku == item.Product.IAPSku)
                        {
                            BC.AppStoreService
                              .VerifyPurchase("metaHorizon",
                                              JsonWriter.Serialize(new Dictionary<string, object>
                                              {
                                                  { "userId", LoginHandler.MetaUserID },
                                                  { "sku",    iap.Sku                 }
                                              }),
                                              OnVerifyPurchaseSuccess,
                                              OnBrainCloudFailure);
                        }
                    }
                }

                isSuccess = true;
            }
        });

        yield return new WaitUntil(() => isSuccess);
        yield return new WaitForFixedUpdate();

        WaitContent.SetActive(false);
        PurchaseContent.SetActive(true);

        MainCanvas.interactable = true;
    }

    private void AddIAPItem(BCProduct product)
    {
        var item = Instantiate(IAPItemTemplate, IAPContent, false);
        item.InitializeIAPItem(product,
                               (sku) => StartCoroutine(HandlePurchaseFlow(sku)));
        item.gameObject.SetActive(true);

        IAPItems.Add(item);
    }

    private IEnumerator HandlePurchaseFlow(string sku)
    {
        MainCanvas.interactable = false;

        yield return new WaitForFixedUpdate();

        bool isSuccess = false;
        IAP.LaunchCheckoutFlow(sku).OnComplete((msg) =>
        {
            if (msg.IsError)
            {
                if (msg.GetError().Message is string message)
                {
                    if (message.Contains("user_cancelled"))
                    {
                        WaitContent.SetActive(true);
                        PurchaseContent.SetActive(false);

                        UpdateInfoLabel($"Purchase Cancelled");

                        isSuccess = true;
                    }
                    else
                    {
                        DisplayError(msg.GetError().Message);
                    }
                }
            }
            else
            {
                WaitContent.SetActive(true);
                PurchaseContent.SetActive(false);

                Purchase purchase = msg.GetPurchase();

                UpdateInfoLabel($"You Purchased:\n{purchase.Sku}");

                Debug.Log("Purchased Product:\n" +
                         $"Sku: {purchase.Sku}\n" +
                         $"Type: {purchase.Type}\n" +
                         $"ID: {purchase.ID}\n" +
                         $"ReportingId: {purchase.ReportingId}\n" +
                         $"GrantTime: {purchase.GrantTime.ToLongDateString()} {purchase.GrantTime.ToLongTimeString()}\n" +
                         $"ExpirationTime: {purchase.ExpirationTime.ToLongDateString()} {purchase.ExpirationTime.ToLongTimeString()}\n" +
                         $"DeveloperPayload:\n{purchase.DeveloperPayload}");

                isSuccess = true;
            }
        });

        yield return new WaitUntil(() => isSuccess);
        yield return new WaitForSecondsRealtime(5.0f);

        yield return GetIAPItems();
    }

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

        }
    }

    private void OnBrainCloudFailure(int status, int reasonCode, string jsonError, object cbObject)
    {
        StopAllCoroutines();

        var error = JsonReader.Deserialize(jsonError) as Dictionary<string, object>;

        DisplayError($"Error Received - Status: {status} || Reason {reasonCode} || Message:\n{error["status_message"]}");
    }

    private void DisplayError(string msg)
    {
        Debug.LogError(msg);

        WaitContent.SetActive(false);
        PurchaseContent.SetActive(false);
        ErrorMessage.gameObject.SetActive(true);

        ErrorMessage.text = string.IsNullOrWhiteSpace(ErrorMessage.text) ? msg
                          : ErrorMessage.text + $"\n{msg}";
    }
}
