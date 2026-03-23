using System;
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
    //[SerializeField] private Image ItemImage;


    private ParentShopInfo _parentShopInfo;
    
    
    public void Init(ParentShopInfo in_parentShopInfo)
    {
        BuyButton.onClick.AddListener(OnBuyButton);
        _parentShopInfo = in_parentShopInfo;
        ItemNameText.text = _parentShopInfo.DisplayName;
        ItemDescriptionText.text = _parentShopInfo.ItemDescription;
        ItemPriceText.text = _parentShopInfo.BuyCost.ToString("#,#");    //#,# adds commas to the string when using ints
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
        Debug.LogWarning("Yay");
    }
    
}
