using Epic.OnlineServices;
using Epic.OnlineServices.Ecom;
using PlayEveryWare.EpicOnlineServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// AuthInterface
using AuthCopyIdTokenOptions = Epic.OnlineServices.Auth.CopyIdTokenOptions;
using AuthCredentials = Epic.OnlineServices.Auth.Credentials;
using AuthIdToken = Epic.OnlineServices.Auth.IdToken;
using AuthLoginCallbackInfo = Epic.OnlineServices.Auth.LoginCallbackInfo;
using AuthLoginCredentialType = Epic.OnlineServices.Auth.LoginCredentialType;
using AuthLoginOptions = Epic.OnlineServices.Auth.LoginOptions;
using AuthScopeFlags = Epic.OnlineServices.Auth.AuthScopeFlags;

// ConnectInterface
using ConnectCopyIdTokenOptions = Epic.OnlineServices.Connect.CopyIdTokenOptions;
using ConnectCredentials = Epic.OnlineServices.Connect.Credentials;
using ConnectIdToken = Epic.OnlineServices.Connect.IdToken;
using ConnectLoginCallbackInfo = Epic.OnlineServices.Connect.LoginCallbackInfo;
using ConnectLoginOptions = Epic.OnlineServices.Connect.LoginOptions;
using CreateUserCallbackInfo = Epic.OnlineServices.Connect.CreateUserCallbackInfo;
using CreateUserOptions = Epic.OnlineServices.Connect.CreateUserOptions;

using JsonParser = BrainCloud.Common.JsonParser;
using JsonReader = BrainCloud.JsonFx.Json.JsonReader;
using JsonWriter = BrainCloud.JsonFx.Json.JsonWriter;

public class EOSHandler : MonoBehaviour
{
    #region Serialized Fields

    [Header("App")]
    [SerializeField] private TMP_Text AppInfoText;

#if UNITY_EDITOR
    [Header("EOS Editor Testing")]
    [SerializeField] private string DevAuthToolCredentialName;
    [SerializeField] private string DevAuthToolHostName;
#endif

    [Header("App Controls")]
    [SerializeField] private Toggle UserControlsToggle;
    [SerializeField] private GameObject[] UserControlsOnOffStates;
    [SerializeField] private Toggle LoggingToggle;
    [SerializeField] private GameObject[] LoggingOnOffStates;
    [SerializeField] private CanvasGroup MainContentCG;
    [SerializeField] private CanvasGroup LoggerContentCG;

    [Header("User Info")]
    [SerializeField] private TMP_Text ProfileIdText;
    [SerializeField] private TMP_Text AnonymousIdText;

    [Header("User Controls")]
    [SerializeField] private Button AuthLogInButton;
    [SerializeField] private Button ConnectLogInButton;
    [SerializeField] private Button RefreshButton;

    [Header("Currency Info")]
    [SerializeField] private Button CurrencySelectorButton_TEMPLATE;
    [SerializeField] private Transform CurrencyContent;

    [Header("Inventory Info")]
    [SerializeField] private Button InventorySelectorButton_TEMPLATE;
    [SerializeField] private Transform InventoryContent;

    [Header("Currency Control")]
    [SerializeField] private TMP_Text SelectedCurrencyText;
    [SerializeField] private TMP_InputField ConsumeAmountInput;
    [SerializeField] private Button ConsumeButton;

    [Header("Inventory Control")]
    [SerializeField] private TMP_Text SelectedInventoryText;
    [SerializeField] private Toggle TradeToggle;
    [SerializeField] private GameObject[] TradeOnOffStates;
    [SerializeField] private Toggle DropToggle;
    [SerializeField] private GameObject[] DropOnOffStates;
    [SerializeField] private TMP_InputField TradeToInput;
    [SerializeField] private Button TradeButton;
    [SerializeField] private TMP_InputField DropAmountInput;
    [SerializeField] private Button DropButton;

    [Header("Controls GameObjects")]
    [SerializeField] private GameObject CurrencyControlsGO;
    [SerializeField] private GameObject InventoryControlsGO;
    [SerializeField] private GameObject HowToControlsGO;

    [Header("Store")]
    [SerializeField] private TMP_Text StoreItem_TEMPLATE;
    [SerializeField] private Transform StoreContent;

    [Header("Checkout PopUp")]
    [SerializeField] private CanvasGroup CheckoutPopUpCG;
    [SerializeField] private TMP_Text ItemNameText;
    [SerializeField] private TMP_Text ItemDescriptionText;
    [SerializeField] private TMP_Text ItemPriceText;
    [SerializeField] private TMP_Text BonusText;
    [SerializeField] private Button ClosePopUpButton;
    [SerializeField] private Button CheckoutButton;

    #endregion

    // App
    private bool LockUI = false;
    private int SelectedCurrencyItem = -1;
    private List<Button> CurrencyItems = new();
    private List<string> CurrencyTypes = new();
    private List<long> CurrencyBalance = new();
    private int SelectedInventoryItem = -1;
    private List<Button> InventoryItems = new();
    private List<string> InventoryDetails = new();
    private List<TMP_Text> StoreItems = new();
    private List<string> StoreItemDefinitions = new();
    private string SelectedStoreItem = string.Empty;

    // brainCloud
    private const float RESPONSE_TIME = 20.0f; // 20 seconds is standard time before dropping for a brainCloud API call
    private const string STORE_ID = "epicGames";
    private BrainCloudWrapper BCWrapper = default;

    // EOS
    private (EpicAccountId id, string token) EpicAccountInfo = default;
    private (ProductUserId id, string token) ConnectUserInfo = default;
    private Dictionary<string, CatalogItem> CatalogItems = null;
    private (string audienceItemId, List<string> entitlementIds) EntitlementTransaction = default;

    // Testing
    private string FailureReason = string.Empty;
    private bool FailureOccurred => !FailureReason.IsEmpty();

    #region Unity Messages

