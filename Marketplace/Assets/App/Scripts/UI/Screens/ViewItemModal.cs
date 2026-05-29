using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
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
    private Button sellButton, equipButton, closeButton, backgroundButton, continueButton, unsubscribeButton, openBundleButton;

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
        openBundleButton.onClick.AddListener(OnOpenBundleButtonClicked);
    }

    private void OnDisable()
    {
        closeButton.onClick.RemoveAllListeners();
        continueButton.onClick.RemoveAllListeners();
        backgroundButton.onClick.RemoveAllListeners();
        equipButton.onClick.RemoveAllListeners();
        sellButton.onClick.RemoveAllListeners();
        unsubscribeButton.onClick.RemoveAllListeners();
        openBundleButton.onClick.RemoveAllListeners();
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
        
        unsubscribeButton.gameObject.SetActive(_data.isSubscription);
        openBundleButton.gameObject.SetActive(_data.isBundle);

        if (_data.isSellable)
        {
            sellCurrencyIcon.sprite = _data.sellCurrency == CurrencyType.Coins ? coinIconSprite : gemIconSprite;
            sellAmountText.text = _data.sellAmount.ToString();
        }

        if(_data.defId == "gold_frame")
        {
            equipButton.gameObject.SetActive(true);
            equipButtonText.text = _data.isEquipped ? "Unequip" : "Equip";
        }
        else if (_data.isEquippable)
        {
            equipButton.gameObject.SetActive(!_data.isEquipped);
            equipButtonText.text = "Equip";
        }
        if (_data.isEquipped)
        {
            itemDescriptionText.text += " [Equipped]";
        }
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

            equipButton.gameObject.SetActive(true);
            equipButtonText.text = _data.isEquipped ? "Unequip" : "Equip";

            if (_data.defId != "gold_frame")
            {
                equipButton.gameObject.SetActive(false);
            }

            if (_data.isEquipped)
            {
                itemDescriptionText.text += " [Equipped]";
            }
            else
            {
                itemDescriptionText.text = _data.description;
            }

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

    private void OnOpenBundleButtonClicked()
    {
        _cg.interactable = false;

        var scriptData = new Dictionary<string, string> { { "itemId", _data.itemId } };
        BCManager.Instance.BCWrapper.ScriptService.RunScript("OpenBundle", BrainCloud.JsonFx.Json.JsonWriter.Serialize(scriptData),
            (string responseJson, object cbObject) =>
            {
                var root = JsonReader.Deserialize<Dictionary<string, object>>(responseJson)["data"] as Dictionary<string, object>;
                var response = root["response"] as Dictionary<string, object>;

                bool error = Convert.ToBoolean(response["error"]);
                if (error)
                {
                    Debug.LogError("Failed to open bundle: " + response["message"]);
                    _cg.interactable = true;
                    return;
                }

                if (response.ContainsKey("currencyData"))
                {
                    var currencyData = response["currencyData"] as Dictionary<string, object>;
                    if (currencyData.ContainsKey("Coins"))
                    {
                        var coinsInfo = currencyData["Coins"] as Dictionary<string, object>;
                        AppManager.Instance.UpdateCoinsAmount(Convert.ToInt32(coinsInfo["balance"]));
                    }
                    if (currencyData.ContainsKey("Gems"))
                    {
                        var gemsInfo = currencyData["Gems"] as Dictionary<string, object>;
                        AppManager.Instance.UpdateGemsAmount(Convert.ToInt32(gemsInfo["balance"]));
                    }
                }

                if (response.ContainsKey("xpAward"))
                {
                    var xpAward = response["xpAward"] as Dictionary<string, object>;
                    int newXpPoints = Convert.ToInt32(xpAward["adjustedXp"]);
                    int currentLevel = Convert.ToInt32(xpAward["experienceLevel"]);
                    int xpToNextLevel = Convert.ToInt32(xpAward["xpToNextLevel"]);
                    bool xpCapped = Convert.ToBoolean(xpAward["xpCapped"]);
                    string statusTitle = xpAward["statusTitle"] as string;

                    AppManager.Instance.userData.XPCapped = xpCapped;
                    AppManager.Instance.UpdateUserLevel(currentLevel, statusTitle, xpToNextLevel);
                    AppManager.Instance.UpdateUserXP(newXpPoints);
                }

                _data.quantity -= 1;
                if (_data.quantity <= 0)
                    InventoryService.Instance.OnItemSold?.Invoke(_data);

                InventoryService.Instance.OnItemBought?.Invoke();
                CloseModal();
            },
            (int status, int reason, string errorJson, object _) =>
            {
                Debug.LogError("OpenBundle script failed: " + errorJson);
                _cg.interactable = true;
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
