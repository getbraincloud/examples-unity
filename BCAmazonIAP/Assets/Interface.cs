using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using com.amazon.device.iap.cpt;

public class Interface : MonoBehaviour
{
    IAmazonIapV2 iapService = AmazonIapV2Impl.Instance;
    Text Status;
    SkuInput request;
    RequestOutput response;

    // Start is called before the first frame update
    void Start()
    {
        iapService.AddGetPurchaseUpdatesResponseListener(EventHandler);
        Status = GameObject.Find("Status").GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void EventHandler(GetPurchaseUpdatesResponse args)
    {
        string requestId = args.RequestId;
        string userId = args.AmazonUserData.UserId;
        string marketplace = args.AmazonUserData.Marketplace;
        List<PurchaseReceipt> receipts = args.Receipts;
        string status = args.Status;
        bool hasMore = args.HasMore;

        // for each purchase receipt you can get the following values
        string receiptId = receipts[0].ReceiptId;
        long cancelDate = receipts[0].CancelDate;
        long purchaseDate = receipts[0].PurchaseDate;
        string sku = receipts[0].Sku;
        string productType = receipts[0].ProductType;

        Status.text = requestId;
    }

    public void OnButtonPress()
    {
        request = new SkuInput();
        request.Sku = "test_bitheads_orb1";
        response = iapService.Purchase(request);
    }
}
