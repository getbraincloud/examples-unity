using BrainCloud.JsonFx.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.UI;
using static ExampleApp;

public class StorePanel : MonoBehaviour, IDetailedStoreListener
{
    private const string OPENING_STORE_TEXT = "Getting products...";
    private const string NO_PRODUCTS_STORE_TEXT = "No products found.";
    private const string ERROR_STORE_TEXT = "Error trying to receive products.";

    [Header("UI Elements")]
    [SerializeField] private TMP_Text CurrencyInfoText = default;
    [SerializeField] private Button CloseStoreButton = default;
    [SerializeField] private Transform IAPContent = default;
    [SerializeField] private TMP_Text StoreInfoText = default;

    [Header("Templates")]
    [SerializeField] private IAPButton IAPButtonTemplate = default;

    private ExampleApp App = null;
    private BrainCloudWrapper BC = null;
    private IStoreController StoreController = null;
    private IExtensionProvider ExtensionProvider = null;

    #region Unity Messages

    private void OnEnable()
    {
        CloseStoreButton.onClick.AddListener(OnCloseStoreButton);

        if (BC != null)
        {
            GetProducts();
        }
    }

    private void Start()
    {
        App = GameObject.FindObjectOfType<ExampleApp>();

        StartCoroutine(GetBrainCloudWrapper());
    }

    private void OnDisable()
    {
        CloseStoreButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        BC = null;
        App = null;
        StoreController = null;
        ExtensionProvider = null;
    }

    #endregion

    private IEnumerator GetBrainCloudWrapper()
    {
        App.IsInteractable = false;

        yield return new WaitUntil(() =>
        {
            BC = BC ?? GameObject.FindObjectOfType<BrainCloudWrapper>();

            return BC != null && BC.Client != null && BC.Client.IsInitialized();
        });

        yield return null;

        GetProducts();
    }

#if UNITY_EDITOR
    private static readonly string TEST_RECEIPT = "{\"Payload\":\"{\\\"json\\\":\\\"{\\\\\\\"orderId\\\\\\\":\\\\\\\"GPA.3342-6433-1657-91103\\\\\\\",\\\\\\\"packageName\\\\\\\":\\\\\\\"com.brainCloud.Authentication2021\\\\\\\",\\\\\\\"productId\\\\\\\":\\\\\\\"gems_up_100\\\\\\\",\\\\\\\"purchaseTime\\\\\\\":1693019562881,\\\\\\\"purchaseState\\\\\\\":0,\\\\\\\"purchaseToken\\\\\\\":\\\\\\\"hekmihflhnilbingmelfgeji.AO-J1Owq6bqZTJ9nvWejKcAkV2VbBW0XpJq41TIJTEExLQ2bWHjfc1EGkaZ8DsP0B3tCjQ-dSbRSOoAdLyOHQdBOXcJ-R3xF7WiL1iUdt7ehMRytX8QZjDk\\\\\\\",\\\\\\\"quantity\\\\\\\":1,\\\\\\\"acknowledged\\\\\\\":false}\\\",\\\"signature\\\":\\\"TXpuf2PENVaudzWkoeR5nDx6p9TkQ4U40Q6mBZKTUa1iPRZ6v+vCPfNK6XV7FnexFSjCBQ4BQPBAzyBYEceZBu+62Y3N1aubGTNOUtPaHht0Xpnp9aRDu5yGr5j7FiwwXz7TVskgNfTn2R7wzeby/mCWmnHa2+eOQ2w1qTHX+yVy0ZGTOQTZvfttkptpFtOtbQWcXge1bawes73sXXYNTjNYJ/hgtKjSHRtGUvjHRiDOQ3kZDWAUyeGGyhyjzeQSak0bjtWI/nbjhIJrebdEr37jIz6WCLZDPZHpefS88dSRfcmhVdp02M2vonnlagDRL9y2H7DBuPnvhuIzMIkJTw==\\\",\\\"skuDetails\\\":[\\\"{\\\\\\\"productId\\\\\\\":\\\\\\\"gems_up_100\\\\\\\",\\\\\\\"type\\\\\\\":\\\\\\\"inapp\\\\\\\",\\\\\\\"title\\\\\\\":\\\\\\\"Gems +100 (Authentication2021)\\\\\\\",\\\\\\\"name\\\\\\\":\\\\\\\"Gems +100\\\\\\\",\\\\\\\"iconUrl\\\\\\\":\\\\\\\"https:\\\\\\\\/\\\\\\\\/lh3.googleusercontent.com\\\\\\\\/xPrjTR1v1c0QTbFPwu44_Rf9VwmIam_KbmTmGUDj4bOxi3F3t3KgiK-wnYMLUpoJVjdB\\\\\\\",\\\\\\\"description\\\\\\\":\\\\\\\"Increase your Gems by 100.\\\\\\\",\\\\\\\"price\\\\\\\":\\\\\\\"$1.39\\\\\\\",\\\\\\\"price_amount_micros\\\\\\\":1390000,\\\\\\\"price_currency_code\\\\\\\":\\\\\\\"CAD\\\\\\\",\\\\\\\"skuDetailsToken\\\\\\\":\\\\\\\"AEuhp4J-xONzdAHaUgX5xPAZWfEv2uxUozu95Qs5QatzFQG-rPomC888PNWhd8Q4tEBY\\\\\\\"}\\\"]}\",\"Store\":\"GooglePlay\",\"TransactionID\":\"hekmihflhnilbingmelfgeji.AO-J1Owq6bqZTJ9nvWejKcAkV2VbBW0XpJq41TIJTEExLQ2bWHjfc1EGkaZ8DsP0B3tCjQ-dSbRSOoAdLyOHQdBOXcJ-R3xF7WiL1iUdt7ehMRytX8QZjDk\"}";

