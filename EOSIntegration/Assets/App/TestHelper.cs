using BrainCloud.JsonFx.Json;
using Epic.OnlineServices;
using Epic.OnlineServices.Ecom;
using PlayEveryWare.EpicOnlineServices;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// EOS Auth
using AuthCopyIdTokenOptions = Epic.OnlineServices.Auth.CopyIdTokenOptions;
using AuthCredentials = Epic.OnlineServices.Auth.Credentials;
using AuthIdToken = Epic.OnlineServices.Auth.IdToken;
using AuthLoginCallbackInfo = Epic.OnlineServices.Auth.LoginCallbackInfo;
using AuthLoginCredentialType = Epic.OnlineServices.Auth.LoginCredentialType;
using AuthLoginOptions = Epic.OnlineServices.Auth.LoginOptions;
using AuthScopeFlags = Epic.OnlineServices.Auth.AuthScopeFlags;

// EOS Connect
using ConnectCopyIdTokenOptions = Epic.OnlineServices.Connect.CopyIdTokenOptions;
using ConnectCredentials = Epic.OnlineServices.Connect.Credentials;
using ConnectIdToken = Epic.OnlineServices.Connect.IdToken;
using ConnectLoginCallbackInfo = Epic.OnlineServices.Connect.LoginCallbackInfo;
using ConnectLoginOptions = Epic.OnlineServices.Connect.LoginOptions;
using CreateUserCallbackInfo = Epic.OnlineServices.Connect.CreateUserCallbackInfo;
using CreateUserOptions = Epic.OnlineServices.Connect.CreateUserOptions;

// EOS Misc
using ExternalCredentialType = Epic.OnlineServices.ExternalCredentialType;

public class TestHelper : MonoBehaviour
{
    [Header("Helper")]
    [SerializeField] private TMP_Text AppInfoLabel = default;

#if UNITY_EDITOR
    [Header("EOS Properties")]
    [SerializeField] private string DevAuthToolCredentialName = string.Empty;
    [SerializeField] private string DevAuthToolHostName = string.Empty;
#endif

    // EOS Auth
    private string AuthIDToken = string.Empty;
    private string ConnectIDToken = string.Empty;
    private EpicAccountId EpicAccountID = default;
    private ProductUserId ProductUserID = default;

    // EOS Purchase
    private CatalogItem? CatalogItem = null;
    private string OfferID = string.Empty;
    private string TransactionID = string.Empty;
    private List<string> EntitlementIDs = null;
    private string EntitlementName = string.Empty;
    private string EntitlementToken = string.Empty;
    private string AudienceItemID = string.Empty;

    // EOS Ownership
    private string DurableOfferID = string.Empty;
    private string OwnershipItemID = string.Empty;
    private string ItemNamespace = string.Empty;
    private string OwnershipToken = string.Empty;
    private bool ItemIsOwned = false;

    // brainCloud
    private const string STORE_ID = "epicGames";

    private BrainCloudWrapper BC = default;
    private string IAPProductID = string.Empty;
    private string PayloadContext = string.Empty;
    private string VerifyPurchaseJson = string.Empty;
    private string VerifyOwnershipJson = string.Empty;

    #region Unity Messages

    private void Awake()
    {
        BC = GetComponent<BrainCloudWrapper>();
    }

    private void Start()
    {
        StartCoroutine(StartEOSTesting());
    }

    private void OnApplicationQuit()
    {
        if (BC != null && BC.Client != null && BC.Client.IsAuthenticated())
        {
            BC.PlayerStateService.Logout();
        }
    }

