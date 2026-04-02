using System;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using UnityEngine;

public class MouseMerchantItem : ShopItem
{
    public override void Init(ShopInfo shopInfo)
    {
        base.Init(shopInfo);
        var listOfItems = GameManager.Instance.SelectedAppChildrenInfo.ownedShopItems;
        if(listOfItems != null && listOfItems.Count > 0)
        {
            if(listOfItems.Contains(_shopInfo.ShopId))
            {
                BuyButton.interactable = false;
            }
        }
    }

    protected override void OnBuyButton()
    {
        if(_shopInfo.BuyCost > 0)
        {
            StateManager.Instance.OpenConfirmPopUp(
                "Are you sure?", 
                $"Buy {_shopInfo.DisplayName} for {_shopInfo.BuyCost} {_shopInfo.BuyCurrency.ToString()}?", 
                OnBuyCallback
            );
        }
        else
        {
            StateManager.Instance.OpenConfirmPopUp(
                "Are you sure?", 
                $"Claim {_shopInfo.DisplayName} for free?", 
                OnBuyCallback
            );
        }

    }
    
    private void OnBuyCallback()
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object>();
        scriptData.Add("childAppId", BitBuddiesConsts.APP_CHILD_ID);
        scriptData.Add("childProfileId", GameManager.Instance.SelectedAppChildrenInfo.profileId);
        
