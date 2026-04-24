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
    private Image _cardArt, _amountDisplayIcon, _buttonIconLeft, _buttonIconRight, _lowerMessageIcon;

    [SerializeField]
    private TextMeshProUGUI _inventoryCount, _messageDisplayText, _amountDisplayText, _primaryPriceText, _secondaryPriceText, _lowerMessageText;

    [SerializeField]
    private GameObject _secondaryPriceDisplay, _lowerMessageDisplay, _amountDisplay, _inventoryCountDisplay, _loadingDisplay;

    [SerializeField]
    private Button _buyButton;

    [SerializeField]
    private DynamicCurrencyAnim currencyAnimPrefab;


    private StoreItemData _data;

    private DateTimeOffset _recoveryTime;
    private Coroutine _timerRoutine;

    private void OnEnable()
    {
        _buyButton.onClick.AddListener(OnBuyButtonClicked);   
    }

    private void OnDisable()
    {
        _buyButton.onClick.RemoveAllListeners();
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
        if(_data.isFree)
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
            _buttonIconLeft.gameObject.SetActive(true);
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

                        //we were either awarded currency or items or both
                        //we will just update our total currency levels and items
                        var currencyAward = response["currencyData"] as Dictionary<string, object>;
                        var coinsInfo = currencyAward["Coins"] as Dictionary<string, object>;
                        var gemsInfo = currencyAward["Gems"] as Dictionary<string, object>;

                        int newCoinsBalance = Convert.ToInt32(coinsInfo["balance"]);
                        int newGemsBalance = Convert.ToInt32(gemsInfo["balance"]);

                        Debug.Log($"new coins: {newCoinsBalance} new gems: {newGemsBalance}");

                        
                        //If we were awarded gems or coins
                        if(_data.rewardCurrency != CurrencyType.Stars)
                        {
                            AnimateCurrencyAward(() =>
                            {
                                AppManager.Instance.UpdateCoinsAmount(newCoinsBalance);
                                AppManager.Instance.UpdateGemsAmount(newGemsBalance);
                            });
                        }

                        //If we were awarded stars which converted to XP
                        if (response.ContainsKey("xpAward"))
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
                        //TODO: Handle displaying awarded items
                    }
                    );
                break;
            case ItemType.Multiplier:
                BCManager.Instance.BCWrapper.ScriptService.RunScript("BuyMultiplier", "{}",
                    (string responseJson, object cbObj) =>
                    {
                        Debug.Log("Bought multiplier: " + responseJson);
                        var data = JsonReader.Deserialize<Dictionary<string, object>>(responseJson)["data"] as Dictionary<string, object>;
                        var response = data["response"] as Dictionary<string, object>;

                        bool success = Convert.ToBoolean(data["success"]);

                        if (success)
                        {
                            UpdateSpentCurrency(response);

                            bool usedMultiplier = Convert.ToBoolean(response["used"]);
                            long activeUntil = Convert.ToInt64(response["activeUntil"]);

                            AppManager.Instance.ToggleCoinMultiplier(usedMultiplier, activeUntil);
                        }
                    });
                break;
            case ItemType.Product:
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

                            //update user inventory with new item - if item is equippable (get that from meta data) then auto-equip it
                            InventoryService.Instance.OnItemBought();

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
