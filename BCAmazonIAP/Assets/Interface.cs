using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using com.amazon.device.iap.cpt;

public class Interface : MonoBehaviour
{
    //create amazon IAP service instance
    IAmazonIapV2 iapService = AmazonIapV2Impl.Instance;
    string amazonUserId = "";
    string amazonReceiptId = "";

    //the sku of the iap you will purchase
    SkuInput purchaseRequest;
    RequestOutput purchaseRequestOutput;

    InputField ItemToPurchaseField;
    InputField emailField;
    string password = "testpassword";
    Text Status;
    Text BCLogs;

    // Start is called before the first frame update
    void Start()
    {
        iapService.AddPurchaseResponseListener(PurchaseResponseEvent);
        Status = GameObject.Find("Status").GetComponent<Text>();
        BCLogs = GameObject.Find("BCLogs").GetComponent<Text>();
        ItemToPurchaseField = GameObject.Find("ItemToPurchaseField").GetComponent<InputField>();
        emailField = GameObject.Find("emailField").GetComponent<InputField>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //BUTTONS
    public void OnAmazonPurchasePress()
    {
        purchaseRequest = new SkuInput();
        purchaseRequest.Sku = ItemToPurchaseField.GetComponent<InputField>().text;
        purchaseRequestOutput = iapService.Purchase(purchaseRequest);
    }

    public void OnBCAuthenticate()
    {
        BCConfig._bc.AuthenticateEmailPassword(emailField.GetComponent<InputField>().text, password, true, OnSuccess_BCAuthenticate, OnError_BCAuthenticate);
        //BCConfig._bc.AuthenticateAnonymous(OnSuccess_BCAuthenticate, OnError_BCAuthenticate);
    }

    public void OnBCGetSalesInventory()
    {
        BCConfig._bc.AppStoreService.GetSalesInventory("amazon", "coins", OnSuccess_GetSalesInventory, OnError_GetSalesInventory);
    }

    public void OnBCVerifyPurchase()
    {
        if (amazonReceiptId != null)
            BCLogs.GetComponent<Text>().text += "amazonReceiptId = " + amazonReceiptId;
        else
            BCLogs.GetComponent<Text>().text += "\namazonReceiptId NULL";
        if (amazonUserId != null)
            BCLogs.GetComponent<Text>().text += "\namazonUserId = " + amazonUserId;
        else
            BCLogs.GetComponent<Text>().text += "\namazonUserId NULL";

        string data = "{\"receiptId\":\"" + amazonReceiptId + "\",\"userId\":\"" + amazonUserId + "\"}";
        Status.GetComponent<Text>().text += "\ndata" + data;
        BCConfig._bc.AppStoreService.VerifyPurchase("amazon", data, OnSuccess_VerifyPurchase, OnError_VerifyPurchase);
    }


    //CALLBACKS
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

        Status.GetComponent<Text>().text = "RESPONSE!";
        if (receiptId != null)
            BCLogs.GetComponent<Text>().text = "receiptId = " + receiptId;
        else
            BCLogs.GetComponent<Text>().text = "receiptId NULL";

        if (amazonReceiptId != null)
            BCLogs.GetComponent<Text>().text += "\namazonReceiptId = " + amazonReceiptId;
        else
            BCLogs.GetComponent<Text>().text += "\namazonReceiptId NULL";

        if (userId != null)
            BCLogs.GetComponent<Text>().text += "\nuserId = " + userId;
        else
            BCLogs.GetComponent<Text>().text += "\nuserId NULL";

        if (amazonUserId != null)
            BCLogs.GetComponent<Text>().text += "\namazonUserId = " + amazonUserId;
        else
            BCLogs.GetComponent<Text>().text += "\namazonUserId NULL";
    }

    public void OnSuccess_BCAuthenticate(string responseData, object cbObject)
    {
        Status.GetComponent<Text>().text = "Authenticated!";
        BCLogs.GetComponent<Text>().text = responseData;
    }

    public void OnError_BCAuthenticate(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Status.GetComponent<Text>().text = "Failed to Authenticate";
        BCLogs.GetComponent<Text>().text = statusMessage;
    }

    public void OnSuccess_GetSalesInventory(string responseData, object cbObject)
    {
        Status.GetComponent<Text>().text = "Sales Inventory Received!\nCheck Logs for itemIds for purchase";
        BCLogs.GetComponent<Text>().text = responseData;
    }

    public void OnError_GetSalesInventory(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Status.GetComponent<Text>().text = "Sales Inventory Not Found";
        BCLogs.GetComponent<Text>().text = statusMessage;
    }

    public void OnSuccess_VerifyPurchase(string responseData, object cbObject)
    {
        Status.GetComponent<Text>().text = "Purchase Verified!";
        BCLogs.GetComponent<Text>().text = responseData;
    }

    public void OnError_VerifyPurchase(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Status.GetComponent<Text>().text = "Unable To Verify Purchase";
        BCLogs.GetComponent<Text>().text = statusMessage;
    }

}
