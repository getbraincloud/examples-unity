using BrainCloud;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// <see cref="BrainCloudAppStore.GetSalesInventory(string, string, SuccessCallback, FailureCallback, object)"/>
/// returns JSON data that contains the product data configured under brainCloud Marketplace's <b>Products</b>.
/// This JSON data can be deserialized into this class.
/// 
/// <br><seealso cref="BrainCloudAppStore"/></br>
/// </summary>
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

    /// <summary>
    /// Gets the <see cref="ProductType"/> from <see cref="type"/>.
    /// </summary>
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

    /// <summary>
    /// Gets the amount of currency this <see cref="BCProduct"/> redeems.
    /// </summary>
    /// <param name="name">The currency as its named under the Marketplace's <b>Virtual Currencies</b>.</param>
    public int GetCurrencyAmount(string name)
    {
        if (currency != null && currency.TryGetValue(name, out int amount))
        {
            return amount;
        }
    
        Debug.LogWarning($"BCProduct.currency does not contain a value for: '{name}'. Returning 0...");
        return 0;
    }

    /// <summary>
    /// Gets the <see cref="BCItem"/> that this <see cref="BCProduct"/> redeems.
    /// </summary>
    /// <param name="name">The <see cref="BCItem"/> as its named under the Marketplace's <b>Item Catalog</b>.</param>
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

/// <summary>
/// <see cref="BCProduct"/> can redeem items as configured under brainCloud Marketplace's <b>Item Catalog</b>.
/// </summary>
[Serializable]
public class BCItem
{
    public string defId;
    public int quantity;

    public BCItem() { }
}

/// <summary>
/// <see cref="BCProduct"/> can have data for individual stores as configured under brainCloud Marketplace's <b>Products</b>.
/// </summary>
[Serializable]
public class BCPriceData
{
    public string id;
    public int referencePrice;
    public bool isPromotion;

    public BCPriceData() { }
}
