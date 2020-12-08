using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using UnityEngine.Purchasing;
using BrainCloud.JsonFx.Json;

public class BrainCloudInterface : MonoBehaviour, IStoreListener //needed for unity iap
{
    //these are simply references to the unity specific canvas system
    Text status;
    string statusText;
    string email;
    string authCode;
    string idToken;

    //purchase
    string productId;
    string orderId;
    string purchaseToken;
    string developerPayload;

    //Google info 
    Dictionary<string, object> wrapper;
    string store;
    string payload;
    Dictionary<string, object> gpDetails;
    string gpJson;
    string gpSig;
    Dictionary<string, object> gpJsonDict;

    //for purchasing
    private static IStoreController m_StoreController;          // The Unity Purchasing system.
    private static IExtensionProvider m_StoreExtensionProvider; // The store-specific Purchasing subsystems.
    public static string kProductIDConsumable = "bc_test_orb";

    // Use this for initialization
    void Start()
    {
        //allow the people who sign in to change profiles. 
        BCConfig._bc.SetAlwaysAllowProfileSwitch(true);
        BCConfig._bc.Client.EnableLogging(true);

        //unity's ugly way to look for gameobjects
        status = GameObject.Find("Status").GetComponent<Text>();

        // If we haven't set up the Unity Purchasing reference
        if (m_StoreController == null)
        {
            // Begin to configure our connection to Purchasing
            InitializePurchasing();
        }

    }

    void Update()
    {
        status.text = statusText;
    }


    public void OnAuthEmail()
    {
        BCConfig._bc.AuthenticateEmailPassword("ryan.daniel.ruth@gmail.com", "password", true, OnSuccess_AuthenticateEmail, OnError_AuthenticateEmail);
    }

    public void OnSuccess_AuthenticateEmail(string responseData, object cbObject)
    {
        statusText = "Logged into braincloud!\n" + responseData;
    }

    public void OnError_AuthenticateEmail(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        statusText = "Failed to Login to braincloud...\n" + statusMessage + "\n" + reasonCode;
    }


    //purchasing 
    public void InitializePurchasing()
    {
        // If we have already connected to Purchasing ...
        if (IsInitialized())
        {
            // ... we are done here.
            return;
        }

        // Create a builder, first passing in a suite of Unity provided stores.
        var configurationBuilder = ConfigurationBuilder.Instance(Google.Play.Billing.GooglePlayStoreModule.Instance()); //For Google purchasing specifically
        //var configurationBuilder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        // Add a product to sell / restore by way of its identifier, associating the general identifier
        // with its store-specific identifiers.
        configurationBuilder.AddProduct(kProductIDConsumable, ProductType.Consumable);

        // Kick off the remainder of the set-up with an asynchrounous call, passing the configuration 
        // and this class' instance. Expect a response either in OnInitialized or OnInitializeFailed.
        UnityPurchasing.Initialize(this, configurationBuilder);
    }

    private bool IsInitialized()
    {
        // Only say we are initialized if both the Purchasing references are set.
        return m_StoreController != null && m_StoreExtensionProvider != null;
    }

    public void OnGooglePurchase()
    {
        // Buy the consumable product using its general identifier. Expect a response either 
        // through ProcessPurchase or OnPurchaseFailed asynchronously.
        BuyProductID(kProductIDConsumable);
    }

    public void OnVerifyPurchase()
    {
        gpJsonDict = (Dictionary<string, object>)MiniJson.JsonDecode(gpJson);

        Dictionary<string, object> receiptData = new Dictionary<string, object>();
        receiptData.Add("productId", gpJsonDict["productId"]);
        receiptData.Add("orderId", gpJsonDict["orderId"]);
        receiptData.Add("token", gpJsonDict["purchaseToken"]);
        //Developer payload is not supported
        //Google Play deprecated developer payload and is replacing it with alternatives that are more meaningful and contextual. 
        receiptData.Add("developerPayload", ""); //So pass in empty string for developer payload.

        string receiptDataString = JsonWriter.Serialize(receiptData);

        BCConfig._bc.AppStoreService.VerifyPurchase("googlePlay", receiptDataString, OnSuccess_VerifyPurchase, OnError_VerifyPurchase);
    }

