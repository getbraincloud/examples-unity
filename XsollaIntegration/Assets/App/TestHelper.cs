using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using Xsolla.SDK.Common;
using Xsolla.SDK.Login;
using Xsolla.SDK.Store;
using JsonReader = BrainCloud.JsonFx.Json.JsonReader;
using JsonWriter = BrainCloud.JsonFx.Json.JsonWriter;

public class TestHelper : MonoBehaviour
{
    [Header("Helper")]
    [SerializeField] private TMP_Text AppInfoLabel = default;

    [Header("Xsolla")]
    [SerializeField] private bool UseSandbox = true;
    [SerializeField] private bool TrySilentLogin = true;
    [SerializeField] private string TestProductSKU = string.Empty;

    // Xsolla Auth
    private XsollaClientConfiguration XsollaConfig = default;
    private XsollaLoginClient XsollaLogin = default;
    private string XsollaAuthID = string.Empty;
    private string XsollaToken = string.Empty;
    private string XsollaRefreshToken = string.Empty;
    private long XsollaTokenExpiresIn = 0;

    // Xsolla Purchasing
    private XsollaStoreClient XsollaStore = default;
    private XsollaStoreClientProduct[] XsollaProducts = default;
    private XsollaStoreClientPurchasedProduct XsollaPurchase = default;

    // brainCloud
    private const string EXTERNAL_NAME = "Xsolla";
    private const string STORE_ID = "xsolla";

    private BrainCloudWrapper BC = default;
    private string IAPProductID = string.Empty;
    private string PayloadContext = string.Empty;
    private string VerifyReceiptResponse = string.Empty;

    #region Unity Messages

    private void Awake()
    {
        BC = GetComponent<BrainCloudWrapper>();
    }

    private void Start()
    {
        StartCoroutine(StartXsollaTesting());
    }

    private void OnApplicationQuit()
    {
        if (XsollaStore != null)
        {
            XsollaStore.Deinitialize(error =>
            {
                if (error != null)
                {
                    LogWarning($"Xsolla store failed to deinitialize: {error}");
                }
            });
        }

        if (BC != null && BC.Client != null && BC.Client.IsAuthenticated())
        {
            BC.PlayerStateService.Logout();
        }
    }

    private void OnDestroy()
    {
        XsollaStore = null;
        XsollaLogin = null;
        BC = null;
    }

    private void Update()
    {
        if (BC != null && BC.Client != null && BC.Client.IsInitialized())
        {
            AppInfoLabel.text = string.Format("{0} ({1}) v{2} (BC v{3})", Application.productName, BC.Client.AppId, BC.Client.AppVersion, BrainCloud.Version.GetVersion());
        }
        else
        {
            AppInfoLabel.text = Application.productName;
        }
    }

    #endregion

    #region Test Helpers

    private string FailureReason = string.Empty;

    private bool FailureOccurred => !string.IsNullOrWhiteSpace(FailureReason);

    private static void Log(object message)
    {
        Debug.Log($"[TEST]   {message}");
    }

    private static void LogWarning(object message)
    {
        Debug.LogWarning($"[TEST]   {message}");
    }

    private static void LogError(object message)
    {
        Debug.LogError($"[TEST]   {message}");
    }

    private static void Assert(bool condition, object message)
    {
        Debug.Assert(condition, $"[TEST]   {message}");
    }

    private static bool TryGetJWTClaim(string jwt, string claim, out string value)
    {
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(jwt))
        {
            return false;
        }

        string[] segments = jwt.Split('.');
        if (segments.Length < 2)
        {
            return false;
        }

        try
        {
            // Base64Url to Base64
            string payload = segments[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            string json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var claims = JsonReader.Deserialize<Dictionary<string, object>>(json);

            if (claims != null && claims.TryGetValue(claim, out object obj) && obj != null)
            {
                value = obj.ToString();
                return !string.IsNullOrWhiteSpace(value);
            }
        }
        catch (Exception e)
        {
            LogWarning($"Could not read claim '{claim}' from the Xsolla token: {e.Message}");
        }

        return false;
    }

    #endregion

    #region Test Initialization

    private IEnumerator StartXsollaTesting()
    {
        yield return null;

        PlayerPrefs.DeleteAll();

        yield return null;

        yield return InitializeBrainCloud();

        yield return InitializeXsolla();

        yield return XsollaAuthenticationTest();

        yield return XsollaPurchaseTest();
    }