    private void Awake()
    {
        UserControlsToggle.isOn = true;
        DropToggle.isOn = true;
        CurrencySelectorButton_TEMPLATE.gameObject.SetActive(false);
        InventorySelectorButton_TEMPLATE.gameObject.SetActive(false);
        StoreItem_TEMPLATE.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // Toggles
        UserControlsToggle.onValueChanged.AddListener(OnUserControlsToggle);
        LoggingToggle.onValueChanged.AddListener(OnLoggingToggle);
        TradeToggle.onValueChanged.AddListener(OnTradeToggle);
        DropToggle.onValueChanged.AddListener(OnDropToggle);

        // Buttons
        AuthLogInButton.onClick.AddListener(OnAuthLogInButton);
        ConnectLogInButton.onClick.AddListener(OnConnectLogInButton);
        RefreshButton.onClick.AddListener(OnRefreshButton);
        ConsumeButton.onClick.AddListener(OnConsumeButton);
        TradeButton.onClick.AddListener(OnTradeButton);
        DropButton.onClick.AddListener(OnDropButton);
        ClosePopUpButton.onClick.AddListener(OnClosePopUpButton);
        CheckoutButton.onClick.AddListener(OnCheckoutButton);

        // Input Fields
        ConsumeAmountInput.onValueChanged.AddListener(OnConsumeAmountValueChanged);
        TradeToInput.onValueChanged.AddListener(OnTradeToValueChanged);
        DropAmountInput.onValueChanged.AddListener(OnDropAmountValueChanged);
    }

    private void Start()
    {
        BCWrapper = GetComponent<BrainCloudWrapper>();

        UpdateBCIds(string.Empty, string.Empty);

        OnClosePopUpButton();
        OnUserControlsToggle(UserControlsToggle.isOn);
        OnLoggingToggle(LoggingToggle.isOn);
        OnTradeToggle(TradeToggle.isOn);
        OnDropToggle(DropToggle.isOn);
        OnConsumeAmountValueChanged(string.Empty);
        OnTradeToValueChanged(string.Empty);
        OnDropAmountValueChanged(string.Empty);
        ShowHowToControls();

        LockUI = true;
        MainContentCG.interactable = false;
        MainContentCG.blocksRaycasts = false;

        StartCoroutine(WaitForEOSPlatformInterface());
    }

    private void OnDisable()
    {
        // Toggles
        UserControlsToggle.onValueChanged.RemoveAllListeners();
        LoggingToggle.onValueChanged.RemoveAllListeners();
        TradeToggle.onValueChanged.RemoveAllListeners();
        DropToggle.onValueChanged.RemoveAllListeners();

        // Buttons
        AuthLogInButton.onClick.RemoveAllListeners();
        ConnectLogInButton.onClick.RemoveAllListeners();
        RefreshButton.onClick.RemoveAllListeners();
        ConsumeButton.onClick.RemoveAllListeners();
        TradeButton.onClick.RemoveAllListeners();
        DropButton.onClick.RemoveAllListeners();
        ClosePopUpButton.onClick.RemoveAllListeners();
        CheckoutButton.onClick.RemoveAllListeners();

        // Input Fields
        ConsumeAmountInput.onValueChanged.RemoveAllListeners();
        TradeToInput.onValueChanged.RemoveAllListeners();
        DropAmountInput.onValueChanged.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        if (CurrencyItems != null)
        {
            CurrencyItems.Clear();
            CurrencyItems = null;
        }

        if (CurrencyTypes != null)
        {
            CurrencyTypes.Clear();
            CurrencyTypes = null;
        }

        if (CurrencyBalance != null)
        {
            CurrencyBalance.Clear();
            CurrencyBalance = null;
        }

        if (InventoryItems != null)
        {
            InventoryItems.Clear();
            InventoryItems = null;
        }

        if (InventoryDetails != null)
        {
            InventoryDetails.Clear();
            InventoryDetails = null;
        }

        if (StoreItems != null)
        {
            StoreItems.Clear();
            StoreItems = null;
        }

        if (StoreItemDefinitions != null)
        {
            StoreItemDefinitions.Clear();
            StoreItemDefinitions = null;
        }

        if (CatalogItems != null)
        {
            CatalogItems.Clear();
            CatalogItems = null;
        }

        if (EntitlementTransaction.entitlementIds != null)
        {
            EntitlementTransaction.entitlementIds.Clear();
            EntitlementTransaction.entitlementIds = null;
        }

        BCWrapper = null;
    }

    private void OnApplicationQuit()
    {
        if (BCWrapper != null && BCWrapper.Client != null && BCWrapper.Client.IsAuthenticated())
        {
            BCWrapper.PlayerStateService.Logout();
        }
    }

    private void Update()
    {
        // Update App info
        if (BCWrapper != null && BCWrapper.Client != null && BCWrapper.Client.IsInitialized())
        {
            AppInfoText.text = string.Format("{0} ({1}) v{2} (BC v{3})",
                                             Application.productName, BCWrapper.Client.AppId, BCWrapper.Client.AppVersion, BrainCloud.Version.GetVersion());
        }
        else
        {
            AppInfoText.text = Application.productName;
        }

        // During brainCloud or EOS calls we'll lock the Main UI so that it can't be interacted with until those processes are done
        if (LockUI)
        {
            MainContentCG.interactable = false;
            MainContentCG.blocksRaycasts = false;
        }
        else
        {
            FailureReason = string.Empty; // We clear this here because it's only used when the LockUI is true
            MainContentCG.interactable = UserControlsToggle.isOn;
            MainContentCG.blocksRaycasts = UserControlsToggle.isOn;
        }
    }

    #endregion

    #region App UI

