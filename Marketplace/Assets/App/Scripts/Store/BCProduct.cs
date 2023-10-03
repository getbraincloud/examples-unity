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
    public BCPriceData priceData;

    public Product unityProduct { get; private set; }

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
    
        Debug.LogWarning($"BCProduct.items does not contain a value for: '{name}'. Returning default...");
        return default;
    }


    public string GetProductID() => priceData.id;

    public string GetLocalizedTitle() => unityProduct.metadata.localizedTitle;

    public string GetLocalizedDescription() => unityProduct.metadata.localizedDescription;

    public decimal GetLocalizedPrice() => unityProduct.metadata.localizedPrice;

    public string GetLocalizedPriceString() => unityProduct.metadata.localizedPriceString;

    public string GetLocalizedISOCurrencyCode() => unityProduct.metadata.isoCurrencyCode;

    public ProductMetadata GetProductMetaData() => unityProduct.metadata;

    public void SetUnityProduct(Product product) => unityProduct = product;
}

[Serializable]
public class BCItem
{
    public string defId;
    public int quantity;

    public BCItem() { }
}

[Serializable]
public class BCPriceData
{
    public string id;
    public int referencePrice;
    public bool isPromotion;

    public BCPriceData() { }
}