        if(_shopInfo.ShopId == "dailyLoveBooster")
        {
            BrainCloudManager.Client.ScriptService.RunScript
            (
                BitBuddiesConsts.CLAIM_LOVE_BOOSTER_SCRIPT_NAME,
                scriptData.Serialize(),
                BrainCloudManager.HandleSuccess("Claim Item Success", OnClaimLoveBoosterSuccess)
            );
        }
        else
        {
            scriptData.Add("itemId", _shopInfo.ShopId);
            BrainCloudManager.Client.ScriptService.RunScript
            (
                BitBuddiesConsts.CLAIM_CHILD_ITEM_SCRIPT_NAME,
                scriptData.Serialize(),
                BrainCloudManager.HandleSuccess("Claim Item Success", OnClaimItemSuccess)
            );
        }
    }
    
    private void OnClaimLoveBoosterSuccess(string jsonResponse)
    {
        /*
        {"packetId":4,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"compileTime":23056,"scriptSize":23439,"renderTime":54,"executeTime":99406},
        "response":{"success":false,"onCooldown":true,"error":"Daily Love Booster is still on cooldown","coolDownUntil":1775157691848,"boosterExpiry":null,
        "multiplier":2},"success":true,"reasonCode":null},"status":200}]}
         */
         Dictionary<string, object> response = (Dictionary<string, object>)JsonReader.Deserialize(jsonResponse);
         Dictionary<string, object> data = (Dictionary<string, object>)response["data"];
         Dictionary<string, object> responseData = (Dictionary<string, object>)data["response"];
         //Convert.ToInt64(cooldownUntilObject["cooldownUntil"]);
         long coolDownUntil = Convert.ToInt64(responseData["coolDownUntil"]);
         long boosterExpiryDuration = Convert.ToInt64(responseData["boosterExpiry"]);
         int multiplier = (int)responseData["multiplier"];
         if(responseData.ContainsKey("error"))
         {
             string error = (string)responseData["error"];
             if(!error.IsNullOrEmpty())
             {
                 StateManager.Instance.OpenInfoPopUp("Claim Booster Failed","Daily Booster has already claimed for today and is still on cool down.");
             }
         }
         else
         {
             GameManager.Instance.SelectedAppChildrenInfo.dailyCooldownUntil = coolDownUntil;
             ToyManager.Instance.StartLoveMultiplierCountdown(multiplier, boosterExpiryDuration);
             _countdownTimer = GetComponent<CountdownTimer>();
             _countdownTimer.StartCountdown(coolDownUntil);
             BuyButton.interactable = false;
             StateManager.Instance.RefreshScreen();
         }
    }
    
    private void OnClaimItemSuccess(string jsonResponse)
    {
        var response = (Dictionary<string, object>)JsonReader.Deserialize(jsonResponse);
        var data = (Dictionary<string, object>)response["data"];
        var responseData = (Dictionary<string, object>)data["response"];
        //Check if the script was successful
        bool successful = (bool)responseData["success"];
        if(!successful)
        {
            string errorMessage = (string)responseData["error"];
            StateManager.Instance.OpenInfoPopUp("Claim Item Failed", errorMessage);
            return;
        }
        /*
         * Daily free boost
         * {"packetId":5,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"scriptSize":37162,"executeTime":142573},
         * "response":{"success":true,"itemId":"dailyLoveBooster","coolDownUntil":1774982028917,"boosterExpiry":17749023483312,
         * "multiplier":2},"success":true,"reasonCode":null},"status":200}]}
         */
        string resultType = (string)responseData["resultType"];
        if(resultType.Equals("levelUpItem"))
        {
            //Aka Instant level up item 
            Dictionary<string, object> levelUpItemData = (Dictionary<string, object>)responseData["levelUpInfo"];
            
            //Get level up before and after
            int levelBefore = (int) levelUpItemData["levelBefore"];
            int levelAfter = (int) levelUpItemData["levelAfter"];
            var appInfo = GameManager.Instance.SelectedAppChildrenInfo;
            
            //Get Rewards
            var rewards = (Dictionary<string, object>)levelUpItemData["rewards"];
            if(rewards != null)
            {
                //FL ToDo: Read in rewards if we have any
            }
            
            //Get parent currency results
            var parentCurrencyResults = (Dictionary<string, object>)responseData["parentCurrency"];
            BrainCloudManager.Instance.CurrentUserInfo.UpdateGems((int)parentCurrencyResults["newBalance"]);
            var amountSpent = (int)parentCurrencyResults["amountSpent"];
            var oldBalance = (int)parentCurrencyResults["oldBalance"];
            GameManager.Instance.SelectedAppChildrenInfo.buddyLevel = levelAfter;
            
            StateManager.Instance.OpenInfoPopUp($"{appInfo.profileName} Leveled Up", $"{appInfo.profileName} leveled up from {levelBefore} to {levelAfter} for {amountSpent} gems");
        }
        else if(resultType.Equals("childCatalogItem"))
        {
            //Aka child catalog item
            Dictionary<string, object> childCurrency = (Dictionary<string, object>)responseData["childCurrency"];
            GameManager.Instance.SelectedAppChildrenInfo.buddyBling = (int)childCurrency["newBalance"];
            string childItemId = (string)responseData["itemId"];
            string itemName = GameManager.Instance.GetChildItemDisplayName(childItemId);
            GameManager.Instance.SelectedAppChildrenInfo.ownedShopItems.Add(childItemId);
             
            StateManager.Instance.OpenInfoPopUp("Item Bought", $"{itemName} bought for {childCurrency["amountSpent"]} bitBling");

        }
        else if(resultType.Equals("parentCurrencyPurchase"))
        {
            //Aka gems for bling
            Dictionary<string, object> parentCurrency = (Dictionary<string, object>)responseData["parentCurrency"];
            int newGemBalance = (int)parentCurrency["newBalance"];
            var amountSpent = (int)parentCurrency["amountSpent"];
            var oldBalance = (int)parentCurrency["oldBalance"];
            BrainCloudManager.Instance.CurrentUserInfo.UpdateGems(newGemBalance);
             
            Dictionary<string, object> payoutCurrency = (Dictionary<string, object>)responseData["payoutCurrency"];
            int newBlingBalance = (int) payoutCurrency["newBalance"];
            int amountAwarded = (int) payoutCurrency["amountAwarded"];
            GameManager.Instance.SelectedAppChildrenInfo.buddyBling = newBlingBalance;
             
            StateManager.Instance.OpenInfoPopUp("bitBling Purchased", $"{amountSpent} gems bought for {amountAwarded} bitBling");
        }
         
        StateManager.Instance.RefreshScreen();
    }
}