    private void OnDestroy()
    {
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

    #endregion

    #region Test Initialization

    private IEnumerator StartEOSTesting()
    {
        yield return new WaitForSecondsRealtime(1.0f); // Waiting for several EOS messages to go through first

        yield return InitializeBrainCloud();

        yield return EOSAuthenticationTest();

        yield return EOSEntitlementTest();

        //yield return EOSOwnershipTest(); // We're not testing for Ownership so this is commented out
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

    #endregion

    #region EOS Authentication Test

    private AuthCredentials BuildAuthCredentials()
    {
#if UNITY_EDITOR
        // DevAuthTool must be running & logged in to run in-editor
        return new AuthCredentials
        {
            Type = AuthLoginCredentialType.Developer,
            Id = DevAuthToolHostName,
            Token = DevAuthToolCredentialName
        };
#else
            string exchangeCode = string.Empty;
            var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in Environment.GetCommandLineArgs())
            {
                string arg = raw.TrimStart('-');
                int split = arg.IndexOf('=');
                if (split > 0)
                    args[arg.Substring(0, split)] = arg.Substring(split + 1);
            }

            if (args.TryGetValue("AUTH_TYPE", out string authType) &&
                authType.Equals("exchangecode", StringComparison.OrdinalIgnoreCase) &&
                args.TryGetValue("AUTH_PASSWORD", out string code))
            {
                exchangeCode = code;
            }

            if (!string.IsNullOrEmpty(exchangeCode))
            {
                // Epic Games Launcher
                return new AuthCredentials
                {
                    Type = AuthLoginCredentialType.ExchangeCode,
                    Id = null,
                    Token = exchangeCode
                };
            }

            // Epic's internal UI
            return new AuthCredentials
            {
                Type = AuthLoginCredentialType.AccountPortal,
                Id = null,
                Token = null
            };
#endif
    }

    private IEnumerator EOSAuthenticationTest()
    {
        if (EpicAccountID != null && ProductUserID != null &&
            !string.IsNullOrWhiteSpace(AuthIDToken) && !string.IsNullOrWhiteSpace(ConnectIDToken))
        {
            LogWarning("EOSAuth properties are not null. Was test already ran?");
            yield break;
        }

        FailureReason = string.Empty;

        yield return new WaitUntil(() => EOSManager.Instance != null &&
                                         EOSManager.Instance.GetEOSPlatformInterface() != null,
                                   TimeSpan.FromSeconds(10.0f),
                                   () => FailureReason = "EOSManager platform never initialized!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            yield return EOSAuthTest_Step1();
            yield return EOSAuthTest_Step2();
            yield return EOSAuthTest_Step3();
            yield return EOSAuthTest_Step4();
            yield return EOSAuthTest_Step5();
        }

        if (!EOSAuthTest_AssertSuccess())
        {
            LogError("<b>EOS Auth Test Failed!</b>");
        }
        else
        {
            Log("<b>EOS Auth Test Finished Successfully!</b>");
        }

        yield return null;
    }

    private IEnumerator EOSAuthTest_Step1()
    {
        Log("STEP 1: Log in & retrieve EpicAccountID.");

        var options = new AuthLoginOptions
        {
            Credentials = BuildAuthCredentials(),
            ScopeFlags = AuthScopeFlags.NoFlags // Requesting scopes triggers a consent loop unless they're configured on the EAS app
        };

        FailureReason = string.Empty;

        Log($"Auth login as {options.Credentials.Value.Type}");

        EOSManager.Instance.GetEOSAuthInterface().Login(ref options, null, (ref AuthLoginCallbackInfo info) =>
        {
            if (!Common.IsOperationComplete(info.ResultCode))
            {
                Log($"Auth login in progress: {info.ResultCode}");
                return;
            }

            if (info.ResultCode != Result.Success)
            {
                FailureReason = $"Auth login failed! ResultCode: {info.ResultCode}";
                return;
            }

            EpicAccountID = info.LocalUserId;

        });

        yield return new WaitUntil(() => FailureOccurred || EpicAccountID != null,
                                   TimeSpan.FromSeconds(180.0f),
                                   () => FailureReason = "Auth login timed out! Failed to retrieve EpicAccountID.");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            Log($"Auth OK! Retrieved EpicAccountID: {EpicAccountID}");
        }

        yield return null;
    }

    private IEnumerator EOSAuthTest_Step2()
    {
        if (EpicAccountID == null)
        {
            LogWarning("STEP SKIPPED! EpicAccountID is null!");
            yield break;
        }

        FailureReason = string.Empty;
        Log("STEP 2: Copy AuthIDToken for Connect Interface & later brainCloud.");

        AuthIdToken? token = null;
        Result result = Result.NotFound;

        yield return new WaitUntil(() =>
        {
            var opts = new AuthCopyIdTokenOptions { AccountId = EpicAccountID };
            result = EOSManager.Instance.GetEOSAuthInterface().CopyIdToken(ref opts, out token);

            return result == Result.Success && token != null;
        },
        TimeSpan.FromSeconds(10.0f),
        () => FailureReason = $"Auth.CopyIdToken never succeeded! Last result: {result}");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            AuthIDToken = token.Value.JsonWebToken;
            Log($"Auth.CopyIdToken Success! AuthIDToken:\n{AuthIDToken}");
        }

