using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Setup for the child shop UI and logic.
/// </summary>
public class MouseMerchant : Shop
{
    [SerializeField] private ShopItem shopItemPrefab;
    [SerializeField] private TextMeshProUGUI GemBalanceText;

    //Set up shop items with Gem balance from parent user.
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