    private void StoreTesting()
    {
        Debug.Log($"Purchase Complete! Product: gems_up_100; Receipt:\n{TEST_RECEIPT}");

        var json = JsonReader.Deserialize<Dictionary<string, object>>(TEST_RECEIPT);
        json = JsonReader.Deserialize<Dictionary<string, object>>(json["Payload"].ToString());
        json = JsonReader.Deserialize<Dictionary<string, object>>(json["json"].ToString());

        void onFailure(int status, int reason, string jsonError, object cbObject)
        {
            App.OnBrainCloudError(status, reason, jsonError, cbObject);

            Debug.LogError($"Unable to verify purchase(s) with brainCloud!");
        }

        BC.AppStoreService.VerifyPurchase("googlePlay",
                                          JsonWriter.Serialize(new Dictionary<string, object>
                                          {
                                              { "productId", json["productId"]     }, { "orderId", json["orderId"]        },
                                              { "token",     json["purchaseToken"] }, { "includeSubscriptionCheck", false }
                                          }),
                                          OnVerifyPurchasesSuccess,
                                          onFailure,
                                          this);
    }
#endif

    #region UI

    private void GetProducts()
    {
        App.IsInteractable = false;
        StoreInfoText.gameObject.SetActive(true);
        StoreInfoText.text = OPENING_STORE_TEXT;

        void onSuccess(string jsonResponse, object cbObject)
        {
            App.ChangePanelState(PanelState.Store);

            // Products created in brainCloud's Marketplace portal get stored as an array under data > productInventory
            var data = (JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)["data"] as Dictionary<string, object>)["productInventory"];
            var inventory = JsonReader.Deserialize<BCProduct[]>(JsonWriter.Serialize(data));

            // Enable Unity IAP and add the products
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (var product in inventory)
            {
                var iapButton = Instantiate(IAPButtonTemplate, IAPContent, false);
                iapButton.SetProductDetails(product);
                iapButton.IsInteractable = false;
                iapButton.gameObject.SetActive(false);

                builder.AddProduct(product.GetGooglePlayPriceData().id, product.IAPProductType);
            }

            UnityPurchasing.Initialize(this, builder);
        };

