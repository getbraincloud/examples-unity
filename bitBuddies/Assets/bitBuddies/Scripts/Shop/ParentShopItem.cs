using System;
using System.Collections.Generic;
using BrainCloud.JSONHelper;
using Gameframework;
using UnityEngine;

public class ParentShopItem : ShopItem
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
        Dictionary<string, object> scriptData = new Dictionary<string, object>();
        scriptData.Add("itemId", _shopInfo.ShopId);
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
        Dictionary<string, object> data = jsonResponse.Deserialize("data");
        Dictionary<string, object> response = data["response"] as Dictionary<string, object>;
        
        Dictionary<string, object> payoutObject = response["payout"] as Dictionary<string, object>;
        if (payoutObject == null)
        {
            Debug.LogError("Payout object is null in OnClaimItemSuccess");
            return;
        }
        
        CurrencyTypes currencyType = Enum.Parse<CurrencyTypes>(payoutObject["currencyType"] as string, true);
        int amount = (int)payoutObject["amount"];
        UserInfo userInfo = BrainCloudManager.Instance.CurrentUserInfo;
        switch (currencyType)
        {
            case CurrencyTypes.Coins:
                userInfo.UpdateCoins(userInfo.Coins + amount);
                break;
            case CurrencyTypes.Gems:
                userInfo.UpdateGems(userInfo.Gems + amount);
                break;
        }
        
        //Check for freebie to set up cooldown clock
        if(_shopInfo.ShopId == "freebie")
        {
            var cooldownUntil = (long)response["coolDownUntil"];
            if(cooldownUntil > 0)
            {
                GameManager.Instance.FreebieItemCooldownUntil = cooldownUntil;
                _countdownTimer.StartCountdown(cooldownUntil);
            }
        }
        
        StateManager.Instance.RefreshScreen();
    }
    
    private void OnClaimItemFailure()
    {
        
    }
}
