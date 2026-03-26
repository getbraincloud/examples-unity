using System.Collections.Generic;
using BrainCloud.JSONHelper;
using Gameframework;
using UnityEngine;

public class MouseMerchantItem : ShopItem
{
    protected override void OnBuyButton()
    {
        StateManager.Instance.OpenConfirmPopUp(
            "Are you sure?", 
            $"Buy {_shopInfo.DisplayName} for {_shopInfo.BuyCost} {_shopInfo.BuyCurrency.ToString()}?", 
            OnBuyCallback
        );
    }
    
    private void OnBuyCallback()
    {
        //ToDo Create a cloud code script to claim item
    }
    
    private void OnClaimItemSuccess(string jsonResponse)
    {
        //ToDo make sure to add daily love booster timer response here
    }
}
