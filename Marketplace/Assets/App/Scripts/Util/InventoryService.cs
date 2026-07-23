using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing.MiniJSON;

public class InventoryService : MonoBehaviour
{
    public static InventoryService Instance { get; private set; }

    public Action<UserItemData> OnItemEquipChange;
    public Action<UserItemData> OnItemSold;
    public Action OnItemBought;
    public Action<UserItemData> OnSingleItemBought;
    public Action OnSubscriptionExpired;
    public Action<bool> OnNoAdsStatusKnown;
    public Action<bool, long> OnXpGeneratorStatusChanged;

    public bool NoAdsSubscriptionActive { get; private set; }
    public bool XpGeneratorActive { get; private set; }
    public long XpGeneratorActiveUntil { get; private set; }

    private Dictionary<string, string> _itemSlots;
    private Dictionary<string, UserItemData> _defaultItems = new();
    private string _noAdsImageUrl = null;
    private Coroutine _subscriptionExpiryWatcher;
    private Coroutine _xpGeneratorTickCoroutine;

    private const long MOCK_SUB_RENEWAL_MS  =  2 * 60 * 1000; //  2 minutes per renewal period
    private const float XP_GENERATOR_POLL_INTERVAL_SECONDS = 2f;

    private struct MockSubscriptionEntry { public long start; public bool autoRenew; public long finalExpiry; }
    private readonly Dictionary<string, MockSubscriptionEntry> _mockSubscriptions = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _itemSlots = new Dictionary<string, string>();

