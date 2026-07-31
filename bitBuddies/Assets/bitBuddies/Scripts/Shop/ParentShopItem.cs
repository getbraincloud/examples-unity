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
            OnBuyCallback();
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
        var scriptData = new Dictionary<string, object>
        {
            { "itemId", _shopInfo.ShopId }
        };

        BrainCloudManager.Client.ScriptService.RunScript(BitBuddiesConsts.CLAIM_ITEM_SCRIPT_NAME,
                                                         scriptData.Serialize(),
                                                         BrainCloudManager.HandleSuccess("Claim Item Success", OnClaimItemSuccess),
                                                         BrainCloudManager.HandleFailure("Claim Item Failure", OnClaimItemFailure));
    }

    private void OnClaimItemSuccess(string jsonResponse)
    {
        var response = jsonResponse.Deserialize("data", "response");

        if (response["payout"] is not Dictionary<string, object> payoutObject)
        {
            Debug.LogError("Payout object is null in OnClaimItemSuccess");
            return;
        }

        UserInfo userInfo = BrainCloudManager.Instance.CurrentUserInfo;

        CurrencyTypes currencyType = payoutObject.GetValue<CurrencyTypes>("currencyType");
        int amount = payoutObject.GetValue<int>("amount");
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
                userInfo.UpdateGems(userInfo.Gems - _shopInfo.BuyCost);
                break;
            case CurrencyTypes.Coins:
                userInfo.UpdateCoins(userInfo.Coins - _shopInfo.BuyCost);
                break;
            case CurrencyTypes.FakeDollars:
                userInfo.UpdateFakeMoney(userInfo.FakeMoney - _shopInfo.BuyCost);
                break;
        }

        if (_shopInfo.RewardAmount > 0)
        {
            StateManager.Instance.PlayCurrencyAnimation(currencyType, GetComponent<RectTransform>().position,
                                                                      _parentMenu.GetCurrencyTextRectTransform(currencyType).position);
        }

        // Check for freebie to set up cooldown clock
        if (_shopInfo.ShopId == "freebie")
        {
            var cooldownUntil = response.GetValue<long>("coolDownUntil");
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
        GameManager.Instance.ShowFailurePopUp(body: "Item failed to claim. Please try again or restart the application.");
    }
}