        yield return null;
    }

    private IEnumerator EOSAuthTest_Step3()
    {
        Log("STEP 3: Use AuthIDToken to retrieve ProductUserID.");

        if (string.IsNullOrWhiteSpace(AuthIDToken))
        {
            LogWarning("STEP SKIPPED! AuthIDToken is null!");
            yield break;
        }

        FailureReason = string.Empty;

        var options = new ConnectLoginOptions
        {
            Credentials = new ConnectCredentials
            {
                Type = ExternalCredentialType.EpicIdToken,
                Token = AuthIDToken
            }
        };

        EOSManager.Instance.GetEOSConnectInterface().Login(ref options, null, (ref ConnectLoginCallbackInfo connectInfo) =>
        {
            if (connectInfo.ResultCode == Result.Success)
            {
                ProductUserID = connectInfo.LocalUserId;
                return;
            }
            else if (connectInfo.ResultCode == Result.InvalidUser) // NOT an error: Means we need to create a product account for the user
            {
                Log("Creating new ProductUserID");

                var createOptions = new CreateUserOptions { ContinuanceToken = connectInfo.ContinuanceToken };
                EOSManager.Instance.GetEOSConnectInterface().CreateUser(ref createOptions, null, (ref CreateUserCallbackInfo userInfo) =>
                {
                    if (userInfo.ResultCode != Result.Success)
                    {
                        FailureReason = $"Connect.CreateUser failed! ResultCode: {userInfo.ResultCode}";
                        return;
                    }

                    ProductUserID = userInfo.LocalUserId;

                    Log($"Product user created!");
                });
                return;
            }

            FailureReason = $"Connect login failed! ResultCode: {connectInfo.ResultCode}";
        });

        yield return new WaitUntil(() => FailureOccurred || ProductUserID != null,
                                   TimeSpan.FromSeconds(180.0f),
                                   () => FailureReason = "Connect login timed out! Failed to retrieve ProductUserID.");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            Log($"Connect OK! Retrieved ProductUserID: {ProductUserID}");
        }

        yield return null;
    }

    private IEnumerator EOSAuthTest_Step4()
    {
        Log("STEP 4: Copy ConnectIDToken for brainCloud.");

        if (ProductUserID == null)
        {
            LogWarning("STEP SKIPPED! ProductUserID is null!");
            yield break;
        }

        FailureReason = string.Empty;

        ConnectIdToken? token = null;
        Result result = Result.NotFound;

        yield return new WaitUntil(() =>
        {
            var opts = new ConnectCopyIdTokenOptions { LocalUserId = ProductUserID };
            result = EOSManager.Instance.GetEOSConnectInterface().CopyIdToken(ref opts, out token);

            return result == Result.Success && token != null;
        },
        TimeSpan.FromSeconds(10.0f),
        () => FailureReason = $"Connect.CopyIdToken never succeeded! Last result: {result}");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            ConnectIDToken = token.Value.JsonWebToken;
            Log($"Connect.CopyIdToken Success! ConnectIDToken:\n{ConnectIDToken}");
        }

        yield return null;
    }

    private IEnumerator EOSAuthTest_Step5()
    {
        Log("STEP 5: Use AuthIDToken to log into brainCloud.");

        if (BC.Client == null || !BC.Client.IsInitialized() ||
            string.IsNullOrWhiteSpace(AuthIDToken) || string.IsNullOrWhiteSpace(ConnectIDToken))
        {
            LogWarning("STEP SKIPPED! BC.Client uninitialized or IdTokens are empty!");
            yield break;
        }

        FailureReason = string.Empty;

        string response = string.Empty;
        BC.AuthenticateEpicGames(EpicAccountID.ToString(), AuthIDToken, true,
            (jsonResponse, cbObject) =>
            {
                response = jsonResponse;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"AuthenticateEpicGames failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
            });

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
                                   TimeSpan.FromSeconds(30.0f),
                                   () => FailureReason = "AuthenticateEpicGames timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            Log($"brainCloud's AuthenticateEpicGames is a success! jsonResponse:\n{response}");
        }

        yield return null;
    }

    private bool EOSAuthTest_AssertSuccess()
    {
        Assert(BC.Client != null, "BrainCloudWrapper.Client is null!");
        Assert(BC.Client != null && BC.Client.IsInitialized(), "BrainCloudWrapper.Client is not initialized!");
        Assert(EpicAccountID != null, "EpicAccountId is null! Login was unsuccessful!");
        Assert(ProductUserID != null, "ProductUserId is null! Login was unsuccessful!");
        Assert(!string.IsNullOrWhiteSpace(AuthIDToken), "AuthIdToken is empty! Did not copy Auth JWT!");
        Assert(!string.IsNullOrWhiteSpace(ConnectIDToken), "ConnectIdToken is empty! Did not copy Connect JWT!");
        Assert(BC.Client != null && BC.Client.IsAuthenticated(), "User is not authenticated in brainCloud!");

        return BC.Client != null &&
               BC.Client.IsInitialized() &&
               EpicAccountID != null &&
               ProductUserID != null &&
               !string.IsNullOrWhiteSpace(AuthIDToken) &&
               !string.IsNullOrWhiteSpace(ConnectIDToken) &&
               BC.Client.IsAuthenticated();
    }

    #endregion

    #region EOS Entitlement Test

    private IEnumerator EOSEntitlementTest()
    {
        if (EpicAccountID == null || !BC.Client.IsAuthenticated())
        {
            LogError("User is not authenticated which means EOSAuthenticationTest failed or was not run first!");
            yield break;
        }

        FailureReason = string.Empty;

        yield return new WaitUntil(() => EOSManager.Instance != null &&
                                         EOSManager.Instance.GetEOSPlatformInterface() != null,
                                   TimeSpan.FromSeconds(10.0f),
                                   () => FailureReason = "EOSManager platform never initialized!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            yield return EOSEntitlementTest_Step1();
            yield return EOSEntitlementTest_Step2();
            yield return EOSEntitlementTest_Step3();
            yield return EOSEntitlementTest_Step4();
            yield return EOSEntitlementTest_Step5();
            yield return EOSEntitlementTest_Step6();
        }

        if (!EOSEntitlementTest_AssertSuccess())
        {
            LogError("<b>EOS Entitlement Test Failed!</b>");
        }
        else
        {
            Log("<b>EOS Entitlement Test Finished Successfully!</b>");
        }
    }

    private IEnumerator EOSEntitlementTest_Step1()
    {
        Log("STEP 1: Query catalog offers.");

        if (EpicAccountID == null)
        {
            LogWarning("STEP SKIPPED! EpicAccountID is null!");
            yield break;
        }

        FailureReason = string.Empty;

        bool done = false;
        var options = new QueryOffersOptions { LocalUserId = EpicAccountID };
        var ecom = EOSManager.Instance.GetEOSEcomInterface();

        ecom.QueryOffers(ref options, null, (ref QueryOffersCallbackInfo info) =>
        {
            if (info.ResultCode != Result.Success)
            {
                FailureReason = $"QueryOffers failed! ResultCode: {info.ResultCode}";
            }

            done = true;
        });

        yield return new WaitUntil(() => FailureOccurred || done,
                                   TimeSpan.FromSeconds(30.0f),
                                   () => FailureReason = "QueryOffers timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
            yield break;
        }
        
        var countOptions = new GetOfferCountOptions { LocalUserId = EpicAccountID };
        uint count = ecom.GetOfferCount(ref countOptions);

        Log($"Found {count} offer{(count == 1 ? string.Empty : "s")}.");

        for (uint i = 0; i < count; i++)
        {
            var copyOptions = new CopyOfferByIndexOptions { LocalUserId = EpicAccountID, OfferIndex = i };
            if (ecom.CopyOfferByIndex(ref copyOptions, out CatalogOffer? offer) != Result.Success || offer == null)
            {
                continue;
            }

            CatalogOffer o = offer.Value;
            double price = o.CurrentPrice64 / Math.Pow(10, o.DecimalPoint);

            Log($"{o.TitleText} [{o.Id}] — {price:0.00} {o.CurrencyCode} (available: {o.AvailableForPurchase}, priceResult: {o.PriceResult})");

            if (o.AvailableForPurchase && o.PriceResult == Result.Success)
            {
                string id = o.Id.ToString();
                Log($"Selected offer: {o.TitleText} [{id}]");

                var itemCountOptions = new GetOfferItemCountOptions { LocalUserId = EpicAccountID, OfferId = o.Id };
                uint itemCount = ecom.GetOfferItemCount(ref itemCountOptions);

                for (uint j = 0; j < itemCount; j++)
                {
                    var itemOptions = new CopyOfferItemByIndexOptions
                    {
                        LocalUserId = EpicAccountID,
                        OfferId = o.Id,
                        ItemIndex = j
                    };

                    if (ecom.CopyOfferItemByIndex(ref itemOptions, out CatalogItem? item) != Result.Success || item == null)
                    {
                        continue;
                    }

                    CatalogItem ci = item.Value;
                    Log($"Offer: {ci.TitleText} (ItemType: {ci.ItemType}, EntitlementName: {ci.EntitlementName} [{ci.Id}])");

                    if (ci.ItemType == EcomItemType.Consumable)
                    {
                        OfferID = id;
                        CatalogItem = ci;
                    }
                }
            }
        }

        if (CatalogItem == null)
        {
            LogError("No purchasable offer found.");
        }

        yield return null;
    }

    private IEnumerator EOSEntitlementTest_Step2()
    {
        Log("STEP 2: Get matching IAPProductID matching OfferID and the PayloadContext from brainCloud.");

        if (CatalogItem == null)
        {
            LogWarning("STEP SKIPPED! CatalogItem is null!");
            yield break;
        }

        FailureReason = string.Empty;

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

        yield return new WaitUntil(() => !string.IsNullOrWhiteSpace(response) || FailureOccurred,
                                   TimeSpan.FromSeconds(30.0f),
                                   () => FailureReason = "GetSalesInventory timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
            yield break;
        }

        var data = (JsonReader.Deserialize<Dictionary<string, object>>(response)["data"]
                       as Dictionary<string, object>)["productInventory"] as Dictionary<string, object>[];

        foreach (var product in data)
        {
            if (product.ContainsKey("priceData") && product["priceData"] is Dictionary<string, object> priceData &&
                priceData != null && priceData.Count > 0 && priceData.ContainsKey("audienceItemId") &&
                priceData["audienceItemId"] is string id && !string.IsNullOrWhiteSpace(id) && id == CatalogItem.Value.Id &&
                product.ContainsKey("payload") && product["payload"] is string payload  && !string.IsNullOrWhiteSpace(payload))
            {
                IAPProductID = id;
                PayloadContext = payload;
            }
        }

        if (string.IsNullOrWhiteSpace(IAPProductID) || string.IsNullOrWhiteSpace(PayloadContext))
        {
            LogError("Failed to retreive Product info from brainCloud!");
            yield break;
        }

        Log($"IAPProductID retrieved: {IAPProductID}");
        Log($"And PayloadContext retrieved: {PayloadContext}");

        yield return null;
    }

    private IEnumerator EOSEntitlementTest_Step3()
    {
        Log($"STEP 3: Checkout offer {CatalogItem.Value.TitleText}.");

        if (CatalogItem == null)
        {
            LogWarning("STEP SKIPPED! CatalogItem is null!");
            yield break;
        }

        FailureReason = string.Empty;

        bool done = false;
        var options = new CheckoutOptions
        {
            LocalUserId = EpicAccountID,
            Entries = new CheckoutEntry[] { new() { OfferId = OfferID } }
        };

        EOSManager.Instance.GetEOSEcomInterface().Checkout(ref options, null, (ref CheckoutCallbackInfo info) =>
        {
            if (info.ResultCode != Result.Success)
            {
                FailureReason = $"Checkout failed! ResultCode: {info.ResultCode}";
            }
            else
            {
                TransactionID = info.TransactionId.ToString();
            }

            done = true;
        });

        yield return new WaitUntil(() => FailureOccurred || done,
                                   TimeSpan.FromSeconds(300.0f),
                                   () => FailureReason = "Checkout timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
            yield break;
        }

        Log($"Checkout OK! TransactionID: {TransactionID}");

        yield return null;
    }

    private IEnumerator EOSEntitlementTest_Step4()
    {
        Log("STEP 4: Query entitlements for the purchase.");

        if (EpicAccountID == null ||
            CatalogItem == null)
        {
            LogWarning("STEP SKIPPED! EpicAccountID or CatalogItem is null!");
            yield break;
        }

        FailureReason = string.Empty;
        EntitlementIDs = new();

        const int MAX_ATTEMPTS = 10; // We'll retry several times until entitlement is retrievable on Epic's side

        for (int attempt = 1; attempt <= MAX_ATTEMPTS; attempt++)
        {
            bool done = false;
            var options = new QueryEntitlementsOptions
            {
                LocalUserId = EpicAccountID,
                EntitlementNames = new Utf8String[0],
                IncludeRedeemed = false
            };

            EOSManager.Instance.GetEOSEcomInterface().QueryEntitlements(ref options, null, (ref QueryEntitlementsCallbackInfo info) =>
            {
                if (info.ResultCode != Result.Success)
                {
                    FailureReason = $"QueryEntitlements failed! ResultCode: {info.ResultCode}";
                    done = true;
                    return;
                }

                var countOptions = new GetEntitlementsCountOptions { LocalUserId = EpicAccountID };
                uint count = EOSManager.Instance.GetEOSEcomInterface().GetEntitlementsCount(ref countOptions);

                for (uint i = 0; i < count; i++)
                {
                    var copyOptions = new CopyEntitlementByIndexOptions
                    {
                        LocalUserId = EpicAccountID,
                        EntitlementIndex = i
                    };

                    if (EOSManager.Instance.GetEOSEcomInterface()
                        .CopyEntitlementByIndex(ref copyOptions, out Entitlement? entitlement) != Result.Success || entitlement == null)
                    {
                        continue;
                    }

                    Entitlement e = entitlement.Value;

                    if (e.Redeemed)
                    {
                        continue;
                    }

                    var itemOptions = new CopyItemByIdOptions { LocalUserId = EpicAccountID, ItemId = e.CatalogItemId };

                    if (EOSManager.Instance.GetEOSEcomInterface().CopyItemById(ref itemOptions, out CatalogItem? item) == Result.Success && item != null &&
                        item.Value.ItemType != EcomItemType.Consumable)
                    {
                        Log($"Skipping {e.EntitlementName} — {item.Value.ItemType}, not a consumable.");
                        continue;
                    }

                    if (e.CatalogItemId.ToString() != CatalogItem.Value.Id)
                    {
                        continue;
                    }

                    EntitlementIDs.Add(e.EntitlementId.ToString());
                    EntitlementName = e.EntitlementName.ToString();
                    AudienceItemID = e.CatalogItemId.ToString();

                    Log($"Entitlement: {EntitlementName} [{e.EntitlementId}] (CatalogItemId: {AudienceItemID})");
                }

                done = true;
            });

            yield return new WaitUntil(() => FailureOccurred || done,
                                       TimeSpan.FromSeconds(15.0f),
                                       () => FailureReason = "QueryEntitlements timed out!");

            if (FailureOccurred)
            {
                LogError(FailureReason);
                yield break;
            }

            if (EntitlementIDs.Count > 0)
            {
                break;
            }

            Log($"No unredeemed entitlement yet (attempt {attempt}/{MAX_ATTEMPTS}) — retrying...");

            yield return new WaitForSecondsRealtime(1.0f);
        }

        if (EntitlementIDs.Count == 0)
        {
            FailureReason = "No unredeemed entitlement appeared after checkout.";
            LogError(FailureReason);
            yield break;
        }

        Log($"Entitlement OK! EntitlementIDs: {string.Join(", ", EntitlementIDs)}");

        yield return null;
    }

    private IEnumerator EOSEntitlementTest_Step5()
    {
        Log("STEP 5: Copy the entitlement token (the receipt for brainCloud).");

        if (EntitlementIDs == null || EntitlementIDs.Count == 0)
        {
            LogWarning("STEP SKIPPED! EntitlementID is empty!");
            yield break;
        }

        FailureReason = string.Empty;

        bool done = false;
        var options = new QueryEntitlementTokenOptions
        {
            LocalUserId = EpicAccountID,
            EntitlementNames = new Utf8String[] { EntitlementName }
        };

        EOSManager.Instance.GetEOSEcomInterface().QueryEntitlementToken(ref options, null, (ref QueryEntitlementTokenCallbackInfo info) =>
        {
            if (info.ResultCode != Result.Success)
            {
                FailureReason = $"QueryEntitlementToken failed! ResultCode: {info.ResultCode}";
            }
            else
            {
                EntitlementToken = info.EntitlementToken.ToString();
            }

            done = true;
        });

        yield return new WaitUntil(() => FailureOccurred || done,
                                   TimeSpan.FromSeconds(30.0f),
                                   () => FailureReason = "QueryEntitlementToken timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
            yield break;
        }

        Log($"EntitlementToken retrieved:\n{EntitlementToken}");

        yield return null;
    }

    private IEnumerator EOSEntitlementTest_Step6()
    {
        Log("STEP 6: Verify the purchase through brainCloud.");

        if (string.IsNullOrWhiteSpace(IAPProductID) ||
            string.IsNullOrWhiteSpace(PayloadContext) ||
            string.IsNullOrWhiteSpace(EntitlementToken) ||
            BC == null || BC.Client == null || !BC.Client.IsAuthenticated())
        {
            LogWarning("STEP SKIPPED! BC.Client is uninitialized or other properties are empty!");
            yield break;
        }

        Log("NOTE: brainCloud will redeem entitlements during the VerifyPurchase call.");

        FailureReason = string.Empty;

        string receiptJson = JsonWriter.Serialize(new Dictionary<string, object>()
        {
            { "entitlementIds",  EntitlementIDs },
            { "audienceItemId",  AudienceItemID }
        });

        bool done = false;
        BC.AppStoreService.CachePurchasePayloadContext(STORE_ID, IAPProductID, PayloadContext,
            (jsonResponse, cbObject) =>
            {
                Log($"CachePurchasePayloadContext success:\n{jsonResponse}");
                done = true;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"CachePurchasePayloadContext failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
                done = true;
            });

        yield return new WaitUntil(() => FailureOccurred || done,
                                   TimeSpan.FromSeconds(30.0f),
                                   () => FailureReason = "CachePurchasePayloadContext timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
            yield break;
        }

        done = false;
        BC.AppStoreService.VerifyPurchase(STORE_ID, receiptJson,
            (jsonResponse, cbObject) =>
            {
                VerifyPurchaseJson = jsonResponse;
                Log($"VerifyPurchase success:\n{jsonResponse}");
                done = true;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"VerifyPurchase failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
                done = true;
            });

        yield return new WaitUntil(() => FailureOccurred || done,
                                   TimeSpan.FromSeconds(30.0f),
                                   () => FailureReason = "VerifyPurchase timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }

        yield return null;
    }

    private bool EOSEntitlementTest_AssertSuccess()
    {
        Assert(!string.IsNullOrWhiteSpace(IAPProductID), "IAPProductID is empty! Cannot verify purchase without it!");
        Assert(!string.IsNullOrWhiteSpace(PayloadContext), "PayloadContext is empty! Payload cannot be cached!");
        Assert(CatalogItem != null, "CatalogItem is null! No offer was selected!");
        Assert(!string.IsNullOrWhiteSpace(TransactionID), "TransactionID is empty! Checkout did not complete!");
        Assert(EntitlementIDs != null && EntitlementIDs.Count > 0, "EntitlementIDs is empty! The grant never landed!");
        Assert(!string.IsNullOrWhiteSpace(AudienceItemID), "AudienceItemID is empty! The grant never landed!");
        Assert(!string.IsNullOrWhiteSpace(EntitlementToken), "EntitlementToken Empty! Nothing to verify!");
        Assert(!string.IsNullOrWhiteSpace(VerifyPurchaseJson), "VerifyPurchaseJson is empty! Purchase wasn't verified by brainCloud!");

        return !string.IsNullOrWhiteSpace(IAPProductID) &&
               !string.IsNullOrWhiteSpace(PayloadContext) &&
               CatalogItem != null &&
               !string.IsNullOrWhiteSpace(TransactionID) &&
               EntitlementIDs != null &&
               EntitlementIDs.Count > 0 &&
               !string.IsNullOrWhiteSpace(AudienceItemID) &&
               !string.IsNullOrWhiteSpace(EntitlementToken) &&
               !string.IsNullOrWhiteSpace(VerifyPurchaseJson);
    }

    #endregion

    // Ownership is not being tested since it's not supported, but this will be kept in-case we revisit
    #region EOS Ownership Test

    private IEnumerator EOSOwnershipTest()
    {
        if (EpicAccountID == null || !BC.Client.IsAuthenticated())
        {
            LogError("User is not authenticated which means EOSAuthenticationTest failed or was not run first!");
            yield break;
        }

        IAPProductID = string.Empty;
        PayloadContext = string.Empty;
        VerifyOwnershipJson = string.Empty;
        FailureReason = string.Empty;

        yield return new WaitUntil(() => EOSManager.Instance != null &&
                                         EOSManager.Instance.GetEOSPlatformInterface() != null,
                                   TimeSpan.FromSeconds(10.0f),
                                   () => FailureReason = "EOSManager platform never initialized!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            yield return EOSOwnershipTest_Step1();
            //yield return EOSOwnershipTest_Step2(); // Commented out until BC support comes in
            //yield return EOSOwnershipTest_Step3(); // Commented out since the base game is already owned
            yield return EOSOwnershipTest_Step4();
            yield return EOSOwnershipTest_Step5();
            yield return EOSOwnershipTest_Step6();
        }

        if (!EOSOwnershipTest_AssertSuccess())
        {
            LogError("<b>EOS Ownership Test Failed!</b>");
        }
        else
        {
            Log("<b>EOS Ownership Test Finished Successfully!</b>");
        }
    }

    private IEnumerator EOSOwnershipTest_Step1()
    {
        Log("STEP 1: Query catalog offers for a durable item.");

        if (EpicAccountID == null)
        {
            LogWarning("STEP SKIPPED!");
            yield break;
        }

        FailureReason = string.Empty;

        bool done = false;
        var options = new QueryOffersOptions { LocalUserId = EpicAccountID };
        var ecom = EOSManager.Instance.GetEOSEcomInterface();

        ecom.QueryOffers(ref options, null, (ref QueryOffersCallbackInfo info) =>
        {
            if (info.ResultCode != Result.Success)
            {
                FailureReason = $"QueryOffers failed! ResultCode: {info.ResultCode}";
            }

            done = true;
        });

        yield return new WaitUntil(() => FailureOccurred || done,
                                   TimeSpan.FromSeconds(30.0f),
                                   () => FailureReason = "QueryOffers timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
            yield break;
        }

        var countOptions = new GetOfferCountOptions { LocalUserId = EpicAccountID };
        uint count = ecom.GetOfferCount(ref countOptions);

        Log($"Found {count} offer{(count == 1 ? string.Empty : "s")}.");

        for (uint i = 0; i < count; i++)
        {
            var copyOptions = new CopyOfferByIndexOptions { LocalUserId = EpicAccountID, OfferIndex = i };
            if (ecom.CopyOfferByIndex(ref copyOptions, out CatalogOffer? offer) != Result.Success || offer == null)
            {
                continue;
            }

            CatalogOffer o = offer.Value;
            double price = o.CurrentPrice64 / Math.Pow(10, o.DecimalPoint);

            Log($"{o.TitleText} [{o.Id}] — {price:0.00} {o.CurrencyCode} (available: {o.AvailableForPurchase}, priceResult: {o.PriceResult})");

            var itemCountOptions = new GetOfferItemCountOptions { LocalUserId = EpicAccountID, OfferId = o.Id };
            uint itemCount = ecom.GetOfferItemCount(ref itemCountOptions);

            for (uint j = 0; j < itemCount; j++)
            {
                var itemOptions = new CopyOfferItemByIndexOptions
                {
                    LocalUserId = EpicAccountID,
                    OfferId = o.Id,
                    ItemIndex = j
                };

                if (ecom.CopyOfferItemByIndex(ref itemOptions, out CatalogItem? item) != Result.Success || item == null)
                {
                    continue;
                }

                CatalogItem ci = item.Value;
                Log($"Offer: {ci.TitleText} (ItemType: {ci.ItemType}, EntitlementName: {ci.EntitlementName})");

                if (ci.ItemType == EcomItemType.Durable && string.IsNullOrWhiteSpace(OwnershipItemID))
                {
                    DurableOfferID = o.Id.ToString();
                    OwnershipItemID = ci.Id.ToString();
                    ItemNamespace = ci.CatalogNamespace.ToString();

                    Log($"Selected durable: {ci.TitleText} [{OwnershipItemID}]");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(OwnershipItemID))
        {
            LogError("No durable item found.");
        }

        yield return null;
    }

    private IEnumerator EOSOwnershipTest_Step2()
    {
        Log($"STEP 2: Get matching IAPProductID matching DurableOfferID and the PayloadContext from brainCloud.");

        if (string.IsNullOrWhiteSpace(DurableOfferID))
        {
            LogWarning("STEP SKIPPED!");
            yield break;
        }

        FailureReason = string.Empty;

        // TODO: Product needs to be set-up on brainCloud (when that becomes available...)

        bool done = false;
        string response = string.Empty;
        BC.AppStoreService.GetSalesInventory(STORE_ID, string.Empty,
            (jsonResponse, cbObject) =>
            {
                response = jsonResponse;
                done = true;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"GetSalesInventory failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
                done = true;
            });

        yield return new WaitUntil(() => FailureOccurred || done,
                                   TimeSpan.FromSeconds(30.0f),
                                   () => FailureReason = "GetSalesInventory timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
            yield break;
        }

        var data = (JsonReader.Deserialize<Dictionary<string, object>>(response)["data"]
                       as Dictionary<string, object>)["productInventory"] as Dictionary<string, object>[];

        foreach (var product in data)
        {
            if (product.ContainsKey("priceData") && product["priceData"] is Dictionary<string, object> priceData &&
                priceData != null && priceData.Count > 0 && priceData.ContainsKey("audienceItemId") &&
                priceData["audienceItemId"] is string id && !string.IsNullOrWhiteSpace(id) && id == DurableOfferID &&
                product.ContainsKey("payload") && product["payload"] is string payload && !string.IsNullOrWhiteSpace(payload))
            {
                IAPProductID = id;
                PayloadContext = payload;
            }
        }

        if (string.IsNullOrWhiteSpace(IAPProductID) || string.IsNullOrWhiteSpace(PayloadContext))
        {
            LogError("Failed to retreive Product info from brainCloud!");
            yield break;
        }

        Log($"IAPProductID retrieved: {IAPProductID}");
        Log($"And PayloadContext retrieved: {PayloadContext}");

        yield return null;
    }

    private IEnumerator EOSOwnershipTest_Step3()
    {
        Log($"STEP 3: Checkout durable offer {DurableOfferID}.");

        if (string.IsNullOrWhiteSpace(DurableOfferID))// ||
            //string.IsNullOrEmpty(IAPProductID) ||
            //string.IsNullOrEmpty(PayloadContext))
        {
            LogWarning("STEP SKIPPED!");
            yield break;
        }

        FailureReason = string.Empty;

        bool done = false;
        var options = new CheckoutOptions
        {
            LocalUserId = EpicAccountID,
            Entries = new CheckoutEntry[] { new() { OfferId = DurableOfferID } }
        };

        EOSManager.Instance.GetEOSEcomInterface().Checkout(ref options, null, (ref CheckoutCallbackInfo info) =>
        {
            if (info.ResultCode != Result.Success)
            {
                FailureReason = $"Checkout failed! ResultCode: {info.ResultCode}";
            }
            else
            {
                TransactionID = info.TransactionId.ToString();
            }

            done = true;
        });

        yield return new WaitUntil(() => FailureOccurred || done,
                                   TimeSpan.FromSeconds(300.0f),
                                   () => FailureReason = "Checkout timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
            yield break;
        }

        Log($"Checkout OK! TransactionID: {TransactionID}");

        yield return null;
    }

    private IEnumerator EOSOwnershipTest_Step4()
    {
        Log("STEP 4: Query ownership of the durable item.");

        if (string.IsNullOrWhiteSpace(OwnershipItemID))
        {
            LogWarning("STEP SKIPPED!");
            yield break;
        }

        FailureReason = string.Empty;

        const int MAX_ATTEMPTS = 10; // We'll retry several times until ownership is reflected on Epic's side

        for (int attempt = 1; attempt <= MAX_ATTEMPTS; attempt++)
        {
            bool done = false;
            var options = new QueryOwnershipOptions
            {
                LocalUserId = EpicAccountID,
                CatalogItemIds = new Utf8String[] { OwnershipItemID },
                CatalogNamespace = ItemNamespace
            };

            EOSManager.Instance.GetEOSEcomInterface().QueryOwnership(ref options, null, (ref QueryOwnershipCallbackInfo info) =>
            {
                if (info.ResultCode != Result.Success)
                {
                    FailureReason = $"QueryOwnership failed! ResultCode: {info.ResultCode}";
                    done = true;
                    return;
                }

                if (info.ItemOwnership != null)
                {
                    foreach (ItemOwnership ownership in info.ItemOwnership)
                    {
                        Log($"Ownership: [{ownership.Id}] {ownership.OwnershipStatus}");

                        if (ownership.Id.ToString() == OwnershipItemID &&
                            ownership.OwnershipStatus == OwnershipStatus.Owned)
                        {
                            ItemIsOwned = true;
                            break;
                        }
                    }
                }

                done = true;
            });

            yield return new WaitUntil(() => FailureOccurred || done,
                                       TimeSpan.FromSeconds(15.0f),
                                       () => FailureReason = "QueryOwnership timed out!");

            if (FailureOccurred)
            {
                LogError(FailureReason);
                yield break;
            }

            if (ItemIsOwned)
            {
                break;
            }

            Log($"Item not owned yet (attempt {attempt}/{MAX_ATTEMPTS}) — retrying...");

            yield return new WaitForSecondsRealtime(1.0f);
        }

        if (!ItemIsOwned)
        {
            FailureReason = "The item is not owned by this account.";
            LogError(FailureReason);
            yield break;
        }

        Log($"Ownership OK! CatalogItemID: {OwnershipItemID}");

        yield return null;
    }

    private IEnumerator EOSOwnershipTest_Step5()
    {
        Log("STEP 5: Copy the ownership token (the receipt for brainCloud).");

        if (!ItemIsOwned)
        {
            LogWarning("STEP SKIPPED!");
            yield break;
        }

        FailureReason = string.Empty;

        bool done = false;
        var options = new QueryOwnershipTokenOptions
        {
            LocalUserId = EpicAccountID,
            CatalogItemIds = new Utf8String[] { OwnershipItemID },
            CatalogNamespace = ItemNamespace
        };

        EOSManager.Instance.GetEOSEcomInterface().QueryOwnershipToken(ref options, null, (ref QueryOwnershipTokenCallbackInfo info) =>
        {
            if (info.ResultCode != Result.Success)
            {
                FailureReason = $"QueryOwnershipToken failed! ResultCode: {info.ResultCode}";
            }
            else
            {
                OwnershipToken = info.OwnershipToken.ToString();
            }

            done = true;
        });

        yield return new WaitUntil(() => FailureOccurred || done,
                                   TimeSpan.FromSeconds(30.0f),
                                   () => FailureReason = "QueryOwnershipToken timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
            yield break;
        }

        Log($"OwnershipToken retrieved:\n{OwnershipToken}");

        yield return null;
    }

    private IEnumerator EOSOwnershipTest_Step6()
    {
        Log("STEP 6: Verify ownership through brainCloud.");

        if (string.IsNullOrWhiteSpace(IAPProductID) ||
            string.IsNullOrWhiteSpace(PayloadContext) ||
            string.IsNullOrWhiteSpace(OwnershipToken) ||
            BC == null || BC.Client == null || !BC.Client.IsAuthenticated())
        {
            LogWarning("STEP SKIPPED!");
            yield break;
        }

        FailureReason = string.Empty;

        // TODO: Need to get the proper receiptJson format
        string receiptJson = JsonWriter.Serialize(new Dictionary<string, object>()
        {
            { "ownershipToken",    OwnershipToken                     },
            { "catalogItemId",     OwnershipItemID                      },
            { "catalogNamespace",  ItemNamespace                      },
            { "offerId",           DurableOfferID                     },
            { "epicAccountId",     EpicAccountID.ToString()           },
            { "sandboxId",         "6f47fc27bf4e4faa947e5c50a7e492d4" },
            { "deploymentId",      "3d2140535d7d4d2aaeac9bc7ea032688" }
        });

        bool done = false;

        // TODO: Test when we have the API properly hooked up
        BC.AppStoreService.CachePurchasePayloadContext(STORE_ID, IAPProductID, PayloadContext,
            (jsonResponse, cbObject) =>
            {
                Log($"CachePurchasePayloadContext success:\n{jsonResponse}");
                done = true;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"CachePurchasePayloadContext failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
                done = true;
            });

        yield return new WaitUntil(() => FailureOccurred || done,
                                   TimeSpan.FromSeconds(30.0f),
                                   () => FailureReason = "CachePurchasePayloadContext timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
            yield break;
        }

        done = false;

        BC.AppStoreService.VerifyPurchase(STORE_ID, receiptJson,
            (jsonResponse, cbObject) =>
            {
                VerifyOwnershipJson = jsonResponse;
                Log($"VerifyPurchase success:\n{jsonResponse}");
                done = true;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"VerifyPurchase failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
                done = true;
            });

        yield return new WaitUntil(() => FailureOccurred || done,
                                   TimeSpan.FromSeconds(30.0f),
                                   () => FailureReason = "VerifyPurchase timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }

        yield return null;
    }

    private bool EOSOwnershipTest_AssertSuccess()
    {
        Assert(!string.IsNullOrWhiteSpace(IAPProductID), "IAPProductID is empty! Cannot verify ownership without it!");
        Assert(!string.IsNullOrWhiteSpace(PayloadContext), "PayloadContext is empty! Payload cannot be cached!");
        Assert(!string.IsNullOrWhiteSpace(DurableOfferID), "DurableOfferID is empty! No durable offer was found!");
        Assert(!string.IsNullOrWhiteSpace(OwnershipItemID), "CatalogItemID is empty! No durable item was selected!");
        Assert(ItemIsOwned, "The item is not owned! Nothing to verify!");
        Assert(!string.IsNullOrWhiteSpace(OwnershipToken), "OwnershipToken is empty! Nothing to verify!");
        Assert(!string.IsNullOrWhiteSpace(VerifyOwnershipJson), "VerifyOwnershipJson is empty! Ownership wasn't verified by brainCloud!");

        return !string.IsNullOrWhiteSpace(IAPProductID) &&
               !string.IsNullOrWhiteSpace(PayloadContext) &&
               !string.IsNullOrWhiteSpace(DurableOfferID) &&
               !string.IsNullOrWhiteSpace(OwnershipItemID) &&
               ItemIsOwned &&
               !string.IsNullOrWhiteSpace(OwnershipToken) &&
               !string.IsNullOrWhiteSpace(VerifyOwnershipJson);
    }

    #endregion // Ownership is not being tested since it's not supported, but this will be kept in-case we revisit
}
