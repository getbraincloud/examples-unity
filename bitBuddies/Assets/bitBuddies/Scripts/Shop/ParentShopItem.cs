using System;
using System.Collections.Generic;
using BrainCloud.JSONHelper;
using Gameframework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ParentShopItem : MonoBehaviour
{

    [SerializeField] private TMP_Text ItemNameText;
    [SerializeField] private TMP_Text ItemDescriptionText;
    [SerializeField] private TMP_Text ItemPriceText;
    [SerializeField] private TMP_Text ItemRewardText;
    [SerializeField] private Image RewardImage;
    [SerializeField] private Button BuyButton;

    [SerializeField] private Image BuyImage;
    //[SerializeField] private Image ItemImage;


    private ParentShopInfo _parentShopInfo;
    
    
    public void Init(ParentShopInfo in_parentShopInfo)
    {
        BuyButton.onClick.AddListener(OnBuyButton);
        _parentShopInfo = in_parentShopInfo;
        ItemNameText.text = _parentShopInfo.DisplayName;
        ItemDescriptionText.text = _parentShopInfo.ItemDescription;
        if(_parentShopInfo.BuyCost > 0)
        {
            ItemPriceText.text = _parentShopInfo.BuyCost.ToString("#,#");    //#,# adds commas to the string when using ints
            BuyImage.enabled = true;
        }
        else
        {
            ItemPriceText.text = "Free";
            BuyImage.enabled = false;
        }

        ItemRewardText.text = _parentShopInfo.RewardAmount.ToString("#,#");
        
        switch (_parentShopInfo.RewardCurrencyType)
        {
            case CurrencyTypes.Coins:
                RewardImage.sprite = AssetLoader.LoadSprite(BitBuddiesConsts.COIN_SPRITE_PATH);
                break;
            
            case CurrencyTypes.Gems:
                RewardImage.sprite = AssetLoader.LoadSprite(BitBuddiesConsts.GEM_SPRITE_PATH);
                break;
        }
        
        switch(_parentShopInfo.BuyCurrency)
        {
            case CurrencyTypes.Coins:
                BuyImage.sprite = AssetLoader.LoadSprite(BitBuddiesConsts.COIN_SPRITE_PATH);
                break;
            case CurrencyTypes.Gems:
                BuyImage.sprite = AssetLoader.LoadSprite(BitBuddiesConsts.GEM_SPRITE_PATH);
                break;
            case CurrencyTypes.FakeDollars:
                BuyImage.sprite = AssetLoader.LoadSprite(BitBuddiesConsts.FAKE_MONEY_SPRITE_PATH);
                break;
        }
    }

    private void OnDestroy()
    {
        BuyButton.onClick.RemoveAllListeners();
    }

    private void OnBuyButton()
    {
        StateManager.Instance.OpenConfirmPopUp("Are you sure?", $"Buy {_parentShopInfo.DisplayName} for {_parentShopInfo.BuyCost} {_parentShopInfo.BuyCurrency.ToString()}?", OnBuyCallback);
    }
    
    private void OnBuyCallback()
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object>();
        scriptData.Add("itemId", _parentShopInfo.ShopId);
        BrainCloudManager.Client.ScriptService.RunScript
        (
            BitBuddiesConsts.CLAIM_ITEM_SCRIPT_NAME,
            scriptData.Serialize(),
            BrainCloudManager.HandleSuccess("Claim Item Success", OnClaimItemSuccess),
            BrainCloudManager.HandleFailure("Claim Item Failure", OnClaimItemFailure)
        );
    }
    
    private void OnClaimItemSuccess(string jsonResponse)
    {
        /*
         * "response": {
              "success": true,
              "itemId": "freebie",
              "coolDownUntil": 1774475149170, <--------- Need to read this in for cooldown clock for freebie if it exists
              "payout": {
                "currencyType": "coins",
                "amount": 1000
              }
            }
         */           
    }
    
    private void OnClaimItemFailure()
    {
        
    }
}