    private void UpdateBCIds(string profileId, string anonymousId)
    {
        bool validIds = Guid.TryParseExact(profileId, "D", out Guid _) &&
                        Guid.TryParseExact(anonymousId, "D", out Guid _);

        ProfileIdText.gameObject.SetActive(validIds);
        AnonymousIdText.gameObject.SetActive(validIds);
        AuthLogInButton.gameObject.SetActive(!validIds);
        ConnectLogInButton.gameObject.SetActive(!validIds);
        RefreshButton.gameObject.SetActive(validIds);

        if (validIds)
        {
            ProfileIdText.text = $"ProfileId: {profileId}";
            AnonymousIdText.text = $"AnonymousId: {anonymousId}";
        }
        else
        {
            ProfileIdText.text = "ProfileId: ---";
            AnonymousIdText.text = "AnonymousId: ---";
        }
    }

    private void ShowCurrencyControls()
    {
        HowToControlsGO.SetActive(false);
        CurrencyControlsGO.SetActive(true);
        InventoryControlsGO.SetActive(false);
    }

    private void ShowInventoryControls()
    {
        HowToControlsGO.SetActive(false);
        CurrencyControlsGO.SetActive(false);
        InventoryControlsGO.SetActive(true);
    }

    private void ShowHowToControls()
    {
        SelectedCurrencyItem = -1;
        SelectedInventoryItem = -1;
        HowToControlsGO.SetActive(true);
        CurrencyControlsGO.SetActive(false);
        InventoryControlsGO.SetActive(false);
    }

    private void OnUserControlsToggle(bool isOn)
    {
        for (int i = 0; i < UserControlsOnOffStates.Length; i++)
        {
            UserControlsOnOffStates[i].SetActive(isOn);
        }

        MainContentCG.alpha = isOn ? 1.0f : 0.0f;
    }

    private void OnLoggingToggle(bool isOn)
    {
        for (int i = 0; i < LoggingOnOffStates.Length; i++)
        {
            LoggingOnOffStates[i].SetActive(isOn);
        }

        LoggerContentCG.alpha = isOn ? 1.0f : 0.0f;
        LoggerContentCG.interactable = isOn;
        LoggerContentCG.blocksRaycasts = isOn;
    }

    private void OnTradeToggle(bool isOn)
    {
        for (int i = 0; i < TradeOnOffStates.Length; i++)
        {
            TradeOnOffStates[i].SetActive(isOn);
        }
    }

    private void OnDropToggle(bool isOn)
    {
        for (int i = 0; i < DropOnOffStates.Length; i++)
        {
            DropOnOffStates[i].SetActive(isOn);
        }
    }

    private void OnAuthLogInButton()
    {
        StartCoroutine(UserLoginProcess(false));
    }

    private void OnConnectLogInButton()
    {
        StartCoroutine(UserLoginProcess(true));
    }

    private void OnRefreshButton()
    {
        StartCoroutine(RefreshUserProcess());
        ShowHowToControls();
    }

    private void OnConsumeButton()
    {
        StartCoroutine(ConsumeCurrencyProcess());
    }

    private void OnTradeButton()
    {
        StartCoroutine(TradeItemProcess());
    }

    private void OnDropButton()
    {
        StartCoroutine(DropItemProcess());
    }

    private void OnClosePopUpButton()
    {
        CheckoutPopUpCG.interactable = false;
        CheckoutPopUpCG.gameObject.SetActive(false);
        SelectedStoreItem = string.Empty;
        LockUI = false;
    }

    private void OnCheckoutButton()
    {
        CheckoutPopUpCG.interactable = false;
        StartCoroutine(CachePayloadContextAndPurchaseProduct());
    }

    private void OnConsumeAmountValueChanged(string value)
    {
        value = value.Trim();
        ConsumeButton.interactable = SelectedCurrencyItem >= 0 &&
                                     !value.IsEmpty() &&
                                     long.TryParse(value, out long amount) &&
                                     amount > 0 && amount <= CurrencyBalance[SelectedCurrencyItem];
    }

    private void OnTradeToValueChanged(string value)
    {
        value = value.Trim();
        TradeButton.interactable = SelectedInventoryItem >= 0 &&
                                   !value.IsEmpty() &&
                                   Guid.TryParseExact(value, "D", out Guid _);
    }

    private void OnDropAmountValueChanged(string value)
    {
        value = value.Trim();
        DropButton.interactable = SelectedInventoryItem >= 0 &&
                                  !value.IsEmpty() &&
                                  long.TryParse(value, out long amount) &&
                                  amount > 0 &&
                                  amount <= JsonParser.GetValue<int>(InventoryDetails[SelectedInventoryItem], "quantity");
    }

    private void OnCurrencyItemSelected(int index)
    {
        SelectedCurrencyItem = index;
        SelectedInventoryItem = -1;
        ConsumeAmountInput.text = string.Empty;
        SelectedCurrencyText.text = $"You have: <b>{CurrencyBalance[index]:N0} {CurrencyTypes[index]}</b>";
        ShowCurrencyControls();
    }

    private void OnInventoryItemSelected(int index)
    {
        SelectedCurrencyItem = -1;
        SelectedInventoryItem = index;
        TradeToInput.text = string.Empty;
        DropAmountInput.text = string.Empty;
        SelectedInventoryText.text = $"Selected: <b>{InventoryItems[index].GetComponentInChildren<TMP_Text>(true).text}</b>";
        ShowInventoryControls();
    }

