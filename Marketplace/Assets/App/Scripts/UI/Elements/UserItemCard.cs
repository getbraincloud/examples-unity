using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UserItemCard : MonoBehaviour
{

    [SerializeField]
    private Image cardArt, categoryIcon, upperMessageIcon;

    [SerializeField]
    private TextMeshProUGUI inventoryCountText, upperMessageText, lowerMessageText, primaryButtonText;

    [SerializeField]
    private GameObject loadingDisplay, lowerMessageDisplay, upperMessageDisplay;

    [SerializeField]
    private Button _primaryButton;

    private UserItemData _data;
    public UserItemData data { get { return _data; } }


    private Button _button;
    private CanvasGroup _cg;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _cg = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(OnCardClicked);
        _primaryButton.onClick.AddListener(OnPrimaryButtonClicked);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveAllListeners();
        _primaryButton.onClick.RemoveAllListeners();
    }

    public void SetUserItemData(UserItemData data)
    {
        _data = data;
        if (_data.isEquippable && _data.isEquipped)
        {
            InventoryService.Instance.OnItemEquipChange(_data);
        }
        UpdateUI();
    }

    public void UpdateEquippedStatus(bool equipped)
    {
        _data.isEquipped = equipped;
        UpdateUI();
    }

    private void OnCardClicked()
    {
        _cg.interactable = false;
        //show modal
        AppManager.Instance.SpawnViewItemModal(_data, this, () =>
        {
            //this UserItemCard could be removed if item was sold before the item modal is removed
            if(this == null)
            {
                return;
            }

            _cg.interactable = true;
        });
    }

    public async Task FetchSprites()
    {
        if (!string.IsNullOrEmpty(_data.imageUrl))
        {
            cardArt.sprite = await ImageCacheService.Instance.GetImageAsync(_data.imageUrl);
        }
        else if (_data.isSubscription && ImageCacheService.Instance.noAdsSprite != null)
        {
            cardArt.sprite = ImageCacheService.Instance.noAdsSprite;
        }

        categoryIcon.sprite = ImageCacheService.Instance.GetSpriteForSection(_data.category);

        return;
    }

    private void CheckEquipped(Action<bool> onChecked)
    {
        Dictionary<string, object> scriptData = new();
        scriptData.Add("itemSlot", _data.equippableSlot);
        BCManager.Instance.BCWrapper.ScriptService.RunScript("GetEquippedItem", JsonWriter.Serialize(scriptData),
            (string responseJson, object cbObject) =>
            {
                var data = JsonReader.Deserialize<Dictionary<string, object>>(responseJson)["data"] as Dictionary<string, object>;
                var response = data["response"] as Dictionary<string, object>;

                string itemId = response["itemId"] as string;
                if(!string.IsNullOrEmpty(itemId) && itemId == _data.itemId)
                {
                    onChecked?.Invoke(true);
                }
                else
                {
                    onChecked?.Invoke(false);
                }
            },
            (int statusCode, int responseCode, string errorJson, object errorObj) =>
            {
                onChecked?.Invoke(false);
            });
    }

    public async void UpdateUI()
    {
        await FetchSprites();

        loadingDisplay.SetActive(false);

        upperMessageText.gameObject.SetActive(true);
        upperMessageText.text = _data.itemName;

        if (_data.isStackable)
        {
            inventoryCountText.gameObject.SetActive(true);
            string maxStack = _data.maxStackable == 0 ? "∞" : _data.maxStackable.ToString();
            inventoryCountText.text = _data.quantity.ToString() + "/" + maxStack;
        }
        else
        {
            inventoryCountText.gameObject.SetActive(false);
        }

        cardArt.gameObject.SetActive(true);

        if (_data.isBundle)
        {
            _primaryButton.gameObject.SetActive(true);
            primaryButtonText.text = "Open";
        }
        else if (_data.isActivatable)
        {
            _primaryButton.gameObject.SetActive(true);
            primaryButtonText.text = "Activate";

            upperMessageIcon.gameObject.SetActive(true);
            upperMessageText.text = _data.activeSeconds + " sec";
        }
        else if (_data.isEquippable)
        {
            if(_data.defId == "gold_frame")
            {
                _primaryButton.gameObject.SetActive(true);
                primaryButtonText.text = _data.isEquipped ? "Unequip" : "Equip";
            }
            else
            {
                _primaryButton.gameObject.SetActive(!_data.isEquipped);
                primaryButtonText.text = "Equip";
                if (_data.isEquipped)
                {
                    upperMessageIcon.gameObject.SetActive(false);
                    upperMessageText.text = "[Equipped]";
                }
            }
        }
        else
        {
            _primaryButton.gameObject.SetActive(false);
        }
    }

    private void OnPrimaryButtonClicked()
    {
        if (_data.isBundle)
            OnOpenBundleClicked();
        else if (_data.isActivatable)
            OnActivateClicked();
        else if (_data.isEquippable)
            OnEquipClicked();
    }

    private void OnOpenBundleClicked()
    {
        _cg.interactable = false;
        var scriptData = new Dictionary<string, string> { { "itemId", _data.itemId } };
        BCManager.Instance.BCWrapper.ScriptService.RunScript("OpenBundle", JsonWriter.Serialize(scriptData),
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
                    AppManager.Instance.userData.XPCapped = Convert.ToBoolean(xpAward["xpCapped"]);
                    AppManager.Instance.UpdateUserLevel(Convert.ToInt32(xpAward["experienceLevel"]), xpAward["statusTitle"] as string, Convert.ToInt32(xpAward["xpToNextLevel"]));
                    AppManager.Instance.UpdateUserXP(Convert.ToInt32(xpAward["adjustedXp"]));
                }

                _data.quantity -= 1;
                if (_data.quantity <= 0)
                    InventoryService.Instance.OnItemSold?.Invoke(_data);
                else
                    UpdateUI();

                InventoryService.Instance.OnItemBought?.Invoke();
            },
            (int status, int reason, string errorJson, object _) =>
            {
                Debug.LogError("OpenBundle script failed: " + errorJson);
                _cg.interactable = true;
            });
    }

    private void OnActivateClicked()
    {
        _cg.interactable = false;
        var scriptData = new Dictionary<string, string> { { "itemId", _data.itemId } };
        BCManager.Instance.BCWrapper.ScriptService.RunScript("ActivateItem", JsonWriter.Serialize(scriptData),
            (string responseJson, object cbObject) =>
            {
                var root = JsonReader.Deserialize<Dictionary<string, object>>(responseJson)["data"] as Dictionary<string, object>;
                var response = root["response"] as Dictionary<string, object>;

                bool error = Convert.ToBoolean(response["error"]);
                if (error)
                {
                    Debug.LogError("Failed to activate item: " + response["message"]);
                    _cg.interactable = true;
                    return;
                }

                if (response.ContainsKey("activeUntil"))
                {
                    long activeUntil = Convert.ToInt64(response["activeUntil"]);
                    AppManager.Instance.ToggleCoinMultiplier(true, activeUntil);
                }

                _data.quantity -= 1;
                if (_data.quantity <= 0)
                {
                    InventoryService.Instance.OnItemSold?.Invoke(_data);
                }
                else
                {
                    UpdateUI();
                    _cg.interactable = true;
                }
            },
            (int status, int reason, string errorJson, object _) =>
            {
                Debug.LogError("ActivateItem script failed: " + errorJson);
                _cg.interactable = true;
            });
    }

    private void OnEquipClicked()
    {
        _cg.interactable = false;
        bool targetEquip = !_data.isEquipped;
        InventoryService.Instance.ToggleItemEquipped(_data, targetEquip, (bool success) =>
        {
            if (success)
            {
                _data.isEquipped = targetEquip;
                InventoryService.Instance.OnItemEquipChange?.Invoke(_data);
            }
            UpdateUI();
            _cg.interactable = true;
        });
    }
}
