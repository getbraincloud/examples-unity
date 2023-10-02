using BrainCloud.JsonFx.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ExampleApp;

public class StorePanel : MonoBehaviour
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

    #region Unity Messages

    private void OnEnable()
    {
        CloseStoreButton.onClick.AddListener(OnCloseStoreButton);

        if (BrainCloudMarketplace.IsInitialized)
        {
            GetProducts();
        }
    }

    private void Start()
    {
        App = FindObjectOfType<ExampleApp>();

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
    }

    #endregion

    private IEnumerator GetBrainCloudWrapper()
    {
        App.IsInteractable = false;

        yield return new WaitUntil(() =>
        {
            BC = BC ?? FindObjectOfType<BrainCloudWrapper>();

            return BC != null && BC.Client != null && BC.Client.IsInitialized();
        });

        yield return null;

        GetProducts();

        //StoreTesting();
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

    private void OnVerifyPurchasesSuccess(string jsonResponse, object cbObject)
    {
        var data = ((JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)
            ["data"] as Dictionary<string, object>)
            ["transactionSummary"] as Dictionary<string, object>)
            ["transactionDetails"];
        var details = JsonReader.Deserialize<Dictionary<string, object>[]>(JsonWriter.Serialize(data));

        List<string> successTransactions = new();
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

            if (!string.IsNullOrWhiteSpace(status))
            {
                failedTransactions.Add($"{productId} - {status}");
            }
            else
            {
                successTransactions.Add($"{productId} - {status}");
            }
        }

        if (failedTransactions.Count > 0)
        {
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
    }
#endif

    #region UI

    private void GetProducts()
    {
        App.IsInteractable = false;

        CurrencyInfoText.text = "---";
        StoreInfoText.gameObject.SetActive(true);
        StoreInfoText.text = OPENING_STORE_TEXT;

        void onFetchProducts(BCProduct[] inventory)
        {
            App.IsInteractable = true;
            if (inventory == null || inventory.Length < 0 || BrainCloudMarketplace.ErrorOccurred)
            {
                StoreInfoText.text = BrainCloudMarketplace.ErrorOccurred ? ERROR_STORE_TEXT : NO_PRODUCTS_STORE_TEXT;
                return;
            }

            foreach (var product in inventory)
            {
                var iapButton = Instantiate(IAPButtonTemplate, IAPContent, false);
                iapButton.SetProductDetails(product);
                iapButton.OnButtonAction += OnPurchaseBCProduct;
            }

            UpdateUserData();
            StoreInfoText.gameObject.SetActive(false);
        }

        if (BrainCloudMarketplace.IsInitialized)
        {
            BrainCloudMarketplace.FetchProducts(onFetchProducts);
        }
        else
        {
            BrainCloudMarketplace.Initialize(BC, onFetchProducts);
        }
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

    private void OnPurchaseBCProduct(BCProduct product)
    {
        App.IsInteractable = false;

        void onPurchaseFinished(BCProduct[] purchasedProducts)
        {
            if (purchasedProducts != null && purchasedProducts.Length > 0)
            {
                foreach (var item in purchasedProducts)
                {
                    Debug.Log($"Purchase Success: {item.title} (ID: {item.GetGooglePlayPriceData().id} | Price: {item.GetGooglePlayPriceData().GetIAPPrice()} | Type: {item.IAPProductType})");
                }
            }

            App.IsInteractable = true;
        }

        BrainCloudMarketplace.PurchaseProduct(product, onPurchaseFinished);
    }

    private void OnRedeemBCItem(BCItem item)
    {
        //App.IsInteractable = false;

        // TODO: Be able to redeem currencies for items

        //Debug.Log($"Redeeming {item.defId} x{item.quantity}");
    }

    #endregion
}
