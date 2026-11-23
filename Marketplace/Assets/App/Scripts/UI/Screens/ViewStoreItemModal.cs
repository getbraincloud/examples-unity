using BrainCloud;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ViewStoreItemModal : MonoBehaviour
{
    [SerializeField]
    private Image cardArt, buyCurrencyIcon, rewardCurrencyIcon;

    [SerializeField]
    private TextMeshProUGUI itemTitleText, itemDescriptionText, itemTypeText, priceText, rewardAmountText, actionButtonText;

    [SerializeField]
    private Button actionButton, closeButton, backgroundButton;

    [SerializeField]
    private GameObject loadingSpinner, priceDisplay, rewardDisplay;

    [Header("Redemption Codes")]
    [SerializeField]
    private GameObject redemptionTitle;

    [SerializeField]
    private TMP_InputField codeInput;

    [SerializeField]
    private TextMeshProUGUI errorMessageText;

    [SerializeField]
    private Button redeemCodeButton, continueButton;

    [SerializeField]
    private Sprite coinsRewardArt;

    private StoreItemData _data;
    private Action _onActionButton;
    private Action _onClosed;
    private Animator _anim;
    private bool _actionTriggered;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        closeButton.onClick.AddListener(CloseModal);
        backgroundButton.onClick.AddListener(CloseModal);
        actionButton.onClick.AddListener(OnActionButtonClicked);
        redeemCodeButton.onClick.AddListener(OnRedeemClicked);
        continueButton.onClick.AddListener(CloseModal);
        codeInput.onSubmit.AddListener(_ => OnRedeemClicked());
        codeInput.onValueChanged.AddListener(_ => errorMessageText.gameObject.SetActive(false));
    }

    private void OnDisable()
    {
        closeButton.onClick.RemoveAllListeners();
        backgroundButton.onClick.RemoveAllListeners();
        actionButton.onClick.RemoveAllListeners();
        redeemCodeButton.onClick.RemoveAllListeners();
        continueButton.onClick.RemoveAllListeners();
        codeInput.onSubmit.RemoveAllListeners();
        codeInput.onValueChanged.RemoveAllListeners();
    }

    public void SetData(StoreItemData data, Action onActionButton, Action onClosed)
    {
        _data = data;
        _onActionButton = onActionButton;
        _onClosed = onClosed;

        if (_data.itemType == ItemType.RedemptionCode)
            ShowEnterCodeView();
        else
            UpdateUI();
    }

    private async void UpdateUI()
    {
        if (!string.IsNullOrEmpty(_data.imageUrl))
        {
            Sprite fetchedSprite = await ImageCacheService.Instance.GetImageAsync(_data.imageUrl);
            if (fetchedSprite != null)
            {
                cardArt.sprite = fetchedSprite;
                loadingSpinner.SetActive(false);
                cardArt.gameObject.SetActive(true);
            }
        }

        itemTitleText.text = _data.itemName;
        itemDescriptionText.text = _data.message;
        itemTypeText.text = GetItemTypeLabel();

        if (_data.IsOwned)
        {
            actionButtonText.text = "[Owned]";
            actionButton.interactable = false;
        }
        else
        {
            actionButtonText.text = _data.isFree ? Globals.STORE_ITEM_BUTTON_TEXT_FREE : "Buy";
        }

        UpdatePriceDisplay();
        UpdateRewardDisplay();
    }

    private string GetItemTypeLabel()
    {
        return _data.itemType switch
        {
            ItemType.Freebie   => "Freebie",
            ItemType.Bundle    => "Bundle",
            ItemType.Product   => "Premium",
            ItemType.Item      => "Item",
            ItemType.Multiplier => "Multiplier",
            _                  => _data.itemType.ToString()
        };
    }

    private void UpdatePriceDisplay()
    {
        if (_data.isFree || _data.isOwned)
        {
            priceDisplay.SetActive(false);
            return;
        }

        priceDisplay.SetActive(true);

        if (_data.itemType == ItemType.Product)
        {
            buyCurrencyIcon.gameObject.SetActive(false);

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
            priceText.text = priceString ?? "$" + _data.currentPrice.ToString("0.00");
        }
        else
        {
            Sprite currencySprite = ImageCacheService.Instance.GetSpriteForCurrency(_data.buyPrices.Keys.FirstOrDefault());
            buyCurrencyIcon.gameObject.SetActive(currencySprite != null);
            if (currencySprite != null)
                buyCurrencyIcon.sprite = currencySprite;

            string prefix = _data.buyPrices.Keys.FirstOrDefault() == CurrencyType.Cash ? "$" : string.Empty;
            priceText.text = prefix + _data.buyPrices.FirstOrDefault().Value;
        }
    }

    private void UpdateRewardDisplay()
    {
        if (!_data.isCurrency)
        {
            rewardDisplay.SetActive(false);
            return;
        }

        rewardDisplay.SetActive(true);
        rewardCurrencyIcon.sprite = ImageCacheService.Instance.GetSpriteForCurrency(_data.rewardCurrency);
        rewardAmountText.text = _data.itemAmount.ToString();
    }

    #region Redemption Codes

    private async void ShowEnterCodeView()
    {
        itemTitleText.gameObject.SetActive(false);
        itemTypeText.gameObject.SetActive(false);
        priceDisplay.SetActive(false);
        rewardDisplay.SetActive(false);
        actionButton.gameObject.SetActive(false);
        errorMessageText.gameObject.SetActive(false);

        itemDescriptionText.text = "Enter Code";
        codeInput.gameObject.SetActive(true);
        redeemCodeButton.gameObject.SetActive(true);

        SetArt(await ImageCacheService.Instance.GetImageAsync(_data.imageUrl));
    }

    private void SetArt(Sprite sprite)
    {
        if (this == null || sprite == null) return;
        cardArt.sprite = sprite;
        loadingSpinner.SetActive(false);
        cardArt.gameObject.SetActive(true);
    }

    private void OnRedeemClicked()
    {
        string code = codeInput.text.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code) || !redeemCodeButton.interactable) return;

        SetRedeemInteractable(false);

        // No codeType: short promo codes resolve to their shared campaign. The campaign's
        // fulfillment script grants the reward and reports it back in customRedemptionInfo.
        BCManager.Instance.BCWrapper.RedemptionCodeService.RedeemCode(code, null, null,
            (string responseJson, object _) =>
            {
                if (this == null) return;

                var data = JsonReader.Deserialize<Dictionary<string, object>>(responseJson)["data"] as Dictionary<string, object>;
                var reward = data["customRedemptionInfo"] as Dictionary<string, object>;

                if (reward != null && reward["rewardType"] as string == "currency")
                    OnCoinsRedeemed(reward);
                else if (reward != null && reward["rewardType"] as string == "item")
                    OnItemRedeemed(reward);
                else
                {
                    Debug.LogWarning("RedeemCode returned no usable reward: " + responseJson);
                    ShowCodeError("Invalid Code");
                }
            },
            (int status, int reason, string errorJson, object _) =>
            {
                Debug.LogWarning("RedeemCode failed: " + errorJson);
                if (this == null) return;

                bool alreadyRedeemed = reason == ReasonCodes.REDEMPTION_CODE_REDEEMED ||
                                       reason == ReasonCodes.REDEMPTION_CODE_REDEEMED_BY_SELF ||
                                       reason == ReasonCodes.REDEMPTION_CODE_REDEEMED_BY_OTHER;
                ShowCodeError(alreadyRedeemed ? "Already Redeemed" : "Invalid Code");
            });
    }

    private void OnCoinsRedeemed(Dictionary<string, object> response)
    {
        int amount = Convert.ToInt32(response["amount"]);
        AppManager.Instance.UpdateCoinsAmount(Convert.ToInt32(response["balance"]));

        ShowRewardView($"{amount:N0} Coins");
        rewardDisplay.SetActive(true);
        rewardCurrencyIcon.sprite = ImageCacheService.Instance.GetSpriteForCurrency(CurrencyType.Coins);
        rewardAmountText.text = amount.ToString("N0");
        SetArt(coinsRewardArt);
    }

    private async void OnItemRedeemed(Dictionary<string, object> response)
    {
        UserItemData item = InventoryService.Instance.ParseGainedItem(
            response["gainedItemId"] as string, response["gainedItem"] as Dictionary<string, object>);

        if (item == null)
        {
            InventoryService.Instance.OnItemBought?.Invoke();
            ShowRewardView("New Item");
            return;
        }

        InventoryService.Instance.OnSingleItemBought?.Invoke(item);
        ShowRewardView(item.itemName);

        // Redeemed equippables (e.g. Golden Shirt) go straight onto the avatar
        if (item.isEquippable)
        {
            InventoryService.Instance.ToggleItemEquipped(item, true, (bool equipped) =>
            {
                if (!equipped) return;
                item.isEquipped = true;
                InventoryService.Instance.OnItemEquipChange?.Invoke(item);
            });
        }

        SetArt(await ImageCacheService.Instance.GetImageAsync(item.imageUrl));
    }

    private void ShowRewardView(string title)
    {
        redemptionTitle.SetActive(true);
        codeInput.gameObject.SetActive(false);
        errorMessageText.gameObject.SetActive(false);
        redeemCodeButton.gameObject.SetActive(false);
        itemDescriptionText.gameObject.SetActive(false);

        itemTitleText.text = title;
        itemTitleText.gameObject.SetActive(true);
        continueButton.gameObject.SetActive(true);
    }

    private void ShowCodeError(string message)
    {
        errorMessageText.text = message;
        errorMessageText.gameObject.SetActive(true);
        SetRedeemInteractable(true);
    }

    private void SetRedeemInteractable(bool interactable)
    {
        redeemCodeButton.interactable = interactable;
        codeInput.interactable = interactable;
    }

    #endregion

    private void CloseModal()
    {
        _anim.SetBool("fadeOut", true);
    }

    private void OnActionButtonClicked()
    {
        _actionTriggered = true;
        CloseModal();
    }

    public void OnFadeOutComplete()
    {
        _onClosed?.Invoke();
        if (_actionTriggered)
            _onActionButton?.Invoke();
        Destroy(gameObject);
    }
}