    private IEnumerator InitializeBrainCloud()
    {
        if (BC.Client != null && BC.Client.IsInitialized())
        {
            yield break;
        }

        FailureReason = string.Empty;
        BC.Init();

        yield return new WaitUntil(() => BC.Client != null && BC.Client.IsInitialized(),
                                   TimeSpan.FromSeconds(10.0f),
                                   () => FailureReason = "brainCloud never initialized!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            Log("brainCloud Initialized!");
        }

        yield return null;
    }

    private IEnumerator InitializeXsolla()
    {
        if (XsollaLogin != null)
        {
            yield break;
        }

        FailureReason = string.Empty;

        XsollaClientSettingsAsset asset = XsollaClientSettingsAsset.Instance();
        if (asset == null || asset.settings == null)
        {
            FailureReason = "Xsolla settings asset is missing! Configure it via the Xsolla menu in the Editor.";
            LogError(FailureReason);
            yield break;
        }

        XsollaConfig = XsollaClientConfiguration.Builder.Create()
                                                        .SetSettings(asset.settings)
                                                        .SetSandbox(UseSandbox)
                                                        .SetLogLevel(XsollaLogLevel.Debug)
                                                        .Build();

        XsollaLogin = XsollaLoginClient.Builder.Create()
                                               .SetConfiguration(XsollaConfig)
                                               .Build();

        yield return new WaitUntil(() => XsollaLogin.GetConfiguration() != null,
                                   TimeSpan.FromSeconds(10.0f),
                                   () => FailureReason = "Xsolla configuration never resolved!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            Log($"Xsolla Initialized! (sandbox: {UseSandbox})");
        }

        yield return null;
    }

    #endregion

    #region Xsolla Authentication Test

    private IEnumerator XsollaAuthenticationTest()
    {
        if (!string.IsNullOrWhiteSpace(XsollaAuthID))
        {
            yield break;
        }

        FailureReason = string.Empty;

        yield return new WaitUntil(() => XsollaLogin != null && XsollaLogin.GetConfiguration() != null,
                                   TimeSpan.FromSeconds(10.0f),
                                   () => FailureReason = "Xsolla login client was never initialized!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            yield return XsollaAuthTest_Step1();
            yield return XsollaAuthTest_Step2();
            yield return XsollaAuthTest_Step3();
        }

        if (!XsollaAuthTest_AssertSuccess())
        {
            LogError("<b>Xsolla Auth Test Failed!</b>");
        }
        else
        {
            Log("<b>Xsolla Auth Test Finished Successfully!</b>");
        }

        yield return null;
    }