    private void OnStoreItemSelected(int index)
    {
        LockUI = true;
        CheckoutPopUpCG.gameObject.SetActive(true);
        CheckoutPopUpCG.interactable = true;

        // Set-up popup
        SelectedStoreItem = StoreItemDefinitions[index];
        ItemNameText.text = JsonParser.GetString(SelectedStoreItem, "title");
        ItemDescriptionText.text = JsonParser.GetString(SelectedStoreItem, "description");
        decimal price = JsonParser.GetValue<int>(SelectedStoreItem, "priceData", "referencePrice") / 100m; // Prices shown are in USD by default
        ItemPriceText.text = "$" + price.ToString("N2", CultureInfo.InvariantCulture);

        // Set-up bonus
        if (SelectedStoreItem.Contains("bonusRewards") &&
            JsonParser.GetString(SelectedStoreItem, "bonusRewards") is string bonusJson &&
            !bonusJson.IsEmpty() && bonusJson != "null" && bonusJson.Contains("epicGames"))
        {
            var bonus = JsonReader.Deserialize<Dictionary<string, object>>(JsonParser.GetString(bonusJson, "epicGames"));
            List<string> bonusItems = new();

            // Get bonus inventory items
            if (bonus.ContainsKey("items") &&
                bonus["items"] is Dictionary<string, object> items &&
                items != null && items.Count > 0)
            {
                foreach (var itemObj in items.Values)
                {
                    var item = itemObj as Dictionary<string, object>;
                    bonusItems.Add($"{item["defId"]} x{item["quantity"]}");
                }
            }

            // Get bonus currencies
            if (bonus.ContainsKey("currency") &&
                bonus["currency"] is Dictionary<string, object> currency &&
                currency != null && currency.Count > 0)
            {
                foreach (var key in currency.Keys)
                {
                    bonusItems.Add($"{currency[key]:N0} {key}");
                }
            }

            bonusItems.RemoveAll(string.IsNullOrWhiteSpace);

            if (bonusItems.Count > 0)
            {
                BonusText.text = "Bonus with EOS purchase: " + string.Join(", ", bonusItems);
                BonusText.gameObject.SetActive(true);
            }
        }
        else
        {
            BonusText.gameObject.SetActive(false);
        }
    }

    #endregion

    #region brainCloud

    private IEnumerator UserLoginProcess(bool useConnectInterface)
    {
        bool checkIfShouldLogFailure()
        {
            if (FailureOccurred)
            {
                Debug.LogError(FailureReason);
                return true;
            }

            return false;
        }

        LockUI = true;

        yield return null;

        BCWrapper.Init(); // Init BrainCloudWrapper...

        yield return new WaitUntil(() => BCWrapper.Client != null && BCWrapper.Client.IsInitialized(),
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "brainCloud never initialized!");

        if (checkIfShouldLogFailure())
        {
            yield break;
        }
        else
        {
            Debug.Log("brainCloud Initialized!");
        }

        yield return null;

        yield return EOSAuthLoginProcess();

        if (checkIfShouldLogFailure())
        {
            yield break;
        }

        if (useConnectInterface)
        {
            yield return EOSConnectLoginProcess();

            if (checkIfShouldLogFailure())
            {
                yield break;
            }
        }

        if ((useConnectInterface && ConnectUserInfo.id == null && ConnectUserInfo.token.IsEmpty()) ||
            (EpicAccountInfo.id == null && EpicAccountInfo.token.IsEmpty()))
        {
            FailureReason = "Do not have the proper credentials for AuthenticateEpicGames!";
            checkIfShouldLogFailure();
        }

        // Log into brainCloud with AuthenticateEpicGames
        string response = string.Empty;
        BCWrapper.AuthenticateEpicGames(useConnectInterface ? ConnectUserInfo.id.ToString() : EpicAccountInfo.id.ToString(),
                                        useConnectInterface ? ConnectUserInfo.token : EpicAccountInfo.token,
                                        true,
            (jsonResponse, cbObject) =>
            {
                response = jsonResponse;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"AuthenticateEpicGames failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
            });

        yield return new WaitUntil(() => FailureOccurred || !response.IsEmpty(),
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "AuthenticateEpicGames timed out!");

        if (checkIfShouldLogFailure())
        {
            yield break;
        }

        UpdateBCIds(BCWrapper.GetStoredProfileId(), BCWrapper.GetStoredAnonymousId());

        // Then get user currencies, iventory, and store items
        yield return UpdateUserCurrency(response); // Send the response since it already includes currency
        yield return UpdateUserInventory();
        yield return UpdateStoreItems();

        // Finally initialize EOS store
        yield return InitializeEOSEntitlements();

        LockUI = false;
    }

    private IEnumerator RefreshUserProcess()
    {
        LockUI = true;

        yield return UpdateUserCurrency();
        yield return UpdateUserInventory();
        yield return UpdateStoreItems();

        LockUI = false;
    }

    private IEnumerator ConsumeCurrencyProcess()
    {
        LockUI = true;

        // TODO: Need to write a ConsumeCurrency script...
        string response = string.Empty;
#pragma warning disable CS0618 // Type or member is obsolete
        BCWrapper.VirtualCurrencyService.ConsumeCurrency(CurrencyTypes[SelectedCurrencyItem], Convert.ToUInt64(ConsumeAmountInput.text),
            (jsonResponse, cbObject) =>
            {
                response = jsonResponse;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"ConsumeCurrency failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
            });
#pragma warning restore CS0618 // Type or member is obsolete

        ConsumeAmountInput.text = string.Empty;
        OnConsumeAmountValueChanged(string.Empty);

        yield return new WaitUntil(() => FailureOccurred || !response.IsEmpty(),
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "ConsumeCurrency timed out!");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            LockUI = false;
            yield break;
        }

        yield return UpdateUserCurrency(response);

        OnCurrencyItemSelected(SelectedCurrencyItem); // Update the UI

