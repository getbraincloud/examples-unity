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
    private Image cardArt, categoryIcon;

    [SerializeField]
    private TextMeshProUGUI inventoryCountText, upperMessageText, lowerMessageText;

    [SerializeField]
    private GameObject loadingDisplay, lowerMessageDisplay;

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
    }

    private void OnDisable()
    {
        _button.onClick.RemoveAllListeners();
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

        lowerMessageText.text = _data.itemName;

        if (_data.isStackable)
        {
            inventoryCountText.gameObject.SetActive(true);
            inventoryCountText.text = _data.quantity.ToString() + "/" + _data.maxStackable.ToString();
        }
        else
        {
            inventoryCountText.gameObject.SetActive(false);
        }

        if (_data.isEquippable)
        {
            upperMessageText.gameObject.SetActive(true);
            upperMessageText.text = _data.isEquipped ? "[Equipped]" : "[Unequiped]";
        }
        else
        {
            upperMessageText.gameObject.SetActive(false);
        }

        lowerMessageDisplay.SetActive(true);
        cardArt.gameObject.SetActive(true);
        
    }
}
