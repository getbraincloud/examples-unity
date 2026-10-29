using System;

[Serializable]
public class UserItemData
{
    public string itemId;
    public string defId;
    public string itemName;
    public string description;
    public string category;
    public string imageUrl;
    public int quantity;
    public bool isEquippable;
    public string equippableSlot;
    public bool isEquipped;
    public bool isSellable;
    public CurrencyType sellCurrency;
    public int sellAmount;
    public bool isStackable;
    public int maxStackable;
    public bool isSubscription;
    public bool isAutoRenewing;
    public long subscriptionExpiryMs;
    public bool isDefault;
    public bool autoEquip;
    public bool isBundle;
    public bool isActivatable;
    public int activeSeconds;
}
