using BrainCloud;
using BrainCloud.JSONHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using Gameframework;
using BrainCloud.JsonFx.Json;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using UnityEngine;


public class BrainCloudManager : SingletonBehaviour<BrainCloudManager>
{
    public static BrainCloudClient Client => Wrapper != null ? Wrapper.Client : null;
    public static BrainCloudWrapper Wrapper { get; private set; }
    public UserInfo CurrentUserInfo { get; set ; }
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
        /*
         * {"packetId":0,"responses":[{"data":{"abTestingId":90,"lastLogin":1755270684595,"server_time":1755270684633,
         * "refundCount":0,"logouts":0,"timeZoneOffset":-5.0,"experiencePoints":0,"maxBundleMsgs":10,"createdAt":1754489020301,
         * "parentProfileId":null,"emailAddress":"dude@place.com","experienceLevel":0,"countryCode":"CA","vcClaimed":0,"currency":
         * {"Gems":{"consumed":0,"balance":170,"purchased":0,"awarded":170,"revoked":0},"Coins":{"consumed":0,"balance":300,"purchased":0,"awarded":300,
         * "revoked":0}},"id":"ec2f4f95-ba13-4d42-b1e3-7407a86cc635","compressIfLarger":0,"amountSpent":0,"retention":
         * {"d00":true,"d01":true,"d02":true,"d03":true,"d05":true,"d06":true,"d07":true,"d08":true,"d09":true},"previousLogin":1755270303250,
         * "playerName":"dude","pictureUrl":null,"incoming_events":[],"failedRedemptionsTotal":0,"sessionId":"ucebku0j0iji44jga410mtjhb2",
         * "languageCode":"en","vcPurchased":0,"isTester":false,"summaryFriendData":null,"loginCount":107,"emailVerified":true,"xpCapped":false,
         * "profileId":"ec2f4f95-ba13-4d42-b1e3-7407a86cc635","newUser":"false","allTimeSecs":0,"playerSessionExpiry":1200,"sent_events":[],
         * "maxKillCount":11,"rewards":{"rewardDetails":{},"currency":{},"rewards":{}},"statistics":{"Level":4}},"status":200}]}
         */
        //Check if user manually logged in or reconnected,
        //if reconnected then assign the values.
        var data = jsonResponse.Deserialize("data");

        var username = data["playerName"] as string;
        if(username.IsNullOrEmpty() && !CurrentUserInfo.Username.IsNullOrEmpty())
        {
            Wrapper.PlayerStateService.UpdateName(CurrentUserInfo.Username);
        }
        else if(!username.IsNullOrEmpty())
        {
            CurrentUserInfo.UpdateUsername(username);
        }
        

            
        var email = data["emailAddress"] as string;
        if(email.IsNullOrEmpty() && !CurrentUserInfo.Email.IsNullOrEmpty())
        {
            Wrapper.PlayerStateService.UpdateContactEmail(CurrentUserInfo.Email);
            IsEmailAuthenticated = true;
        }
        else if(email.IsNullOrEmpty() && CurrentUserInfo.Email.IsNullOrEmpty())
        {
            IsEmailAuthenticated = false;
        }
        else 
        {
            IsEmailAuthenticated = true;
            CurrentUserInfo.UpdateEmail(email);
        }
        var currency = data["currency"] as Dictionary<string, object>;
        if(currency != null)
        {
            var gems = currency["gems"] as Dictionary<string, object>;
            CurrentUserInfo.UpdateGems((int)gems["balance"]);
            
            var coins = currency["coins"] as Dictionary<string, object>;
            CurrentUserInfo.UpdateCoins((int)coins["balance"]);
            
            var fakeMoney = currency["fakeDollars"] as Dictionary<string, object>;
            CurrentUserInfo.UpdateFakeMoney((int)fakeMoney["balance"]);
        }
        
        CurrentUserInfo.UpdateLevel((int) data["experienceLevel"]);
        CurrentUserInfo.UpdateXP((int) data["experiencePoints"]);
        CurrentUserInfo.UpdateStats(data["statistics"] as Dictionary<string, object>);
        if(StatTracker.Instance.GetStat(BitBuddiesConsts.LOGIN_COUNT_STAT_NAME) == 0)
        {
            var loginCount = (int) data["loginCount"];
            StatTracker.Instance.IncrementStat(BitBuddiesConsts.LOGIN_COUNT_STAT_NAME, loginCount);   
        }
        