        LockUI = false;
    }

    private IEnumerator TradeItemProcess()
    {
        LockUI = true;

        string response = string.Empty;
        BCWrapper.UserItemsService.GiveUserItemTo(TradeToInput.text,
            JsonParser.GetString(InventoryDetails[SelectedInventoryItem], "itemId"),
            -1, 1, true,
            (jsonResponse, cbObject) =>
            {
                response = jsonResponse;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"GiveUserItemTo failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
            });

        TradeToInput.text = string.Empty;
        OnTradeToValueChanged(string.Empty);

        yield return new WaitUntil(() => FailureOccurred || !response.IsEmpty(),
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "GiveUserItemTo timed out!");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            LockUI = false;
            yield break;
        }

        uint quantity = JsonParser.GetValue<uint>(InventoryDetails[SelectedInventoryItem], "quantity"); // Get the quantity BEFORE we update user items

        yield return UpdateUserInventory();

        if (quantity <= 1) // So if it was the last item, we would change the controls
        {
            ShowHowToControls();
        }
        else // Update the UI
        {
            OnInventoryItemSelected(SelectedInventoryItem);
        }
        
        LockUI = false;
    }

    private IEnumerator DropItemProcess()
    {
        LockUI = true;

        string response = string.Empty;
        int dropAmount = Convert.ToInt32(DropAmountInput.text);
        BCWrapper.UserItemsService.DropUserItem(JsonParser.GetString(InventoryDetails[SelectedInventoryItem], "itemId"),
            dropAmount, false,
            (jsonResponse, cbObject) =>
            {
                response = jsonResponse;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"DropUserItem failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
            });

        DropAmountInput.text = string.Empty;
        OnDropAmountValueChanged(string.Empty);

        yield return new WaitUntil(() => FailureOccurred || !response.IsEmpty(),
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "DropUserItem timed out!");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            LockUI = false;
            yield break;
        }

        uint quantity = JsonParser.GetValue<uint>(InventoryDetails[SelectedInventoryItem], "quantity"); // Get the quantity BEFORE we update user items

        yield return UpdateUserInventory();

        if (quantity <= dropAmount) // So if it was the last item, we would change the controls
        {
            ShowHowToControls();
        }
        else // Update the UI
        {
            OnInventoryItemSelected(SelectedInventoryItem);
        }

        LockUI = false;
    }

    private IEnumerator UpdateUserCurrency(string response = "")
    {
        if (response.IsEmpty())
        {
            BCWrapper.VirtualCurrencyService.GetCurrency(string.Empty,
            (jsonResponse, cbObject) =>
            {
                response = jsonResponse;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"GetUserItemsPage failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
            });

            yield return new WaitUntil(() => FailureOccurred || !response.IsEmpty(),
                                       TimeSpan.FromSeconds(RESPONSE_TIME),
                                       () => FailureReason = "GetUserItemsPage timed out!");

            if (FailureOccurred)
            {
                Debug.LogError(FailureReason);
                yield break;
            }
        }

        CurrencyTypes.Clear();
        CurrencyBalance.Clear();
        try
        {
            string currencyKey = string.Empty;
            var currencies = JsonReader.Deserialize<Dictionary<string, object>>(JsonParser.GetString(response, "data"));
            if (currencies.ContainsKey("currencyMap"))
            {
                currencyKey = "currencyMap";
            }
            else if (currencies.ContainsKey("currency"))
            {
                currencyKey = "currency";
            }
            else
            {
                throw new Exception("Currency response data doesn't include the expected key for user currency!");
            }

            currencies = currencies[currencyKey] as Dictionary<string, object>;
            foreach (var key in currencies.Keys)
            {
                string name = key.ToString();
                if (currencies[key] is Dictionary<string, object> currency &&
                    currency.ContainsKey("balance") && currency["balance"] is object balance)
                {
                    CurrencyTypes.Add(name);
                    CurrencyBalance.Add(Convert.ToInt64(balance));
                }
            }
        }
        catch (Exception e)
        {
            FailureReason = $"Failed to get user currencies! Exception: {e}";
        }

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            yield break;
        }

        // Destroy all of the currency items before we create new ones
        for (int i = 0; i < CurrencyItems.Count; i++)
        {
            CurrencyItems[i].onClick.RemoveAllListeners();
            Destroy(CurrencyItems[i].gameObject);
            CurrencyItems[i] = null;
        }

        CurrencyItems.Clear();

        yield return new WaitForSecondsRealtime(0.03333f); // Need to allow content fitters to update...

        if (CurrencyTypes.Count > 0)
        {
            for (int i = 0; i < CurrencyTypes.Count; i++)
            {
                var currencyButton = Instantiate(CurrencySelectorButton_TEMPLATE, CurrencyContent, false);
                currencyButton.GetComponentInChildren<TMP_Text>(true).text = $"{CurrencyBalance[i]:N0} {CurrencyTypes[i]}";
                currencyButton.gameObject.name = $"{name}SelectorButton";
                currencyButton.gameObject.SetActive(true);
                CurrencyItems.Add(currencyButton);
            }

            for (int i = 0; i < CurrencyItems.Count; i++)
            {
                int index = i;
                CurrencyItems[i].onClick.RemoveAllListeners();
                CurrencyItems[i].onClick.AddListener(() => OnCurrencyItemSelected(index));
            }
        }
    }

    private IEnumerator UpdateUserInventory()
    {
        string response = string.Empty;
        BCWrapper.UserItemsService.GetUserItemsPage("{}", true,
            (jsonResponse, cbObject) =>
            {
                response = jsonResponse;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"GetUserItemsPage failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
            });

        yield return new WaitUntil(() => FailureOccurred || !response.IsEmpty(),
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "GetUserItemsPage timed out!");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            yield break;
        }

        InventoryDetails.Clear();
        try
        {
            var inventory = JsonParser.GetArrayString(response, "data", "results", "items");
            if (inventory != null && inventory.Length > 0)
            {
                foreach (var item in inventory)
                {
                    string details = item;
                    InventoryDetails.Add(details);
                }
            }
        }
        catch (Exception e)
        {
            FailureReason = $"Failed to get user inventory! Exception: {e}";
        }

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            yield break;
        }

        // Destroy all of the inventory items before we create new ones
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            InventoryItems[i].onClick.RemoveAllListeners();
            Destroy(InventoryItems[i].gameObject);
            InventoryItems[i] = null;
        }

        InventoryItems.Clear();

        yield return new WaitForSecondsRealtime(0.03333f); // Need to allow content fitters to update...

        if (InventoryDetails.Count > 0)
        {
            foreach (var item in InventoryDetails)
            {
                var inventoryButton = Instantiate(InventorySelectorButton_TEMPLATE, InventoryContent, false);
                inventoryButton.GetComponentInChildren<TMP_Text>(true).text = $"{JsonParser.GetString(item, "itemDef", "name")} x{JsonParser.GetString(item, "quantity")}";
                inventoryButton.gameObject.name = $"{JsonParser.GetString(item, "defId")}SelectorButton";
                inventoryButton.gameObject.SetActive(true);
                InventoryItems.Add(inventoryButton);
            }

            for (int i = 0; i < InventoryItems.Count; i++)
            {
                int index = i;
                InventoryItems[i].onClick.RemoveAllListeners();
                InventoryItems[i].onClick.AddListener(() => OnInventoryItemSelected(index));
            }
        }
    }

    private IEnumerator UpdateStoreItems()
    {
        string response = string.Empty;
        BCWrapper.AppStoreService.GetSalesInventory(STORE_ID, string.Empty,
            (jsonResponse, cbObject) =>
            {
                response = jsonResponse;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"GetSalesInventory failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
            });

        yield return new WaitUntil(() => FailureOccurred || !response.IsEmpty(),
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "GetSalesInventory timed out!");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            yield break;
        }

        StoreItemDefinitions.Clear();
        try
        {
            var products = JsonParser.GetArrayString(response, "data", "productInventory");
            foreach (var product in products)
            {
                string definition = product;
                StoreItemDefinitions.Add(definition);
            }
        }
        catch (Exception e)
        {
            FailureReason = $"Failed to get user inventory! Exception: {e}";
        }

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            yield break;
        }

        if (CatalogItems != null) // If the CatalogItems are populated, we will add them here; otherwise EOS entitlement query will do it
        {
            yield return PopulateStoreItems();
        }
    }

    private IEnumerator CachePayloadContextAndPurchaseProduct()
    {
        string response = string.Empty;
        string iapId = JsonParser.GetString(SelectedStoreItem, "itemId");
        string payload = JsonParser.GetString(SelectedStoreItem, "payload");
        BCWrapper.AppStoreService.CachePurchasePayloadContext(STORE_ID, iapId, payload,
            (jsonResponse, cbObject) =>
            {
                response = jsonResponse;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"CachePurchasePayloadContext failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
            });

        yield return new WaitUntil(() => FailureOccurred || !response.IsEmpty(),
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "CachePurchasePayloadContext timed out!");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            OnClosePopUpButton();
            yield break;
        }

        yield return PurchaseEOSEntitlement(); // Make the actual purchase...

        if (FailureOccurred)
        {
            OnClosePopUpButton();
            yield break;
        }

        yield return VerifyPurchase();

        if (FailureOccurred)
        {
            OnClosePopUpButton();
            yield break;
        }

        yield return RefreshUserProcess();

        OnClosePopUpButton();
    }

    private IEnumerator VerifyPurchase()
    {
        string receiptJson = JsonWriter.Serialize(new Dictionary<string, object>()
        {
            ["audienceItemId"] = EntitlementTransaction.audienceItemId,
            ["entitlementIds"] = EntitlementTransaction.entitlementIds
        });

        Debug.Log($"Receipt being sent to VerifyPurchase('{STORE_ID}'):\n{receiptJson.FormatJSON()}");

        string response = string.Empty;
        BCWrapper.AppStoreService.VerifyPurchase(STORE_ID, receiptJson,
            (jsonResponse, cbObject) =>
            {
                response = jsonResponse;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"VerifyPurchase failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
            });

        yield return new WaitUntil(() => FailureOccurred || !response.IsEmpty(),
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "VerifyPurchase timed out!");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
        }

        if (response.IsEmpty())
        {
            FailureReason = "VerifyPurchase failed! Transaction could not be found or some other error occurred.";
        }

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
        }
    }

    #endregion

    #region Epic Online Services

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

    private IEnumerator WaitForEOSPlatformInterface()
    {
        LockUI = true;

        yield return new WaitUntil(() => EOSManager.Instance != null &&
                                         EOSManager.Instance.GetEOSPlatformInterface() != null,
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "EOSManager platform never initialized!");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
        }
        else
        {
            LockUI = false;
        }
    }

    private IEnumerator EOSAuthLoginProcess()
    {
        var options = new AuthLoginOptions
        {
            Credentials = BuildAuthCredentials(),
            ScopeFlags = AuthScopeFlags.NoFlags
        };

        FailureReason = string.Empty;

        Debug.Log($"Auth login as {options.Credentials.Value.Type}");

        // First we'll get the EpicAccountId
        EOSManager.Instance.GetEOSAuthInterface().Login(ref options, null, (ref AuthLoginCallbackInfo info) =>
        {
            if (!Common.IsOperationComplete(info.ResultCode))
            {
                Debug.Log($"Auth login in progress: {info.ResultCode}");
                return;
            }

            if (info.ResultCode != Result.Success)
            {
                FailureReason = $"Auth login failed! ResultCode: {info.ResultCode}";
                return;
            }

            EpicAccountInfo.id = info.LocalUserId;
        });

        yield return new WaitUntil(() => FailureOccurred || EpicAccountInfo.id != null,
                                   TimeSpan.FromSeconds(180.0f),
                                   () => FailureReason = "Auth login timed out! Failed to retrieve EpicAccountID.");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            yield break;
        }

        // Next we'll get the token for the EpicAccountId
        AuthIdToken? token = null;
        Result result = Result.NotFound;

        yield return new WaitUntil(() =>
        {
            var opts = new AuthCopyIdTokenOptions { AccountId = EpicAccountInfo.id };
            result = EOSManager.Instance.GetEOSAuthInterface().CopyIdToken(ref opts, out token);

            return result == Result.Success && token != null;
        },
        TimeSpan.FromSeconds(RESPONSE_TIME),
        () => FailureReason = $"Auth.CopyIdToken never succeeded! Last result: {result}");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            yield break;
        }

        EpicAccountInfo.token = token.Value.JsonWebToken;
    }

    private IEnumerator EOSConnectLoginProcess()
    {
        // We need to use the credentials from the user's EpicAccountId for the ConnectInterface
        if (EpicAccountInfo.id == null || EpicAccountInfo.token.IsEmpty())
        {
            FailureReason = "A failure occurred with getting the EpicAccountId credentials. Cannot log in using ConnectInterface.";
            yield break;
        }

        // With the EpicAccountId credentials we can retrieve the credentials needed for the ConnectInterface
        var options = new ConnectLoginOptions
        {
            Credentials = new ConnectCredentials
            {
                Type = ExternalCredentialType.EpicIdToken, // Currently we only support the EpicIdToken
                Token = EpicAccountInfo.token
            }
        };

        EOSManager.Instance.GetEOSConnectInterface().Login(ref options, null, (ref ConnectLoginCallbackInfo connectInfo) =>
        {
            if (connectInfo.ResultCode == Result.Success)
            {
                ConnectUserInfo.id = connectInfo.LocalUserId;
                return;
            }
            else if (connectInfo.ResultCode == Result.InvalidUser) // NOT an error: Means we need to create a product account for the user
            {
                Debug.Log("Creating new ConnectInterface user.");

                var createOptions = new CreateUserOptions { ContinuanceToken = connectInfo.ContinuanceToken };
                EOSManager.Instance.GetEOSConnectInterface().CreateUser(ref createOptions, null, (ref CreateUserCallbackInfo userInfo) =>
                {
                    if (userInfo.ResultCode != Result.Success)
                    {
                        FailureReason = $"Connect.CreateUser failed! ResultCode: {userInfo.ResultCode}";
                        return;
                    }

                    ConnectUserInfo.id = userInfo.LocalUserId;

                    Debug.Log($"New ConnectInterface user created!");
                });
                return;
            }

            FailureReason = $"Connect login failed! ResultCode: {connectInfo.ResultCode}";
        });

        yield return new WaitUntil(() => FailureOccurred || ConnectUserInfo.id != null,
                                   TimeSpan.FromSeconds(180.0f),
                                   () => FailureReason = "Connect login timed out! Failed to retrieve ProductUserId.");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            yield break;
        }

        // Get the connect user's authentication token
        ConnectIdToken? token = null;
        Result result = Result.NotFound;

        yield return new WaitUntil(() =>
        {
            var opts = new ConnectCopyIdTokenOptions { LocalUserId = ConnectUserInfo.id };
            result = EOSManager.Instance.GetEOSConnectInterface().CopyIdToken(ref opts, out token);

            return result == Result.Success && token != null;
        },
        TimeSpan.FromSeconds(RESPONSE_TIME),
        () => FailureReason = $"Connect.CopyIdToken never succeeded! Last result: {result}");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            yield break;
        }

        ConnectUserInfo.token = token.Value.JsonWebToken;
    }

    private IEnumerator InitializeEOSEntitlements()
    {
        List<string> eosEntitlements = new();
        for (int i = 0; i < StoreItemDefinitions.Count; i++)
        {
            string entitlement = JsonParser.GetString(StoreItemDefinitions[i], "priceData", "audienceItemId");
            eosEntitlements.Add(entitlement);
        }

        bool done = false;
        var options = new QueryOffersOptions { LocalUserId = EpicAccountInfo.id };
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
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "QueryOffers timed out!");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            yield break;
        }

        var countOptions = new GetOfferCountOptions { LocalUserId = EpicAccountInfo.id };
        uint count = ecom.GetOfferCount(ref countOptions);

        Debug.Log($"Found {count} offer{(count == 1 ? string.Empty : "s")}.");

        if (CatalogItems == null)
        {
            CatalogItems = new();
        }
        else
        {
            CatalogItems.Clear();
        }

        for (uint i = 0; i < count; i++)
        {
            var copyOptions = new CopyOfferByIndexOptions { LocalUserId = EpicAccountInfo.id, OfferIndex = i };
            if (ecom.CopyOfferByIndex(ref copyOptions, out CatalogOffer? offer) != Result.Success || offer == null)
            {
                continue;
            }

            CatalogOffer o = offer.Value;
            double price = o.CurrentPrice64 / Math.Pow(10, o.DecimalPoint);

            Debug.Log($"{o.TitleText} [{o.Id}] — {price:0.00} {o.CurrencyCode} (available: {o.AvailableForPurchase}, priceResult: {o.PriceResult})");

            if (o.AvailableForPurchase && o.PriceResult == Result.Success)
            {
                string id = o.Id.ToString();
                var itemCountOptions = new GetOfferItemCountOptions { LocalUserId = EpicAccountInfo.id, OfferId = o.Id };
                uint itemCount = ecom.GetOfferItemCount(ref itemCountOptions);

                for (uint j = 0; j < itemCount; j++)
                {
                    var itemOptions = new CopyOfferItemByIndexOptions
                    {
                        LocalUserId = EpicAccountInfo.id,
                        OfferId = o.Id,
                        ItemIndex = j
                    };

                    if (ecom.CopyOfferItemByIndex(ref itemOptions, out CatalogItem? item) != Result.Success || item == null)
                    {
                        continue;
                    }

                    // For any matching entitlement on brainCloud's Marketplace config, add them here
                    foreach (var entitlement in eosEntitlements)
                    {
                        if (item.Value.ItemType == EcomItemType.Consumable && entitlement == item.Value.Id)
                        {
                            CatalogItems.Add(o.Id, item.Value);
                        }
                    }
                }
            }
        }

        if (CatalogItems.IsNullOrEmpty())
        {
            Debug.Log("No purchasable offers found.");
        }

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
        }

        yield return PopulateStoreItems();
    }

    private IEnumerator PopulateStoreItems()
    {
        if (CatalogItems == null)
        {
            Debug.LogError("EOS entitlements haven't been queried! Cannot populate store items.");
            yield break;
        }

        // Destroy all of the store items before we create new ones
        for (int i = 0; i < StoreItems.Count; i++)
        {
            StoreItems[i].GetComponentInChildren<Button>(true).onClick.RemoveAllListeners();
            Destroy(StoreItems[i].gameObject);
            StoreItems[i] = null;
        }

        StoreItems.Clear();

        yield return new WaitForSecondsRealtime(0.03333f); // Need to allow content fitters to update...

        if (StoreItemDefinitions.Count > 0)
        {
            for (int i = 0; i < StoreItemDefinitions.Count; i++)
            {
                string item = StoreItemDefinitions[i];
                string audienceItemId = JsonParser.GetString(item, "priceData", "audienceItemId");
                foreach (var ci in CatalogItems.Values)
                {
                    if (ci.Id == audienceItemId)
                    {
                        // Setup button GO
                        var storeItemButton = Instantiate(StoreItem_TEMPLATE, StoreContent, false);
                        storeItemButton.text = JsonParser.GetString(item, "title");
                        storeItemButton.gameObject.name = $"{JsonParser.GetString(item, "itemId")}StoreItem";
                        storeItemButton.gameObject.SetActive(true);
                
                        // Add button functionality
                        int index = i;
                        var clickButton = storeItemButton.GetComponentInChildren<Button>(true);
                        clickButton.onClick.RemoveAllListeners();
                        clickButton.onClick.AddListener(() => OnStoreItemSelected(index));
                
                        StoreItems.Add(storeItemButton);
                        break;
                    }
                }
            }
        }
    }

    private IEnumerator PurchaseEOSEntitlement()
    {
        EntitlementTransaction.audienceItemId = string.Empty;
        EntitlementTransaction.entitlementIds = new();

        string audienceItemId = JsonParser.GetString(SelectedStoreItem, "priceData", "audienceItemId");
        string payload = JsonParser.GetString(SelectedStoreItem, "payload");
        string offerId = string.Empty;

        // Get the OfferId
        foreach (var id in CatalogItems.Keys)
        {
            if (CatalogItems[id].Id == audienceItemId)
            {
                offerId = id;
                break;
            }
        }

        // STEP 1: We need to checkout the entitlement
        string transactionId = string.Empty;
        var checkoutOptions = new CheckoutOptions
        {
            LocalUserId = EpicAccountInfo.id,
            Entries = new CheckoutEntry[] { new() { OfferId = offerId } }
        };

        EOSManager.Instance.GetEOSEcomInterface().Checkout(ref checkoutOptions, null, (ref CheckoutCallbackInfo info) =>
        {
            if (info.ResultCode != Result.Success)
            {
                FailureReason = $"Checkout failed! ResultCode: {info.ResultCode}";
            }
            else
            {
                transactionId = info.TransactionId.ToString();
            }
        });

        yield return new WaitUntil(() => FailureOccurred || !transactionId.IsEmpty(),
                                   TimeSpan.FromSeconds(300.0f),
                                   () => FailureReason = "Checkout timed out!");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            yield break;
        }

        Debug.Log($"Checkout OK! TransactionID: {transactionId}");

        const int MAX_ATTEMPTS = 10; // We'll retry several times until entitlement is retrievable on Epic's side

        // STEP 2: Get our user's entitlements
        for (int attempt = 1; attempt <= MAX_ATTEMPTS; attempt++)
        {
            bool done = false;
            var queryOptions = new QueryEntitlementsOptions
            {
                LocalUserId = EpicAccountInfo.id,
                EntitlementNames = new Utf8String[0],
                IncludeRedeemed = false
            };

            EOSManager.Instance.GetEOSEcomInterface().QueryEntitlements(ref queryOptions, null, (ref QueryEntitlementsCallbackInfo info) =>
            {
                if (info.ResultCode != Result.Success)
                {
                    FailureReason = $"QueryEntitlements failed! ResultCode: {info.ResultCode}";
                    return;
                }

                var countOptions = new GetEntitlementsCountOptions { LocalUserId = EpicAccountInfo.id };
                uint count = EOSManager.Instance.GetEOSEcomInterface().GetEntitlementsCount(ref countOptions);

                for (uint i = 0; i < count; i++)
                {
                    var copyOptions = new CopyEntitlementByIndexOptions
                    {
                        LocalUserId = EpicAccountInfo.id,
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

                    var itemOptions = new CopyItemByIdOptions { LocalUserId = EpicAccountInfo.id, ItemId = e.CatalogItemId };

                    if (EOSManager.Instance.GetEOSEcomInterface().CopyItemById(ref itemOptions, out CatalogItem? item) == Result.Success && item != null &&
                        item.Value.ItemType != EcomItemType.Consumable)
                    {
                        Debug.Log($"Skipping {e.EntitlementName} — {item.Value.ItemType}, not a consumable.");
                        continue;
                    }

                    if (e.CatalogItemId.ToString() != CatalogItems[offerId].Id)
                    {
                        continue;
                    }

                    EntitlementTransaction.audienceItemId = e.CatalogItemId.ToString();
                    EntitlementTransaction.entitlementIds.Add(e.EntitlementId.ToString());
                }

                done = true;
            });

            yield return new WaitUntil(() => FailureOccurred || done,
                                       TimeSpan.FromSeconds(RESPONSE_TIME),
                                       () => FailureReason = "QueryEntitlements timed out!");

            if (FailureOccurred)
            {
                Debug.LogError(FailureReason);
                yield break;
            }

            if (EntitlementTransaction.entitlementIds.Count > 0)
            {
                break;
            }

            Debug.Log($"No unredeemed entitlement yet (attempt {attempt}/{MAX_ATTEMPTS}) — retrying...");

            yield return new WaitForSecondsRealtime(1.0f);
        }

        if (EntitlementTransaction.entitlementIds.Count == 0)
        {
            FailureReason = "No unredeemed entitlement appeared after checkout.";
            Debug.LogError(FailureReason);
            yield break;
        }
    }

    #endregion
}
