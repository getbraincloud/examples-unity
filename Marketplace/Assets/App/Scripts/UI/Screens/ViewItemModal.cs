using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ViewItemModal : MonoBehaviour
{
    [SerializeField]
    private Image cardArt, sellCurrencyIcon;

    [SerializeField]
    private TextMeshProUGUI itemTitleText, itemDescriptionText, equipButtonText, sellAmountText;

    [SerializeField]
    private Button sellButton, equipButton, closeButton, backgroundButton, continueButton, unsubscribeButton;

    [SerializeField]
    private GameObject loadingSpinner;

    [SerializeField]
    private Sprite coinIconSprite, gemIconSprite;

    private UserItemData _data;
    private UserItemCard _itemCardRef;
    private Animator _anim;
    private CanvasGroup _cg;

    private Action _onClosed;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _cg = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        closeButton.onClick.AddListener(CloseModal);
        continueButton.onClick.AddListener(CloseModal);
        backgroundButton.onClick.AddListener(CloseModal);
        equipButton.onClick.AddListener(OnEquipButtonClicked);
        sellButton.onClick.AddListener(OnSellButtonClicked);
        unsubscribeButton.onClick.AddListener(OnUnsubscribeButtonClicked);
    }

    private void OnDisable()
    {
        closeButton.onClick.RemoveAllListeners();
        continueButton.onClick.RemoveAllListeners();
        backgroundButton.onClick.RemoveAllListeners();
        equipButton.onClick.RemoveAllListeners();
        sellButton.onClick.RemoveAllListeners();
        unsubscribeButton.onClick.RemoveAllListeners();
    }

    public void SetData(UserItemData itemData, UserItemCard itemCardRef, Action onClosed)
    {
        _data = itemData;
        _itemCardRef = itemCardRef;
        _onClosed = onClosed;

        UpdateUI();
    }

    private async Task FetchImage()
    {
        if (!string.IsNullOrEmpty(_data.imageUrl))
        {
            cardArt.sprite = await ImageCacheService.Instance.GetImageAsync(_data.imageUrl);
        }
        else if (_data.isSubscription && ImageCacheService.Instance.noAdsSprite != null)
        {
            cardArt.sprite = ImageCacheService.Instance.noAdsSprite;
        }

        if (cardArt.sprite != null)
        {
            loadingSpinner.SetActive(false);
            cardArt.gameObject.SetActive(true);
        }
    }

    private async void UpdateUI()
    {
        await FetchImage();

        itemTitleText.text = _data.itemName;
        itemDescriptionText.text = _data.description;

        sellButton.gameObject.SetActive(_data.isSellable);
        equipButton.gameObject.SetActive(_data.isEquippable);
        unsubscribeButton.gameObject.SetActive(_data.isSubscription);

        if (_data.isSellable)
        {
            sellCurrencyIcon.sprite = _data.sellCurrency == CurrencyType.Coins ? coinIconSprite : gemIconSprite;
            sellAmountText.text = _data.sellAmount.ToString();
        }

        equipButtonText.text = _data.isEquipped ? "Unequip" : "Equip";
    }

    private void CloseModal()
    {
        _anim.SetBool("fadeOut", true);
    }

    private void OnEquipButtonClicked()
    {
        _cg.interactable = false;
        InventoryService.Instance.ToggleItemEquipped(_data, !_data.isEquipped, (bool success) =>
        {
            _data.isEquipped = success ? !_data.isEquipped : _data.isEquipped;
            _itemCardRef.SetUserItemData(_data);
            if (!_data.isEquipped)
                InventoryService.Instance.OnItemEquipChange?.Invoke(_data);
            equipButtonText.text = _data.isEquipped ? "Unequip" : "Equip";
            _cg.interactable = true;
        });
    }

    private void OnSellButtonClicked()
    {
        _cg.interactable = false;

        InventoryService.Instance.SellItem(_data,
            (CurrencyType refundCurrency, int refundAmount) =>
        {
            //TODO display "item sold" message with a button to dismiss
            sellButton.gameObject.SetActive(false);
            equipButton.gameObject.SetActive(false);
            continueButton.gameObject.SetActive(true);

            itemDescriptionText.text = "Item sold.";
        });
    }

    private void OnUnsubscribeButtonClicked()
    {
        Application.OpenURL($"https://play.google.com/store/account/subscriptions?sku=no_ads&package={Application.identifier}");
    }

    public void OnFadeOutComplete()
    {
        _onClosed?.Invoke();
        Destroy(gameObject);
    }
}