        var summaryFriendData = data["summaryFriendData"] as Dictionary<string, object>;
        if(summaryFriendData != null)
        {
            int nextLevelUp =  (int) summaryFriendData["nextLevelUpXP"];
            if(nextLevelUp > CurrentUserInfo.CurrentXP)
            {
                CurrentUserInfo.UpdateNextLevelUp(nextLevelUp);
            }
            else if(nextLevelUp == 0)
            {
                Wrapper.PlayerStatisticsService.GetNextExperienceLevel(HandleSuccess("GetNextXP Success", OnGetNextLevelUp));
            }
            if(summaryFriendData.ContainsKey("previousLevelXP"))
            {
                CurrentUserInfo.PreviousLevelUp = (int) summaryFriendData["previousLevelXP"];
            }
        }
        else
        {
            Wrapper.PlayerStatisticsService.GetNextExperienceLevel(HandleSuccess("GetNextXP Success", OnGetNextLevelUp));
        }
        
        Dictionary<string, object> scriptData = new Dictionary<string, object> {{"childAppId", BitBuddiesConsts.APP_CHILD_ID}};
        Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.GET_CHILD_ACCOUNTS_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("Getting Child Accounts Success", OnGetChildAccounts),
            HandleFailure("Getting Child Accounts Failed", OnFailureCallback)
        );
        string[] propertyNames = new [] {"MysteryBoxInfo", "RewardPickUpLifetime", "ChildAccountMaximum"}; 
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
    
    private void OnGetNextLevelUp(string jsonResponse)
    {
        var data = jsonResponse.Deserialize("data");
        var xpDetails = data["xp_level"] as Dictionary<string, object>;
        if(xpDetails != null)
        {
            int nextLevelUp =  (int) xpDetails["experience"];
            if(nextLevelUp != 0)
            {
                CurrentUserInfo.PreviousLevelUp = 0;
                CurrentUserInfo.UpdateNextLevelUp(nextLevelUp);
                Dictionary<string, object> scriptData = new Dictionary<string, object>();
                scriptData.Add("nextLevelUpXP", nextLevelUp);
                scriptData.Add("previousLevelUpXP", CurrentUserInfo.PreviousLevelUp);
                Wrapper.PlayerStateService.UpdateSummaryFriendData(scriptData.Serialize());
            }
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
        var listOfBoxInfo =  new List<MysteryBoxInfo>();

        foreach (var keyValuePair in lootboxes)
        {
            var boxDict = (Dictionary<string, object>)keyValuePair.Value;
            
            MysteryBoxInfo boxInfo = new MysteryBoxInfo();
            boxInfo.Rarity = boxDict["rarity"] as string;
            boxInfo.RarityEnum = Enum.Parse<Rarity>(boxDict["rarity"] as string);
            boxInfo.BoxName = boxInfo.Rarity + " Box";
            boxInfo.currencyType = Enum.Parse<CurrencyTypes>((string)boxDict["unlockType"]);
            boxInfo.UnlockAmount = (int)boxDict["unlockAmount"];
            boxInfo.LevelRequirement = (int)boxDict["levelRequirement"];
            
            listOfBoxInfo.Add(boxInfo);
        }
        GameManager.Instance.MysteryBoxes = listOfBoxInfo;

        var rewardPickUpLifetimeObj = data["RewardPickUpLifetime"]as Dictionary<string, object>;
        if(float.TryParse((string) rewardPickUpLifetimeObj["value"], out float value))
        {
            GameManager.Instance.RewardPickupDuration = value;
        }
        else if(double.TryParse((string) rewardPickUpLifetimeObj["value"], out double value2))
        {
            GameManager.Instance.RewardPickupDuration = (float)value2;
        }
        else
        {
            GameManager.Instance.RewardPickupDuration = 20;
        }
        
        var childAccountMaxObj = data["ChildAccountMaximum"]as Dictionary<string, object>;
        if(int.TryParse((string) childAccountMaxObj["value"], out int value3))
        {
            GameManager.Instance.ChildCountMaximum = value3;
        }
    }
    
    private void OnGetQuestInfo(string jsonResponse)
    {
        var data = jsonResponse.Deserialize("data");
        if(data == null) return;

        var response = data["response"] as Dictionary<string, object>;
        var quests = response["quests"] as Dictionary<string, object>[];
        var listOfQuests =  new List<QuestInfo>();
        
        if(quests != null || quests.Length > 0)
        {
            for (int i = 0; i < quests.Length; ++i)
            {
                QuestInfo questInfo = new QuestInfo();
                questInfo.QuestTitle = quests[i]["title"] as string;
                questInfo.QuestStatToTrack = quests[i]["statToTrack"] as string;
                questInfo.QuestId = quests[i]["questId"] as string;
                questInfo.QuestStatus = Enum.Parse<QUEST_STATUS>(quests[i]["status"] as string);
                questInfo.QuestLineIndex = (int) quests[i]["questLineIndex"];
                questInfo.QuestRequiredProgress = Convert.ToInt32(quests[i]["thresholdRequired"]);
                if(questInfo.QuestId.Contains("bitBuddies"))
                {
                    questInfo.QuestType = QuestTypes.BitBuddies;
                }
                else if(questInfo.QuestId.Contains("general"))
                {
                    questInfo.QuestType = QuestTypes.General;
                }
                else if(questInfo.QuestId.Contains("bitBling"))
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
        if(shopItems != null && shopItems.Length > 0)
        {
            for (int i = 0; i < shopItems.Length; ++i)
            {
                ShopInfo shopInfo = new ShopInfo();
                shopInfo.ShopId = shopItems[i]["itemId"] as string;
                shopInfo.DisplayName = shopItems[i]["displayName"] as string;
                shopInfo.ItemDescription = shopItems[i]["description"] as string;
                shopInfo.RewardAmount = (int)shopItems[i]["payoutAmount"];
                shopInfo.RewardCurrencyType = Enum.Parse<CurrencyTypes>(shopItems[i]["payoutType"] as string, true);
                
                if(shopItems[i].ContainsKey("buyPriceValue"))
                {
                    shopInfo.BuyCost = (int)shopItems[i]["buyPriceValue"];                    
                }
                if(shopItems[i].ContainsKey("buyPriceType") && shopInfo.BuyCost > 0)
                {
                    shopInfo.BuyCurrency = Enum.Parse<CurrencyTypes>(shopItems[i]["buyPriceType"] as string, true);
                }

                listOfShopItems.Add(shopInfo);
            }
            
            GameManager.Instance.ParentShopInfos = listOfShopItems;
        }
        
        var cooldownUntilObject = response["freebieCooldown"] as Dictionary<string, object>;
        if(cooldownUntilObject != null && cooldownUntilObject.Count > 0)
        {
            long cooldownUntil = Convert.ToInt64(cooldownUntilObject["cooldownUntil"]);
            
            if(cooldownUntil > 0 && CountdownTimer.GetRemainingTime(cooldownUntil) > TimeSpan.Zero)
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
        var listOfBenchInfo =  new List<ToyBenchInfo>();
        var listOfShopInfo =  new List<ShopInfo>();

        foreach (var itemDict in itemInfos)
        {
            string category = itemDict["category"] as string;
            if(category.Equals("toys", StringComparison.OrdinalIgnoreCase))
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
            else if(category.Equals("mouseMerchant", StringComparison.OrdinalIgnoreCase))
            {
                ShopInfo shopInfo = new ShopInfo();
                shopInfo.ShopId = itemDict["defId"] as string;
                shopInfo.DisplayName = itemDict["displayName"] as string;
                shopInfo.ItemDescription = itemDict["description"] as string;
                
                if(itemDict.ContainsKey("buyPriceValue"))
                {
                    shopInfo.BuyCost = (int)itemDict["buyPriceValue"];  
                    if(shopInfo.BuyCost > 0)
                    {
                        shopInfo.BuyCurrency = Enum.Parse<CurrencyTypes>(itemDict["buyPriceType"] as string, true);                        
                    }                  
                }
                
                if(itemDict.ContainsKey("multiplier"))
                {
                    //ToDo: Set up multiplier field for toy collection
                    //var value = (int)itemDict["multiplier"];
                    //if(value > 0)
                    //{
                    //    //do something
                    //}
                    //var duration = (int)itemDict["duration"];
                    //if(duration > 0)
                    //{
                    //    //do something
                    //}
                }
                
                if(itemDict.ContainsKey("priceAmount"))
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
    /*
     * {"packetId":1,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"scriptSize":12305,"executeTime":109561},
     * "response":{"getChildProfiles":{"data":{"children":[{"profileName":"sanji",
     * "profileId":"e068fdfb-f36e-4c9d-862a-d86f20d5e54b","appId":"50974",
     * "summaryFriendData":{"coinMultiplier":1,"coinPerHour":40,"maxCoinCapacity":100,"buddySpritePath":"BuddySprites/buddy-1","
     * rarity":"starter","level":1,"experiencePoints":0,"lastIdleTimestamp":1.762372115799E12,"nextLevelUpXP":5},
     * "extraData":{"xp":{"xpLevel":1,"xpPoints":48,"nextXpLevel":100},
     * "currency":{"buddyBling":{"consumed":0,"balance":100,"purchased":0,"awarded":100,"revoked":0}},
     * "stats":{"CoinsGainedForParent":197,"LoveEarned":0}}}]},"status":200}},"success":true,"reasonCode":null},"status":200}
     */
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data =  packet["data"] as Dictionary<string, object>;
        var response = data["response"] as Dictionary<string, object>;
        var getChildAccountObject = response["getChildProfiles"] as Dictionary<string, object>;
        var data2 = getChildAccountObject["data"] as Dictionary<string, object>;
        var children = data2["children"] as Dictionary<string, object>[];
        var appChildrenInfos = new List<AppChildrenInfo>();

        //If user has no child profiles, exit out
        if(children == null || children.Length == 0)
        {
            StateManager.Instance.RefreshScreen();
            _isProcessing = false;
            return;
        }

        float hourInSeconds = 3600;
        
        for(int i = 0; i < children.Length; i++)
        {
            var summaryFriendData = children[i]["summaryFriendData"] as Dictionary<string, object>;
            
         
            var dataInfo = new AppChildrenInfo();
            if(children != null)
            {
                //Get Child data
                dataInfo.profileName = children[i]["profileName"] as string;
                dataInfo.profileId = children[i]["profileId"] as string;   
            }
            
            if(summaryFriendData != null)
            {
                dataInfo.summaryFriendData = summaryFriendData;
                //Get Summary data
                if(summaryFriendData.ContainsKey("rarity"))
                {
                    dataInfo.rarity = summaryFriendData["rarity"] as string;   
                }
                if(summaryFriendData.ContainsKey("buddySpritePath"))
                {
                    dataInfo.buddySpritePath =  summaryFriendData["buddySpritePath"] as string;
                }
                else
                {
                    dataInfo.buddySpritePath = BitBuddiesConsts.DEFAULT_SPRITE_PATH_FOR_BUDDY;
                }

                try
                {
                    if(summaryFriendData["coinMultiplier"] is double multiplier)
                    {
                        dataInfo.coinMultiplier = (float) multiplier;
                    }
                }
                catch (Exception e)
                {
                    var multiplierInt = (int) summaryFriendData["coinMultiplier"];
                    if(multiplierInt > 0)
                    {
                        dataInfo.coinMultiplier = multiplierInt;
                    }
                    else
                    {
                        dataInfo.coinMultiplier = 1.0f;
                    }

                    Debug.LogWarning("Coin Multiplier exception: " + e.Message);
                }
                if(summaryFriendData.ContainsKey("experiencePoints"))
                {
                    dataInfo.currentXP  = (int) summaryFriendData["experiencePoints"];                    
                }
                if(summaryFriendData.ContainsKey("level"))
                {
                    dataInfo.buddyLevel = (int) summaryFriendData["level"];                    
                }
                if(summaryFriendData.ContainsKey("nextLevelUpXP"))
                {
                    dataInfo.nextLevelUp =  (int) summaryFriendData["nextLevelUpXP"];                    
                }
                if(summaryFriendData.ContainsKey("previousLevelUpReq"))
                {
                    dataInfo.previousLevelUp =  (int) summaryFriendData["previousLevelUpReq"];
                }
                else
                {
                    dataInfo.previousLevelUp = 0;
                }
                
                dataInfo.coinPerHour = (int) summaryFriendData["coinPerHour"];
                dataInfo.maxCoinCapacity = (int) summaryFriendData["maxCoinCapacity"];   
                dataInfo.lastIdleTimestamp = DateTimeOffset.FromUnixTimeMilliseconds((long) summaryFriendData["lastIdleTimestamp"]).UtcDateTime;
                TimeSpan timeDifference = DateTime.UtcNow - dataInfo.lastIdleTimestamp;
                
                float coinsPerSecond = dataInfo.coinPerHour / hourInSeconds;
                int coinsEarned = Mathf.FloorToInt(coinsPerSecond * (float)timeDifference.TotalSeconds);
                if(coinsEarned > 0 && coinsEarned < dataInfo.maxCoinCapacity)
                {
                    dataInfo.coinsEarnedInHolding = coinsEarned;
                }
                else
                {
                    dataInfo.coinsEarnedInHolding = dataInfo.maxCoinCapacity;
                }
            }
            
            if(children[i].ContainsKey("extraData"))
            {
                var extraData = children[i]["extraData"] as Dictionary<string, object>;
                if(extraData != null)
                {
                    var currency = extraData["currency"] as Dictionary<string, object>;
                    if(currency != null)
                    {
                        var buddyBling = currency["buddyBling"] as Dictionary<string, object>;
                        if(buddyBling != null)
                        {
                            dataInfo.buddyBling = (int) buddyBling["balance"];
                        }
                    }
                
                    var stats = extraData["stats"] as Dictionary<string, object>;
                    if(stats != null)
                    {
                        dataInfo.coinsEarnedInLifetime = (int) stats["CoinsGainedForParent"];
                        //dataInfo.loveEarnedInLifetime = (int) stats["LoveEarned"];
                    }
                    
                    var items = extraData["items"] as Dictionary<string, object>[];
                    if(items != null)
                    {
                        dataInfo.ownedToys = new List<string>();
                        dataInfo.ownedShopItems = new List<string>();
                        for (int x = 0; x < items.Length; x++)
                        {
                            string itemCategory = items[x]["category"] as string;

                            if(itemCategory.Equals("toys", StringComparison.OrdinalIgnoreCase))
                            {
                                dataInfo.ownedToys.Add(items[x]["itemId"] as string);
                            }
                            else if(itemCategory.Equals("mouseMerchant", StringComparison.OrdinalIgnoreCase))
                            {
                                dataInfo.ownedShopItems.Add(items[x]["itemId"] as string);
                            }

                            string itemId = items[x]["itemId"] as string;
                            if (itemId.Equals(BitBuddiesConsts.JSON_DAILY_LOVE_BOOSTER_ITEM))
                            {
                                //Getting info on daily love booster item for time expirys
                                long durationInSeconds = Convert.ToInt64(items[x]["durationInSeconds"]);
                                long createdAt = Convert.ToInt64(items[x]["createdAt"]);
                                dataInfo.dailyBoosterExpiryUntil = createdAt + (durationInSeconds * 1000);
                                dataInfo.dailyCooldownUntil = Convert.ToInt64(items[x]["cooldownUntil"]);
                                dataInfo.loveMultiplier = (int)items[x]["loveMultiplier"];
                            }

                        }
                    }
                }
            }

            appChildrenInfos.Add(dataInfo);
        }

        Dictionary<string, object> scriptData = new Dictionary<string, object>();
        scriptData.Add("childAppId", BitBuddiesConsts.APP_CHILD_ID);
        scriptData.Add("profileId", appChildrenInfos[0].profileId);
        Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.GET_CHILD_ITEM_CATALOG_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("GetItemCatalog Success", OnGetItemCatalog),
            HandleFailure("GetItemCatalog Failed", OnFailureCallback)
        );
        
        _childInfoIndex = 0;
        GameManager.Instance.AppChildrenInfos = appChildrenInfos;
        CompletedGettingCurrencies();
    }
    
    private void GetChildStatsAndCurrencyData()
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object>
        {
            {"childAppId", BitBuddiesConsts.APP_CHILD_ID},
            {"childProfileId", GameManager.Instance.AppChildrenInfos[_childInfoIndex].profileId}
        };
        
        //Get data from cloud code scripts
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
        Dictionary<string, object> packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        Dictionary<string, object> data = packet["data"] as Dictionary<string, object>;
        Dictionary<string, object> response = data["response"] as Dictionary<string, object>;
        _statsRetrieved = true;
        // var parentStats = response["parentStats"] as Dictionary<string, object>;
        // var statistics = parentStats["statistics"] as Dictionary<string, object>; 
        // UserInfo.UpdateLevel((int) statistics["Level"]);
        if(response == null)
        {
            CompletedGettingCurrencies();
            return;
        }
        if(response.ContainsKey("childStats"))
        {
            var childStatsResponse = response["childStats"] as Dictionary<string, object>;
            var childStatistics =  childStatsResponse["statistics"] as Dictionary<string, object>;
        
            if(_childInfoIndex < GameManager.Instance.AppChildrenInfos.Count - 1)
            {
                if(_statsRetrieved && _currencyRetrieved)
                {
                    _childInfoIndex++;
                    GetChildStatsAndCurrencyData();   
                }
            }
            if(CurrentUserInfo.Coins > 0)
            {
                CompletedGettingCurrencies();
            }   
        }
    }
    
    private void OnGetCurrenciesSuccess(string jsonResponse, object cbObject)
    {
        Dictionary<string, object> packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        Dictionary<string, object> data = packet["data"] as Dictionary<string, object>;
        Dictionary<string, object> response = data["response"] as Dictionary<string, object>;
        
        /*
         * {"packetId":1,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"evaluateTime":18707,"scriptSize":4017},
         * "response":{"parentStats":{"statistics":{"Level":3}}},"success":true,"reasonCode":null},"status":200},
         * {"data":{"runTimeData":{"hasIncludes":true,"evaluateTime":13287,"scriptSize":3708},"response":{},
         * "success":true,"reasonCode":null},"status":200}]}
         */
        if (response == null) return;
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
        if(response.TryGetValue("buddyBling", out var blingValue))
        {
            var blingInfo = blingValue as Dictionary<string, object>;
            GameManager.Instance.AppChildrenInfos[_childInfoIndex].buddyBling = (int)blingInfo["balance"];   
        }
        _currencyRetrieved = true;
        
        if(_childInfoIndex < GameManager.Instance.AppChildrenInfos.Count - 1)
        {
            if(_statsRetrieved && _currencyRetrieved)
            {
                _childInfoIndex++;
                GetChildStatsAndCurrencyData();   
            }
        }
        if(CurrentUserInfo.Level > 0)
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
        /*
         * {"packetId":3,"responses":[{"data":{"runTimeData":{"hasIncludes":false,
         * "compileTime":1476,"scriptSize":285,"renderTime":4,"executeTime":10346},
         * "response":{"consumeCurrencyResult":{"data":{"currencyMap":{"gems":{"consumed":0,
         * "balance":500,"purchased":0,"awarded":500,"revoked":0},"coins":{"consumed":65000,
         * "balance":0,"purchased":0,"awarded":65000,"revoked":0}}},"status":200}},
         * "success":true,"reasonCode":null},"status":200}]}
         */
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var firstData =  packet["data"] as Dictionary<string, object>;
        var response = firstData["response"] as Dictionary<string, object>;
        var result = response["consumeCurrencyResult"] as Dictionary<string, object>;
        var secondData = result["data"] as Dictionary<string, object>;
        var currencyMap = secondData["currencyMap"] as Dictionary<string, object>;
        var coins = currencyMap["coins"] as Dictionary<string, object>;
        CurrentUserInfo.UpdateCoins((int) coins["balance"]);
        StateManager.Instance.RefreshScreen();
    }
    
    public void RewardCoinsToParent(int in_coins)
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object> {{"increaseAmount", in_coins}};
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
        /*
         * {"packetId":4,"responses":[{"data":{"runTimeData":{"hasIncludes":false,"evaluateTime":16716,"scriptSize":284,"renderTime":3},
         * "response":{"getResult":{"data":{"currencyMap":{"Gems":{"consumed":0,"balance":160,"purchased":0,"awarded":160,"revoked":0},
         * "Coins":{"consumed":0,"balance":200,"purchased":0,"awarded":200,"revoked":0}}},"status":200}},"success":true,"reasonCode":null},
         * "status":200}]}
         */
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var firstData =  packet["data"] as Dictionary<string, object>;
        var response = firstData["response"] as Dictionary<string, object>;
        var getResult = response["getResult"] as Dictionary<string, object>;
        var secondData = getResult["data"] as Dictionary<string, object>;
        var currencyMap = secondData["currencyMap"] as Dictionary<string, object>;
        var coins = currencyMap["coins"] as Dictionary<string, object>;
        CurrentUserInfo.UpdateCoins((int) coins["balance"]);
        StateManager.Instance.RefreshScreen();
    }
    
    public void RewardGemsToParent(int in_gems)
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object> {{"increaseAmount", in_gems}};
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
        /*
         * {"packetId":3,"responses":[{"data":{"runTimeData":{"hasIncludes":false,"evaluateTime":13247,"scriptSize":283,"renderTime":4},
         * "response":{"getResult":{"data":{"currencyMap":{"Gems":{"consumed":0,"balance":160,"purchased":0,"awarded":160,"revoked":0},
         * "Coins":{"consumed":0,"balance":100,"purchased":0,"awarded":100,"revoked":0}}},"status":200}},"success":true,"reasonCode":null},
         * "status":200}]}
         */
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var firstData =  packet["data"] as Dictionary<string, object>;
        var response = firstData["response"] as Dictionary<string, object>;
        var getResult = response["getResult"] as Dictionary<string, object>;
        var secondData = getResult["data"] as Dictionary<string, object>;
        var currencyMap = secondData["currencyMap"] as Dictionary<string, object>;
        var gems = currencyMap["gems"] as Dictionary<string, object>;
        CurrentUserInfo.UpdateGems((int) gems["balance"]);
        StateManager.Instance.RefreshScreen();
    }
    
    public void LevelUpParent()
    {
        var scriptData = new Dictionary<string, object>();
        scriptData.Add("x", 50);
        Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.INCREASE_XP_FOR_PARENT_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("LevelUpParent Success", OnLevelUpParent),
            HandleFailure("LevelUpParent Failed", OnFailureCallback)
        );
    }
    
    private void OnLevelUpParent(string jsonResponse, object cbObject)
    {
        //UserInfo.UpdateLevel(/*(int) statistics["Level"]*/);
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data =  packet["data"] as Dictionary<string, object>;
        var response = data["response"] as Dictionary<string, object>;
        if(response == null) return;

        if(response.ContainsKey("nextLevelUpXP"))
        {
            CurrentUserInfo.NextLevelUp = (int) response["nextLevelUpXP"];
        }
        if(response.ContainsKey("previousLevelUpReq"))
        {
            CurrentUserInfo.PreviousLevelUp = (int) response["previousLevelUpReq"];
        }
        if(response.ContainsKey("experiencePoints"))
        {
            CurrentUserInfo.CurrentXP = (int) response["experiencePoints"];
        }
        if(response.ContainsKey("level"))
        {
            CurrentUserInfo.UpdateLevel((int) response["level"]);
        }
                
        StateManager.Instance.RefreshScreen();
    }
    
    public void AwardBlingToChild(int in_amount)
    {
        //Params for AwardBlingToChild(childAppId, profileId, increaseAmount)
        Dictionary<string, object> scriptData = new Dictionary<string, object>
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
        /*
         * {"packetId":4,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"evaluateTime":92353,
         * "scriptSize":4953,"renderTime":23},"response":{"runTimeData":{"hasIncludes":false,"evaluateTime":9248,
         * "scriptSize":289,"renderTime":1},"response":{"getResult":{"data":{"currencyMap":
         * {"buddyBling":{"consumed":0,"balance":210,"purchased":0,"awarded":210,"revoked":0}}},"status":200}},
         * "success":true,"reasonCode":null},"success":true,"reasonCode":null},"status":200}]
         */
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data =  packet["data"] as Dictionary<string, object>;
        var response = data["response"] as Dictionary<string, object>;
        var currencyMap = response["currencyMap"] as Dictionary<string, object>;
        var buddyBling = currencyMap["buddyBling"] as Dictionary<string, object>;
        GameManager.Instance.SelectedAppChildrenInfo.buddyBling = (int) buddyBling["balance"];
        StateManager.Instance.RefreshScreen();
    }
    
    private void OnFailureCallback()
    {
        //FL: ToDo: Create an error catching system where we catch reason codes and display them for the user with StateManager
    }

    private Action _updateNameAction;
    public void UpdateChildProfileName(string in_newName, string in_profileId, Action OnSuccessAction)
    {
        if(_isProcessing) return;
        _isProcessing = true;
        _updateNameAction = OnSuccessAction;
        Dictionary<string, object> scriptData = new Dictionary<string, object>
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
        /*
         * {"packetId":13,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"evaluateTime":79720,"scriptSize":4766},
         * "response":{"userAdjusted":{"newName":"nami","profileId":"48cc33fa-b92a-4331-96a9-f2c737bd3d28"}},
         * "success":true,"reasonCode":null},"status":200}]}
         */
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
            if(child.profileId.Equals(profileId))
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

        //var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        /*{"packetId":4,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"evaluateTime":124599,"scriptSize":8130,"renderTime":28},
         "response":{"buddyConfig":{"rarity":"legendary","coinMultiplier":2,"coinPerHour":150,"maxCoinCapacity":1500,"buddyId":"Buddy04"},
         "getProfileResult":{"data":{"children":[{"profileName":"sora","profileId":"abecf46c-8d5f-441d-9acf-8ecaaf665a2b","appId":"49162"},
         {"profileName":"bob","profileId":"d58ec1f2-e465-4aa8-9906-e2dc2b153793","appId":"49162"},{"profileName":"riku",
         "profileId":"959454d3-31f5-433a-9dc8-8e8f96a2657c","appId":"49162"}]},"status":200}},"success":true,"reasonCode":null},"status":200}]}
         */
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data =  packet["data"] as Dictionary<string, object>;
        var response = data["response"] as Dictionary<string, object>;
        var profileChildren = response["children"] as Dictionary<string, object>[];
        var appChildrenInfos = new List<AppChildrenInfo>();
        if (profileChildren != null)
        {
            for (int i = 0; i < profileChildren.Length; i++)
            {
                var summaryData = profileChildren[i]["summaryFriendData"] as Dictionary<string, object>;
                var dataInfo = new AppChildrenInfo();
                //Get Child data
                dataInfo.profileName = profileChildren[i]["profileName"] as string;
                dataInfo.profileId = profileChildren[i]["profileId"] as string;

                if (summaryData != null)
                {
                    dataInfo.summaryFriendData = summaryData;
                    //Get Entity data
                    dataInfo.rarity = summaryData["rarity"] as string;
                    dataInfo.buddySpritePath = summaryData["buddySpritePath"] as string;
                    var multiplier = summaryData["coinMultiplier"] as double?;
                    if (multiplier != null)
                    {
                        dataInfo.coinMultiplier = (float) multiplier;
                    }
                    else
                    {
                        dataInfo.coinMultiplier = 1.0f;
                    }

                    dataInfo.coinPerHour = (int) summaryData["coinPerHour"];
                    dataInfo.maxCoinCapacity = (int) summaryData["maxCoinCapacity"];
                    dataInfo.nextLevelUp = (int) summaryData["nextLevelUpXP"];
                }

                appChildrenInfos.Add(dataInfo);
            }
        }

        if (appChildrenInfos.Count == 0 || appChildrenInfos[0].profileId.IsNullOrEmpty())
        {
            Debug.LogError("Child Profile ID is missing. Cant fetch data.");
            return;
        }
        
        //Stat will be updated on the server from the cloud code script when adding a new child account
        StatTracker.Instance.IncrementStat(BitBuddiesConsts.BUDDIES_OWNED_STAT_NAME);
        
        // Dictionary<string, object> scriptData = new Dictionary<string, object>();
        // scriptData.Add("childAppId", BitBuddiesConsts.APP_CHILD_ID);
        // scriptData.Add("profileId", appChildrenInfos[0].profileId);
        // Wrapper.ScriptService.RunScript
        // (
        //     BitBuddiesConsts.GET_CHILD_ITEM_CATALOG_SCRIPT_NAME,
        //     scriptData.Serialize(),
        //     HandleSuccess("GetItemCatalog Success", OnGetItemCatalog),
        //     HandleFailure("GetItemCatalog Failed", OnFailureCallback)
        // );
        
        _childInfoIndex = 0;
        GameManager.Instance.AppChildrenInfos = appChildrenInfos;
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
