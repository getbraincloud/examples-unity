using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MouseMerchant : Shop
{
    [SerializeField] private ShopItem shopItemPrefab;
    [SerializeField] private TextMeshProUGUI GemBalanceText;

    public override void SetupShop()
    {
        GemBalanceText.text = BrainCloudManager.Instance.CurrentUserInfo.Gems.ToString();

        if (ItemSpawnPoint.transform.childCount > 0) 
            return;
        
        List<ShopInfo> shopItems = GameManager.Instance.ChildShopInfos;
        foreach (ShopInfo shopItem in shopItems)
        {
            var parentShopItem = Instantiate(shopItemPrefab, ItemSpawnPoint.transform);
            parentShopItem.Init(shopItem);
        }
    }
}
