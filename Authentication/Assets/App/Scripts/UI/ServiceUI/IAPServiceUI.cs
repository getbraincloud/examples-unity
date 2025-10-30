using BrainCloud;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_WINMD_SUPPORT
using System.Threading.Tasks;
using Windows.ApplicationModel.Store;
using Windows.Services.Store;
using Windows.System;
using System.Linq;
using System;
#endif

public class IAPServiceUI : ContentUIBehaviour
{
    [SerializeField] private Transform _productsContainer;
    [SerializeField] private Button _buyButton;
    [SerializeField] private IAPItem _itemPrefab;

    private IAPItem _selectedItem = null;
    private BrainCloudAppStore _appStoreService = default;
    private BrainCloudScript _scriptService = default;

    private List<IAPItem> _IAPItems = new List<IAPItem>();

#if ENABLE_WINMD_SUPPORT
    private StoreContext storeContext;
#endif

    string msStoreId;

    protected override void Start()
    {
        _appStoreService = BCManager.AppStoreService;
        _scriptService = BCManager.ScriptService;

#if ENABLE_WINMD_SUPPORT
        storeContext = StoreContext.GetDefault();

        Debug.Log($"User: {storeContext.User}");
#endif
        //"https://onestore.microsoft.com/b2b/keys/create/collections"
        //https://onestore.microsoft.com

        GetADToken("https://onestore.microsoft.com");

        InitializeUI();

        base.Start();

        _buyButton.interactable = false;
        _buyButton.onClick.AddListener(OnBuyButtonClicked);
    }

