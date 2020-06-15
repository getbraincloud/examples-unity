using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
//include this
using com.amazon.device.iap.cpt;

public class BrainCloudInterface : MonoBehaviour
{
    Text statusLogs;
    Text BCLogs;
    InputField ItemToPurchaseField;

    string amazonUserId = "";
    string amazonReceiptId = "";

    //amazon IAP object
    IAmazonIapV2 iapService = AmazonIapV2Impl.Instance;
    RequestOutput amazonResponse;
    SkuInput purchaseRequest;
    RequestOutput purchaseRequestOutput;

    void Start ()
    {
        BCConfig._bc.SetAlwaysAllowProfileSwitch(true);
        BCConfig._bc.Client.EnableLogging(true);

        statusLogs = GameObject.Find("StatusLogs").GetComponent<Text>();
        BCLogs = GameObject.Find("BCLogs").GetComponent<Text>();
        ItemToPurchaseField = GameObject.Find("ItemToPurchaseField").GetComponent<InputField>();

        //amazon listeners
        iapService.AddPurchaseResponseListener(PurchaseResponseEvent);
        //iapService.AddGetProductDataResponseListener(GetProductDataResponseEvent);
        //iapService.AddGetPurchaseUpdatesResponseListener(GetPurchaseUpdatesEvent);

        //purchase details
        purchaseRequest = new SkuInput();
    }

    void Update ()
    {
	}

    //private void GetPurchaseUpdatesEvent(GetPurchaseUpdatesResponse args)
    //{
    //    string requestId = args.RequestId;
    //    string userId = args.AmazonUserData.UserId;
    //    string marketplace = args.AmazonUserData.Marketplace;
    //    List<PurchaseReceipt> receipts = args.Receipts;
    //    string status = args.Status;
    //    bool hasMore = args.HasMore;

    //    // for each purchase receipt you can get the following values
    //    string receiptId = receipts[0].ReceiptId;
    //    long cancelDate = receipts[0].CancelDate;
    //    long purchaseDate = receipts[0].PurchaseDate;
    //    string sku = receipts[0].Sku;
    //    string productType = receipts[0].ProductType;
    //}

    //private void GetProductDataResponseEvent(GetProductDataResponse args)
    //{
    //    string requestId = args.RequestId;
    //    Dictionary<string, ProductData> productDataMap = args.ProductDataMap;
    //    List<string> unavailableSkus = args.UnavailableSkus;
    //    string status = args.Status;

    //    // for each item in the productDataMap you can get the following values for a given SKU
    //    // (replace "sku" with the actual SKU)
    //    string sku = productDataMap["sku"].Sku;
    //    string productType = productDataMap["sku"].ProductType;
    //    string price = productDataMap["sku"].Price;
    //    string title = productDataMap["sku"].Title;
    //    string description = productDataMap["sku"].Description;
    //    string smallIconUrl = productDataMap["sku"].SmallIconUrl;
    //}

    private void PurchaseResponseEvent(PurchaseResponse args)
    {
        string requestId = args.RequestId;
        string userId = args.AmazonUserData.UserId;
        string marketplace = args.AmazonUserData.Marketplace;
        string receiptId = args.PurchaseReceipt.ReceiptId;
        long cancelDate = args.PurchaseReceipt.CancelDate;
        long purchaseDate = args.PurchaseReceipt.PurchaseDate;
        string sku = args.PurchaseReceipt.Sku;
        string productType = args.PurchaseReceipt.ProductType;
        string status = args.Status;

        amazonUserId = userId;
        amazonReceiptId = receiptId;

        statusLogs.GetComponent<Text>().text = "RESPONSE!";
    }

    public void OnBCAuthenticate()
    {
        BCConfig._bc.AuthenticateAnonymous(OnSuccess_BCAuthenticate, OnError_BCAuthenticate);
    }

    public void OnBCGetSalesInventory()
    {
        BCConfig._bc.AppStoreService.GetSalesInventory("amazon", "coins", OnSuccess_GetSalesInventory, OnError_GetSalesInventory);
    }

    public void OnBCVerifyPurchase()
    {
        string data = "{\"receiptData\":{\"receiptId\":" + amazonReceiptId + ",\"userId\":" + amazonUserId + "}}";
        statusLogs.GetComponent<Text>().text = data;
        BCConfig._bc.AppStoreService.VerifyPurchase("amazon", data, OnSuccess_VerifyPurchase, OnError_VerifyPurchase);
    }

    public void OnPurchaseItem()
    {
        purchaseRequest.Sku = ItemToPurchaseField.GetComponent<InputField>().text;
        purchaseRequestOutput = iapService.Purchase(purchaseRequest);
        statusLogs.GetComponent<Text>().text = "Making Purchase!";
    }

    public void OnSuccess_BCAuthenticate(string responseData, object cbObject)
    {
        statusLogs.GetComponent<Text>().text = "Authenticated!";
        BCLogs.GetComponent<Text>().text = responseData;
    }

    public void OnError_BCAuthenticate(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        statusLogs.GetComponent<Text>().text = "Failed to Authenticate";
        BCLogs.GetComponent<Text>().text = statusMessage;
    }

    public void OnSuccess_GetSalesInventory(string responseData, object cbObject)
    {
        statusLogs.GetComponent<Text>().text = "Sales Inventory Received!\nCheck Logs for itemIds for purchase";
        BCLogs.GetComponent<Text>().text = responseData;
    }

    public void OnError_GetSalesInventory(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        statusLogs.GetComponent<Text>().text = "Sales Inventory Not Found";
        BCLogs.GetComponent<Text>().text = statusMessage;
    }

    public void OnSuccess_VerifyPurchase(string responseData, object cbObject)
    {
        statusLogs.GetComponent<Text>().text = "Purchase Verified!";
        BCLogs.GetComponent<Text>().text = responseData;
    }

    public void OnError_VerifyPurchase(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        //statusLogs.GetComponent<Text>().text = "Unable To Verify Purchase";
        BCLogs.GetComponent<Text>().text = statusMessage;
    }



}
