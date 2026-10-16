using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Xsolla.SDK.Common;
using Xsolla.SDK.Login;
using Xsolla.SDK.Store;

using JsonParser = BrainCloud.Common.JsonParser;
using JsonReader = BrainCloud.JsonFx.Json.JsonReader;
using JsonWriter = BrainCloud.JsonFx.Json.JsonWriter;

public class XsollaHandler : MonoBehaviour
{
    #region Serialized Fields

    [Header("App")]
    [SerializeField] private TMP_Text AppInfoText;

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
    private Dictionary<string, int> CurrencyBalance = new();
    private int SelectedInventoryItem = -1;
    private List<Button> InventoryItems = new();
    private List<string> InventoryDetails = new();
    private List<TMP_Text> StoreItems = new();
    private List<string> StoreItemDefinitions = new();
    private string SelectedStoreItem = string.Empty;

    // brainCloud
    private const float RESPONSE_TIME = 20.0f;
    private const string STORE_ID = "xsolla";
    private BrainCloudWrapper BCWrapper = default;

    // Xsolla
    private XsollaClientConfiguration XsollaConfig = default;
    private XsollaLoginClient XsollaLogin = default;
    private XsollaStoreClient XsollaStore = default;
    private XsollaStoreClientProduct[] XsollaProducts = default;
    private XsollaStoreClientPurchasedProduct XsollaPurchase = default;

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
        RefreshButton.onClick.AddListener(OnRefreshButton);
        ConsumeButton.onClick.AddListener(OnConsumeButton);
        TradeButton.onClick.AddListener(OnTradeButton);
        DropButton.onClick.AddListener(OnDropButton);
        ClosePopUpButton.onClick.AddListener(OnClosePopUpButton);
        CheckoutButton.onClick.AddListener(OnCheckoutButton);

        // Input Fields
        ConsumeAmountInput.onValueChanged.AddListener(OnConsumeAmountValueChanged);
        TradeToInput.onValueChanged.AddListener(OnTradeToValueChanged);

        // Instantiated Buttons
        for (int i = 0; i < CurrencyItems.Count; i++)
        {
            CurrencyItems[i].onClick.RemoveAllListeners();
            CurrencyItems[i].onClick.AddListener(() => OnCurrencyItemSelected(i));
        }

        // Instantiated Buttons
        for (int i = 0; i < CurrencyItems.Count; i++)
        {
            CurrencyItems[i].onClick.RemoveAllListeners();
            CurrencyItems[i].onClick.AddListener(() => OnCurrencyItemSelected(i));
        }

        // Instantiated Buttons
        for (int i = 0; i < CurrencyItems.Count; i++)
        {
            int index = i;
            CurrencyItems[i].onClick.AddListener(() => OnCurrencyItemSelected(index));
        }

        for (int i = 0; i < InventoryItems.Count; i++)
        {
            int index = i;
            InventoryItems[i].onClick.AddListener(() => OnInventoryItemSelected(index));
        }