    private void GetADToken(string resource)
    {
        SuccessCallback successCallback = (response, cbObject) =>
        {
            Debug.Log("Response: " + response);
            Dictionary<string, object> data = response.Deserialize("data");

            string token = data["response"] as string;

            Debug.Log("Token: " + token);

            GetMSStoreId(token, BCManager.Client.ProfileId);
        };
        FailureCallback failureCallback = (status, code, error, cbObject) =>
        {
            Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error));
        };

        _scriptService.RunScript("GetAzureADToken", "{}", successCallback, failureCallback);
    }

    private async void GetMSStoreId(string ADToken, string userId)
    {

#if ENABLE_WINMD_SUPPORT
        try{
            msStoreId = await storeContext.GetCustomerCollectionsIdAsync(ADToken, userId);

            Debug.Log("Microsoft Store Id: " + msStoreId);

        } catch (System.Exception ex){
            Debug.LogError($"Error getting ms store ID: {ex.Message}");
        }
#endif
    }

    private async void ConsumeUnfulfilledProduct(string productId)
    {
#if ENABLE_WINMD_SUPPORT
                StoreConsumableResult result = await storeContext.GetConsumableBalanceRemainingAsync(productId);
                if (result.Status == StoreConsumableStatus.Succeeded)
                {
                    Debug.Log($"Balance remaining: {result.BalanceRemaining}");
                    if(result.BalanceRemaining > 0){
                        //consume unfulfilled purchase
                        Guid trackingId = Guid.NewGuid();
                        await storeContext.ReportConsumableFulfillmentAsync(productId, result.BalanceRemaining, trackingId);
                    }
                }
#endif
    }



    protected override void InitializeUI()
    {
        //load products
        string storeId = "windows";
        string userCurrency = "{\"userCurrency\":\"CAD\"}";

        _IAPItems.Clear();

        SuccessCallback successCallback = (response, cbObject) =>
        {
            Debug.Log(string.Format("Success | {0}", response));
            //load UI items
            Dictionary<string, object>[] data = response.Deserialize("data").GetJSONArray("productInventory");

            for (int i = 0; i < data.Length; i++)
            {
                Debug.Log("looping item: " + data[i]["itemId"]);
                //Instantiate UI item
                IAPItem UIItem = Instantiate(_itemPrefab, _productsContainer);
                IAPItemData itemData = new IAPItemData();
                itemData.name = data[i]["itemId"].ToString();
                itemData.title = data[i]["title"].ToString();
                itemData.imageUrl = data[i]["imageUrl"].ToString();

                Dictionary<string, object> priceData = (Dictionary<string, object>)data[i]["priceData"];
                itemData.productId = priceData["id"].ToString();

                ConsumeUnfulfilledProduct(itemData.productId);

                UIItem.InitializeItem(itemData, OnItemSelected);

                _IAPItems.Add(UIItem);
            }
        };
        FailureCallback failureCallback = (status, code, error, cbObject) =>
        {
            Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error));
        };

        _appStoreService.GetSalesInventory(storeId, userCurrency, successCallback, failureCallback);

    }


    private void OnItemSelected(IAPItem item)
    {
        foreach(IAPItem iapItem in _IAPItems)
        {
            iapItem.itemSelected = false;
        }

        _selectedItem = item;
        _buyButton.interactable = true;
    }

    private void OnBuyButtonClicked()
    {
        if (_selectedItem == null) return;

        _buyButton.interactable = false;

        string productId = _selectedItem.data.productId;

        Debug.Log("Request purchase: " + productId);
        //make purchase request here from windows store
        RequestPurchase(productId);

    }

    private async void RequestPurchase(string productId)
    {
#if ENABLE_WINMD_SUPPORT
        StorePurchaseResult result = await storeContext.RequestPurchaseAsync(productId);

        Debug.Log($"Result: {result.Status}");
        switch (result.Status)
        {
            case StorePurchaseStatus.Succeeded:
                Debug.Log("Purchase successful!");
                
                // get some info on the purchase
                string[] filterList = new string[] { "Consumable", "Durable", "UnmanagedConsumable" };

                StoreProductQueryResult collection = await storeContext.GetUserCollectionAsync(filterList);

                StoreProduct selectedProduct = collection.Products.Values.FirstOrDefault(p => p.StoreId == productId);

                Debug.Log("Got user collection");
                Debug.Log(collection.Products.Count + " products in users collection");

                string transactionId = string.Empty;

                foreach(StoreProduct product in collection.Products.Values){
                    Debug.Log("Product name: " + product.Title);
                    Debug.Log("Product ID: " + product.StoreId);
                    Debug.Log("Product IAO token: " + product.InAppOfferToken);
                    Debug.Log("Product IsInUserCollection: " + product.IsInUserCollection);
                }

                //Get the receipt data to pass on the purchase data to brainCloud in order to verify the purchase
                if(selectedProduct != null){
                    Debug.Log("Selected product: " + selectedProduct);

                    if(selectedProduct.Skus.Any()){

                        string jsonData = selectedProduct.Skus[0].CollectionData.ExtendedJsonData;

                        Dictionary<string, object> data = jsonData.Deserialize(null);

                        transactionId = data["transactionId"] as string;

                        Dictionary<string, object> receiptData = new Dictionary<string, object>();

                        receiptData.Add("msStoreId", msStoreId);
                        receiptData.Add("productId", data["productId"]);
                        receiptData.Add("skuId", data["skuId"]);
                        receiptData.Add("transactionId", transactionId);
                        receiptData.Add("localTicketRef", data["localTicketReference"]);

                        Debug.Log("Receipt data: " + receiptData.Serialize());

                        JWT jwtData = JSONHelper.DecodeJWT(msStoreId);
                        Dictionary<string, object> payloadData = jwtData.payload.Deserialize(null);
                        string userIdRef = payloadData["http://schemas.microsoft.com/marketplace/2015/08/claims/key/userId"] as string;

                        Debug.Log("userIdRef: " + userIdRef);

                        SuccessCallback successCallback = (response, cbObject) =>
                        {
                            Debug.Log(string.Format("Receipt verified successfully | {0}", response));
                            ConsumeProductUWP(productId, transactionId, userIdRef, msStoreId);
                        };
                        FailureCallback failureCallback = (status, code, error, cbObject) =>
                        {
                            Debug.Log(string.Format("Failed to verify receipt | {0}  {1}  {2}", status, code, error));
                        };

                        _appStoreService.VerifyPurchase("windows", receiptData.Serialize(), successCallback, failureCallback);
                    }
                }
                break;
            case StorePurchaseStatus.NotPurchased:
                Debug.Log("Purchase canceled.");
                Debug.Log($"Extended Error: {result.ExtendedError?.Message}");
                break;
            case StorePurchaseStatus.NetworkError:
            case StorePurchaseStatus.ServerError:
                Debug.Log("Network or server error.");
                Debug.Log($"Extended Error: {result.ExtendedError?.Message}");
                break;
        }
#endif
    }

    private async void ConsumeProductUWP(string productId, string transactionId, string userId, string msStoreId)
    {
        /*
        //start by calling script to get AD token
        SuccessCallback successCallback = (response, cbObject) =>
        {
            Dictionary<string, object> data = response.Deserialize("data", "response");
            Debug.Log("Successfully consumed product");
            Debug.Log(data.Serialize());

        };
        FailureCallback failureCallback = (status, code, error, cbObject) =>
        {
            Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error));
        };

        Dictionary<string, object> scriptData = new Dictionary<string, object>();

        scriptData["localTicketRef"] = userId;
        scriptData["identityValue"] = msStoreId;
        scriptData["productId"] = productId;
        scriptData["transactionId"] = transactionId;

        _scriptService.RunScript("ConsumeUWPProduct", scriptData.Serialize(), successCallback, failureCallback);
        */
#if ENABLE_WINMD_SUPPORT

        Guid trackingId = Guid.NewGuid();
        StoreConsumableResult consumeResult = await storeContext.ReportConsumableFulfillmentAsync(productId, 1, trackingId);
        Debug.Log($"[IAP] Fulfillment result status: {consumeResult.Status}");
                
        if (consumeResult.Status == StoreConsumableStatus.Succeeded)
        {
            Debug.Log($"[IAP] Remaining balance: {consumeResult.BalanceRemaining}");
            Debug.Log($"[IAP] Tracking ID: {consumeResult.TrackingId}");
        }
        else
        {
            Debug.LogWarning($"[IAP] Fulfillment failed. Status: {consumeResult.Status}");
            if (consumeResult.ExtendedError != null)
            {
                Debug.LogError($"[IAP] Extended error: {consumeResult.ExtendedError.Message}");
            }
        }
                
#endif
    }
}
