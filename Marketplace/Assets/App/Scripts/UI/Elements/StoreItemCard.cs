using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;
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
            //hide left button icon
            _buttonIconLeft.gameObject.SetActive(false);
            _buttonIconRight.gameObject.SetActive(false);
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

            string currencyTypeString = string.Empty;
            if(_data.buyPrices.Keys.FirstOrDefault() == CurrencyType.Cash)
            {
                currencyTypeString = "$";
            }
            _primaryPriceText.text = currencyTypeString + " " + _data.buyPrices.FirstOrDefault();
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
        Debug.Log("BUY ME " + _data.defId);
        if(_data.category == "Freebies")
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
