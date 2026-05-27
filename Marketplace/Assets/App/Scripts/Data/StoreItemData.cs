using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]

public class StoreItemData
{
    public string itemName;
    public string category;
    public string message;
    public string defId;
    public string itemId;
    public string imageUrl;
    public ItemType itemType;
    public Dictionary<CurrencyType, decimal> buyPrices;
    public CurrencyType rewardCurrency;
    public decimal currentPrice = 0.00m;
    public decimal oldPrice = 0.00m;
    public int inventoryAmount;
    public int itemAmount;
    public int usesLeft;
    public int maxUses;
    public int quantity;
    public int activeSeconds;
    public long coolDownUntil;
    public long recoveryUntil;
    public bool isFree = false;
    public bool isOnCooldown = false;
    public bool isOnPromotion = false;
    public bool isCurrency = false;
    public bool autoOpen = false;

    public StoreItemData() { }

    public void UpdateFromJson(Dictionary<string,object> itemJson)
    {
        if (itemJson["itemId"] as string == itemId)
        {
            //we are updating from the correct item data
            quantity = Convert.ToInt32(itemJson["quantity"]);
            usesLeft = Convert.ToInt32(itemJson["usesLeft"]);
            maxUses = Convert.ToInt32(itemJson["maxUses"]);
            coolDownUntil = Convert.ToInt64(itemJson["coolDownUntil"]);
            recoveryUntil = Convert.ToInt64(itemJson["recoveryUntil"]);

            var itemDef = itemJson["itemDef"] as Dictionary<string, object>;

            imageUrl = itemDef["image"] as string;

            isOnCooldown = recoveryUntil != -1;
        }
    }
}

