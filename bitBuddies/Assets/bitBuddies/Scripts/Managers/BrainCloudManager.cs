using BrainCloud;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BrainCloudManager : SingletonBehaviour<BrainCloudManager>
{
    private const int PARENT_PINK_STAR_INCREASE_AMOUNT = 10;

    public static BrainCloudClient Client => Wrapper != null ? Wrapper.Client : null;
    public static BrainCloudWrapper Wrapper { get; private set; }
    public UserInfo CurrentUserInfo { get; set; }
    public bool IsEmailAuthenticated { get; set; }

    private bool _isProcessing;
    public bool IsProcessingRequest
    {
        get { return _isProcessing; }
        set { _isProcessing = value; }
    }

    private int _childInfoIndex;
    private bool _statsRetrieved;
    private bool _currencyRetrieved;

    public override void StartUp()
    {
        CurrentUserInfo = new UserInfo();
        Wrapper = gameObject.AddComponent<BrainCloudWrapper>();
        Wrapper.Init();
        Wrapper.Client.MaxDepth = 100;
    }

    public bool CanReconnectUser()
    {
        return Wrapper.CanReconnect();
    }

    public void ReconnectUser()
    {
        _isProcessing = true;
        Wrapper.Reconnect
        (
            HandleSuccess("Authenticate Success", OnAuthenticateSuccess),
            HandleFailure("Authenticate Failed", OnFailureCallback)
        );
    }

    public void OnAuthenticateSuccess(string jsonResponse)
    {
        // Check if user manually logged in or reconnected,
        // if reconnected then assign the values.
        var data = jsonResponse.Deserialize("data");

        var username = data["playerName"] as string;
        if (username.IsNullOrEmpty() && !CurrentUserInfo.Username.IsNullOrEmpty())
        {
            Wrapper.PlayerStateService.UpdateUserName(CurrentUserInfo.Username);
        }
        else if (!username.IsNullOrEmpty())
        {
            CurrentUserInfo.UpdateUsername(username);
        }

        var email = data["emailAddress"] as string;
        if (email.IsNullOrEmpty() && !CurrentUserInfo.Email.IsNullOrEmpty())
        {
            Wrapper.PlayerStateService.UpdateContactEmail(CurrentUserInfo.Email);
            IsEmailAuthenticated = true;
        }
        else if (email.IsNullOrEmpty() && CurrentUserInfo.Email.IsNullOrEmpty())
        {
            IsEmailAuthenticated = false;
        }
        else
        {
            IsEmailAuthenticated = true;
            CurrentUserInfo.UpdateEmail(email);
        }

        var currency = data["currency"] as Dictionary<string, object>;
        if (currency != null)
        {
            var gems = currency["gems"] as Dictionary<string, object>;
            CurrentUserInfo.UpdateGems((int)gems["balance"]);

            var coins = currency["coins"] as Dictionary<string, object>;
            CurrentUserInfo.UpdateCoins((int)coins["balance"]);

            var fakeMoney = currency["fakeDollars"] as Dictionary<string, object>;
            CurrentUserInfo.UpdateFakeMoney((int)fakeMoney["balance"]);
        }

        CurrentUserInfo.UpdateLevel((int)data["experienceLevel"]);
        CurrentUserInfo.UpdateXP((int)data["experiencePoints"]);
        CurrentUserInfo.UpdateStats(data["statistics"] as Dictionary<string, object>);
        StatTracker.Instance.IncrementStat(BitBuddiesConsts.LOGIN_COUNT_STAT_NAME, (int)data["loginCount"]);
        if (StatTracker.Instance.GetStat(BitBuddiesConsts.LOGIN_COUNT_STAT_NAME) == 0)
        {
            var loginCount = (int)data["loginCount"];
            StatTracker.Instance.IncrementStat(BitBuddiesConsts.LOGIN_COUNT_STAT_NAME, loginCount);
        }

        Wrapper.GamificationService.ReadXpLevelsMetaData(HandleSuccess("ReadXPLevelsData Success", OnReadXPLevelsData));

        Dictionary<string, object> scriptData = new Dictionary<string, object> { { "childAppId", BitBuddiesConsts.APP_CHILD_ID } };
        Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.GET_CHILD_ACCOUNTS_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("Getting Child Accounts Success", OnGetChildAccounts),
            HandleFailure("Getting Child Accounts Failed", OnFailureCallback)
        );

        string[] propertyNames = new[] { "MysteryBoxInfo", "ChildAccountMaximum", "BuddyMoveSpeedInfo" };
        Wrapper.GlobalAppService.ReadSelectedProperties
        (
            propertyNames,
            HandleSuccess("Get Global Properties Success", OnGetGlobalProperties),
            HandleFailure("Get Mystery Box Info Failed", OnFailureCallback)
        );

        Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.GET_QUEST_INFO_SCRIPT_NAME,
            "{}",
            HandleSuccess("Getting Quest Info Success", OnGetQuestInfo),
            HandleFailure("Getting Quest Info Failed", OnFailureCallback)
        );
    }

    private void OnReadXPLevelsData(string jsonResponse)
    {
        var data = jsonResponse.Deserialize("data");
        if (data.ContainsKey("xp_levels") &&
            data["xp_levels"] is Dictionary<string, object>[] xp_levels &&
            xp_levels != null && xp_levels.Length > 0)
        {
            CurrentUserInfo.UpdateLevelUpInfo(xp_levels);
        }
        else
        {
            throw new Exception("Did not receive XP Levels Data!");
        }
    }

    private void OnGetGlobalProperties(string jsonResponse)
    {
        var response = (Dictionary<string, object>)JsonReader.Deserialize(jsonResponse);
        var data = (Dictionary<string, object>)response["data"];

        //Getting mystery buddy boxes
        var mysteryBoxInfo = (Dictionary<string, object>)data["MysteryBoxInfo"];
        string innerJson = (string)mysteryBoxInfo["value"];
        var lootboxes = (Dictionary<string, object>)JsonReader.Deserialize(innerJson);
        var listOfBoxInfo = new List<MysteryBoxInfo>();

        foreach (var keyValuePair in lootboxes)
        {
            var boxDict = (Dictionary<string, object>)keyValuePair.Value;

            MysteryBoxInfo boxInfo = new MysteryBoxInfo();
            boxInfo.Rarity = boxDict.GetString("rarity");
            boxInfo.RarityEnum = boxDict.GetValue<Rarity>("rarity");
            boxInfo.BoxName = boxDict.GetString("boxName");
            boxInfo.currencyType = boxDict.GetValue<CurrencyTypes>("unlockType");
            boxInfo.UnlockAmount = boxDict.GetValue<int>("unlockAmount");
            boxInfo.LevelRequirement = boxDict.GetValue<int>("levelRequirement");

            listOfBoxInfo.Add(boxInfo);
        }
        GameManager.Instance.MysteryBoxes = listOfBoxInfo;

        var childAccountMaxObj = data["ChildAccountMaximum"] as Dictionary<string, object>;
        if (int.TryParse((string)childAccountMaxObj["value"], out int value3))
        {
            GameManager.Instance.ChildCountMaximum = value3;
        }

        var buddyMoveSpeedObj = (Dictionary<string, object>)data["BuddyMoveSpeedInfo"];
        string moveSpeedJson = (string)buddyMoveSpeedObj["value"];
        // moveSpeedJson = "{\"Starter\":1,\"Basic\":1.1,\"Rare\":1.2,\"SuperRare\":1.3,\"Legendary\":1.4}"

        var moveSpeedDict = (Dictionary<string, object>)JsonReader.Deserialize(moveSpeedJson);

        List<float> moveSpeeds = new List<float>();
        foreach (var kvp in moveSpeedDict)
        {
            float speed = Convert.ToSingle(kvp.Value);
            moveSpeeds.Add(speed);
        }

        GameManager.Instance.BuddyMoveSpeeds = moveSpeeds;
    }

    private void OnGetQuestInfo(string jsonResponse)
    {
        var data = jsonResponse.Deserialize("data");

        if (data == null)
        {
            return;
        }

        var response = data["response"] as Dictionary<string, object>;
        var quests = response["quests"] as Dictionary<string, object>[];
        var listOfQuests = new List<QuestInfo>();

        if (quests != null || quests.Length > 0)
        {
            for (int i = 0; i < quests.Length; ++i)
            {
                QuestInfo questInfo = new QuestInfo();
                questInfo.QuestTitle = quests[i]["title"] as string;
                questInfo.QuestStatToTrack = quests[i]["statToTrack"] as string;
                questInfo.QuestId = quests[i]["questId"] as string;
                questInfo.QuestStatus = Enum.Parse<QUEST_STATUS>(quests[i]["status"] as string);
                questInfo.QuestLineIndex = (int)quests[i]["questLineIndex"];
                questInfo.QuestRequiredProgress = Convert.ToInt32(quests[i]["thresholdRequired"]);
                if (questInfo.QuestId.Contains("bitBuddies"))
                {
                    questInfo.QuestType = QuestTypes.BitBuddies;
                }
                else if (questInfo.QuestId.Contains("general"))
                {
                    questInfo.QuestType = QuestTypes.General;
                }
                else if (questInfo.QuestId.Contains("bitBling"))
                {
                    questInfo.QuestType = QuestTypes.BitBling;
                }

                var rewards = quests[i]["reward"] as Dictionary<string, object>;
                string key = rewards.Keys.First();
                questInfo.RewardCurrencyType = Enum.Parse<CurrencyTypes>(char.ToUpper(key[0]) + key.Substring(1));
                questInfo.QuestRewardAmount = (int)rewards.Values.First();

                listOfQuests.Add(questInfo);
            }
            listOfQuests.Sort((x, y) => x.QuestLineIndex.CompareTo(y.QuestLineIndex));
            GameManager.Instance.SetQuestsLists(listOfQuests);
        }

        var shopItems = response["shopItems"] as Dictionary<string, object>[];
        var listOfShopItems = new List<ShopInfo>();
        if (shopItems != null && shopItems.Length > 0)
        {
            for (int i = 0; i < shopItems.Length; ++i)
            {
                ShopInfo shopInfo = new ShopInfo();
                shopInfo.ShopId = shopItems[i]["itemId"] as string;
                shopInfo.DisplayName = shopItems[i]["displayName"] as string;
                shopInfo.ItemDescription = shopItems[i]["description"] as string;
                shopInfo.RewardAmount = (int)shopItems[i]["payoutAmount"];
                shopInfo.RewardCurrencyType = Enum.Parse<CurrencyTypes>(shopItems[i]["payoutType"] as string, true);

                if (shopItems[i].ContainsKey("buyPriceValue"))
                {
                    shopInfo.BuyCost = (int)shopItems[i]["buyPriceValue"];
                }
                if (shopItems[i].ContainsKey("buyPriceType") && shopInfo.BuyCost > 0)
                {
                    shopInfo.BuyCurrency = Enum.Parse<CurrencyTypes>(shopItems[i]["buyPriceType"] as string, true);
                }

                listOfShopItems.Add(shopInfo);
            }

            GameManager.Instance.ParentShopInfos = listOfShopItems;
        }

        var cooldownUntilObject = response["freebieCooldown"] as Dictionary<string, object>;
        if (cooldownUntilObject != null && cooldownUntilObject.Count > 0)
        {
            long cooldownUntil = Convert.ToInt64(cooldownUntilObject["cooldownUntil"]);

            if (cooldownUntil > 0 && CountdownTimer.GetRemainingTime(cooldownUntil) > TimeSpan.Zero)
            {
                GameManager.Instance.FreebieItemCooldownUntil = cooldownUntil;
            }

        }
    }

    private void OnGetItemCatalog(string jsonResponse)
    {
        //Getting Toy Bench info
        var response = (Dictionary<string, object>)JsonReader.Deserialize(jsonResponse);
        var data = (Dictionary<string, object>)response["data"];
        var response2 = (Dictionary<string, object>)data["response"];
        var itemInfos = (Dictionary<string, object>[])response2["items"];
        var listOfBenchInfo = new List<ToyBenchInfo>();
        var listOfShopInfo = new List<ShopInfo>();

        foreach (var itemDict in itemInfos)
        {
            string category = itemDict["category"] as string;
            if (category.Equals("toys", StringComparison.OrdinalIgnoreCase))
            {
                ToyBenchInfo benchInfo = new ToyBenchInfo();
                benchInfo.BenchId = itemDict["benchId"] as string;
                benchInfo.LevelRequirement = (int)itemDict["levelRequirement"];
                benchInfo.LoveRewardAmount = (int)itemDict["lovePayout"];
                benchInfo.CoinRewardAmount = (int)itemDict["coinPayout"];
                benchInfo.BuddyBlingRewardAmount = (int)itemDict["buddyBlingPayout"];
                benchInfo.UnlockCost = (int)itemDict["unlockAmount"];
                benchInfo.Cooldown = (int)itemDict["cooldown"];
                benchInfo.CoinSpawnAmount = (int)itemDict["coinSpawnAmount"];
                benchInfo.LoveSpawnAmount = (int)itemDict["loveSpawnAmount"];
                benchInfo.BuddyBlingSpawnAmount = (int)itemDict["buddyBlingSpawnAmount"];
                benchInfo.DisplayName = itemDict["displayName"] as string;

                listOfBenchInfo.Add(benchInfo);
            }
            else if (category.Equals("mouseMerchant", StringComparison.OrdinalIgnoreCase))
            {
                ShopInfo shopInfo = new ShopInfo();
                shopInfo.ShopId = itemDict["defId"] as string;
                shopInfo.DisplayName = itemDict["displayName"] as string;
                shopInfo.ItemDescription = itemDict["description"] as string;

                if (itemDict.ContainsKey("buyPriceValue"))
                {
                    shopInfo.BuyCost = (int)itemDict["buyPriceValue"];
                    if (shopInfo.BuyCost > 0)
                    {
                        shopInfo.BuyCurrency = Enum.Parse<CurrencyTypes>(itemDict["buyPriceType"] as string, true);
                    }
                }

                if (itemDict.ContainsKey("priceAmount"))
                {
                    shopInfo.BuyCost = (int)itemDict["priceAmount"];
                    shopInfo.BuyCurrency = Enum.Parse<CurrencyTypes>(itemDict["priceType"] as string, true);
                    shopInfo.RewardAmount = (int)itemDict["payoutAmount"];
                    shopInfo.RewardCurrencyType = Enum.Parse<CurrencyTypes>(itemDict["payoutType"] as string, true);
                }

                listOfShopInfo.Add(shopInfo);
            }
        }

        GameManager.Instance.ToyBenchInfos = listOfBenchInfo;
        GameManager.Instance.ChildShopInfos = listOfShopInfo;
    }

    private void OnGetChildAccounts(string jsonResponse)
    {
        var data = jsonResponse.Deserialize("data", "response");

        var xpLevels = data?.GetJSONArray("xp_levels");
        if (xpLevels != null && xpLevels.Length > 0)
        {
            AppChildrenInfo.UpdateLevelUpInfo(xpLevels);
        }

        // If user has no child profiles, exit out
        var children = data?.GetJSONArray("children");
        if (children == null || children.Length == 0)
        {
            StateManager.Instance.RefreshScreen();
            _isProcessing = false;
            return;
        }

        ReadChildrenInfo(children);
        GetChildItemCatalog();

        _childInfoIndex = 0;
        CompletedGettingCurrencies();

        ToyManager.OnBuddyLeveledUp -= ParentReceivesXP;
        ToyManager.OnBuddyLeveledUp += ParentReceivesXP;
    }

    private void GetChildItemCatalog()
    {
        var scriptData = new Dictionary<string, object>
        {
            { "childAppId", BitBuddiesConsts.APP_CHILD_ID                      },
            { "profileId",  GameManager.Instance.AppChildrenInfos[0].profileId }
        };

        Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.GET_CHILD_ITEM_CATALOG_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("GetItemCatalog Success", OnGetItemCatalog),
            HandleFailure("GetItemCatalog Failed", OnFailureCallback)
        );
    }

    private void ReadChildrenInfo(Dictionary<string, object>[] children)
    {
        var appChildrenInfos = new List<AppChildrenInfo>();

        for (int i = 0; i < children.Length; i++)
        {
            var child = new AppChildrenInfo();

            if (children != null)
            {
                // Get Basic Child data
                child.profileName = children[i].GetString("profileName");
                child.profileId = children[i].GetString("profileId");

                if (children[i].GetJSONObject("data") is var data && data != null && data.Count > 0)
                {
                    // Get Buddy Info
                    if (data.GetJSONObject("buddyInfo") is var buddyInfo && buddyInfo != null && buddyInfo.Count > 0)
                    {
                        child.buddyType = buddyInfo.GetString("name");
                        child.buddyLevel = buddyInfo.GetValue<int>("buddyLevel");
                        child.currentXP = buddyInfo.GetValue<int>("currentXP");
                        child.rarity = buddyInfo.GetValue<Rarity>("rarity");
                        child.coinMultiplier = buddyInfo.GetValue<double>("coinMultiplier") is double mult && mult > 0.0 ? (float)mult : 1.0f;
                        child.buddySpritePath = buddyInfo.GetString("buddySpritePath") is string path && !string.IsNullOrWhiteSpace(path) ? path : BitBuddiesConsts.DEFAULT_SPRITE_PATH_FOR_BUDDY;
                        child.coinPerHour = buddyInfo.GetValue<int>("coinPerHour");
                        child.maxCoinCapacity = buddyInfo.GetValue<int>("maxCoinCapacity");
                        child.lastIdleTimestamp = buddyInfo.GetDateTime("lastIdleTimestamp");
                    }
                    else
                    {
                        throw new Exception("ReadChildrenInfo: Buddy Info is Null/Missing!");
                    }

                    // Currency
                    if (data.GetJSONObject("currency") is var currency && currency != null && currency.Count > 0 &&
                        currency.GetJSONObject("buddyBling") is var buddyBling && buddyBling != null && buddyBling.Count > 0)
                    {
                        child.buddyBling = buddyBling.GetValue<int>("balance");
                    }

                    // Stats
                    if (data.GetJSONObject("stats") is var stats && stats != null && stats.Count > 0)
                    {
                        child.coinsEarnedInLifetime = stats.GetValue<int>("CoinsGainedForParent");
                        //dataInfo.loveEarnedInLifetime = stats.GetValue<int>("LoveEarned"); TODO: What was this here for??
                    }

                    // Items
                    if (data.GetJSONArray("items") is var items && items != null && items.Length > 0)
                    {
                        child.ownedToys = new List<string>();
                        child.ownedShopItems = new List<string>();
                        foreach (var item in items)
                        {
                            string itemCategory = item.GetString("category");
                            if (itemCategory.Equals("toys", StringComparison.OrdinalIgnoreCase))
                            {
                                child.ownedToys.Add(item.GetString("itemId"));
                            }
                            else if (itemCategory.Equals("mouseMerchant", StringComparison.OrdinalIgnoreCase))
                            {
                                child.ownedShopItems.Add(item.GetString("itemId"));
                            }

                            string itemId = item.GetString("itemId");
                            if (itemId.Equals(BitBuddiesConsts.JSON_DAILY_LOVE_BOOSTER_ITEM))
                            {
                                // Getting info on daily love booster item for time expirys
                                long durationInSeconds = item.GetValue<long>("durationInSeconds");
                                long createdAt = item.GetValue<long>("createdAt");
                                child.dailyBoosterExpiryUntil = createdAt + (durationInSeconds * 1000);
                                child.dailyCooldownUntil = item.GetValue<long>("cooldownUntil");
                                child.loveMultiplier = item.GetValue<int>("loveMultiplier");
                            }
                        }
                    }

                    // Achievements
                    if (data.GetJSONArray("achievements") is var achievements && achievements != null && achievements.Length > 0)
                    {
                        child.childAchievements = new List<ChildAchievementInfo>();
                        foreach (var achieve in achievements)
                        {
                            ChildAchievementInfo info = new()
                            {
                                AchievementId = achieve.GetString("achievementId"),
                                DisplayName = achieve.GetString("title"),
                                Status = achieve.GetString("status"),
                                LevelRequirement = achieve.GetValue<int>("index")
                            };

                            child.childAchievements.Add(info);
                        }
                    }
                }
            }
            else
            {
                throw new Exception("ReadChildrenInfo: Buddy/Child Profile is Null/Missing!");
            }

            appChildrenInfos.Add(child);
        }

        GameManager.Instance.AppChildrenInfos = appChildrenInfos;
    }

    private void GetChildStatsAndCurrencyData()
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object>
        {
            {"childAppId", BitBuddiesConsts.APP_CHILD_ID},
            {"childProfileId", GameManager.Instance.AppChildrenInfos[_childInfoIndex].profileId}
        };

        Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.GET_STATS_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("Stats Retrieved", OnGetStatsSuccess),
            HandleFailure("Getting Stats Failed", OnFailureCallback)
        );

        Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.GET_CURRENCIES_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("Get Currencies Success", OnGetCurrenciesSuccess),
            HandleFailure("Getting Currencies Failed", OnFailureCallback)
        );
    }

    private void OnGetStatsSuccess(string jsonResponse, object cbObject)
    {
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = packet["data"] as Dictionary<string, object>;
        _statsRetrieved = true;

        // var parentStats = response["parentStats"] as Dictionary<string, object>;
        // var statistics = parentStats["statistics"] as Dictionary<string, object>; 
        // UserInfo.UpdateLevel((int) statistics["Level"]);
        if (data["response"] is not Dictionary<string, object> response)
        {
            CompletedGettingCurrencies();
            return;
        }
        if (response.ContainsKey("childStats"))
        {
            var childStatsResponse = response["childStats"] as Dictionary<string, object>;
            var childStatistics = childStatsResponse["statistics"] as Dictionary<string, object>;

            if (_childInfoIndex < GameManager.Instance.AppChildrenInfos.Count - 1)
            {
                if (_statsRetrieved && _currencyRetrieved)
                {
                    _childInfoIndex++;
                    GetChildStatsAndCurrencyData();
                }
            }
            if (CurrentUserInfo.Coins > 0)
            {
                CompletedGettingCurrencies();
            }
        }
    }

    private void OnGetCurrenciesSuccess(string jsonResponse, object cbObject)
    {
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = packet["data"] as Dictionary<string, object>;

        if (data["response"] is not Dictionary<string, object> response)
        {
            return;
        }

        // if(response.TryGetValue("Gems", out var gemValue))
        // {
        //     var gemsInfo = gemValue as Dictionary<string, object>;
        //     UserInfo.UpdateGems((int) gemsInfo["balance"]);            
        // }
        // if(response.TryGetValue("Coins", out var coinValue))
        // {
        //     var coinsInfo = coinValue as Dictionary<string, object>;
        //     UserInfo.UpdateCoins((int) coinsInfo["balance"]);   
        // }
        if (response.TryGetValue("buddyBling", out var blingValue))
        {
            var blingInfo = blingValue as Dictionary<string, object>;
            GameManager.Instance.AppChildrenInfos[_childInfoIndex].buddyBling = (int)blingInfo["balance"];
        }
        _currencyRetrieved = true;

        if (_childInfoIndex < GameManager.Instance.AppChildrenInfos.Count - 1)
        {
            if (_statsRetrieved && _currencyRetrieved)
            {
                _childInfoIndex++;
                GetChildStatsAndCurrencyData();
            }
        }

        if (CurrentUserInfo.Level > 0)
        {
            CompletedGettingCurrencies();
        }
    }

    private void CompletedGettingCurrencies()
    {
        _isProcessing = false;
        StateManager.Instance.RefreshScreen();
    }

    public void OnConsumeCoins(string jsonResponse)
    {
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var firstData = packet["data"] as Dictionary<string, object>;
        var response = firstData["response"] as Dictionary<string, object>;
        var result = response["consumeCurrencyResult"] as Dictionary<string, object>;
        var secondData = result["data"] as Dictionary<string, object>;
        var currencyMap = secondData["currencyMap"] as Dictionary<string, object>;
        var coins = currencyMap["coins"] as Dictionary<string, object>;

        CurrentUserInfo.UpdateCoins((int)coins["balance"]);
        StateManager.Instance.RefreshScreen();
    }

    public void RewardCoinsToParent(int in_coins)
    {
        var scriptData = new Dictionary<string, object>
        {
            { "increaseAmount", in_coins }
        };

        Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.AWARD_COINS_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("RewardCoinsToParent Success", OnRewardCoinsToParent),
            HandleFailure("RewardCoinsToParent Failed", OnFailureCallback)
        );
    }

    private void OnRewardCoinsToParent(string jsonResponse, object cbObject)
    {
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var firstData = packet["data"] as Dictionary<string, object>;
        var response = firstData["response"] as Dictionary<string, object>;
        var getResult = response["getResult"] as Dictionary<string, object>;
        var secondData = getResult["data"] as Dictionary<string, object>;
        var currencyMap = secondData["currencyMap"] as Dictionary<string, object>;
        var coins = currencyMap["coins"] as Dictionary<string, object>;

        CurrentUserInfo.UpdateCoins((int)coins["balance"]);
        StateManager.Instance.RefreshScreen();
    }

    public void RewardGemsToParent(int in_gems)
    {
        var scriptData = new Dictionary<string, object>
        {
            { "increaseAmount", in_gems }
        };

        Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.AWARD_GEMS_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("RewardGemsToParent Success", OnRewardGemsToParent),
            HandleFailure("RewardGemsToParent Failed", OnFailureCallback)
        );
    }

    private void OnRewardGemsToParent(string jsonResponse, object cbObject)
    {
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var firstData = packet["data"] as Dictionary<string, object>;
        var response = firstData["response"] as Dictionary<string, object>;
        var getResult = response["getResult"] as Dictionary<string, object>;
        var secondData = getResult["data"] as Dictionary<string, object>;
        var currencyMap = secondData["currencyMap"] as Dictionary<string, object>;
        var gems = currencyMap["gems"] as Dictionary<string, object>;

        CurrentUserInfo.UpdateGems((int)gems["balance"]);
        StateManager.Instance.RefreshScreen();
    }

    public void ParentReceivesXP(int xpMultiplier = 1)
    {
        if (xpMultiplier < 1)
        {
            xpMultiplier = 1;
        }

        Wrapper.PlayerStatisticsService.IncrementExperiencePoints(PARENT_PINK_STAR_INCREASE_AMOUNT * xpMultiplier,
                                                                  HandleSuccess("LevelUpParent Success", OnLevelUpParent),
                                                                  HandleFailure("LevelUpParent Failed", OnFailureCallback));
    }

    private void OnLevelUpParent(string jsonResponse, object cbObject)
    {
        var data = jsonResponse.Deserialize();

        if (data.GetJSONObject("data") is not Dictionary<string, object> response || response.Count == 0)
        {
            return;
        }

        if (response.ContainsKey("experiencePoints"))
        {
            CurrentUserInfo.UpdateXP(response.GetValue<int>("experiencePoints"));
        }

        if (response.ContainsKey("experienceLevel"))
        {
            CurrentUserInfo.UpdateLevel(response.GetValue<int>("experienceLevel"));
        }

        var experienceLevels = response.GetJSONObject("rewardDetails")
                                      ?.GetJSONObject("xp")
                                      ?.GetJSONArray("experienceLevels");

        if (experienceLevels != null)
        {
            foreach (var xpLevel in experienceLevels)
            {
                if (xpLevel.GetValue<int>("level") != CurrentUserInfo.Level)
                {
                    continue;
                }

                var currency = xpLevel.GetJSONObject("rewards")?.GetJSONObject("currency");
                if (currency == null)
                {
                    break;
                }

                int coins = currency.GetValue<int>("coins");
                int gems = currency.GetValue<int>("gems");
                GameManager.Instance.HomeRewards["coins"] = coins;
                GameManager.Instance.HomeRewards["gems"] = gems;
                CurrentUserInfo.UpdateCoins(CurrentUserInfo.Coins + coins);
                CurrentUserInfo.UpdateGems(CurrentUserInfo.Gems + gems);

                break;
            }
        }

        StateManager.Instance.RefreshScreen();
    }

    public void AwardBlingToChild(int in_amount)
    {
        // Params for AwardBlingToChild(childAppId, profileId, increaseAmount)
        var scriptData = new Dictionary<string, object>
        {
            {"childAppId", BitBuddiesConsts.APP_CHILD_ID},
            {"profileId", GameManager.Instance.SelectedAppChildrenInfo.profileId},
            {"increaseAmount", in_amount}
        };

        Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.AWARD_BLING_TO_CHILD_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("Award Bling Successful", OnAwardBlingToChild),
            HandleFailure("Award Bling Failed", OnFailureCallback)
        );
    }

    private void OnAwardBlingToChild(string jsonResponse)
    {
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = packet["data"] as Dictionary<string, object>;
        var response = data["response"] as Dictionary<string, object>;
        var currencyMap = response["currencyMap"] as Dictionary<string, object>;
        var buddyBling = currencyMap["buddyBling"] as Dictionary<string, object>;

        GameManager.Instance.SelectedAppChildrenInfo.buddyBling = (int)buddyBling["balance"];
        StateManager.Instance.RefreshScreen();
    }

    private void OnFailureCallback()
    {
        //TODO: Create an error catching system where we catch reason codes and display them for the user with StateManager
    }

    private Action _updateNameAction;
    public void UpdateChildProfileName(string in_newName, string in_profileId, Action OnSuccessAction)
    {
        if (_isProcessing)
        {
            return;
        }

        _isProcessing = true;
        _updateNameAction = OnSuccessAction;

        var scriptData = new Dictionary<string, object>
        {
            {"childAppId", BitBuddiesConsts.APP_CHILD_ID},
            {"newName", in_newName},
            {"profileId", in_profileId},
        };

        Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.UPDATE_CHILD_PROFILE_NAME_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("Updated child name success", OnUpdateProfileName),
            HandleFailure("Updated child name failed", OnFailureCallback)
        );
    }

    private void OnUpdateProfileName(string jsonResponse)
    {
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = packet["data"] as Dictionary<string, object>;
        var response = data["response"] as Dictionary<string, object>;
        var userAdjusted = response["userAdjusted"] as Dictionary<string, object>;
        var newName = userAdjusted["newName"] as string;
        var profileId = userAdjusted["profileId"] as string;
        _isProcessing = false;

        //Destroy(FindAnyObjectByType<MysteryBoxPanelUI>().gameObject);
        var listOfChildren = GameManager.Instance.AppChildrenInfos;
        foreach (var child in listOfChildren)
        {
            if (child.profileId.Equals(profileId))
            {
                child.profileName = newName;
                break;
            }
        }

        GameManager.Instance.AppChildrenInfos = listOfChildren;
        StateManager.Instance.RefreshScreen();
        if (_updateNameAction != null)
        {
            _updateNameAction();
        }
    }

    public void OnAddChildProfile(string jsonResponse)
    {
        var children = jsonResponse.Deserialize("data", "response")
                                  ?.GetJSONArray("children");

        if (children != null && children.Length > 0)
        {
            ReadChildrenInfo(children);
        }

        if (GameManager.Instance.AppChildrenInfos.Count == 1)
        {
            GetChildItemCatalog();
        }

        //Stat will be updated on the server from the cloud code script when adding a new child account
        StatTracker.Instance.IncrementStat(BitBuddiesConsts.BUDDIES_OWNED_STAT_NAME);
        _childInfoIndex = 0;
        GetChildStatsAndCurrencyData();
    }

    public void ClearDataForLogout()
    {
        CurrentUserInfo = new UserInfo();
        IsEmailAuthenticated = false;
    }

    #region Callback Creation Helpers

    /// <summary>
    /// Creates a callback used for various brainCloud API calls for when they return as a success.
    /// This will also format a log into the console with all the relevant information.
    /// </summary>
    /// <param name="logMessage">Optional information to provide context on the success.</param>
    /// <param name="onSuccess">Optional callback to invoke after successful API calls.</param>
    public static SuccessCallback HandleSuccess(string logMessage = "", Action onSuccess = null) =>
        InternalHandleSuccess(logMessage, onSuccess?.Target, onSuccess != null ? (_, _) => onSuccess.Invoke() : null);

    /// <summary>
    /// Creates a callback used for various brainCloud API calls for when they return as a success.
    /// This will also format a log into the console with all the relevant information and as
    /// well as invoke the onSuccess Action with the JSON response.
    /// </summary>
    /// <param name="logMessage">Optional information to provide context on the success.</param>
    /// <param name="onSuccessS">Optional callback to invoke after successful API calls which passes the JSON response.</param>
    public static SuccessCallback HandleSuccess(string logMessage = "", Action<string> onSuccessS = null) =>
        InternalHandleSuccess(logMessage, onSuccessS?.Target, onSuccessS != null ? (jsonResponse, _) => onSuccessS.Invoke(jsonResponse) : null);

    /// <summary>
    /// Creates a callback for various brainCloud API calls for when they return as a success.
    /// This will also format a log into the console with all the relevant information and as
    /// well as invoke the onSuccess Action with the JSON response and the callback object.
    /// </summary>
    /// <param name="logMessage">Optional information to provide context on the success.</param>
    /// <param name="onSuccessSO">Optional callback to invoke after successful API calls which passes the JSON response and the callback object.</param>
    public static SuccessCallback HandleSuccess(string logMessage = "", Action<string, object> onSuccessSO = null) =>
        InternalHandleSuccess(logMessage, onSuccessSO?.Target, onSuccessSO);

    /// <summary>
    /// Creates a callback for various brainCloud API calls for when they return as a failure.
    /// This will also format a log into the console with all the relevant information.
    /// </summary>
    /// <param name="errorMessage">Optional information to provide context on the failure.</param>
    /// <param name="onFailure">Optional callback to invoke after failed API calls.</param>
    public static FailureCallback HandleFailure(string errorMessage = "", Action onFailure = null) =>
        InternalHandleFailure(errorMessage, onFailure?.Target, onFailure != null ? (_, _) => onFailure.Invoke() : null);

    /// <summary>
    /// Creates a callback for various brainCloud API calls for when they return as a failure.
    /// This will also format a log into the console with all the relevant information and as
    /// well as invoke the onFailure Action with an <see cref="ErrorResponse"/>.
    /// </summary>
    /// <param name="errorMessage">Optional information to provide context on the failure.</param>
    /// <param name="onFailureER">Optional callback to invoke after failed API calls which contains the JSON error.</param>
    public static FailureCallback HandleFailure(string errorMessage, Action<ErrorResponse> onFailureER = null) =>
        InternalHandleFailure(errorMessage, onFailureER?.Target, onFailureER != null ? (jsonError, _) => onFailureER.Invoke(jsonError) : null);

    /// <summary>
    /// Creates a callback for various brainCloud API calls for when they return as a failure.
    /// This will also format a log into the console with all the relevant information and as
    /// well as invoke the onFailure Action with an <see cref="ErrorResponse"/> and the callback object.
    /// </summary>
    /// <param name="errorMessage">Optional information to provide context on the failure.</param>
    /// <param name="onFailureERO">Optional callback to invoke after failed API calls which passes the JSON error and the callback object.</param>
    public static FailureCallback HandleFailure(string errorMessage, Action<ErrorResponse, object> onFailureERO = null) =>
        InternalHandleFailure(errorMessage, onFailureERO?.Target, onFailureERO);

    private static SuccessCallback InternalHandleSuccess(string logMessage, object targetObject, Action<string, object> onSuccess)
    {
        logMessage = string.IsNullOrWhiteSpace(logMessage) ? "Success" : logMessage;
        return (jsonResponse, cbObject) =>
        {
            cbObject ??= targetObject;
            string cbObjectName = cbObject != null ? cbObject.GetType().Name : string.Empty;
            if (cbObjectName.Contains("DisplayClass")) // Generated Class
            {
                cbObject = null;
            }
            else if (!string.IsNullOrWhiteSpace(cbObjectName))
            {
                logMessage = $"{cbObjectName}: {logMessage}";
            }

#if UNITY_EDITOR
            logMessage = $"{logMessage}\nJSON Response:\n{jsonResponse}";
            if (cbObject is MonoBehaviour mbObject)
            {
                Debug.Log(logMessage, mbObject);
            }
            else
            {
                Debug.Log(logMessage);
            }
#else
            Debug.Log($"{logMessage}\nJSON Response:\n{jsonResponse}");
#endif

            onSuccess?.Invoke(jsonResponse, cbObject);
        };
    }

    private static FailureCallback InternalHandleFailure(string errorMessage, object targetObject, Action<ErrorResponse, object> onFailure = null)
    {
        errorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Failure" : errorMessage;
        return (status, reasonCode, jsonError, cbObject) =>
        {
            cbObject ??= targetObject;
            string cbObjectName = cbObject != null ? cbObject.GetType().Name : string.Empty;
            if (cbObjectName.Contains("DisplayClass")) // Generated Class
            {
                cbObject = null;
            }
            else if (!string.IsNullOrWhiteSpace(cbObjectName))
            {
                errorMessage = $"{cbObjectName}: {errorMessage}";
            }

#if UNITY_EDITOR
            errorMessage = $"{errorMessage} - Status: {status} - Reason: {reasonCode}\nJSON Response:\n{jsonError}";
            if (cbObject is MonoBehaviour mbObject)
            {
                Debug.LogError(errorMessage, mbObject);
            }
            else
            {
                Debug.LogError(errorMessage);
            }
#else
            Debug.Log($"{errorMessage} - Status: {status} - Reason: {reasonCode}\nJSON Response:\n{jsonError}");
#endif

            onFailure?.Invoke(jsonError.Deserialize<ErrorResponse>(), cbObject);
        };
    }

    #endregion
}
