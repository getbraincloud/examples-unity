using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Xsolla.SDK.Store;

using JsonParser = BrainCloud.Common.JsonParser;
using JsonReader = BrainCloud.JsonFx.Json.JsonReader;
using JsonWriter = BrainCloud.JsonFx.Json.JsonWriter;

public class XsollaHandler : MonoBehaviour
{
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

    // App UI Items
    private int SelectedCurrencyItem = -1;
    private List<Button> CurrencyItems = new();
    private Dictionary<string, int> CurrencyBalance = new();
    private int SelectedInventoryItem = -1;
    private List<Button> InventoryItems = new();
    private List<string> InventoryDetails = new();
    private List<TMP_Text> StoreItems = new();
    private List<string> StoreItemDefinitions = new();

    // Xsolla Purchasing
    private XsollaStoreClient XsollaStore = default;
    private XsollaStoreClientProduct[] XsollaProducts = default;
    private XsollaStoreClientPurchasedProduct XsollaPurchase = default;

    // brainCloud
    private const float BC_RESPONSE_TIME = 20.0f;
    private const string BC_STORE_ID = "xsolla";

    private BrainCloudWrapper BCWrapper = default;

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

        OnUserControlsToggle(UserControlsToggle.isOn);
        OnLoggingToggle(LoggingToggle.isOn);
        OnTradeToggle(TradeToggle.isOn);
        OnDropToggle(DropToggle.isOn);
        ShowHowToControls();

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

        // Input Fields
        ConsumeAmountInput.onValueChanged.RemoveAllListeners();
        TradeToInput.onValueChanged.RemoveAllListeners();

        // Instantiated Buttons
        for (int i = 0; i < CurrencyItems.Count; i++)
        {
            CurrencyItems[i].onClick.RemoveAllListeners();
        }

        for (int i = 0; i < InventoryItems.Count; i++)
        {
            InventoryItems[i].onClick.RemoveAllListeners();
        }

        for (int i = 0; i < StoreItems.Count; i++)
        {
            StoreItems[i].GetComponentInChildren<Button>(true).onClick.RemoveAllListeners();
        }
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

    private IEnumerator UserLoginProcess()
    {
        MainContentCG.interactable = false;

        yield return null;

        BCWrapper.Init(); // Init BrainCloudWrapper...

        yield return new WaitUntil(() => BCWrapper.Client != null && BCWrapper.Client.IsInitialized(),
                                   TimeSpan.FromSeconds(BC_RESPONSE_TIME),
                                   () => FailureReason = "brainCloud never initialized!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
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
                                   TimeSpan.FromSeconds(BC_RESPONSE_TIME),
                                   () => FailureReason = "AuthenticateAnonymous/Reconnect timed out!");

        if (FailureOccurred)
        {
            LogError(FailureReason);
            yield break;
        }
        else
        {
            UpdateBCIds(BCWrapper.GetStoredProfileId(), BCWrapper.GetStoredAnonymousId());
        }

        // Get User Currency
        CurrencyBalance.Clear();
        try
        {
            var currencies = JsonReader.Deserialize<Dictionary<string, object>>(JsonParser.GetString(response, "data", "currency"));
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
        else if (CurrencyBalance.Count > 0)
        {
            foreach (var name in CurrencyBalance.Keys)
            {
                var currencyButton = Instantiate(CurrencySelectorButton_TEMPLATE, CurrencyContent, false);
                currencyButton.GetComponentInChildren<TMP_Text>(true).text = $"{CurrencyBalance[name]:N0} {name}";
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

        // Get User Inventory
        response = string.Empty;
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
                                   TimeSpan.FromSeconds(BC_RESPONSE_TIME),
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
                InventoryDetails.Add(item); //JsonParser.GetString(item, "itemDef", "name")
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
        else if (InventoryDetails.Count > 0)
        {
            foreach (var item in InventoryDetails)
            {
                var inventoryButton = Instantiate(InventorySelectorButton_TEMPLATE, InventoryContent, false);
                inventoryButton.GetComponentInChildren<TMP_Text>(true).text = JsonParser.GetString(item, "itemDef", "name");
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

        // Get Store Inventory
        response = string.Empty;
        BCWrapper.AppStoreService.GetSalesInventory(BC_STORE_ID, string.Empty,
            (jsonResponse, cbObject) =>
            {
                response = jsonResponse;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"GetSalesInventory failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
            });

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
                                   TimeSpan.FromSeconds(BC_RESPONSE_TIME),
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
                StoreItemDefinitions.Add(product);
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
        else if (StoreItemDefinitions.Count > 0)
        {
            foreach (var item in StoreItemDefinitions)
            {
                var storeItemButton = Instantiate(StoreItem_TEMPLATE, StoreContent, false);
                storeItemButton.text = JsonParser.GetString(item, "title");
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

        MainContentCG.interactable = true;
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
        MainContentCG.interactable = isOn;
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
        Log($"Store Item Definition:\n{StoreItemDefinitions[index]}");
    }

    #endregion

    #region brainCloud



    #endregion
}
