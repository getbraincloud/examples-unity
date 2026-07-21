using BrainCloud.JSONHelper;
using Gameframework;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main logic for the parent shop items which includes
/// - Setting up the shop item
/// - Buying the item(if its a freebie or something from the item catalog)
/// - Increment stats locally if specific items are bought
/// - Updating the UI
/// </summary>
public class ParentShopItem : ShopItem
{
    protected override void OnBuyButton()
    {
        if (_shopInfo.BuyCost == 0)
        {
            PopUpUI.Show("Are you sure?", false)
                   .AddBodyText($"Obtain {_shopInfo.DisplayName} for free?")
                   .AddButton("Close", PopUpUI.ButtonColor.Blue, null)
                   .AddButton("Confirm", PopUpUI.ButtonColor.Green, OnBuyCallback, GameManager.CanBuyItem(_shopInfo.BuyCurrency, _shopInfo.BuyCost));
        }
        else
        {
            PopUpUI.Show("Are you sure?", false)
                   .AddBodyText($"Buy {_shopInfo.DisplayName} for {_shopInfo.BuyCost:N0} {_shopInfo.BuyCurrency}?")
                   .AddButton("Close", PopUpUI.ButtonColor.Blue, null)
                   .AddButton("Confirm", PopUpUI.ButtonColor.Green, OnBuyCallback, GameManager.CanBuyItem(_shopInfo.BuyCurrency, _shopInfo.BuyCost));
        }

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
                StatTracker.Instance.IncrementStat(BitBuddiesConsts.BOUGHT_COINS_WITH_GEMS_STAT_NAME);
                break;
            case CurrencyTypes.Gems:
                userInfo.UpdateGems(userInfo.Gems + amount);
                StatTracker.Instance.IncrementStat(BitBuddiesConsts.BOUGHT_GEMS_WITH_COINS_STAT_NAME);
                break;
        }
        RectTransform spawnRectTransform = _parentMenu.transform.GetChild(1).GetComponent<RectTransform>();
        CurrencyTypes buyCurrencyType = _shopInfo.BuyCurrency;
        switch (buyCurrencyType)
        {
            case CurrencyTypes.Gems:
                RectTransform target = _parentMenu.GetCurrencyTextRectTransform(CurrencyTypes.Gems);
                RectTransform coinButtonRectTransform = GetComponent<RectTransform>();
                coinButtonRectTransform.position -= Vector3.left * 50;
                StateManager.Instance.PlayCurrencyAnimationWorld(coinButtonRectTransform, target, CurrencyTypes.Coins, _parentMenu.CanvasRectTransform, spawnRectTransform);
                userInfo.UpdateGems(userInfo.Gems - _shopInfo.BuyCost);
                break;
            case CurrencyTypes.Coins:
                userInfo.UpdateCoins(userInfo.Coins - _shopInfo.BuyCost);
                break;
            case CurrencyTypes.FakeDollars:
                userInfo.UpdateFakeMoney(userInfo.FakeMoney - _shopInfo.BuyCost);
                break;
        }

        //Check for freebie to set up cooldown clock
        if (_shopInfo.ShopId == "freebie")
        {
            var cooldownUntil = (long)response["coolDownUntil"];
            if (cooldownUntil > 0)
            {
                GameManager.Instance.FreebieItemCooldownUntil = cooldownUntil;
                _countdownTimer.StartCountdown(cooldownUntil);
                BuyButton.interactable = false;
                FreebieImage.enabled = false;
            }
        }

        StateManager.Instance.RefreshScreen();
    }

    private void OnClaimItemFailure()
    {
        //FL ToDo: Create a pop up displaying the error messsage
    }
}
