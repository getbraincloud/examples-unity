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
    private const string STORE_ID = "xsolla";
    private BrainCloudWrapper BCWrapper = default;

    // Xsolla
    private string BCXsollaDefinition = string.Empty;
    private XsollaClientConfiguration XsollaConfig = default;
    private XsollaLoginClient XsollaLogin = default;
    private XsollaStoreClient XsollaStore = default;
    private XsollaStoreClientProduct[] XsollaProducts = default;
    private XsollaStoreClientPurchasedProduct XsollaPurchaseMade = default;

    // Testing
    private string FailureReason = string.Empty;
    private bool FailureOccurred => !string.IsNullOrWhiteSpace(FailureReason);

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
        DropAmountInput.onValueChanged.AddListener(OnDropAmountValueChanged);
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
        OnConsumeAmountValueChanged(string.Empty);
        OnTradeToValueChanged(string.Empty);
        OnDropAmountValueChanged(string.Empty);
        ShowHowToControls();

        LockUI = true;
        MainContentCG.interactable = false;
        MainContentCG.blocksRaycasts = false;

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
                    Debug.LogWarning($"Xsolla store failed to deinitialize: {error}");
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

        // During brainCloud or Xsolla calls we'll lock the Main UI so that it can't be interacted with until those processes are done
        if (LockUI)
        {
            MainContentCG.interactable = false;
            MainContentCG.blocksRaycasts = false;
        }
        else // Doing it like this gives a frame wait before it actually unlocks
        {
            MainContentCG.interactable = UserControlsToggle.isOn;
            MainContentCG.blocksRaycasts = UserControlsToggle.isOn;
        }
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
                                     !string.IsNullOrWhiteSpace(value) &&
                                     Convert.ToInt64(value) is long amount &&
                                     amount > 0 && amount <= CurrencyBalance[SelectedCurrencyItem];
    }

    private void OnTradeToValueChanged(string value)
    {
        value = value.Trim();
        TradeButton.interactable = SelectedInventoryItem >= 0 &&
                                   !string.IsNullOrWhiteSpace(value) &&
                                   Guid.TryParseExact(value, "D", out Guid _);
    }

    private void OnDropAmountValueChanged(string value)
    {
        value = value.Trim();
        DropButton.interactable = SelectedInventoryItem >= 0 &&
                                  !string.IsNullOrWhiteSpace(value) &&
                                  Convert.ToInt32(value) is int amount &&
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
            !string.IsNullOrWhiteSpace("bonusJson") && bonusJson != "null" && bonusJson.Contains("xsolla"))
        {
            var bonus = JsonReader.Deserialize<Dictionary<string, object>>(JsonParser.GetString(bonusJson, "xsolla"));
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
                BonusText.text = "Bonus with Xsolla purchase: " + string.Join(", ", bonusItems);
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

    private IEnumerator UserLoginProcess()
    {
        bool checkIfShouldLogFailure()
        {
            if (FailureOccurred)
            {
                Debug.LogError(FailureReason);
                //LockUI = false;
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

        UpdateBCIds(BCWrapper.GetStoredProfileId(), BCWrapper.GetStoredAnonymousId());

        // Then get user currencies, iventory, and store items
        yield return UpdateUserCurrency(response); // Send the response since it already includes currency
        yield return UpdateUserInventory();
        yield return UpdateStoreItems();

        // Get Xsolla's integration config from brainCloud
        response = string.Empty;
        BCWrapper.AppStoreService.GetStoreInfo(STORE_ID,
            (jsonResponse, cbObject) =>
            {
                response = jsonResponse;
            },
            (status, reasonCode, jsonError, cbObject) =>
            {
                FailureReason = $"AuthenticateAnonymous failed! Status: {status} | Code: {reasonCode} | jsonError:\n{jsonError}";
            });

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "AuthenticateAnonymous/Reconnect timed out!");

        if (checkIfShouldLogFailure())
        {
            yield break;
        }

        BCXsollaDefinition = JsonParser.GetString(response, "data");

        // Finally attempt to log into Xsolla & set-up the Xsolla store
        yield return XsollaLoginProcess();
        yield return InitializeXsollaStore();

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

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
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

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
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
        BCWrapper.UserItemsService.DropUserItem(JsonParser.GetString(InventoryDetails[SelectedInventoryItem], "itemId"),
            Convert.ToInt32(DropAmountInput.text), false,
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

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
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

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
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

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
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

        if (XsollaProducts != null) // If the Xsolla store's been populated, we will add them here; otherwise Xsolla store's init will do it
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

        yield return new WaitUntil(() => FailureOccurred || !string.IsNullOrWhiteSpace(response),
                                   TimeSpan.FromSeconds(RESPONSE_TIME),
                                   () => FailureReason = "CachePurchasePayloadContext timed out!");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
            OnClosePopUpButton();
            yield break;
        }

        yield return PurchaseXollaProduct(); // Make the actual purchase...

        if (FailureOccurred)
        {
            OnClosePopUpButton();
            yield break;
        }

        yield return VerifyPurchase(XsollaPurchaseMade);

        if (FailureOccurred)
        {
            OnClosePopUpButton();
            yield break;
        }

        // Consume purchases if the store config is set to not consume via the WebHook
        if (!JsonParser.GetValue<bool>(BCXsollaDefinition, "storeInfo", "consumeOnPurchase"))
        {
            XsollaStore.ConsumeProduct(XsollaPurchaseMade.sku, XsollaPurchaseMade.quantity, XsollaPurchaseMade.transactionId, err =>
            {
                if (err != null)
                {
                    Debug.LogWarning($"Could not consume {XsollaPurchaseMade.sku}: {err}");
                }
            });
        }

        yield return RefreshUserProcess();

        OnClosePopUpButton();
    }

    private IEnumerator VerifyPurchase(XsollaStoreClientPurchasedProduct product)
    {
        var receipt = new Dictionary<string, object>
        {
            ["orderId"] = product.orderId.ToString()
        };

        string receiptJson = JsonWriter.Serialize(receipt);
        Debug.Log($"Receipt being sent to VerifyPurchase('{STORE_ID}'):\n{receiptJson.FormatJSON()}");

        const int RETRY = 5;
        int count = 0;
        float retryTime = 1.0f;
        string response = string.Empty;

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
                    Debug.Log($"Couldn't find transaction ({count}/{RETRY}); Trying again...");

                    yield return new WaitForSecondsRealtime(retryTime);

                    retryTime = retryTime *= 2.0f;
                }
                else
                {
                    Debug.LogError($"Couldn't find transaction ({count}/{RETRY})!");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            FailureReason = "VerifyPurchase failed! Transaction could not be found or some other error occurred.";
        }

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
        }
    }

    #endregion

    #region Xsolla

    private IEnumerator XsollaLoginProcess()
    {
        XsollaClientSettingsAsset asset = XsollaClientSettingsAsset.Instance();
        if (asset == null || asset.settings == null)
        {
            Debug.LogError("Xsolla settings asset is missing! Configure it via the Xsolla menu in the Editor.");
            yield break;
        }

        XsollaConfig = XsollaClientConfiguration.Builder.Create()
                                                        .SetSettings(asset.settings)
                                                        .SetSandbox(JsonParser.GetValue<bool>(BCXsollaDefinition, "storeInfo", "sandbox"))
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
            Debug.LogError(FailureReason);
        }
        else
        {
            Debug.Log($"Xsolla Initialized! (sandbox: {true})");
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
                    Debug.LogWarning($"Silent login failed ({error}); falling back to the login widget.");
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
            Debug.LogError(FailureReason);
        }
        else
        {
            Debug.Log($"Xsolla Auth OK! Xsolla accessToken: {XsollaConfig.accessToken}");
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
                                                       Debug.LogWarning($"Restore reported an error: {error}");
                                                   }
                                                   else if (product != null)
                                                   {
                                                       Debug.Log($"Missed purchase: {product.sku} x{product.quantity}; need to verify or consume.");
                                                       if (!JsonParser.GetValue<bool>(BCXsollaDefinition, "storeInfo", "consumeOnPurchase"))
                                                       {
                                                           XsollaStore.ConsumeProduct(product.sku, product.quantity, product.transactionId, err =>
                                                           {
                                                               if (err != null)
                                                               {
                                                                   Debug.LogWarning($"Could not consume {product.sku}: {err}");
                                                               }
                                                           });
                                                       }
                                                       else
                                                       {
                                                           if (string.IsNullOrWhiteSpace(product.orderId.ToString()) ||
                                                               product.orderId == 0)
                                                           {
                                                               Debug.LogWarning("Product orderId is 0! Cannot consume via VerifyPurchase.");
                                                           }
                                                           else
                                                           {
                                                               StartCoroutine(VerifyPurchase(product));
                                                           }
                                                       }
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
            Debug.LogError(FailureReason);
        }

        yield return PopulateStoreItems();
    }

    private IEnumerator PopulateStoreItems()
    {
        if (XsollaStore == null)
        {
            Debug.LogError("XsollaStore hasn't been initialized yet! Cannot populate store items.");
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
                string sku = JsonParser.GetString(item, "priceData", "sku");
                foreach (var product in XsollaProducts)
                {
                    if (product.sku == sku)
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

    private IEnumerator PurchaseXollaProduct()
    {
        XsollaPurchaseMade = null;
        string productSku = JsonParser.GetString(SelectedStoreItem, "priceData", "sku");
        string payload = JsonParser.GetString(SelectedStoreItem, "payload");
        XsollaStore.PurchaseProduct(productSku, payload, (product, error) =>
        {
            if (error != null)
            {
                FailureReason = $"Xsolla purchase failed! {error}";
                return;
            }

            XsollaPurchaseMade = product;
        });

        yield return new WaitUntil(() => FailureOccurred || XsollaPurchaseMade != null,
                                   TimeSpan.FromSeconds(300.0f),
                                   () => FailureReason = "Xsolla purchase timed out!");

        if (FailureOccurred)
        {
            Debug.LogError(FailureReason);
        }
    }

    #endregion
}
