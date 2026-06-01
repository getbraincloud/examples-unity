using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Action = System.Action;

public class StoreItemCard : MonoBehaviour
{
    [SerializeField]
    private Image _cardArt,
                  _amountDisplayIcon,
                  _buttonIconLeft,
                  _buttonIconRight,
                  _upperMessageIcon,
                  _lowerMessageIcon;

    [SerializeField]
    private TextMeshProUGUI _inventoryCount,
                            _messageDisplayText,
                            _amountDisplayText,
                            _primaryPriceText,
                            _secondaryPriceText,
                            _lowerMessageText;

    [SerializeField]
    private GameObject _secondaryPriceDisplay,
                        _upperMessageDisplay,
                        _lowerMessageDisplay,
                        _amountDisplay,
                        _inventoryCountDisplay,
                        _loadingDisplay;

    [SerializeField]
    private Button _buyButton;

    [SerializeField]
    private DynamicCurrencyAnim currencyAnimPrefab;


    private StoreItemData _data;

    private Button _button;
    private CanvasGroup _cg;

    private DateTimeOffset _recoveryTime;
    private Coroutine _timerRoutine;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _cg = GetComponent<CanvasGroup>();
    }

    private bool IsItemOwned => _data != null && (_data.IsOwned ||
        (_data.defId == "no_ads" && InventoryService.Instance != null && InventoryService.Instance.NoAdsSubscriptionActive));

    private void OnEnable()
    {
        _buyButton.onClick.AddListener(OnBuyButtonClicked);
        AppManager.Instance.OnCoinsUpdated += OnCurrencyChanged;
        AppManager.Instance.OnGemsUpdated += OnCurrencyChanged;
        _button.onClick.AddListener(OnCardClicked);
        if (InventoryService.Instance != null)
            InventoryService.Instance.OnNoAdsStatusKnown += OnNoAdsStatusKnown;
    }

    private void OnCardClicked()
    {
        _cg.interactable = false;
        AppManager.Instance.SpawnViewStoreItemModal(_data, () => _buyButton.onClick.Invoke(), () =>
        {
            if (this == null) return;
            _cg.interactable = true;
        });
    }

    private void OnDisable()
    {
        _buyButton.onClick.RemoveAllListeners();
        _button.onClick.RemoveAllListeners();
        if (AppManager.Instance != null)
        {
            AppManager.Instance.OnCoinsUpdated -= OnCurrencyChanged;
            AppManager.Instance.OnGemsUpdated -= OnCurrencyChanged;
        }
        if (InventoryService.Instance != null)
            InventoryService.Instance.OnNoAdsStatusKnown -= OnNoAdsStatusKnown;
    }

    private void OnNoAdsStatusKnown(bool _) => UpdateUI();

    private void OnCurrencyChanged(int _)
    {
        if (!IsItemOwned)
            _buyButton.interactable = CanAfford();
    }

    private bool CanAfford()
    {
        if (_data == null || _data.isFree || _data.itemType == ItemType.Freebie)
            return true;

        foreach (var kvp in _data.buyPrices)
        {
            if (kvp.Key == CurrencyType.Coins && (int)kvp.Value > AppManager.Instance.userData.Coins)
                return false;
            if (kvp.Key == CurrencyType.Gems && (int)kvp.Value > AppManager.Instance.userData.Gems)
                return false;
        }
        return true;
    }

    public void SetStoreItemData(StoreItemData data)
    {
        _data = data;
        //update UI
        _recoveryTime = DateTimeOffset.FromUnixTimeMilliseconds(data.recoveryUntil);

        UpdateUI();
    }
    public async Task FetchSprites()
    {
        if (!string.IsNullOrEmpty(_data.imageUrl))
        {
            _cardArt.sprite = await ImageCacheService.Instance.GetImageAsync(_data.imageUrl);
        }
            

        _amountDisplayIcon.sprite = ImageCacheService.Instance.GetSpriteForCurrency(_data.rewardCurrency);

        return;
    }
    public async void UpdateUI()
    {
        await FetchSprites();

        _loadingDisplay.SetActive(false);
        _cardArt.gameObject.SetActive(true);

        ToggleInventoryAmountDisplay(false);
        if (IsItemOwned && _data.itemType != ItemType.Freebie)
        {
            _primaryPriceText.text = "[Owned]";
            _buttonIconLeft.gameObject.SetActive(false);
            _buttonIconRight.gameObject.SetActive(false);
            _buyButton.interactable = false;
        }
        else if(_data.isFree || _data.itemType == ItemType.Freebie)
        {
            _primaryPriceText.text = Globals.STORE_ITEM_BUTTON_TEXT_FREE;
            _buttonIconLeft.gameObject.SetActive(false);
            _buttonIconRight.gameObject.SetActive(false);
        }
        else if (_data.itemType == ItemType.Product)
        {
            _buttonIconLeft.gameObject.SetActive(false);
            _buttonIconRight.gameObject.SetActive(false);

            // Prefer the localized price string from Unity IAP; fall back to referencePrice
            string priceString = null;
            if (BrainCloudMarketplace.IsInitialized)
            {
                BCProduct[] inventory = BrainCloudMarketplace.GetInventory();
                if (inventory != null)
                {
                    foreach (BCProduct product in inventory)
                    {
                        if (product.GetProductID() == _data.defId)
                        {
                            priceString = product.GetLocalizedPriceString();
                            break;
                        }
                    }
                }
            }

            _primaryPriceText.text = priceString ?? "$" + _data.currentPrice.ToString("0.00");
            _secondaryPriceText.text = "$" + _data.oldPrice.ToString("0.00");
        }
        else
        {
            _buttonIconLeft.gameObject.SetActive(false);
            _buttonIconRight.gameObject.SetActive(true);

            Sprite buyCurrencyIcon = ImageCacheService.Instance.GetSpriteForCurrency(_data.buyPrices.Keys.FirstOrDefault());
            if (buyCurrencyIcon != null)
            {
                _buttonIconRight.sprite = buyCurrencyIcon;
            }
            else
            {
                _buttonIconRight.gameObject.SetActive(false);
            }

            string currencyTypeString = _data.buyPrices.Keys.FirstOrDefault() == CurrencyType.Cash ? "$" : string.Empty;
            _primaryPriceText.text = currencyTypeString + " " + _data.buyPrices.FirstOrDefault().Value;
            _secondaryPriceText.text = currencyTypeString + " " + _data.oldPrice.ToString();
        }

        _messageDisplayText.text = _data.message;

        if (_data.isOnCooldown)
        {
            ToggleCooldownDisplay(true);
            //display cooldown timer
            StartTimerCountdown();
        }
        else
        {
            ToggleCooldownDisplay(false);
        }

        //If the item we are buying is virtual currency, then we show an amount display
        if (_data.isCurrency)
        {
            ToggleAmountDisplay(true);

            _amountDisplayText.text = _data.itemAmount.ToString();
            
        }
        else
        {
            ToggleAmountDisplay(false);
        }

        if (_data.isOnPromotion)
        {
            ToggleSecondaryPriceDisplay(true);
        }
        else
        {
            ToggleSecondaryPriceDisplay(false);
        }

        if (_data.itemType == ItemType.Multiplier)
        {
            _upperMessageDisplay.SetActive(true);
            _messageDisplayText.text = _data.activeSeconds + " Sec";
            _upperMessageIcon.sprite = ImageCacheService.Instance.timerSprite;
        }

        if (!IsItemOwned)
            _buyButton.interactable = CanAfford();
    }

    public void ToggleCooldownDisplay(bool enable)
    {
        _lowerMessageDisplay.SetActive(enable);
        _buyButton.gameObject.SetActive(!enable);
    }

    public void ToggleAmountDisplay(bool enable)
    {
        _amountDisplay.SetActive(enable);
    }

    public void ToggleSecondaryPriceDisplay(bool enable)
    {
        _secondaryPriceDisplay.SetActive(enable);
    }

    public void ToggleInventoryAmountDisplay(bool enable)
    {
        _inventoryCountDisplay.SetActive(enable);
    }

    private void OnBuyButtonClicked()
    {
        Debug.Log("BUY ME " + _data.defId + " TYPE:" + _data.itemType);

        switch (_data.itemType)
        {
            case ItemType.Freebie:
                ActivateFreebie();
                break;
            case ItemType.Bundle:
                if (_data.autoOpen)
                {
                    Dictionary<string, string> scriptData = new();
                    scriptData.Add("defId", _data.defId);
                    string scriptDataString = JsonWriter.Serialize(scriptData);
                    BCManager.Instance.BCWrapper.ScriptService.RunScript("BuyAndOpenBundle", scriptDataString,
                        (string responseJson, object cbObject) =>
                        {
                            var data = JsonReader.Deserialize<Dictionary<string, object>>(responseJson)["data"] as Dictionary<string, object>;
                            var response = data["response"] as Dictionary<string, object>;

                            bool error = Convert.ToBoolean(response["error"]);
                            if (error)
                            {
                                string message = response["message"] as string;
                                Debug.Log("Error buying bundle: " + message);
                                return;
                            }

                            UpdateSpentCurrency(response);

                            var currencyAward = response["currencyData"] as Dictionary<string, object>;
                            var coinsInfo = currencyAward["Coins"] as Dictionary<string, object>;
                            var gemsInfo = currencyAward["Gems"] as Dictionary<string, object>;

                            int newCoinsBalance = Convert.ToInt32(coinsInfo["balance"]);
                            int newGemsBalance = Convert.ToInt32(gemsInfo["balance"]);

                            if (_data.rewardCurrency != CurrencyType.Stars)
                            {
                                AnimateCurrencyAward(() =>
                                {
                                    AppManager.Instance.UpdateCoinsAmount(newCoinsBalance);
                                    AppManager.Instance.UpdateGemsAmount(newGemsBalance);
                                });
                            }

                            if (response.ContainsKey("xpAward"))
                            {
                                var xpAward = response["xpAward"] as Dictionary<string, object>;

                                int newXpPoints = Convert.ToInt32(xpAward["adjustedXp"]);
                                int currentLevel = Convert.ToInt32(xpAward["experienceLevel"]);
                                int xpToNextLevel = Convert.ToInt32(xpAward["xpToNextLevel"]);
                                bool xpCapped = Convert.ToBoolean(xpAward["xpCapped"]);
                                string statusTitle = xpAward["statusTitle"] as string;

                                AnimateCurrencyAward(() =>
                                {
                                    AppManager.Instance.userData.XPCapped = xpCapped;
                                    AppManager.Instance.UpdateUserLevel(currentLevel, statusTitle, xpToNextLevel);
                                    AppManager.Instance.UpdateUserXP(newXpPoints);
                                });
                            }
                        });
                }
                else
                {
                    Dictionary<string, string> buyBundleData = new();
                    buyBundleData.Add("defId", _data.defId);
                    BCManager.Instance.BCWrapper.ScriptService.RunScript("BuyItem", JsonWriter.Serialize(buyBundleData),
                        (string responseJson, object cbObject) =>
                        {
                            var data = JsonReader.Deserialize<Dictionary<string, object>>(responseJson)["data"] as Dictionary<string, object>;
                            var response = data["response"] as Dictionary<string, object>;

                            bool success = Convert.ToBoolean(data["success"]);
                            if (success)
                            {
                                UpdateSpentCurrency(response);
                                InventoryService.Instance.OnItemBought?.Invoke();
                            }
                        },
                        (int status, int reason, string errorJson, object _) =>
                        {
                            Debug.LogError("Failed to buy bundle: " + errorJson);
                        });
                }
                break;
            case ItemType.Multiplier:
                Dictionary<string, string> buyMultiplierData = new();
                buyMultiplierData.Add("defId", _data.defId);
                BCManager.Instance.BCWrapper.ScriptService.RunScript("BuyItem", JsonWriter.Serialize(buyMultiplierData),
                    (string responseJson, object cbObj) =>
                    {
                        var data = JsonReader.Deserialize<Dictionary<string, object>>(responseJson)["data"] as Dictionary<string, object>;
                        var response = data["response"] as Dictionary<string, object>;

                        bool success = Convert.ToBoolean(data["success"]);
                        if (success)
                        {
                            UpdateSpentCurrency(response);

                            string gainedItemId = response.ContainsKey("gainedItemId") ? response["gainedItemId"] as string : null;
                            var gainedItemDict = response.ContainsKey("gainedItem") ? response["gainedItem"] as Dictionary<string, object> : null;
                            UserItemData newItem = InventoryService.Instance.ParseGainedItem(gainedItemId, gainedItemDict);

                            if (newItem != null)
                                InventoryService.Instance.OnSingleItemBought?.Invoke(newItem);
                            else
                                InventoryService.Instance.OnItemBought?.Invoke();
                        }
                    },
                    (int status, int reason, string errorJson, object _) =>
                    {
                        Debug.LogError("Failed to buy multiplier: " + errorJson);
                    });
                break;
            case ItemType.Product:
                if (AppManager.MockPurchasesEnabled)
                {
                    BCProduct[] mockInventory = BrainCloudMarketplace.GetMockInventory();
                    if (mockInventory == null)
                    {
                        Debug.LogWarning("[Mock IAP] No products loaded yet.");
                        break;
                    }

                    BCProduct mockProductToBuy = null;
                    foreach (BCProduct p in mockInventory)
                    {
                        if (p.itemId == _data.itemId || p.priceData?.id == _data.defId)
                        {
                            mockProductToBuy = p;
                            break;
                        }
                    }

                    if (mockProductToBuy == null)
                    {
                        Debug.LogWarning($"[Mock IAP] Product '{_data.itemId}' not found in mock inventory.");
                        break;
                    }

                    BrainCloudMarketplace.MockPurchaseProduct(mockProductToBuy, (BCProduct[] purchased) =>
                    {
                        if (purchased != null && purchased.Length > 0)
                        {
                            Debug.Log($"[Mock IAP] Purchase successful: {_data.itemId}");
                            if (mockProductToBuy.IAPProductType == UnityEngine.Purchasing.ProductType.Subscription)
                                InventoryService.Instance.RegisterMockSubscription(mockProductToBuy.itemId);
                            InventoryService.Instance.OnItemBought?.Invoke();
                        }
                        else
                        {
                            Debug.LogWarning($"[Mock IAP] Purchase failed: {_data.itemId}");
                        }
                    });
                    break;
                }

                Debug.Log($"[IAP] Buy tapped for '{_data.itemId}'. IsInitialized={BrainCloudMarketplace.IsInitialized}");

                if (!BrainCloudMarketplace.IsInitialized)
                {
                    Debug.LogWarning("[IAP] Store not ready — Unity IAP is still initializing. Check that BrainCloudMarketplace component is in the scene.");
                    break;
                }

                BCProduct[] inventory = BrainCloudMarketplace.GetInventory();
                Debug.Log($"[IAP] GetInventory returned {(inventory == null ? "null" : inventory.Length + " product(s)")}");
                if (inventory == null) break;

                BCProduct productToBuy = null;
                foreach (BCProduct p in inventory)
                {
                    if (p.GetProductID() == _data.defId)
                    {
                        productToBuy = p;
                        break;
                    }
                }

                if (productToBuy == null)
                {
                    Debug.LogWarning($"Product '{_data.defId}' not found in IAP inventory.");
                    break;
                }

                BrainCloudMarketplace.PurchaseProduct(productToBuy, (BCProduct[] purchased) =>
                {
                    if (purchased != null && purchased.Length > 0)
                    {
                        Debug.Log($"Purchase successful: {_data.itemId}");
                        InventoryService.Instance.OnItemBought?.Invoke();
                    }
                    else
                    {
                        Debug.LogWarning($"Purchase cancelled or failed: {_data.itemId}");
                    }
                });
                break;

            case ItemType.Item:
                Dictionary<string, string> buyItemData = new();
                buyItemData.Add("defId", _data.defId);
                BCManager.Instance.BCWrapper.ScriptService.RunScript("BuyItem", JsonWriter.Serialize(buyItemData),
                    (string responseJson, object cbObj) =>
                    {
                        Debug.Log("Bought item: " + responseJson);
                        var data = JsonReader.Deserialize<Dictionary<string, object>>(responseJson)["data"] as Dictionary<string, object>;
                        var response = data["response"] as Dictionary<string, object>;

                        bool success = Convert.ToBoolean(data["success"]);

                        if (success)
                        {
                            UpdateSpentCurrency(response);

                            string gainedItemId = response.ContainsKey("gainedItemId") ? response["gainedItemId"] as string : null;
                            var gainedItemDict = response.ContainsKey("gainedItem") ? response["gainedItem"] as Dictionary<string, object> : null;
                            UserItemData newItem = InventoryService.Instance.ParseGainedItem(gainedItemId, gainedItemDict);

                            if (newItem != null)
                            {
                                InventoryService.Instance.OnSingleItemBought?.Invoke(newItem);

                                if (newItem.autoEquip)
                                {
                                    InventoryService.Instance.ToggleItemEquipped(newItem, true, (bool equipped) =>
                                    {
                                        if (equipped)
                                        {
                                            newItem.isEquipped = true;
                                            InventoryService.Instance.OnItemEquipChange?.Invoke(newItem);
                                        }
                                    });
                                }
                            }
                            else
                                InventoryService.Instance.OnItemBought?.Invoke();
                        }
                    });


                break;
        }
    }

    private void UpdateSpentCurrency(Dictionary<string, object> response)
    {
        var currencySpent = response["currencySpent"] as Dictionary<string, object>;

        if (currencySpent.ContainsKey("Gems"))
        {
            //we spent Gems so we will immediately update our Gems amount
            int GemsSpent = Convert.ToInt32(currencySpent["Gems"]);
            AppManager.Instance.ConsumeGems(GemsSpent);
        }
        if (currencySpent.ContainsKey("Coins"))
        {
            int CoinsSpent = Convert.ToInt32(currencySpent["Coins"]);
            AppManager.Instance.ConsumeCoins(CoinsSpent);
        }
    }

    private void ActivateFreebie()
    {
        //this is a freebie which means our user owns this item and just needs to use it
        var payload = new Dictionary<string, object>
            {
                { "itemId", _data.itemId }
            };

        string requestPayload = JsonWriter.Serialize(payload);
        BCManager.Instance.BCWrapper.ScriptService.RunScript("UseFreebie", requestPayload,
            (string responseJson, object cb) =>
            {
                Debug.Log("Got response for using freebie: " + responseJson);
                var data = JsonReader.Deserialize<Dictionary<string, object>>(responseJson)["data"] as Dictionary<string, object>;
                var response = data["response"] as Dictionary<string, object>;

                if (response.ContainsKey("success"))
                {
                    //operation was a success
                    var updatedItemData = response["item"] as Dictionary<string, object>;
                    if (response.ContainsKey("currencyAward"))
                    {
                        Debug.Log("We were awarded currency");
                        //we were awarded currency
                        var currencyAward = response["currencyAward"] as Dictionary<string, object>;
                        var coinsInfo = currencyAward["Coins"] as Dictionary<string, object>;
                        var gemsInfo = currencyAward["Gems"] as Dictionary<string, object>;

                        int newCoinsBalance = Convert.ToInt32(coinsInfo["balance"]);
                        int newGemsBalance = Convert.ToInt32(gemsInfo["balance"]);

                        Debug.Log($"new coins: {newCoinsBalance} new gems: {newGemsBalance}");

                        //test dynamic animation
                        AnimateCurrencyAward(() =>
                        {
                            AppManager.Instance.UpdateCoinsAmount(newCoinsBalance);
                            AppManager.Instance.UpdateGemsAmount(newGemsBalance);
                        });

                    }
                    else if (response.ContainsKey("xpAward"))
                    {
                        Debug.Log("We were awarded xp");
                        //we were awarded some xp
                        var xpAward = response["xpAward"] as Dictionary<string, object>;

                        int newXpPoints = Convert.ToInt32(xpAward["adjustedXp"]);
                        int currentLevel = Convert.ToInt32(xpAward["experienceLevel"]);
                        int xpToNextLevel = Convert.ToInt32(xpAward["xpToNextLevel"]);
                        bool xpCapped = Convert.ToBoolean(xpAward["xpCapped"]);
                        string statusTitle = xpAward["statusTitle"] as string;

                        AnimateCurrencyAward(() =>
                        {
                            //TODO: Do this in a better way so that some UI gets updated when we reach the level cap
                            AppManager.Instance.userData.XPCapped = xpCapped;
                            //Should we be passing level name here? Not really a feature within the scope but will keep it optional for future
                            AppManager.Instance.UpdateUserLevel(currentLevel, statusTitle, xpToNextLevel);
                            AppManager.Instance.UpdateUserXP(newXpPoints);
                        });
                    }
                    //update this items data then update its UI
                    _data.UpdateFromJson(updatedItemData);
                    _recoveryTime = DateTimeOffset.FromUnixTimeMilliseconds(_data.recoveryUntil);
                    UpdateUI();
                }
                else
                {
                    string errorMessage = response["message"] as string;
                }
            },
        (int statusCode, int reasonCode, string errorJson, object errorCb) =>
        {

        });
    }

    private void AnimateCurrencyAward(Action onComplete)
    {
        RectTransform sourceRect = _amountDisplayIcon.gameObject.GetComponent<RectTransform>();

        AppManager.Instance.AnimateDynamicAward(sourceRect, _data.rewardCurrency, () =>
        {
            onComplete?.Invoke();
        });
    }

    private void StartTimerCountdown()
    {
        if (_timerRoutine != null)
            StopCoroutine(_timerRoutine);

        _timerRoutine = StartCoroutine(CountdownRoutine());
    }
    private IEnumerator CountdownRoutine()
    {
        while (_data.isOnCooldown)
        {
            TimeSpan remaining = _recoveryTime - DateTimeOffset.UtcNow;

            if(remaining.TotalSeconds <= 0)
            {
                _lowerMessageText.text = "00:00:00";
                _data.isOnCooldown = false;
                ToggleCooldownDisplay(false);
                yield break;
            }

            _lowerMessageText.text = $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";

            yield return new WaitForSecondsRealtime(1f);
        }
    }
}
