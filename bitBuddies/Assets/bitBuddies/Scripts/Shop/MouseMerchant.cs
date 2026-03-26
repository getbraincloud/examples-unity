using System.Collections.Generic;
using UnityEngine;

public class MouseMerchant : Shop
{
    [SerializeField] private ShopItem shopItemPrefab;

    public override void SetupShop()
    {
        List<ShopInfo> shopItems = GameManager.Instance.ChildShopInfos;
        foreach (ShopInfo shopItem in shopItems)
        {
            var parentShopItem = Instantiate(shopItemPrefab, ItemSpawnPoint.transform);
            parentShopItem.Init(shopItem);
        }
    }
}
