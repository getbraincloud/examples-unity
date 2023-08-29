using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

[Serializable]
public class BCProduct
{
    public string itemId;
    public string type;
    public string category;
    public string title;
    public string description;
    public string imageUrl;
    public Dictionary<string, int> currency;
    public Dictionary<string, BCItem> items;
    public Dictionary<string, object> data;
    public Dictionary<string, object> priceData;

    public BCProduct() { }

    public ProductType IAPProductType
    {
        get
        {
            switch(type)
            {
                case "Consumable":
                    return ProductType.Consumable;
                case "Nonconsumable":
                    return ProductType.NonConsumable;
                case "Subscription":
                    return ProductType.Subscription;
                default:
                    Debug.LogWarning($"BCProduct.type is null or unknown: '{type}'. Returning default value of {ProductType.Consumable}...");
                    return ProductType.Consumable;
            }
        }
    }

    public int GetCurrencyAmount(string name)
    {
        if (currency != null && currency.TryGetValue(name, out int amount))
        {
            return amount;
        }
    
        Debug.LogWarning($"BCProduct.currency does not contain a value for: '{name}'. Returning 0...");
        return 0;
    }
    
    public BCItem GetItem(string name)
    {
        if (items != null && items.TryGetValue(name, out BCItem item))
        {
            return item;
        }
    
        Debug.LogWarning($"BCProduct.currency does not contain a value for: '{name}'. Returning default...");
        return default;
    }
    
    public T GetData<T>() where T : new() => JsonReader.Deserialize<T>(JsonWriter.Serialize(priceData));

    public BCGooglePlayPriceData GetGooglePlayPriceData() => JsonReader.Deserialize<BCGooglePlayPriceData>(JsonWriter.Serialize(priceData));
}

[Serializable]
public class BCItem
{
    public string defId;
    public int quantity;

    public BCItem() { }
}

[Serializable]
public class BCGooglePlayPriceData
{
    public string id;
    public int referencePrice;
    public bool isPromotion;

    public BCGooglePlayPriceData() { }

    public string GetIAPPrice() => (referencePrice/100.0f).ToString("C");
}
