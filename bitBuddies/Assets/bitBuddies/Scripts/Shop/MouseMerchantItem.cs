using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using System;
using System.Collections.Generic;

/// <summary>
/// Main logic for the mouse merchant shop items which includes
/// - Setting up the shop item
/// - Buying the item(if its a love booster or something from the item catalog)
/// - Increment stats locally if specific items are bought
/// - Updating the UI
/// </summary>
public class MouseMerchantItem : ShopItem
{
    public override void Init(ShopInfo shopInfo)
    {
        base.Init(shopInfo);
        var listOfItems = GameManager.Instance.SelectedAppChildrenInfo.ownedShopItems;
        if (listOfItems != null && listOfItems.Count > 0)
        {
            if (listOfItems.Contains(_shopInfo.ShopId))
            {
                BuyButton.interactable = false;
            }
        }
        if (_shopInfo.ShopId.Contains("level"))
        {
            if (GameManager.Instance.SelectedAppChildrenInfo.buddyLevel >= 10)
            {
                BuyButton.interactable = false;
            }
        }
    }

    protected override void OnBuyButton()
    {
        if (_shopInfo.BuyCost > 0)
        {
            PopUpUI.Show("Are you sure?", false)
                   .AddBodyText($"Buy {_shopInfo.DisplayName} for {_shopInfo.BuyCost} {_shopInfo.BuyCurrency.ToString()}?")
                   .AddButton("Close", PopUpUI.ButtonColor.Blue, null)
                   .AddButton("Confirm", PopUpUI.ButtonColor.Green, OnBuyCallback, GameManager.CanBuyItem(_shopInfo.BuyCurrency, _shopInfo.BuyCost));
        }
        else
        {
            PopUpUI.Show("Are you sure?", false)
                   .AddBodyText($"Claim {_shopInfo.DisplayName} for free?")
                   .AddButton("Close", PopUpUI.ButtonColor.Blue, null)
                   .AddButton("Confirm", PopUpUI.ButtonColor.Green, OnBuyCallback);
        }

    }

    private void OnBuyCallback()
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object>();
        scriptData.Add("childAppId", BitBuddiesConsts.APP_CHILD_ID);
        scriptData.Add("childProfileId", GameManager.Instance.SelectedAppChildrenInfo.profileId);

