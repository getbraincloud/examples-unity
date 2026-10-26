using System;
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
    }

    private void OnDisable()
    {
        closeButton.onClick.RemoveAllListeners();
        backgroundButton.onClick.RemoveAllListeners();
        actionButton.onClick.RemoveAllListeners();
    }

    public void SetData(StoreItemData data, Action onActionButton, Action onClosed)
    {
        _data = data;
        _onActionButton = onActionButton;
        _onClosed = onClosed;
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
