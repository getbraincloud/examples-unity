using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryService : MonoBehaviour
{
    public static InventoryService Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void GetAllUserItems(Action<List<StoreItemData>> onSuccess,
                            Action<string> onFailure)
    {
        List<StoreItemData> allItems = new List<StoreItemData>();

        FetchPageRecursive(
            pageNumber: 1,
            accumulatedItems: allItems,
            onSuccess: onSuccess,
            onFailure: onFailure
        );
    }

    private void FetchPageRecursive(
    int pageNumber,
    List<StoreItemData> accumulatedItems,
    Action<List<StoreItemData>> onSuccess,
    Action<string> onFailure)
    {
        if (accumulatedItems == null)
            accumulatedItems = new List<StoreItemData>();



        GetUserItemsContext context = new GetUserItemsContext
        {
            pagination = new GetUserItemsPagination
            {
                rowsPerPage = 50,
                pageNumber = pageNumber,
            }
        };

        string contextString = JsonWriter.Serialize(context);

        BCManager.Instance.BCWrapper.UserItemsService.GetUserItemsPage(
            contextString,
            true,
            (string responseJson, object cb) =>
            {
                var pageResult = ParseUserItems(responseJson);

                accumulatedItems.AddRange(pageResult.Items);

                if (pageResult.MoreAfter)
                {
                    // Fetch next page
                    FetchPageRecursive(
                        pageNumber + 1,
                        accumulatedItems,
                        onSuccess,
                        onFailure
                    );
                }
                else
                {
                    // Finished all pages
                    onSuccess?.Invoke(accumulatedItems);
                }
            },
            (int statusCode, int responseCode, string errorJson, object errorCb) =>
            {
                onFailure?.Invoke(errorJson);
            }
        );
    }

    private class PageParseResult
    {
        public List<StoreItemData> Items;
        public bool MoreAfter;
    }

    private PageParseResult ParseUserItems(string json)
    {
        var root = JsonReader.Deserialize<Dictionary<string, object>>(json);
        var data = root["data"] as Dictionary<string, object>;
        var results = data["results"] as Dictionary<string, object>;

        bool moreAfter = Convert.ToBoolean(results["moreAfter"]);
        var itemsArray = results["items"] as object[];

        List<StoreItemData> parsedItems = new List<StoreItemData>();

        if (itemsArray == null || itemsArray.Length == 0)
        {
            Debug.LogWarning("[Inventory] Items array was null or empty");
            return new PageParseResult
            {
                Items = parsedItems,
                MoreAfter = false
            };
        }

        foreach (var itemObj in itemsArray)
        {
            
            var itemDict = itemObj as Dictionary<string, object>;
            var itemDef = itemDict["itemDef"] as Dictionary<string, object>;
            var meta = itemDef["meta"] as Dictionary<string, object>;

            CurrencyType rewardCurrency = CurrencyType.None;
            int rewardAmount = 0;

            if(meta != null && meta.ContainsKey("currency"))
            {
                switch(meta["currency"] as string)
                {
                    case "Coins":
                        rewardCurrency = CurrencyType.Coins;
                        break;
                    case "Gems":
                        rewardCurrency = CurrencyType.Gems;
                        break;
                    case "Stars":
                        rewardCurrency = CurrencyType.Stars;
                        break;
                }

                rewardAmount = Convert.ToInt32(meta["amount"]);
            }

            var buyPriceData = itemDef["buyPrice"] as Dictionary<string, object>;

            Dictionary<CurrencyType, decimal> buyPrices = new();

            foreach (var kvp in buyPriceData)
            {
                if (Enum.TryParse(kvp.Key, out CurrencyType currency))
                {
                    buyPrices[currency] = Convert.ToDecimal(kvp.Value);
                }
            }

            long recoveryUntil = Convert.ToInt64(itemDict["recoveryUntil"]);

            parsedItems.Add(new StoreItemData
            {
                itemId = itemDict["itemId"] as string,
                defId = itemDict["defId"] as string,
                imageUrl = itemDef["image"] as string,
                quantity = Convert.ToInt32(itemDict["quantity"]),
                usesLeft = Convert.ToInt32(itemDict["usesLeft"]),
                maxUses = Convert.ToInt32(itemDict["maxUses"]),
                recoveryUntil = recoveryUntil,
                coolDownUntil = Convert.ToInt64(itemDict["coolDownUntil"]),
                category = itemDef["category"] as string,
                buyPrices = buyPrices,
                rewardCurrency = rewardCurrency,
                isCurrency = rewardCurrency != CurrencyType.None,
                itemAmount = rewardAmount,
                isFree = buyPrices.Count == 0,
                isOnCooldown = recoveryUntil != -1
            });
        }

        return new PageParseResult
        {
            Items = parsedItems,
            MoreAfter = moreAfter
        };
    }
}
