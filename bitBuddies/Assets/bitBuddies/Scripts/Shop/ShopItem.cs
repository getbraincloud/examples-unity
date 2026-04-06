using System;
using System.Collections.Generic;
using BrainCloud.JSONHelper;
using Gameframework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{

    [SerializeField] protected TMP_Text ItemNameText;
    [SerializeField] protected TMP_Text ItemDescriptionText;
    [SerializeField] protected TMP_Text ItemPriceText;
    [SerializeField] protected TMP_Text ItemRewardText;
    [SerializeField] protected Image RewardImage;
    [SerializeField] protected Button BuyButton;

    [SerializeField] protected Image BuyImage;
    //[SerializeField] private Image ItemImage;


    protected ShopInfo _shopInfo;
    public ShopInfo ItemInfo { get { return _shopInfo; } }

    protected CountdownTimer _countdownTimer;
    
    public void EnableBuyButton()
    {
        BuyButton.interactable = true;
    }
    
    public virtual void Init(ShopInfo shopInfo)
    {
        BuyButton.onClick.AddListener(OnBuyButton);
        _shopInfo = shopInfo;
        ItemNameText.text = _shopInfo.DisplayName;
        ItemDescriptionText.text = _shopInfo.ItemDescription;
        if(_shopInfo.BuyCost > 0)
        {
            ItemPriceText.text = _shopInfo.BuyCost.ToString("#,#");    //#,# adds commas to the string when using ints
            BuyImage.enabled = true;
        }
        else
        {
            ItemPriceText.text = "Free";
            BuyImage.enabled = false;
        }

        ItemRewardText.text = _shopInfo.RewardAmount.ToString("#,#");
        
        if(_shopInfo.RewardAmount > 0)
        {
            RewardImage.sprite = GetCurrencySprite(_shopInfo.RewardCurrencyType);
        }
        else
        {
            RewardImage.enabled = false;
        }
        
        BuyImage.sprite = GetCurrencySprite(_shopInfo.BuyCurrency);
        
        if(_shopInfo.ShopId == "freebie")
        {
            _countdownTimer = GetComponent<CountdownTimer>();
            if(GameManager.Instance.FreebieItemCooldownUntil > 0)
            {
                _countdownTimer.StartCountdown(GameManager.Instance.FreebieItemCooldownUntil);
                BuyButton.interactable = false;
            }
        }
        else if(_shopInfo.ShopId == "dailyLoveBooster")
        {
            RewardImage.sprite = GetCurrencySprite(CurrencyTypes.Love);
            var appInfo = GameManager.Instance.SelectedAppChildrenInfo;
            if(appInfo.dailyCooldownUntil > 0)
            {
                _countdownTimer = GetComponent<CountdownTimer>();
                _countdownTimer.StartCountdown(appInfo.dailyCooldownUntil);
                BuyButton.interactable = false;
            }
        }
        //check for instant level up, if user is maxed level
        else if(shopInfo.ShopId.Contains("instant") || shopInfo.ShopId.Contains("LevelUp"))
        {
            if(GameManager.Instance.SelectedAppChildrenInfo.buddyLevel >= 10)
            {
                BuyButton.interactable = false;
            }
        }
    }
    
    protected Sprite GetCurrencySprite(CurrencyTypes currencyType)
    {
        switch(currencyType)
        {
            case CurrencyTypes.Coins:
                return AssetLoader.LoadSprite(BitBuddiesConsts.COIN_SPRITE_PATH);
            case CurrencyTypes.Gems:
                return AssetLoader.LoadSprite(BitBuddiesConsts.GEM_SPRITE_PATH);
            case CurrencyTypes.FakeDollars:
                return AssetLoader.LoadSprite(BitBuddiesConsts.FAKE_MONEY_SPRITE_PATH);
            case CurrencyTypes.Love:
            case CurrencyTypes.Level:
                return AssetLoader.LoadSprite(BitBuddiesConsts.LOVE_SPRITE_PATH);
            case CurrencyTypes.BuddyBling:
                return AssetLoader.LoadSprite(BitBuddiesConsts.BIT_BLING_SPRITE_PATH);
        }
        return null;
    }

    protected void OnDestroy()
    {
        BuyButton.onClick.RemoveAllListeners();
    }

    protected virtual void OnBuyButton() {}
}