    public void OnSuccess_VerifyPurchase(string responseData, object cbObject)
    {
        statusText = "Verified Purchase!\n" + responseData;
    }

    public void OnError_VerifyPurchase(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        statusText = "Failed to Verify Purchase...\n" + statusMessage + "\n" + reasonCode;
    }


    void BuyProductID(string productId)
    {
        // If Purchasing has been initialized ...
        if (IsInitialized())
        {
            // ... look up the Product reference with the general product identifier and the Purchasing 
            // system's products collection.
            Product product = m_StoreController.products.WithID(productId);

            // If the look up found a product for this device's store and that product is ready to be sold ... 
            if (product != null && product.availableToPurchase)
            {
                Debug.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
                // ... buy the product. Expect a response either through ProcessPurchase or OnPurchaseFailed 
                // asynchronously.
                m_StoreController.InitiatePurchase(product);
            }
            // Otherwise ...
            else
            {
                // ... report the product look-up failure situation  
                Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
            }
        }
        // Otherwise ...
        else
        {
            // ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or 
            // retrying initiailization.
            Debug.Log("BuyProductID FAIL. Not initialized.");
        }
    }

    public void OnShowGoogleStats()
    {
        statusText = "STORE: " + store +"\nPAYLOAD: " + payload + "\nJSON: " + gpJson + "\nSIGNATURE: " + gpSig;
    }

    public void OnShowJSONStats()
    {
        gpJsonDict = (Dictionary<string, object>)MiniJson.JsonDecode(gpJson);

        statusText = "PRODUCTID: " + gpJsonDict["productId"] + "\n:ORDERID " + gpJsonDict["orderId"] + "\nTOKEN: " + gpJsonDict["purchaseToken"];
    }

    //  
    // --- IStoreListener callbacks
    //

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        // Purchasing has succeeded initializing. Collect our Purchasing references.
        statusText = "OnInitialized: Google Store PASS";
        //Debug.Log("OnInitialized: PASS");

        // Overall Purchasing system, configured with products for this application.
        m_StoreController = controller;
        // Store specific subsystem, for accessing device-specific store features.
        m_StoreExtensionProvider = extensions;
    }


    public void OnInitializeFailed(InitializationFailureReason error)
    {
        // Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
        statusText = "OnInitializeFailed InitializationFailureReason:" + error;
        //statusText = "blah bala";
        //Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
    }


    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        // A consumable product has been purchased by this user.
        if (String.Equals(args.purchasedProduct.definition.id, kProductIDConsumable, StringComparison.Ordinal))
        {
            //Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
            statusText = "ProcessPurchase: PASS. Product: " + args.purchasedProduct.definition.id;
        }
        else
        {
            //Debug.Log(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
            statusText = "ProcessPurchase: FAIL. Unrecognized product: " + args.purchasedProduct.definition.id;
        }

        wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(args.purchasedProduct.receipt);
        store = (string)wrapper["Store"];
        payload = (string)wrapper["Payload"];
        gpDetails = (Dictionary<string, object>)MiniJson.JsonDecode(payload);
        gpJson = (string)gpDetails["json"];
        gpSig = (string)gpDetails["signature"];

        // Return a flag indicating whether this product has completely been received, or if the application needs 
        // to be reminded of this purchase at next app launch. Use PurchaseProcessingResult.Pending when still 
        // saving purchased products to the cloud, and when that save is delayed. 
        return PurchaseProcessingResult.Complete;
    }


    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        // A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
        // this reason with the user to guide their troubleshooting actions.
        //Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
        statusText = "OnPurchaseFailed: FAIL. Product: " + product.definition.storeSpecificId + ", PurchaseFailureReason: " + failureReason;
    }
}