        if (_shopInfo.ShopId == "dailyLoveBooster")
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
        if (responseData.ContainsKey("error"))
        {
            string error = (string)responseData["error"];
            if (!error.IsNullOrEmpty())
            {
                PopUpUI.Show("Claim Booster Failed")
                       .AddBodyText("Daily Booster has already claimed for today and is still on cool down.");
            }
        }
        else
        {
            GameManager.Instance.SelectedAppChildrenInfo.UpdateLoveBoosterInfo(multiplier, boosterExpiryDuration, coolDownUntil);
            ToyManager.Instance.StartLoveMultiplierCountdown(multiplier, boosterExpiryDuration);
            _countdownTimer = GetComponent<CountdownTimer>();
            _countdownTimer.StartCountdown(coolDownUntil);
            BuyButton.interactable = false;
            FreebieImage.enabled = false;
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
        if (!successful)
        {
            string errorMessage = (string)responseData["error"];
            PopUpUI.Show("Claim Item Failed")
                   .AddBodyText(errorMessage);
            return;
        }

        string resultType = (string)responseData["resultType"];
        bool statIncremented = (bool)responseData["statIncremented"];
        if (resultType.Equals("levelUpItem"))
        {
            //Aka Instant level up item 
            Dictionary<string, object> levelUpItemData = (Dictionary<string, object>)responseData["levelUpInfo"];

            //Get level up before and after
            int levelBefore = (int)levelUpItemData["levelBefore"];
            int levelAfter = (int)levelUpItemData["levelAfter"];
            var appInfo = GameManager.Instance.SelectedAppChildrenInfo;

            //Get Rewards
            var rewards = (Dictionary<string, object>)levelUpItemData["rewards"];
            if (rewards != null)
            {
                var achievementObject = rewards["playerAchievements"] as string[];
                if (achievementObject != null && achievementObject.Length > 0)
                {
                    GameManager.Instance.UpdateAchievementAwarded(achievementObject);
                }
            }

            //Get parent currency results
            var parentCurrencyResults = (Dictionary<string, object>)responseData["parentCurrency"];
            BrainCloudManager.Instance.CurrentUserInfo.UpdateGems((int)parentCurrencyResults["newBalance"]);
            var amountSpent = (int)parentCurrencyResults["amountSpent"];
            var oldBalance = (int)parentCurrencyResults["oldBalance"];
            var previousLevelReq = (int)levelUpItemData["previousLevelXPReq"];
            var nextLevelReq = (int)levelUpItemData["nextLevelXPReq"];
            appInfo.previousLevelUp = previousLevelReq;
            appInfo.nextLevelUp = nextLevelReq;
            appInfo.buddyLevel = levelAfter;
            if (statIncremented)
            {
                StatTracker.Instance.IncrementStat(BitBuddiesConsts.BUDDIES_LEVELED_UP_STAT_NAME);
                StatTracker.Instance.IncrementStat(BitBuddiesConsts.BOUGHT_LEVELUP_PROMOS_STAT_NAME);
                if (levelAfter >= 5)
                {
                    StatTracker.Instance.IncrementStat(BitBuddiesConsts.LEVEL5_BUDDIES_STAT_NAME);
                }
            }

            PopUpUI.Show($"{appInfo.profileName} Leveled Up")
                   .AddBodyText($"{appInfo.profileName} leveled up from {levelBefore} to {levelAfter} for {amountSpent} gems");
            GameManager.Instance.UpdateSelectedAppChildrenInfo(appInfo);
            ToyManager.Instance.CheckForUnlockedBenches();
        }
        else if (resultType.Equals("childCatalogItem"))
        {
            //Aka child catalog item
            Dictionary<string, object> childCurrency = (Dictionary<string, object>)responseData["childCurrency"];
            GameManager.Instance.SelectedAppChildrenInfo.buddyBling = (int)childCurrency["newBalance"];
            string childItemId = (string)responseData["itemId"];
            string itemName = GameManager.Instance.GetChildItemDisplayName(childItemId);
            if (GameManager.Instance.SelectedAppChildrenInfo.ownedShopItems == null)
            {
                GameManager.Instance.SelectedAppChildrenInfo.ownedShopItems = new List<string>();
            }
            GameManager.Instance.SelectedAppChildrenInfo.ownedShopItems.Add(childItemId);

            PopUpUI.Show("Item Bought")
                   .AddBodyText($"{itemName} bought for {childCurrency["amountSpent"]} bitBling");
            BuyButton.interactable = false;
            if (statIncremented)
            {
                if (childItemId.Contains("hat"))
                {
                    StatTracker.Instance.IncrementStat(BitBuddiesConsts.HATS_BOUGHT_STAT_NAME);
                }
                else if (childItemId.Contains("sunglasses"))
                {
                    StatTracker.Instance.IncrementStat(BitBuddiesConsts.SUNGLASSES_BOUGHT_STAT_NAME);
                }
                else if (childItemId.Contains("gold"))
                {
                    StatTracker.Instance.IncrementStat(BitBuddiesConsts.CHAINS_BOUGHT_STAT_NAME);
                }
            }

        }
        else if (resultType.Equals("parentCurrencyPurchase"))
        {
            //Aka gems for bling
            Dictionary<string, object> parentCurrency = (Dictionary<string, object>)responseData["parentCurrency"];
            int newGemBalance = (int)parentCurrency["newBalance"];
            var amountSpent = (int)parentCurrency["amountSpent"];
            var oldBalance = (int)parentCurrency["oldBalance"];
            BrainCloudManager.Instance.CurrentUserInfo.UpdateGems(newGemBalance);

            Dictionary<string, object> payoutCurrency = (Dictionary<string, object>)responseData["payoutCurrency"];
            int newBlingBalance = (int)payoutCurrency["newBalance"];
            int amountAwarded = (int)payoutCurrency["amountAwarded"];
            GameManager.Instance.SelectedAppChildrenInfo.buddyBling = newBlingBalance;

            PopUpUI.Show("bitBling Purchased")
                   .AddBodyText($"{amountSpent} gems bought for {amountAwarded} bitBling");
        }

        StateManager.Instance.RefreshScreen();
    }
}