        BC.AppStoreService.GetSalesInventory("googlePlay",
                                             "{\"userCurrency\":\"CAD\"}",
                                             onSuccess,
                                             App.OnBrainCloudError,
                                             this);
    }

    private void UpdateUserData()
    {
        CurrencyInfoText.text = "---";//string.Format(IAP_INFO_FORMAT, UserData.EnergyAmount, "Gems", UserData.CurrencyAmount, UserData.HasSpecialItem ? "YES" : "NO");
    }

    private void OnCloseStoreButton()
    {
        var iapButtons = IAPContent.GetComponentsInChildren<IAPButton>();
        for (int i = 0; i < iapButtons.Length; i++)
        {
            Destroy(iapButtons[i].gameObject);
        }

        App.ChangePanelState(PanelState.Main);
    }

    private void OnPurchaseBCProduct(BCProduct bcProduct)
    {
#if !UNITY_EDITOR
        App.IsInteractable = false;

        if (StoreController != null)
        {
            string id = bcProduct.GetGooglePlayPriceData().id;
            var iapProduct = StoreController.products.WithID(id);

            if (iapProduct != null && iapProduct.availableToPurchase)
            {
                StoreController.InitiatePurchase(iapProduct);

                Debug.Log($"Purchasing: {bcProduct.title} (ID: {id} | Price: {bcProduct.GetGooglePlayPriceData().GetIAPPrice()} | Type: {bcProduct.IAPProductType})");
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
        Debug.Log($"Purchasing: {bcProduct.title} (ID: {bcProduct.GetGooglePlayPriceData().id} | Price: {bcProduct.GetGooglePlayPriceData().GetIAPPrice()} | Type: {bcProduct.IAPProductType})");
#endif
    }

    private void OnRedeemBCItem(BCItem item)
    {
        //App.IsInteractable = false;

        // TODO: Be able to redeem currencies for items

        //Debug.Log($"Redeeming {item.defId} x{item.quantity}");
    }

    #endregion

    #region brainCloud

    private void OnVerifyPurchasesSuccess(string jsonResponse, object cbObject)
    {
        var data = ((JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)
            ["data"] as Dictionary<string, object>)
            ["transactionSummary"] as Dictionary<string, object>)
            ["transactionDetails"];
        var details = JsonReader.Deserialize<Dictionary<string, object>[]>(JsonWriter.Serialize(data));

        List<string> failedTransactions = new();
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
#if !UNITY_EDITOR
            else if (StoreController.products.WithID(productId) is Product product && product.hasReceipt)
            {
                StoreController.ConfirmPendingPurchase(product);
            }
            else
            {
                status = "Unknown Error";
            }
#endif
            if (!string.IsNullOrWhiteSpace(status))
            {
                failedTransactions.Add($"{productId} - {status}");
            }
        }

        if (failedTransactions.Count > 0)
        {
            Debug.Log($"One or more purchases were unable to be fully processed:\n{LoggerUI.FormatJSON(JsonWriter.Serialize(failedTransactions))}");
        }
        else
        {
            Debug.Log($"Purchase(s) verified with brainCloud!");
        }

        App.IsInteractable = true;
    }

    #endregion

    #region Unity IAP

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        App.IsInteractable = true;

        StoreController = controller;
        ExtensionProvider = extensions;

        bool noProducts = true;
        for (int i = 0; i < IAPContent.childCount; i++)
        {
            var iapButton = IAPContent.GetChild(i).GetComponent<IAPButton>();
            var product = StoreController.products.WithID(iapButton.ProductData.GetGooglePlayPriceData().id);
            iapButton.gameObject.SetActive(product != null && product.availableToPurchase && product.definition.enabled);

            if (iapButton.isActiveAndEnabled)
            {
                if (product.definition.type == ProductType.Subscription && HasSubscription(product.definition.id))
                {
                    iapButton.IsInteractable = false;
                }
                else
                {
                    noProducts = false;
                    iapButton.IsInteractable = true;
                    iapButton.OnButtonAction += OnPurchaseBCProduct;
                }
            }
        }

        StoreInfoText.gameObject.SetActive(noProducts);
        StoreInfoText.text = NO_PRODUCTS_STORE_TEXT;

        Debug.Log("Unity IAP updated.");
    }

    public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        App.IsInteractable = true;

        var errorMessage = $"Unity IAP failed to initialize. Reason: {error}.";
        if (string.IsNullOrWhiteSpace(message))
        {
            errorMessage += $"\nDetails: {message}";
        }

        StoreInfoText.gameObject.SetActive(true);
        StoreInfoText.text = ERROR_STORE_TEXT;

        StoreController = null;
        ExtensionProvider = null;

        Debug.LogError(errorMessage);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        App.IsInteractable = true;

        // Retrieve the purchased product
        var product = args.purchasedProduct;
        Debug.Log($"Purchase Complete! Product: {product.definition.id}; Receipt:\n{product.receipt}");

        var json = JsonReader.Deserialize<Dictionary<string, object>>(product.receipt);
        json = JsonReader.Deserialize<Dictionary<string, object>>(json["Payload"].ToString());
        json = JsonReader.Deserialize<Dictionary<string, object>>(json["json"].ToString());

        void onFailure(int status, int reason, string jsonError, object cbObject)
        {
            App.OnBrainCloudError(status, reason, jsonError, cbObject);

            Debug.LogError($"Unable to verify purchase(s) with brainCloud!");
        }

        BC.AppStoreService.VerifyPurchase("googlePlay",
                                          JsonWriter.Serialize(new Dictionary<string, object>
                                          {
                                              { "productId", json["productId"]     },
                                              { "orderId",   json["orderId"]       },
                                              { "token",     json["purchaseToken"] },
                                              { "includeSubscriptionCheck", product.definition.type == ProductType.Subscription }
                                          }),
                                          OnVerifyPurchasesSuccess,
                                          onFailure,
                                          this);

        return PurchaseProcessingResult.Pending;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        App.IsInteractable = true;

        Debug.LogError($"Purchase Failed. Product: {product.definition.id}. Reason: {failureReason}");
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        App.IsInteractable = true;

        Debug.LogError($"Purchase Failed. Product: {product.definition.id}. Reason: {failureDescription.reason}" +
                       (!string.IsNullOrWhiteSpace(failureDescription.message) ? $"\nDetails: {failureDescription.message}" : string.Empty));
    }

    public bool HasSubscription(string productId)
    {
        if (StoreController.products.WithID(productId) is Product subscription &&
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

    #endregion
}