    private IEnumerator XsollaAuthTest_Step1()
    {
        FailureReason = string.Empty;
        Log("STEP 1: Log into Xsolla to retrieve an access token.");

        Action<XsollaLoginToken, XsollaLoginClientError> onLogin = null;
        bool silentAttempted = false;

        onLogin = (token, error) =>
        {
            if (error != null)
            {
                // A failed silent login just means there's no cached session; fall back to the widget
                if (TrySilentLogin && !silentAttempted)
                {
                    silentAttempted = true;
                    LogWarning($"Silent login failed ({error}); falling back to the login widget.");
                    XsollaLogin.Login(onLogin);
                    return;
                }

                FailureReason = $"Xsolla login failed! {error}";
                return;
            }

            XsollaToken = token.accessToken;
            XsollaRefreshToken = token.refreshToken;
            XsollaTokenExpiresIn = token.expiresIn;

            if (!TryGetJWTClaim(XsollaToken, "sub", out XsollaAuthID))
            {
                FailureReason = "Xsolla login succeeded but the access token has no 'sub' claim!";
                return;
            }
        };

        if (TrySilentLogin)
        {
            XsollaLogin.LoginSilently(onLogin);
        }
        else
        {
            silentAttempted = true;
            XsollaLogin.Login(onLogin);
        }

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(XsollaAuthID),
                                   TimeSpan.FromSeconds(180.0f),
                                   () => FailureReason = "Auth login timed out! Failed to retrieve XsollaAuthID.");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            Log($"Auth OK! Retrieved XsollaAuthID: {XsollaAuthID}");
            Log($"Token expires in {XsollaTokenExpiresIn}s — refresh token present: {!string.IsNullOrWhiteSpace(XsollaRefreshToken)}");
        }

        yield return null;
    }

    private IEnumerator XsollaAuthTest_Step2()
    {
        if (BC.Client == null || !BC.Client.IsInitialized() || string.IsNullOrWhiteSpace(XsollaAuthID))
        {
            yield break;
        }

        FailureReason = string.Empty;
        Log($"STEP 2: Use External({EXTERNAL_NAME}) auth to log into brainCloud.");

        string response = string.Empty;
        BC.AuthenticateExternal(XsollaAuthID, XsollaToken, EXTERNAL_NAME, true,
            (jsonResponse, cbObject) =>
            {
                response = jsonResponse;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"AuthenticateAdvanced failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
            });

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
                                   TimeSpan.FromSeconds(30.0f),
                                   () => FailureReason = "AuthenticateAdvanced timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            Log($"brainCloud auth is a success! jsonResponse:\n{response}");
            Log($"brainCloud ProfileId: {BC.Client.ProfileId}  <-  Xsolla user: {XsollaAuthID}");
        }

        yield return null;
    }

    private IEnumerator XsollaAuthTest_Step3()
    {
        if (BC.Client == null || !BC.Client.IsAuthenticated() || XsollaConfig == null)
        {
            yield break;
        }

        FailureReason = string.Empty;
        Log("STEP 3: Link the brainCloud profile into the Xsolla client configuration.");

        XsollaConfig.accessToken = XsollaToken;
        XsollaConfig.userId = BC.Client.ProfileId;
        XsollaConfig.trackingId = BC.Client.ProfileId.Replace("-", string.Empty);

        Log($"Xsolla config now carries userId/trackingId: {BC.Client.ProfileId}");

        yield return null;
    }

    private bool XsollaAuthTest_AssertSuccess()
    {
        Assert(BC.Client != null, "BrainCloudWrapper.Client is null!");
        Assert(BC.Client.IsInitialized(), "BrainCloudWrapper.Client is not initialized!");
        Assert(!string.IsNullOrWhiteSpace(XsollaAuthID), "XsollaAuthID is null! Login was unsuccessful!");
        Assert(!string.IsNullOrWhiteSpace(XsollaToken), "XsollaToken is null! No access token to bridge with!");
        Assert(!string.IsNullOrWhiteSpace(BC.Client.ProfileId), "brainCloud ProfileId is empty! No profile was created or resumed.");

        return BC.Client != null &&
               BC.Client.IsInitialized() &&
               !string.IsNullOrWhiteSpace(XsollaAuthID) &&
               !string.IsNullOrWhiteSpace(XsollaToken) &&
               !string.IsNullOrWhiteSpace(BC.Client.ProfileId) &&
               BC.Client.IsAuthenticated();
    }

    #endregion

    #region Xsolla Purchase Test

    private IEnumerator XsollaPurchaseTest()
    {
        if (string.IsNullOrWhiteSpace(XsollaAuthID) || BC == null || BC.Client == null || !BC.Client.IsAuthenticated())
        {
            throw new Exception("User is not authenticated which means XsollaAuthenticationTest failed or was not run first!");
        }

        IAPProductID = string.Empty;
        PayloadContext = string.Empty;
        FailureReason = string.Empty;

        if (string.IsNullOrWhiteSpace(TestProductSKU))
        {
            FailureReason = "TestProductSKU must be set on the TestHelper component!";
        }

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            yield return XsollaPurchaseTest_Step1();
            yield return XsollaPurchaseTest_Step2();
            yield return XsollaPurchaseTest_Step3();
            yield return XsollaPurchaseTest_Step4();
            yield return XsollaPurchaseTest_Step5();
        }

        if (!XsollaPurchaseTest_AssertSuccess())
        {
            LogError("<b>Xsolla Purchase Test Failed!</b>");
        }
        else
        {
            Log("<b>Xsolla Purchase Test Finished Successfully!</b>");
        }
    }

    private IEnumerator XsollaPurchaseTest_Step1()
    {
        FailureReason = string.Empty;
        Log("STEP 1: Initialize the Xsolla store client and fetch products.");

        bool initialized = false;

        XsollaStore = XsollaStoreClient.Builder.Create()
                                               .SetConfiguration(XsollaConfig)
                                               .AddProduct(TestProductSKU)
                                               .SetOnRestore((product, error) =>
                                               {
                                                   if (error != null)
                                                   {
                                                       LogWarning($"Restore reported an error: {error}");
                                                   }
                                                   else if (product != null)
                                                   {
                                                       Log($"Consuming prior purchase: {product.sku} x{product.quantity}");
                                                       XsollaStore.ConsumeProduct(product.sku, product.quantity, product.transactionId, err =>
                                                       {
                                                           if (err != null) LogWarning($"Could not consume {product.sku}: {err}");
                                                       });
                                                   }
                                               })
                                               .Build();

        XsollaStore.Initialize((products, error) =>
        {
            if (error != null)
            {
                FailureReason = $"Xsolla store failed to initialize! {error}";
                return;
            }

            XsollaProducts = products;
            initialized = true;
        });

        yield return new WaitUntil(() => FailureOccurred || initialized,
                                   TimeSpan.FromSeconds(30.0f),
                                   () => FailureReason = "Xsolla store initialization timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            Log($"Xsolla store ready with {(XsollaProducts != null ? XsollaProducts.Length : 0)} product(s).");

            if (XsollaProducts != null)
            {
                foreach (var product in XsollaProducts)
                {
                    Log($"  - {product.sku}: {product.title} @ {product.formattedPriceWithDiscount}");
                }
            }
        }

        yield return null;
    }

    private IEnumerator XsollaPurchaseTest_Step2()
    {
        if (FailureOccurred)
        {
            yield break;
        }

        FailureReason = string.Empty;
        Log("STEP 2: Get the brainCloud sales inventory and pull the payload for the IAP product.");

        string response = string.Empty;
        BC.AppStoreService.GetSalesInventory(STORE_ID, string.Empty,
            (jsonResponse, cbObject) =>
            {
                response = jsonResponse;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"GetSalesInventory failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
            });

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
                                   TimeSpan.FromSeconds(30.0f),
                                   () => FailureReason = "GetSalesInventory timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
            yield break;
        }

        Log($"GetSalesInventory jsonResponse:\n{response}");

        try
        {
            var data = (JsonReader.Deserialize<Dictionary<string, object>>(response)["data"]
                       as Dictionary<string, object>)["productInventory"] as Dictionary<string, object>[];

            foreach (var product in data)
            {
                if (product.ContainsKey("priceData") && product["priceData"] is Dictionary<string, object> priceData &&
                    priceData != null && priceData.Count > 0 && priceData.ContainsKey("sku") &&
                    priceData["sku"] is string sku && !string.IsNullOrWhiteSpace(sku) && sku == TestProductSKU &&
                    product.ContainsKey("payload") && product["payload"] is string payload && !string.IsNullOrWhiteSpace(payload))
                {
                    IAPProductID = sku;
                    PayloadContext = payload;
                }
            }
        }
        catch (Exception e)
        {
            FailureReason = $"Could not parse the sales inventory response: {e.Message}";
        }

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else if (string.IsNullOrWhiteSpace(IAPProductID))
        {
            FailureReason = $"IAP product '{TestProductSKU}' was not found in the '{STORE_ID}' sales inventory!";
            LogError(FailureReason);
        }
        else
        {
            Log($"Found IAP product {IAPProductID} with payload: {PayloadContext}");
        }

        yield return null;
    }

    private IEnumerator XsollaPurchaseTest_Step3()
    {
        if (FailureOccurred)
        {
            yield break;
        }

        FailureReason = string.Empty;
        Log($"STEP 3: Purchase '{TestProductSKU}' through the Xsolla webstore");

        // The PayloadContext rides along as the developer payload so the receipt that
        // the payload can be matched with what brainCloud expects from the WebHook
        XsollaStore.PurchaseProduct(TestProductSKU, PayloadContext, (product, error) =>
        {
            if (error != null)
            {
                FailureReason = $"Xsolla purchase failed! {error}";
                return;
            }

            XsollaPurchase = product;
        });

        yield return new WaitUntil(() => FailureOccurred || XsollaPurchase != null,
                                   TimeSpan.FromSeconds(300.0f),
                                   () => FailureReason = "Xsolla purchase timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            Log($"Purchase complete! sku: {XsollaPurchase.sku} | order: {XsollaPurchase.orderId} | " +
                $"invoice: {XsollaPurchase.invoiceId} | txn: {XsollaPurchase.transactionId} | status: {XsollaPurchase.status}");
        }

        yield return null;
    }

    private IEnumerator XsollaPurchaseTest_Step4()
    {
        if (FailureOccurred || XsollaPurchase == null)
        {
            yield break;
        }

        FailureReason = string.Empty;
        Log("STEP 4: Verify the Xsolla purchase on brainCloud.");

        var receipt = new Dictionary<string, object>
        {
            ["orderId"] = XsollaPurchase.orderId.ToString()
        };

        string receiptJson = JsonWriter.Serialize(receipt);
        Log($"Receipt being sent to VerifyPurchase('{STORE_ID}'):\n{receiptJson.FormatJSON()}");

        const int RETRY = 5;
        int count = 0;
        float retryTime = 1.0f;
        VerifyReceiptResponse = string.Empty;

        // The WebHook for the purchase can take time to process on brainCloud
        // so we will retry a couple times until it comes back successfully
        while (++count <= RETRY)
        {
            BC.AppStoreService.VerifyPurchase(STORE_ID, receiptJson,
            (jsonResponse, cbObject) =>
            {
                VerifyReceiptResponse = jsonResponse;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"VerifyPurchase failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
            });

            yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(VerifyReceiptResponse),
                                       TimeSpan.FromSeconds(30.0f),
                                       () => FailureReason = "VerifyPurchase timed out!");

            if (FailureOccurred)
            {
                break;
            }

            Dictionary<string, object>[] data;
            try
            {
                data = ((JsonReader.Deserialize<Dictionary<string, object>>(VerifyReceiptResponse)
                       ["data"] as Dictionary<string, object>)
                       ["transactionSummary"] as Dictionary<string, object>)
                       ["transactionDetails"] as Dictionary<string, object>[];
            }
            catch (Exception e)
            {
                FailureReason = $"Could not parse the VerifyPurchase response: {e.Message}";
                break;
            }

            if (data == null || data.Length == 0)
            {
                FailureReason = "VerifyPurchase failed! transactionDetails came out as being null/empty" +
                                "meaning no purchases were successfully verified.";
                break;
            }
            else
            {
                bool transactionNotFound = false;
                foreach (var detail in data)
                {
                    if (detail.TryGetValue("transactionResultCode", out object code) &&
                        Convert.ToInt32(code) == 107) // Transaction not found
                    {
                        transactionNotFound = true;
                        break;
                    }
                }

                if (!transactionNotFound)
                {
                    break; // Transaction WAS found
                }

                VerifyReceiptResponse = string.Empty;

                if (count < RETRY)
                {
                    Log($"Couldn't find transaction. Trying again... ({count}/{RETRY})");

                    yield return new WaitForSecondsRealtime(retryTime);

                    retryTime = retryTime *= 2.0f;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(VerifyReceiptResponse))
        {
            FailureReason = "VerifyPurchase failed! Transaction could not be found or some other error occurred.";
        }

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            Log($"VerifyPurchase is a success! jsonResponse:\n{VerifyReceiptResponse}");
        }

        yield return null;
    }

    private IEnumerator XsollaPurchaseTest_Step5()
    {
        if (FailureOccurred || string.IsNullOrWhiteSpace(VerifyReceiptResponse))
        {
            yield break;
        }

        FailureReason = string.Empty;
        Log("STEP 5: Consuming Xsolla Purchase after being verified.");

        bool consumed = false;
        XsollaStore.ConsumeProduct(XsollaPurchase.sku, XsollaPurchase.quantity, XsollaPurchase.transactionId, err =>
        {
            if (err != null)
            {
                FailureReason = $"Could not consume {XsollaPurchase.sku}: {err}";
            }
            else
            {
                consumed = true;
            }
        });

        yield return new WaitUntil(() => FailureOccurred || consumed,
                                   TimeSpan.FromSeconds(30.0f),
                                   () => FailureReason = "ConsumeProduct timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            Log($"Consumed {XsollaPurchase.sku} x{XsollaPurchase.quantity}.");
        }
    }

    private bool XsollaPurchaseTest_AssertSuccess()
    {
        Assert(!string.IsNullOrWhiteSpace(IAPProductID), "IAPProductID is empty! Cannot verify purchase without it!");
        Assert(!string.IsNullOrWhiteSpace(PayloadContext), "PayloadContext is empty! Developer Payload wasn't filled!");
        Assert(XsollaPurchase != null, "No Xsolla purchase was completed!");
        Assert(XsollaPurchase == null || XsollaPurchase.orderId > 0, "The Xsolla purchase doesn't have a valid orderId!");
        Assert(!string.IsNullOrWhiteSpace(VerifyReceiptResponse), "VerifyReceiptResponse is empty! Purchase was not verified!");
        Assert(!FailureOccurred, $"The purchase test reported a failure: {FailureReason}");

        return !string.IsNullOrWhiteSpace(IAPProductID) &&
               !string.IsNullOrWhiteSpace(PayloadContext) &&
               XsollaPurchase != null &&
               XsollaPurchase.orderId > 0 &&
               !string.IsNullOrWhiteSpace(VerifyReceiptResponse) &&
               !FailureOccurred;
    }

    #endregion
}