        for (int i = 0; i < StoreItems.Count; i++)
        {
            int index = i;
            StoreItems[i].GetComponentInChildren<Button>(true).onClick.AddListener(() => OnStoreItemSelected(index));
        }
    }

    private void Start()
    {
        BCWrapper = GetComponent<BrainCloudWrapper>();

        UpdateBCIds("---", "---");

        OnClosePopUpButton();
        OnUserControlsToggle(UserControlsToggle.isOn);
        OnLoggingToggle(LoggingToggle.isOn);
        OnTradeToggle(TradeToggle.isOn);
        OnDropToggle(DropToggle.isOn);
        ShowHowToControls();
        MainContentCG.interactable = false;

        StartCoroutine(UserLoginProcess());
    }

    private void OnDisable()
    {
        // Toggles
        UserControlsToggle.onValueChanged.RemoveAllListeners();
        LoggingToggle.onValueChanged.RemoveAllListeners();
        TradeToggle.onValueChanged.RemoveAllListeners();
        DropToggle.onValueChanged.RemoveAllListeners();

        // Buttons
        RefreshButton.onClick.RemoveAllListeners();
        ConsumeButton.onClick.RemoveAllListeners();
        TradeButton.onClick.RemoveAllListeners();
        DropButton.onClick.RemoveAllListeners();
        ClosePopUpButton.onClick.RemoveAllListeners();
        CheckoutButton.onClick.RemoveAllListeners();

        // Input Fields
        ConsumeAmountInput.onValueChanged.RemoveAllListeners();
        TradeToInput.onValueChanged.RemoveAllListeners();

        // Instantiated Buttons
        if (CurrencyItems != null)
        {
            for (int i = 0; i < CurrencyItems.Count; i++)
            {
                if (CurrencyItems[i] != null)
                {
                    CurrencyItems[i].onClick.RemoveAllListeners();
                }
            }
        }

        if (InventoryItems != null)
        {
            for (int i = 0; i < InventoryItems.Count; i++)
            {
                if (InventoryItems[i] != null)
                {
                    InventoryItems[i].onClick.RemoveAllListeners();
                }
            }
        }

        if (StoreItems != null)
        {
            for (int i = 0; i < StoreItems.Count; i++)
            {
                if (StoreItems[i] != null)
                {
                    StoreItems[i].GetComponentInChildren<Button>(true).onClick.RemoveAllListeners();
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (CurrencyItems != null)
        {
            CurrencyItems.Clear();
            CurrencyItems = null;
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

        XsollaStore = null;
        BCWrapper = null;
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

        if (BCWrapper != null && BCWrapper.Client != null && BCWrapper.Client.IsAuthenticated())
        {
            BCWrapper.PlayerStateService.Logout();
        }
    }

    private void Update()
    {
        if (BCWrapper != null && BCWrapper.Client != null && BCWrapper.Client.IsInitialized())
        {
            AppInfoText.text = string.Format("{0} ({1}) v{2} (BC v{3})",
                                             Application.productName, BCWrapper.Client.AppId, BCWrapper.Client.AppVersion, BrainCloud.Version.GetVersion());
        }
        else
        {
            AppInfoText.text = Application.productName;
        }
    }

    #endregion

    #region App Control

    private string FailureReason = string.Empty;

    private bool FailureOccurred => !string.IsNullOrWhiteSpace(FailureReason);

    private static void Log(object message)
    {
        Debug.Log(message);
    }

    private static void LogWarning(object message)
    {
        Debug.LogWarning(message);
    }

    private static void LogError(object message)
    {
        Debug.LogError(message);
    }

    private static void Assert(bool condition, object message)
    {
        Debug.Assert(condition, message);
    }

    #endregion

    #region App UI

    private void UpdateBCIds(string profileId, string anonymousId)
    {
        ProfileIdText.text = $"ProfileId: {profileId}";
        AnonymousIdText.text = $"AnonymousId: {anonymousId}";
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
        MainContentCG.interactable = isOn && !LockUI;
        MainContentCG.blocksRaycasts = isOn;
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

    private void OnRefreshButton()
    {
        StartCoroutine(RefreshUserProcess());
        ShowHowToControls();
    }

    private void OnConsumeButton()
    {

    }

    private void OnTradeButton()
    {

    }

    private void OnDropButton()
    {

    }

    private void OnClosePopUpButton()
    {
        CheckoutPopUpCG.interactable = false;
        CheckoutPopUpCG.gameObject.SetActive(false);
        SelectedStoreItem = string.Empty;
        MainContentCG.interactable = UserControlsToggle.isOn && !LockUI;
        MainContentCG.blocksRaycasts = true;
    }

    private void OnCheckoutButton()
    {
        CheckoutPopUpCG.interactable = false;
        StartCoroutine(CachePayloadContextAndPurchaseProduct());
    }

    private void OnConsumeAmountValueChanged(string value)
    {

    }

    private void OnTradeToValueChanged(string value)
    {

    }

    private void OnCurrencyItemSelected(int index)
    {
        if (index == SelectedCurrencyItem)
        {
            return;
        }

        SelectedCurrencyItem = index;
        SelectedInventoryItem = -1;
        ConsumeAmountInput.text = string.Empty;
        SelectedCurrencyText.text = $"You have: <b>{CurrencyItems[index].GetComponentInChildren<TMP_Text>(true).text}</b>";

        ShowCurrencyControls();
    }

    private void OnInventoryItemSelected(int index)
    {
        if (index == SelectedInventoryItem)
        {
            return;
        }

        SelectedCurrencyItem = -1;
        SelectedInventoryItem = index;
        TradeToInput.text = string.Empty;
        SelectedInventoryText.text = $"Selected: <b>{InventoryItems[index].GetComponentInChildren<TMP_Text>(true).text}</b>";

        ShowInventoryControls();
    }

    private void OnStoreItemSelected(int index)
    {
        CheckoutPopUpCG.gameObject.SetActive(true);
        CheckoutPopUpCG.interactable = true;
        MainContentCG.interactable = false;
        MainContentCG.blocksRaycasts = false;

        // Set-up popup...
        SelectedStoreItem = StoreItemDefinitions[index];
        ItemNameText.text = JsonParser.GetString(SelectedStoreItem, "title");
        ItemDescriptionText.text = JsonParser.GetString(SelectedStoreItem, "description");
        decimal price = JsonParser.GetValue<int>(SelectedStoreItem, "priceData", "referencePrice") / 100m;
        ItemPriceText.text = "$" + price.ToString("N2", CultureInfo.InvariantCulture);

        // Set-up bonus...
        if (SelectedStoreItem.Contains("bonusRewards") &&
            JsonParser.GetString(SelectedStoreItem, "bonusRewards") is string bonusJson &&
            !string.IsNullOrWhiteSpace("bonusJson") && bonusJson != "null")
        {
            var bonus = JsonReader.Deserialize<Dictionary<string, object>>(JsonParser.GetString(bonusJson, "xsolla"));
            List<string> items = new();

            BonusText.text = "Bonus with purchase: " + JsonWriter.Serialize(bonus);
        }
        else
        {
            BonusText.gameObject.SetActive(false);
        }
    }

    #endregion

    #region brainCloud

    private IEnumerator UserLoginProcess()
    {
        bool checkIfShouldLogFailure()
        {
            if (FailureOccurred)
            {
                LogError(FailureReason);
                LockUI = false;
                MainContentCG.interactable = UserControlsToggle.isOn;
                return true;
            }

            return false;
        }

        MainContentCG.interactable = false;
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
            Log("brainCloud Initialized!");
        }

        yield return null;

        // Log into brainCloud as Anonymous user
        string response = string.Empty;
        if (BCWrapper.CanReconnect())
        {
            BCWrapper.Reconnect(
                (jsonResponse, cbObject) =>
                {
                    response = jsonResponse;
                },
                (status, reasonCode, jsonError, cbObject) =>
                {
                    FailureReason = $"Reconnect failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
                });
        }
        else
        {
            BCWrapper.AuthenticateAnonymous(
                (jsonResponse, cbObject) =>
                {
                    response = jsonResponse;
                },
                (status, reasonCode, jsonError, cbObject) =>
                {
                    FailureReason = $"AuthenticateAnonymous failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
                });
        }

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "AuthenticateAnonymous/Reconnect timed out!");

        if (checkIfShouldLogFailure())
        {
            yield break;
        }
        else
        {
            UpdateBCIds(BCWrapper.GetStoredProfileId(), BCWrapper.GetStoredAnonymousId());
        }

        // Then get user currencies, iventory, and store items
        yield return UpdateUserCurrency(response); // Send the response since it already includes currency
        yield return UpdateUserInventory();
        yield return UpdateStoreItems();

        // Now attempt to log into Xsolla & set-up the Xsolla store
        yield return XsollaLoginProcess();
        yield return InitializeXsollaStore();

        LockUI = false;
        MainContentCG.interactable = UserControlsToggle.isOn;
    }

    private IEnumerator RefreshUserProcess()
    {
        MainContentCG.interactable = false;
        LockUI = true;

        yield return UpdateUserCurrency();
        yield return UpdateUserInventory();
        yield return UpdateStoreItems();

        LockUI = false;
        MainContentCG.interactable = UserControlsToggle.isOn;
    }

    private IEnumerator UpdateUserCurrency(string response = "")
    {
        if (string.IsNullOrWhiteSpace(response))
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

            yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
                                       TimeSpan.FromSeconds(RESPONSE_TIME),
                                       () => FailureReason = "GetUserItemsPage timed out!");

            if (FailureOccurred)
            {
                LogError(FailureReason);
                yield break;
            }
        }

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
                    CurrencyBalance.Add(name, Convert.ToInt32(balance));
                }
            }
        }
        catch (Exception e)
        {
            FailureReason = $"Failed to get user currencies! Exception: {e}";
        }

        if (FailureOccurred)
        {
            LogError(FailureReason);
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

        yield return new WaitForFixedUpdate(); // Need to allow content fitters to update...

        if (CurrencyBalance.Count > 0)
        {
            foreach (var name in CurrencyBalance.Keys)
            {
                var currencyButton = Instantiate(CurrencySelectorButton_TEMPLATE, CurrencyContent, false);
                currencyButton.GetComponentInChildren<TMP_Text>(true).text = $"{CurrencyBalance[name]:N0} {name}";
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

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "GetUserItemsPage timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
            yield break;
        }

        InventoryDetails.Clear();
        try
        {
            var inventory = JsonParser.GetArrayString(response, "data", "results", "items");
            foreach (var item in inventory)
            {
                string details = item;
                InventoryDetails.Add(details);
            }
        }
        catch (Exception e)
        {
            FailureReason = $"Failed to get user inventory! Exception: {e}";
        }

        if (FailureOccurred)
        {
            LogError(FailureReason);
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

        yield return new WaitForFixedUpdate(); // Need to allow content fitters to update...

        if (InventoryDetails.Count > 0)
        {
            foreach (var item in InventoryDetails)
            {
                var inventoryButton = Instantiate(InventorySelectorButton_TEMPLATE, InventoryContent, false);
                inventoryButton.GetComponentInChildren<TMP_Text>(true).text = JsonParser.GetString(item, "itemDef", "name");
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

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "GetSalesInventory timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
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
            LogError(FailureReason);
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

        yield return new WaitForFixedUpdate(); // Need to allow content fitters to update...

        if (StoreItemDefinitions.Count > 0)
        {
            foreach (var item in StoreItemDefinitions)
            {
                var storeItemButton = Instantiate(StoreItem_TEMPLATE, StoreContent, false);
                storeItemButton.text = JsonParser.GetString(item, "title");
                storeItemButton.gameObject.name = $"{JsonParser.GetString(item, "itemId")}StoreItem";
                storeItemButton.gameObject.SetActive(true);
                StoreItems.Add(storeItemButton);
            }

            for (int i = 0; i < StoreItems.Count; i++)
            {
                int index = i;
                var storeButton = StoreItems[i].GetComponentInChildren<Button>(true);
                storeButton.onClick.RemoveAllListeners();
                storeButton.onClick.AddListener(() => OnStoreItemSelected(index));
            }
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

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "CachePurchasePayloadContext timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
            OnClosePopUpButton();
            yield break;
        }

        yield return PurchaseXollaProduct(); // Make the actual purchase...

        if (FailureOccurred)
        {
            OnClosePopUpButton();
            yield break;
        }

        var receipt = new Dictionary<string, object>
        {
            ["orderId"] = XsollaPurchase.orderId.ToString()
        };

        string receiptJson = JsonWriter.Serialize(receipt);
        Log($"Receipt being sent to VerifyPurchase('{STORE_ID}'):\n{receiptJson.FormatJSON()}");

        const int RETRY = 5;
        int count = 0;
        float retryTime = 1.0f;
        response = string.Empty;

        // The WebHook for the purchase can take time to process on brainCloud
        // so we will retry a couple times until it comes back successfully
        while (++count <= RETRY)
        {
            BCWrapper.AppStoreService.VerifyPurchase(STORE_ID, receiptJson,
                (jsonResponse, cbObject) =>
                {
                    response = jsonResponse;
                },
                (status, reasonCode, jsonError, cbObject) =>
                {
                    FailureReason = $"VerifyPurchase failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
                });

            yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
                                       TimeSpan.FromSeconds(RESPONSE_TIME),
                                       () => FailureReason = "VerifyPurchase timed out!");

            if (FailureOccurred)
            {
                break;
            }

            Dictionary<string, object>[] data;
            try
            {
                data = ((JsonReader.Deserialize<Dictionary<string, object>>(response)
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

                response = string.Empty;

                if (count < RETRY)
                {
                    Log($"Couldn't find transaction. Trying again... ({count}/{RETRY})");

                    yield return new WaitForSecondsRealtime(retryTime);

                    retryTime = retryTime *= 2.0f;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            FailureReason = "VerifyPurchase failed! Transaction could not be found or some other error occurred.";
        }

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }

        yield return RefreshUserProcess();

        OnClosePopUpButton();
    }

    #endregion

    #region Xsolla

    private IEnumerator XsollaLoginProcess()
    {
        XsollaClientSettingsAsset asset = XsollaClientSettingsAsset.Instance();
        if (asset == null || asset.settings == null)
        {
            LogError("Xsolla settings asset is missing! Configure it via the Xsolla menu in the Editor.");
            yield break;
        }

        XsollaConfig = XsollaClientConfiguration.Builder.Create()
                                                        .SetSettings(asset.settings)
                                                        .SetSandbox(true)
                                                        .SetLogLevel(XsollaLogLevel.Debug)
                                                        .Build();

        XsollaLogin = XsollaLoginClient.Builder.Create()
                                               .SetConfiguration(XsollaConfig)
                                               .Build();

        yield return new WaitUntil(() => XsollaLogin.GetConfiguration() != null,
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "Xsolla configuration never resolved!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            Log($"Xsolla Initialized! (sandbox: {true})");
        }

        bool silentAttempted = false;
        void onLogin(XsollaLoginToken token, XsollaLoginClientError error)
        {
            if (error != null)
            {
                // A failed silent login just means there's no cached session; fall back to the widget
                if (!silentAttempted)
                {
                    silentAttempted = true;
                    LogWarning($"Silent login failed ({error}); falling back to the login widget.");
                    XsollaLogin.Login(onLogin);
                    return;
                }

                FailureReason = $"Xsolla login failed! {error}";
                return;
            }

            XsollaConfig.accessToken = token.accessToken;
            XsollaConfig.userId = BCWrapper.Client.ProfileId;
        }

        XsollaLogin.LoginSilently(onLogin);

        yield return new WaitUntil(() => FailureOccurred || XsollaConfig.userId == BCWrapper.Client.ProfileId,
                                   TimeSpan.FromSeconds(180.0f),
                                   () => FailureReason = "Auth login timed out! Failed to retrieve XsollaAuthID.");

        if (FailureOccurred)
        {
            LogError(FailureReason);
        }
        else
        {
            Log($"Xsolla Auth OK! Xsolla accessToken: {XsollaConfig.accessToken}");
        }
    }

    private IEnumerator InitializeXsollaStore()
    {
        bool initialized = false;

        List<string> xsollaProducts = new();

        for (int i = 0; i < StoreItemDefinitions.Count; i++)
        {
            string product = JsonParser.GetString(StoreItemDefinitions[i], "priceData", "sku");
            xsollaProducts.Add(product);
        }

        XsollaStore = XsollaStoreClient.Builder.Create()
                                               .SetConfiguration(XsollaConfig)
                                               .AddProducts(xsollaProducts.ToArray())
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
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
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
    }

    private IEnumerator PurchaseXollaProduct()
    {
        string productSku = JsonParser.GetString(SelectedStoreItem, "priceData", "sku");
        string payload = JsonParser.GetString(SelectedStoreItem, "payload");
        XsollaStore.PurchaseProduct(productSku, payload, (product, error) =>
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
    }

    #endregion
}