        Instance = this;
    }

    public static string GetPlatformStoreId()
    {
        if (AppManager.MockPurchasesEnabled)
        {
            // In mock mode we bypass real stores — map to googlePlay or itunes so
            // products exist in the brainCloud inventory regardless of actual platform.
            switch (Application.platform)
            {
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "itunes";
                default:
                    return "googlePlay";
            }
        }

        switch (Application.platform)
        {
            case RuntimePlatform.Android:
                return "googlePlay";
            case RuntimePlatform.IPhonePlayer:
                return "itunes";
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
                return "itunes";
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
                return "steam";
            case RuntimePlatform.WSAPlayerX86:
            case RuntimePlatform.WSAPlayerX64:
            case RuntimePlatform.WSAPlayerARM:
                return "windowsPhone";
            default:
                return "googlePlay";
        }
    }

    public void GetEquippedItems(Action<Dictionary<string, string>> onComplete)
    {
        _itemSlots.Clear();
        BCManager.Instance.BCWrapper.ScriptService.RunScript("GetEquippedItems", "{}",
            (string responseJson, object cbObj) =>
            {
                var root = JsonReader.Deserialize<Dictionary<string, object>>(responseJson);
                var data = root["data"] as Dictionary<string, object>;
                var response = data["response"] as Dictionary<string, object>;

                bool error = Convert.ToBoolean(response["error"]);

                if (error)
                {
                    string message = response["message"] as string;
                    onComplete?.Invoke(null);
                }
                else
                {
                    Dictionary<string, object> equippedItems = response["equippedItems"] as Dictionary<string, object>;

                    foreach (var kvp in equippedItems)
                    {
                        _itemSlots.Add(kvp.Key, kvp.Value as string);
                    }

                    onComplete?.Invoke(_itemSlots);
                }
            },
            (int statusCode, int errorCode, string errorJson, object errorObj) =>
            {
                onComplete?.Invoke(null);
            });
    }

    public void FetchStoreItems(string storeId, Action<List<StoreItemData>> onSuccess, Action<string> onFailure)
    {
        var scriptData = new Dictionary<string, object>
        {
            { "storeId", storeId }
        };

        BCManager.Instance.BCWrapper.ScriptService.RunScript(
            "FetchStoreItems",
            JsonWriter.Serialize(scriptData),
            (string responseJson, object cbObject) =>
            {
                List<StoreItemData> allItems = ParseFetchStoreItemsResponse(responseJson);
                onSuccess?.Invoke(allItems);
            },
            (int statusCode, int responseCode, string errorJson, object errorCb) =>
            {
                onFailure?.Invoke(errorJson);
            });
    }

    private List<StoreItemData> ParseFetchStoreItemsResponse(string json)
    {
        var root = JsonReader.Deserialize<Dictionary<string, object>>(json);
        var data = root["data"] as Dictionary<string, object>;
        var response = data["response"] as Dictionary<string, object>;

        List<StoreItemData> allItems = new List<StoreItemData>();

        var freebiesArray = response["freebies"] as object[];
        if (freebiesArray != null)
            allItems.AddRange(ParseFreebieItems(freebiesArray));

        var catalogArray = response["catalogItems"] as object[];
        if (catalogArray != null)
            allItems.AddRange(ParseCatalogItems(catalogArray));

        var storeProductsArray = response["storeProducts"] as object[];
        if (storeProductsArray != null && storeProductsArray.Length > 0)
        {
            BCProduct[] bcProducts = BuildBCProducts(storeProductsArray);
            if (AppManager.MockPurchasesEnabled)
                BrainCloudMarketplace.SetMockProducts(bcProducts);
            else
                BrainCloudMarketplace.InitializeWithProducts(bcProducts);
            allItems.AddRange(ParseStoreProducts(storeProductsArray));
        }

        allItems.Sort((a, b) =>
        {
            int categoryCompare = GetCategoryOrder(a).CompareTo(GetCategoryOrder(b));
            if (categoryCompare != 0)
                return categoryCompare;

            // Only the "Products" (cash store) category needs a curated sub-order -
            // leave every other category's relative ordering untouched.
            if (a.category != "Products")
                return 0;

            int productOrderCompare = GetProductOrder(a).CompareTo(GetProductOrder(b));
            if (productOrderCompare != 0)
                return productOrderCompare;

            // Within the gems group, smallest amount first
            return a.itemAmount.CompareTo(b.itemAmount);
        });

        return allItems;
    }

    private BCProduct[] BuildBCProducts(object[] productsArray)
    {
        var result = new BCProduct[productsArray.Length];
        for (int i = 0; i < productsArray.Length; i++)
        {
            var dict = productsArray[i] as Dictionary<string, object>;
            var priceDict = dict["priceData"] as Dictionary<string, object>;

            // "id" is present for Android (Google Play ID); iOS uses an "ids" array instead.
            string priceId = priceDict.ContainsKey("id") ? priceDict["id"] as string : null;

            // Parse the "ids" array used by iOS (each entry has "appId" and "itunesId").
            Dictionary<string, string>[] idsArray = null;
            if (priceDict.ContainsKey("ids") && priceDict["ids"] is object[] rawIds)
            {
                idsArray = new Dictionary<string, string>[rawIds.Length];
                for (int j = 0; j < rawIds.Length; j++)
                {
                    var rawEntry = rawIds[j] as Dictionary<string, object>;
                    var entry = new Dictionary<string, string>();
                    if (rawEntry != null)
                    {
                        foreach (var kvp in rawEntry)
                            entry[kvp.Key] = kvp.Value as string;
                    }
                    idsArray[j] = entry;
                }
            }

            result[i] = new BCProduct
            {
                itemId   = dict["itemId"] as string,
                type     = dict.ContainsKey("type")     ? dict["type"] as string     : null,
                title    = dict.ContainsKey("title")    ? dict["title"] as string    : null,
                imageUrl = dict.ContainsKey("imageUrl") ? dict["imageUrl"] as string : null,
                payload  = dict.ContainsKey("payload")  ? dict["payload"] as string  : null,
                priceData = new BCPriceData
                {
                    id = priceId,
                    ids = idsArray,
                    referencePrice = Convert.ToInt32(priceDict["referencePrice"]),
                    isPromotion = Convert.ToBoolean(priceDict["isPromotion"])
                }
            };
        }
        return result;
    }

    private static int GetInventoryItemOrder(UserItemData item)
    {
        if (item.isBundle)      return 0;
        if (item.isActivatable) return 1;
        return 2;
    }

    /// <summary>
    /// Sub-ordering within the "Products" (cash store) category: gems offers first,
    /// then no_ads, then any other (non-consumable) product.
    /// </summary>
    private static int GetProductOrder(StoreItemData item)
    {
        if (item.isCurrency && item.rewardCurrency == CurrencyType.Gems) return 0;
        if (item.itemId == "no_ads") return 1;
        return 2;
    }

    private static int GetCategoryOrder(StoreItemData item)
    {
        switch (item.category)
        {
            case "Freebies":  return 0;
            case "Bundles":   return 1;
            case "Items":     return 2;
            case "Products":  return 3;
            default:          return 4;
        }
    }

    private List<StoreItemData> ParseStoreProducts(object[] productsArray)
    {
        List<StoreItemData> parsedItems = new List<StoreItemData>();

        if (productsArray == null || productsArray.Length == 0)
            return parsedItems;

        foreach (var productObj in productsArray)
        {
            var productDict = productObj as Dictionary<string, object>;
            var priceData = productDict["priceData"] as Dictionary<string, object>;
            var defaultPriceData = productDict.ContainsKey("defaultPriceData")
                ? productDict["defaultPriceData"] as Dictionary<string, object>
                : null;

            bool isPromotion = priceData != null && Convert.ToBoolean(priceData["isPromotion"]);
            decimal currentPrice = priceData != null ? Convert.ToDecimal(priceData["referencePrice"]) / 100m : 0m;
            decimal oldPrice = isPromotion && defaultPriceData != null
                ? Convert.ToDecimal(defaultPriceData["referencePrice"]) / 100m
                : 0m;

            string storeProductId = (priceData != null && priceData.ContainsKey("id"))
                ? priceData["id"] as string
                : productDict["itemId"] as string;

            string productItemId = productDict["itemId"] as string;
            string productImageUrl = productDict["imageUrl"] as string;

            if (productItemId == "no_ads")
                _noAdsImageUrl = productImageUrl;

            var currencyDict = productDict.ContainsKey("currency")
                ? productDict["currency"] as Dictionary<string, object>
                : null;

            CurrencyType rewardCurrency = CurrencyType.None;
            int itemAmount = 0;
            bool isCurrency = false;

            if (currencyDict != null && currencyDict.Count > 0)
            {
                isCurrency = true;
                if (currencyDict.ContainsKey("Gems"))
                {
                    rewardCurrency = CurrencyType.Gems;
                    itemAmount = Convert.ToInt32(currencyDict["Gems"]);
                }
                else if (currencyDict.ContainsKey("Coins"))
                {
                    rewardCurrency = CurrencyType.Coins;
                    itemAmount = Convert.ToInt32(currencyDict["Coins"]);
                }
            }

            bool productIsOwned = productDict.ContainsKey("isOwned") && Convert.ToBoolean(productDict["isOwned"]);

            // no_ads is a subscription — hide it only while the subscription is active
            if (productItemId == "no_ads" && NoAdsSubscriptionActive) continue;
            // All other owned non-consumables are hidden from the store
            if (productItemId != "no_ads" && productIsOwned) continue;

            parsedItems.Add(new StoreItemData
            {
                itemId = productItemId,
                defId = storeProductId,
                itemName = productDict.ContainsKey("title") ? productDict["title"] as string : string.Empty,
                message = productDict.ContainsKey("description") ? productDict["description"] as string : string.Empty,
                imageUrl = productImageUrl,
                category = "Products",
                itemType = ItemType.Product,
                buyPrices = new Dictionary<CurrencyType, decimal>(),
                currentPrice = currentPrice,
                oldPrice = oldPrice,
                isOnPromotion = isPromotion,
                isFree = false,
                isCurrency = isCurrency,
                rewardCurrency = rewardCurrency,
                itemAmount = itemAmount,
                isOwned = productIsOwned,
            });
        }

        return parsedItems;
    }

    private List<StoreItemData> ParseCatalogItems(object[] itemsArray)
    {
        List<StoreItemData> parsedItems = new List<StoreItemData>();

        if (itemsArray == null || itemsArray.Length == 0)
        {
            Debug.LogWarning("[Inventory] Catalog items array was null or empty");
            return parsedItems;
        }

        foreach (var itemObj in itemsArray)
        {
            CurrencyType rewardCurrency = CurrencyType.None;
            int rewardAmount = 0;

            var itemDict = itemObj as Dictionary<string, object>;
            var meta = itemDict["meta"] as Dictionary<string, object>;

            string itemTypeString = itemDict["type"] as string;
            string category = itemDict["category"] as string;
            string itemDefId = itemDict["defId"] as string;

            int activeSeconds = 0;

            ItemType itemType = ItemType.Item;
            switch (itemTypeString)
            {
                case "ITEM":
                    itemType = ItemType.Item;
                    break;
                case "BUNDLE":
                    itemType = ItemType.Bundle;
                    break;
            }

            bool autoOpen = false;
            if (itemType == ItemType.Bundle && meta != null && meta.ContainsKey("autoOpen"))
            {
                var val = meta["autoOpen"];
                autoOpen = val is bool b ? b : string.Equals(val as string, "true", StringComparison.OrdinalIgnoreCase);
            }

            if (itemDefId == "coin_multiplier")
            {
                itemType = ItemType.Multiplier;
                activeSeconds = Convert.ToInt32(itemDict["activeSecs"]);
            }

            if (category == "Freebies")
                continue;

            if (itemDict.ContainsKey("currency"))
            {
                var currencyData = itemDict["currency"] as Dictionary<string, object>;

                if (currencyData != null)
                {
                    foreach (KeyValuePair<string, object> entry in currencyData)
                    {
                        rewardAmount = Convert.ToInt32(entry.Value);

                        if (Enum.TryParse(entry.Key, out CurrencyType curType))
                        {
                            rewardCurrency = curType;
                        }
                    }
                }
            }

            var buyPriceData = itemDict["buyPrice"] as Dictionary<string, object>;
            Dictionary<CurrencyType, decimal> buyPrices = new();

            bool isPromotion = buyPriceData.ContainsKey("isPromotion") && Convert.ToBoolean(buyPriceData["isPromotion"]);

            foreach (KeyValuePair<string, object> kvp in buyPriceData)
            {
                if (Enum.TryParse(kvp.Key, out CurrencyType curType))
                {
                    buyPrices[curType] = Convert.ToDecimal(kvp.Value);
                }
            }

            decimal oldPrice = 0m;
            if (isPromotion && itemDict.ContainsKey("defaultBuyPrice"))
            {
                var defaultBuyPriceData = itemDict["defaultBuyPrice"] as Dictionary<string, object>;
                if (defaultBuyPriceData != null)
                {
                    foreach (KeyValuePair<string, object> kvp in defaultBuyPriceData)
                    {
                        if (Enum.TryParse(kvp.Key, out CurrencyType _))
                        {
                            oldPrice = Convert.ToDecimal(kvp.Value);
                            break;
                        }
                    }
                }
            }

            bool isStackable = itemDict.ContainsKey("stackable") && Convert.ToBoolean(itemDict["stackable"]);
            int maxStackable = itemDict.ContainsKey("maxStackable") ? Convert.ToInt32(itemDict["maxStackable"]) : 0;
            int inventoryAmount = itemDict.ContainsKey("inventoryAmount") ? Convert.ToInt32(itemDict["inventoryAmount"]) : 0;

            var catalogItem = new StoreItemData
            {
                itemId = string.Empty,
                itemName = itemDict["name"] as string,
                message = itemDict["desc"] as string,
                defId = itemDefId,
                imageUrl = itemDict["image"] as string,
                quantity = 0,
                usesLeft = 0,
                maxUses = 0,
                recoveryUntil = -1,
                coolDownUntil = -1,
                itemType = itemType,
                category = category,
                buyPrices = buyPrices,
                rewardCurrency = rewardCurrency,
                isCurrency = rewardCurrency != CurrencyType.None,
                itemAmount = rewardAmount,
                isFree = buyPrices.Count == 0,
                isOnCooldown = false,
                isOnPromotion = isPromotion,
                oldPrice = oldPrice,
                activeSeconds = activeSeconds,
                autoOpen = autoOpen,
                isStackable = isStackable,
                maxStackable = maxStackable,
                inventoryAmount = inventoryAmount
            };

            if (!catalogItem.IsOwned)
                parsedItems.Add(catalogItem);
        }

        return parsedItems;
    }

    private List<StoreItemData> ParseFreebieItems(object[] freebiesArray)
    {
        List<StoreItemData> parsedItems = new List<StoreItemData>();

        if (freebiesArray == null || freebiesArray.Length == 0)
        {
            Debug.LogWarning("[Inventory] Freebies array was null or empty");
            return parsedItems;
        }

        foreach (var itemObj in freebiesArray)
        {
            var itemDict = itemObj as Dictionary<string, object>;
            var itemDef = itemDict["itemDef"] as Dictionary<string, object>;
            var meta = itemDef["meta"] as Dictionary<string, object>;

            CurrencyType rewardCurrency = CurrencyType.None;
            int rewardAmount = 0;

            if (meta != null && meta.ContainsKey("currency"))
            {
                switch (meta["currency"] as string)
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

            int freebieQuantity = Convert.ToInt32(itemDict["quantity"]);
            bool freebieStackable = itemDef.ContainsKey("stackable") && Convert.ToBoolean(itemDef["stackable"]);
            int freebieMaxStackable = itemDef.ContainsKey("maxStackable") ? Convert.ToInt32(itemDef["maxStackable"]) : 0;

            parsedItems.Add(new StoreItemData
            {
                itemName = itemDef["name"] as string,
                message = itemDef["desc"] as string,
                itemId = itemDict["itemId"] as string,
                defId = itemDict["defId"] as string,
                imageUrl = itemDef["image"] as string,
                quantity = freebieQuantity,
                usesLeft = itemDict["usesLeft"] != null ? Convert.ToInt32(itemDict["usesLeft"]) : 0,
                maxUses = itemDict["maxUses"] != null ? Convert.ToInt32(itemDict["maxUses"]) : 0,
                itemType = ItemType.Freebie,
                recoveryUntil = recoveryUntil,
                coolDownUntil = Convert.ToInt64(itemDict["coolDownUntil"]),
                category = itemDef["category"] as string,
                buyPrices = buyPrices,
                rewardCurrency = rewardCurrency,
                isCurrency = rewardCurrency != CurrencyType.None,
                itemAmount = rewardAmount,
                isFree = buyPrices.Count == 0,
                isOnCooldown = recoveryUntil != -1,
                isStackable = freebieStackable,
                maxStackable = freebieMaxStackable,
                inventoryAmount = freebieQuantity
            });
        }

        return parsedItems;
    }

    /// <summary>
    /// Registers a mock subscription for <paramref name="productId"/> that auto-renews every
    /// <see cref="MOCK_SUB_RENEWAL_MS"/> (2 min) indefinitely until the user unsubscribes via
    /// <see cref="UnsubscribeMockSubscription"/>. Persists start/auto-renew state to brainCloud
    /// user attributes so the subscription survives app restarts.
    /// </summary>
    public void RegisterMockSubscription(string productId)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var entry = new MockSubscriptionEntry
        {
            start       = now,
            autoRenew   = true,
            finalExpiry = 0
        };
        _mockSubscriptions[productId] = entry;
        Debug.Log($"[Mock Sub] Registered '{productId}' — auto-renewing every {MOCK_SUB_RENEWAL_MS / 60000} min.");

        BCManager.Instance.BCWrapper.PlayerStateService.UpdateAttributes(
            JsonWriter.Serialize(new Dictionary<string, object>
            {
                { $"mockSubStart_{productId}",      entry.start.ToString() },
                { $"mockSubAutoRenew_{productId}",   "true"                },
                { $"mockSubFinalExpiry_{productId}", "0"                   }
            }),
            false,
            (string _, object __) => Debug.Log($"[Mock Sub] Persisted '{productId}' timing to user attributes."),
            (int _, int __, string err, object ___) => Debug.LogError($"[Mock Sub] Failed to persist timing: {err}"));
    }

    /// <summary>
    /// Turns off auto-renew for a mock subscription (Windows/Mac mock-purchase mode). The
    /// subscription stays active through the period already paid for, then expires and the
    /// product becomes purchasable again instead of renewing. Reports the frozen expiry
    /// (period end) back to the caller so displayed UI can show the correct date, since the
    /// caller's own copy of the expiry may be stale by the time the user clicks unsubscribe.
    /// </summary>
    public void UnsubscribeMockSubscription(string productId, Action<long> onComplete = null)
    {
        if (!_mockSubscriptions.TryGetValue(productId, out var entry))
        {
            Debug.LogWarning($"[Mock Sub] Tried to unsubscribe from '{productId}' but no active mock subscription was found.");
            onComplete?.Invoke(0);
            return;
        }

        long now       = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long elapsed   = now - entry.start;
        long periodEnd = entry.start + ((elapsed / MOCK_SUB_RENEWAL_MS) + 1) * MOCK_SUB_RENEWAL_MS;

        entry.autoRenew   = false;
        entry.finalExpiry = periodEnd;
        _mockSubscriptions[productId] = entry;

        Debug.Log($"[Mock Sub] Auto-renew disabled for '{productId}' — will expire at period end.");

        BCManager.Instance.BCWrapper.PlayerStateService.UpdateAttributes(
            JsonWriter.Serialize(new Dictionary<string, object>
            {
                { $"mockSubAutoRenew_{productId}",   "false" },
                { $"mockSubFinalExpiry_{productId}", periodEnd.ToString() }
            }),
            false,
            (string _, object __) =>
            {
                Debug.Log($"[Mock Sub] Persisted unsubscribe state for '{productId}'.");
                onComplete?.Invoke(periodEnd);
            },
            (int _, int __, string err, object ___) =>
            {
                Debug.LogError($"[Mock Sub] Failed to persist unsubscribe state: {err}");
                onComplete?.Invoke(periodEnd);
            });
    }

    private void GetMockSubscriptionStatus(string productId, Action<bool, long, bool> onComplete)
    {
        // If already loaded in memory, evaluate immediately
        if (_mockSubscriptions.TryGetValue(productId, out var cached))
        {
            EvaluateMockSubscription(productId, cached, onComplete);
            return;
        }

        // Not in memory (fresh app start) — load persisted timestamps from user attributes
        BCManager.Instance.BCWrapper.PlayerStateService.GetAttributes(
            (string attrJson, object _) =>
            {
                var attrData = (JsonReader.Deserialize<Dictionary<string, object>>(attrJson)["data"]
                    as Dictionary<string, object>)["attributes"] as Dictionary<string, object>;

                string startKey       = $"mockSubStart_{productId}";
                string autoRenewKey   = $"mockSubAutoRenew_{productId}";
                string finalExpiryKey = $"mockSubFinalExpiry_{productId}";

                if (attrData != null && attrData.TryGetValue(startKey, out var rawStart))
                {
                    try
                    {
                        var entry = new MockSubscriptionEntry
                        {
                            start = Convert.ToInt64(rawStart),
                            autoRenew = !attrData.TryGetValue(autoRenewKey, out var rawAutoRenew)
                                || Convert.ToString(rawAutoRenew) == "true",
                            finalExpiry = attrData.TryGetValue(finalExpiryKey, out var rawFinalExpiry)
                                ? Convert.ToInt64(rawFinalExpiry)
                                : 0
                        };
                        _mockSubscriptions[productId] = entry;
                        EvaluateMockSubscription(productId, entry, onComplete);
                    }
                    catch
                    {
                        onComplete?.Invoke(false, 0, false);
                    }
                }
                else
                {
                    onComplete?.Invoke(false, 0, false);
                }
            },
            (int _, int __, string ___, object ____) => onComplete?.Invoke(false, 0, false));
    }

    private void EvaluateMockSubscription(string productId, MockSubscriptionEntry sub, Action<bool, long, bool> onComplete)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (!sub.autoRenew)
        {
            // Unsubscribed - stays active only through the period already paid for.
            if (now >= sub.finalExpiry)
            {
                _mockSubscriptions.Remove(productId);
                onComplete?.Invoke(false, 0, false);
                return;
            }

            onComplete?.Invoke(true, sub.finalExpiry, false);
            return;
        }

        // Auto-renewing indefinitely - always active, expiry is the next renewal boundary.
        long elapsed   = now - sub.start;
        long nextBound = sub.start + ((elapsed / MOCK_SUB_RENEWAL_MS) + 1) * MOCK_SUB_RENEWAL_MS;

        onComplete?.Invoke(true, nextBound, true);
    }

    /// <summary>
    /// Reports (isActive, expiryTimeMs, isAutoRenewing) for the no_ads subscription.
    /// </summary>
    public void GetNoAdsSubscriptionStatus(Action<bool, long, bool> onComplete)
    {
        if (AppManager.MockPurchasesEnabled)
        {
            GetMockSubscriptionStatus("no_ads", onComplete);
            return;
        }

#if UNITY_ANDROID
        BCManager.Instance.BCWrapper.ScriptService.RunScript(
            "VerifyGoogleSubscription",
            "{}",
            (string responseJson, object cbObject) =>
            {
                ParseSubscriptionScriptResponse(responseJson, onComplete);
            },
            (int statusCode, int responseCode, string errorJson, object errorCb) =>
            {
                onComplete?.Invoke(false, 0, false);
            });
#elif UNITY_IOS || UNITY_STANDALONE_OSX
        // Fetch the receipt we stored at purchase time from BC user attributes,
        // then forward it to the VerifyAppleSubscription cloud script.
        BCManager.Instance.BCWrapper.PlayerStateService.GetAttributes(
            (string attrResponseJson, object cbObject) =>
            {
                var attrRoot = JsonReader.Deserialize<Dictionary<string, object>>(attrResponseJson);
                var attrData = attrRoot["data"] as Dictionary<string, object>;
                var attributes = attrData["attributes"] as Dictionary<string, object>;

                if (attributes == null || !attributes.ContainsKey("appleReceipt_no_ads"))
                {
                    onComplete?.Invoke(false, 0, false);
                    return;
                }

                string receipt = attributes["appleReceipt_no_ads"] as string;
                var scriptData = new Dictionary<string, object> { { "receiptData", receipt } };

                BCManager.Instance.BCWrapper.ScriptService.RunScript(
                    "VerifyAppleSubscription",
                    JsonWriter.Serialize(scriptData),
                    (string responseJson, object scriptCbObject) =>
                    {
                        ParseSubscriptionScriptResponse(responseJson, onComplete);
                    },
                    (int statusCode, int responseCode, string errorJson, object errorCb) =>
                    {
                        onComplete?.Invoke(false, 0, false);
                    });
            },
            (int statusCode, int responseCode, string errorJson, object errorCb) =>
            {
                onComplete?.Invoke(false, 0, false);
            });
#else
        // Steam does not have a subscription model for in-game content;
        // no_ads on Steam is a non-consumable one-time purchase.
        onComplete?.Invoke(false, 0, false);
#endif
    }

    private void StartSubscriptionExpiryWatcher(long expiryTimeMs)
    {
        if (_subscriptionExpiryWatcher != null)
            StopCoroutine(_subscriptionExpiryWatcher);

        _subscriptionExpiryWatcher = StartCoroutine(SubscriptionExpiryWatcherCoroutine(expiryTimeMs));
    }

    private IEnumerator SubscriptionExpiryWatcherCoroutine(long expiryTimeMs)
    {
        // Sleep in 60-second realtime chunks. Using WaitForSecondsRealtime means the
        // wait is unaffected by Time.timeScale, and re-checking every 60 seconds means
        // we correctly detect expiry shortly after the app resumes from suspension
        // (Time.unscaledTime doesn't advance while the app is suspended).
        while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < expiryTimeMs)
        {
            long msRemaining = expiryTimeMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            float waitSeconds = Mathf.Min((float)(msRemaining / 1000.0), 60f);
            yield return new WaitForSecondsRealtime(waitSeconds);
        }

        Debug.Log("[InventoryService] Subscription expiry time reached — re-verifying...");

        GetNoAdsSubscriptionStatus((isActive, newExpiryMs, isAutoRenewing) =>
        {
            if (!isActive)
            {
                Debug.Log("[InventoryService] Subscription confirmed expired.");
                NoAdsSubscriptionActive = false;
                _subscriptionExpiryWatcher = null;
                OnSubscriptionExpired?.Invoke();
            }
            else
            {
                // Subscription is still active - either it renewed to a new period (autoRenew)
                // or it's coasting through the final period after being unsubscribed. Restart
                // the watcher against the current expiry either way.
                Debug.Log("[InventoryService] Subscription still active, restarting expiry watcher.");
                StartSubscriptionExpiryWatcher(newExpiryMs);
            }

            // Refresh listeners (inventory cards, store) so displayed expiry/auto-renew
            // state and product availability stay current instead of showing stale data.
            OnItemBought?.Invoke();
        });
    }

    /// <summary>
    /// Result of a <see cref="CollectXpGeneratorXP"/> call. Deliberately does not get applied
    /// to local user data automatically - callers decide when that should happen (e.g.
    /// immediately for live in-session ticking, or deferred until a "Collect" modal is
    /// dismissed for the offline-return flow) since applying it changes what the XP bar and
    /// level text show.
    /// </summary>
    public struct XpGeneratorCollectResult
    {
        public int xpAwarded;
        public bool isActive;
        public long activeUntil;
        public bool hasXpProgress;
        public bool xpCapped;
        public int experienceLevel;
        public int xpToNextLevel;
        public string statusTitle;
        public int adjustedXp;
    }

    /// <summary>
    /// Calls the server to collect any XP accrued by the xp_generator status effect since it
    /// was last collected. Reports how much was awarded, whether the status is still active
    /// (and until when), and the resulting level/XP progress - without applying any of it to
    /// local user data (see <see cref="XpGeneratorCollectResult"/> and <see cref="ApplyXpGeneratorResult"/>).
    /// </summary>
    public void CollectXpGeneratorXP(Action<XpGeneratorCollectResult> onComplete)
    {
        BCManager.Instance.BCWrapper.ScriptService.RunScript("CollectXpGeneratorXP", "{}",
            (string responseJson, object cbObj) =>
            {
                var response = (JsonReader.Deserialize<Dictionary<string, object>>(responseJson)["data"] as Dictionary<string, object>)["response"] as Dictionary<string, object>;

                var result = new XpGeneratorCollectResult
                {
                    xpAwarded = response.ContainsKey("xpAwarded") ? Convert.ToInt32(response["xpAwarded"]) : 0,
                    isActive = response.ContainsKey("isActive") && Convert.ToBoolean(response["isActive"]),
                    activeUntil = response.ContainsKey("activeUntil") ? Convert.ToInt64(response["activeUntil"]) : 0
                };

                if (result.xpAwarded > 0 && response.ContainsKey("adjustedXp"))
                {
                    result.hasXpProgress = true;
                    result.xpCapped = response.ContainsKey("xpCapped") && Convert.ToBoolean(response["xpCapped"]);
                    result.experienceLevel = Convert.ToInt32(response["experienceLevel"]);
                    result.xpToNextLevel = Convert.ToInt32(response["xpToNextLevel"]);
                    result.statusTitle = response["statusTitle"] as string;
                    result.adjustedXp = Convert.ToInt32(response["adjustedXp"]);
                }

                onComplete?.Invoke(result);
            },
            (int statusCode, int responseCode, string errorJson, object errorObj) =>
            {
                Debug.LogError("Failed to collect xp_generator XP: " + errorJson);
                onComplete?.Invoke(new XpGeneratorCollectResult());
            });
    }

    /// <summary>
    /// Applies a previously-fetched <see cref="XpGeneratorCollectResult"/> to local user data,
    /// updating the level/XP bar (and triggering a level-up modal if it crossed a level).
    /// </summary>
    public static void ApplyXpGeneratorResult(XpGeneratorCollectResult result)
    {
        if (!result.hasXpProgress)
            return;

        AppManager.Instance.userData.XPCapped = result.xpCapped;
        AppManager.Instance.UpdateUserLevel(result.experienceLevel, result.statusTitle, result.xpToNextLevel);
        AppManager.Instance.UpdateUserXP(result.adjustedXp);
    }

    /// <summary>
    /// Starts (or restarts) live in-session polling for the xp_generator boost so the XP bar
    /// visibly fills in near-real-time while it's active, instead of only catching up the
    /// next time the app is opened. Polls every <see cref="XP_GENERATOR_POLL_INTERVAL_SECONDS"/>
    /// until <paramref name="activeUntilMs"/> is reached.
    /// </summary>
    public void StartXpGeneratorTracking(long activeUntilMs)
    {
        if (_xpGeneratorTickCoroutine != null)
            StopCoroutine(_xpGeneratorTickCoroutine);

        _xpGeneratorTickCoroutine = StartCoroutine(XpGeneratorTickCoroutine(activeUntilMs));
    }

    private IEnumerator XpGeneratorTickCoroutine(long activeUntilMs)
    {
        XpGeneratorActive = true;
        XpGeneratorActiveUntil = activeUntilMs;
        OnXpGeneratorStatusChanged?.Invoke(true, activeUntilMs);

        while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < activeUntilMs)
        {
            yield return new WaitForSecondsRealtime(XP_GENERATOR_POLL_INTERVAL_SECONDS);
            CollectXpGeneratorXP(ApplyXpGeneratorResult);
        }

        // One final collect to mop up any remainder after the window has closed.
        CollectXpGeneratorXP(ApplyXpGeneratorResult);

        XpGeneratorActive = false;
        XpGeneratorActiveUntil = 0;
        _xpGeneratorTickCoroutine = null;
        OnXpGeneratorStatusChanged?.Invoke(false, 0);
    }

    /// <summary>
    /// Builds the no_ads subscription's inventory description: "Renews on ..." while it will
    /// auto-renew, or "Expires on ..." once auto-renew has been turned off.
    /// </summary>
    public static string BuildSubscriptionDescription(long expiryTimeMs, bool isAutoRenewing)
    {
        var expiry = DateTimeOffset.FromUnixTimeMilliseconds(expiryTimeMs).LocalDateTime;
        string verb = isAutoRenewing ? "Renews" : "Expires";
        return $"{verb} on {expiry:MMM d, yyyy h:mm tt}";
    }

    private static void ParseSubscriptionScriptResponse(string responseJson, Action<bool, long, bool> onComplete)
    {
        var root = JsonReader.Deserialize<Dictionary<string, object>>(responseJson);
        var data = root["data"] as Dictionary<string, object>;
        var response = data["response"] as Dictionary<string, object>;

        bool error = Convert.ToBoolean(response["error"]);
        if (error)
        {
            onComplete?.Invoke(false, 0, false);
            return;
        }

        bool isActive = Convert.ToBoolean(response["isActive"]);
        long expiryTimeMs = isActive ? Convert.ToInt64(response["expiryTimeMs"]) : 0;

        bool isAutoRenewing = false;
        if (isActive)
        {
            if (response.ContainsKey("autoRenewing"))
                isAutoRenewing = Convert.ToBoolean(response["autoRenewing"]);
            else if (response.ContainsKey("autoRenewStatus"))
                isAutoRenewing = Convert.ToString(response["autoRenewStatus"]) == "1";
        }

        onComplete?.Invoke(isActive, expiryTimeMs, isAutoRenewing);
    }

    public void GetUserInventoryItems(Action<List<UserItemData>> onSuccess, Action<string> onFailure, bool refreshEquipped = true)
    {
        List<UserItemData> allItems = new List<UserItemData>();

        void fetchItems()
        {
            _defaultItems.Clear();
            Debug.Log("Equipped slots: " + JsonWriter.Serialize(_itemSlots));
            FetchInventoryItemPageRecursive(
                pageNumber: 1,
                accumulatedItems: allItems,
                onSuccess: (items) =>
                {
                    GetNoAdsSubscriptionStatus((isActive, expiryTimeMs, isAutoRenewing) =>
                    {
                        NoAdsSubscriptionActive = isActive;
                        OnNoAdsStatusKnown?.Invoke(isActive);

                        if (isActive)
                        {
                            items.Add(new UserItemData
                            {
                                itemId = "subscription_no_ads",
                                defId = "no_ads",
                                itemName = "No Ads",
                                description = BuildSubscriptionDescription(expiryTimeMs, isAutoRenewing),
                                category = "Subscriptions",
                                imageUrl = _noAdsImageUrl ?? string.Empty,
                                equippableSlot = string.Empty,
                                isSubscription = true,
                                isAutoRenewing = isAutoRenewing,
                                subscriptionExpiryMs = expiryTimeMs
                            });
                            StartSubscriptionExpiryWatcher(expiryTimeMs);
                        }
                        items.Sort((a, b) => GetInventoryItemOrder(a).CompareTo(GetInventoryItemOrder(b)));
                        onSuccess?.Invoke(items);
                    });
                },
                onFailure: onFailure
            );
        }

        if (refreshEquipped)
            GetEquippedItems((_) => fetchItems());
        else
            fetchItems();
    }

    private void FetchInventoryItemPageRecursive(
        int pageNumber,
        List<UserItemData> accumulatedItems,
        Action<List<UserItemData>> onSuccess,
        Action<string> onFailure)
    {
        if (accumulatedItems == null)
            accumulatedItems = new List<UserItemData>();

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
                var pageResult = ParseInventoryItems(responseJson);

                accumulatedItems.AddRange(pageResult.Items);

                if (pageResult.MoreAfter)
                {
                    FetchInventoryItemPageRecursive(
                        pageNumber + 1,
                        accumulatedItems,
                        onSuccess,
                        onFailure
                    );
                }
                else
                {
                    onSuccess?.Invoke(accumulatedItems);
                }
            },
            (int statusCode, int responseCode, string errorJson, object errorCb) =>
            {
                onFailure?.Invoke(errorJson);
            }
        );
    }

    private class InventoryPageParseResult
    {
        public List<UserItemData> Items;
        public bool MoreAfter;
    }

    private InventoryPageParseResult ParseInventoryItems(string json)
    {
        var root = JsonReader.Deserialize<Dictionary<string, object>>(json);
        var data = root["data"] as Dictionary<string, object>;
        var results = data["results"] as Dictionary<string, object>;

        bool moreAfter = Convert.ToBoolean(results["moreAfter"]);
        var itemsArray = results["items"] as object[];

        List<UserItemData> parsedItems = new List<UserItemData>();

        if (itemsArray == null || itemsArray.Length == 0)
        {
            Debug.LogWarning("[Inventory] Inventory items array was null or empty");
            return new InventoryPageParseResult
            {
                Items = parsedItems,
                MoreAfter = false
            };
        }

        foreach (var itemObj in itemsArray)
        {
            var itemDict = itemObj as Dictionary<string, object>;
            var itemDef = itemDict["itemDef"] as Dictionary<string, object>;

            string itemCategory = itemDef["category"] as string;

            if (itemCategory == "Freebies")
                continue;

            bool isBundle = string.Equals(itemDict["type"] as string, "BUNDLE", StringComparison.OrdinalIgnoreCase);

            var meta = itemDef.ContainsKey("meta") ? itemDef["meta"] as Dictionary<string, object> : null;

            bool isEquippable = false;
            string equippableSlot = "";
            bool isDefault = false;
            bool autoEquip = false;
            if (meta != null && meta.ContainsKey("equippable"))
            {
                string equippableStr = meta["equippable"] as string;
                isEquippable = string.Equals(equippableStr, "true", StringComparison.OrdinalIgnoreCase);
                equippableSlot = meta["equippableSlot"] as string;

                if (meta.ContainsKey("isDefault"))
                    isDefault = string.Equals(meta["isDefault"] as string, "true", StringComparison.OrdinalIgnoreCase);

                if (meta.ContainsKey("autoEquip"))
                    autoEquip = string.Equals(meta["autoEquip"] as string, "true", StringComparison.OrdinalIgnoreCase);
            }

            bool isActivatable = itemDef.ContainsKey("activatable") && Convert.ToBoolean(itemDef["activatable"]);

            int activeSeconds = itemDef.ContainsKey("activeSecs") ? Convert.ToInt32(itemDef["activeSecs"]) : 0;


            var sellPriceData = itemDef["sellPrice"] as Dictionary<string, object>;

            CurrencyType sellCurrencyType = CurrencyType.None;
            int sellAmount = 0;

            foreach (var kvp in sellPriceData)
            {
                if (Enum.TryParse(kvp.Key, out CurrencyType currency))
                {
                    sellCurrencyType = currency;
                    sellAmount = Convert.ToInt32(kvp.Value);
                }
            }

            bool isSellable = sellCurrencyType != CurrencyType.None;
            string itemId = itemDict["itemId"] as string;
            bool isEquipped = false;

            if (isEquippable && _itemSlots.ContainsKey(equippableSlot))
            {
                if (_itemSlots[equippableSlot] == itemId)
                {
                    //this item is equipped
                    isEquipped = true;
                }
            }

            var userItem = new UserItemData
            {
                itemId = itemId,
                defId = itemDict["defId"] as string,
                itemName = itemDef["name"] as string,
                description = itemDef["desc"] as string,
                category = itemCategory,
                imageUrl = itemDef["image"] as string,
                quantity = Convert.ToInt32(itemDict["quantity"]),
                isEquippable = isEquippable,
                equippableSlot = equippableSlot,
                isEquipped = isEquipped,
                isSellable = isSellable,
                sellCurrency = sellCurrencyType,
                sellAmount = sellAmount,
                isStackable = Convert.ToBoolean(itemDef["stackable"]),
                maxStackable = Convert.ToInt32(itemDef["maxStackable"]),
                isDefault = isDefault,
                autoEquip = autoEquip,
                isBundle = isBundle,
                isActivatable = isActivatable,
                activeSeconds = activeSeconds
            };
            parsedItems.Add(userItem);

            if (isDefault && isEquippable && !string.IsNullOrEmpty(equippableSlot))
                _defaultItems[equippableSlot] = userItem;
        }

        return new InventoryPageParseResult
        {
            Items = parsedItems,
            MoreAfter = moreAfter
        };
    }

    public UserItemData ParseGainedItem(string gainedItemId, Dictionary<string, object> gainedItemDict)
    {
        if (gainedItemDict == null) return null;

        var itemDef = gainedItemDict["itemDef"] as Dictionary<string, object>;
        if (itemDef == null) return null;

        string itemCategory = itemDef["category"] as string;
        if (itemCategory == "Freebies") return null;

        var meta = itemDef.ContainsKey("meta") ? itemDef["meta"] as Dictionary<string, object> : null;

        bool isEquippable = false;
        string equippableSlot = "";
        bool isDefault = false;
        bool autoEquip = false;
        if (meta != null && meta.ContainsKey("equippable"))
        {
            string equippableStr = meta["equippable"] as string;
            isEquippable = string.Equals(equippableStr, "true", StringComparison.OrdinalIgnoreCase);
            equippableSlot = meta["equippableSlot"] as string;

            if (meta.ContainsKey("isDefault"))
                isDefault = string.Equals(meta["isDefault"] as string, "true", StringComparison.OrdinalIgnoreCase);

            if (meta.ContainsKey("autoEquip"))
                autoEquip = string.Equals(meta["autoEquip"] as string, "true", StringComparison.OrdinalIgnoreCase);
        }

        bool isActivatable = itemDef.ContainsKey("activatable") && Convert.ToBoolean(itemDef["activatable"]);

        var sellPriceData = itemDef["sellPrice"] as Dictionary<string, object>;
        CurrencyType sellCurrencyType = CurrencyType.None;
        int sellAmount = 0;
        if (sellPriceData != null)
        {
            foreach (var kvp in sellPriceData)
            {
                if (Enum.TryParse(kvp.Key, out CurrencyType currency))
                {
                    sellCurrencyType = currency;
                    sellAmount = Convert.ToInt32(kvp.Value);
                }
            }
        }

        string itemId = gainedItemId ?? (gainedItemDict.ContainsKey("itemId") ? gainedItemDict["itemId"] as string : null);
        if (string.IsNullOrEmpty(itemId)) return null;

        int activeSeconds = itemDef.ContainsKey("activeSecs") ? Convert.ToInt32(itemDef["activeSecs"]) : 0;

        return new UserItemData
        {
            itemId = itemId,
            defId = gainedItemDict.ContainsKey("defId") ? gainedItemDict["defId"] as string : string.Empty,
            itemName = itemDef["name"] as string,
            description = itemDef["desc"] as string,
            category = itemCategory,
            imageUrl = itemDef["image"] as string,
            quantity = gainedItemDict.ContainsKey("quantity") ? Convert.ToInt32(gainedItemDict["quantity"]) : 1,
            isEquippable = isEquippable,
            equippableSlot = equippableSlot,
            isEquipped = false,
            isSellable = sellCurrencyType != CurrencyType.None,
            sellCurrency = sellCurrencyType,
            sellAmount = sellAmount,
            isStackable = Convert.ToBoolean(itemDef["stackable"]),
            maxStackable = Convert.ToInt32(itemDef["maxStackable"]),
            isDefault = isDefault,
            autoEquip = autoEquip,
            isActivatable = isActivatable,
            activeSeconds = activeSeconds
        };
    }

    public void ToggleItemEquipped(UserItemData item, bool equip, Action<bool> onComplete)
    {
        Dictionary<string, object> scriptData = new();

        scriptData["itemId"] = item.itemId;
        scriptData["equip"] = equip;

        BCManager.Instance.BCWrapper.ScriptService.RunScript("EquipItem", JsonWriter.Serialize(scriptData),
            (string responseJson, object cbObject) =>
            {
                var root = JsonReader.Deserialize<Dictionary<string, object>>(responseJson)["data"] as Dictionary<string, object>;
                var response = root["response"] as Dictionary<string, object>;

                bool opSuccess = Convert.ToBoolean(response["success"]);

                if (opSuccess)
                {
                    if (equip)
                        _itemSlots[item.equippableSlot] = item.itemId;
                    else if (_itemSlots.ContainsKey(item.equippableSlot) && _itemSlots[item.equippableSlot] == item.itemId)
                        _itemSlots.Remove(item.equippableSlot);
                }

                onComplete?.Invoke(opSuccess);
            },
            (int statusCode, int errorCode, string errorJson, object errorObj) =>
            {
                onComplete?.Invoke(false);
            });
    }

    public void EquipDefaultItemForSlot(string slot, Action onComplete)
    {
        if (!_defaultItems.TryGetValue(slot, out UserItemData defaultItem))
        {
            onComplete?.Invoke();
            return;
        }

        ToggleItemEquipped(defaultItem, true, (bool success) =>
        {
            if (success)
            {
                defaultItem.isEquipped = true;
                OnItemEquipChange?.Invoke(defaultItem);
            }
            onComplete?.Invoke();
        });
    }

    public void SellItem(UserItemData item, Action<CurrencyType, int> onComplete)
    {
        Dictionary<string, object> scriptData = new();

        scriptData["itemId"] = item.itemId;

        BCManager.Instance.BCWrapper.ScriptService.RunScript("SellItem", JsonWriter.Serialize(scriptData),
            (string responseJson, object cbObject) =>
            {
                var root = JsonReader.Deserialize<Dictionary<string, object>>(responseJson)["data"] as Dictionary<string, object>;
                var response = root["response"] as Dictionary<string, object>;
                var currencyRefunded = response["currencyRefunded"] as Dictionary<string, object>;

                bool opSuccess = Convert.ToBoolean(response["success"]);

                if (opSuccess)
                {
                    if (currencyRefunded.ContainsKey("Gems"))
                    {
                        int GemsRefunded = Convert.ToInt32(currencyRefunded["Gems"]);
                        AppManager.Instance.AddGems(GemsRefunded);
                        onComplete?.Invoke(CurrencyType.Gems, GemsRefunded);
                    }
                    else if (currencyRefunded.ContainsKey("Coins"))
                    {
                        int CoinsRefunded = Convert.ToInt32(currencyRefunded["Coins"]);
                        AppManager.Instance.AddCoins(CoinsRefunded);
                        onComplete?.Invoke(CurrencyType.Coins, CoinsRefunded);
                    }
                    else
                    {
                        onComplete?.Invoke(CurrencyType.None, 0);
                    }

                    if (item.isEquippable && item.isEquipped)
                    {
                        ToggleItemEquipped(item, false, (success) =>
                        {
                            item.isEquipped = false;
                            OnItemEquipChange(item);
                        });
                    }

                    OnItemSold(item);
                }
                else
                {
                    Debug.LogError("Script SellItem failed to sell item " + response["message"] as string);
                }
            });
    }
}
