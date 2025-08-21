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
    private bool _reconnectUser;
    public static BrainCloudWrapper Wrapper { get; private set; } 
    [SerializeField] private UserInfo _userInfo;
    public UserInfo UserInfo
    {
        get => _userInfo;
        set {_userInfo = value;}
    }

    private bool _isProcessing;
    public bool IsProcessingRequest
    {
        get { return _isProcessing; }
    }

    private int _childInfoIndex;
    private bool _statsRetrieved;
    private bool _currencyRetrieved;

    public override void StartUp()
    {
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
        _reconnectUser = true;
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
        if(username.IsNullOrEmpty() && !UserInfo.Username.IsNullOrEmpty())
        {
            Wrapper.PlayerStateService.UpdateName(UserInfo.Username);
        }
        else
        {
            UserInfo.UpdateUsername(username);
        }
            
        var email = data["emailAddress"] as string;
        if(email.IsNullOrEmpty() && !UserInfo.Email.IsNullOrEmpty())
        {
            Wrapper.PlayerStateService.UpdateContactEmail(UserInfo.Email);
        }
        else 
        {
            UserInfo.UpdateEmail(email);
        }
        var currency = data["currency"] as Dictionary<string, object>;
        if(currency != null)
        {
            var gems = currency["Gems"] as Dictionary<string, object>;
            UserInfo.UpdateGems((int)gems["balance"]);
            
            var coins = currency["Coins"] as Dictionary<string, object>;
            UserInfo.UpdateCoins((int)coins["balance"]);
        }
        var stats = data["statistics"] as Dictionary<string, object>;
        if(stats != null)
        {
            UserInfo.UpdateLevel((int)stats["Level"]);
        }
        
        Dictionary<string, object> scriptData = new Dictionary<string, object> {{"childAppId", BrainCloudConsts.APP_CHILD_ID}};
        Wrapper.ScriptService.RunScript
        (
            BrainCloudConsts.GET_CHILD_ACCOUNTS_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("Getting Child Accounts Success", OnGetChildAccounts),
            HandleFailure("Getting Child Accounts Failed", OnFailureCallback)
        );
        string[] propertyNames = new [] {"MysteryBoxInfo"}; 
        Wrapper.GlobalAppService.ReadSelectedProperties
        (
            propertyNames, 
            HandleSuccess("Get Mystery Box Info Success", OnGetMysteryBoxInfo),
            HandleFailure("Get Mystery Box Info Failed", OnFailureCallback)
        );
    }
    
    private void OnGetMysteryBoxInfo(string jsonResponse)
    {
        /*
         * {"data":{"MysteryBoxInfo":{"name":"MysteryBoxInfo","value":"
         * {\"CommonBox\":{\"unlockType\": \"coins\",\"unlockAmount\": 5000,\"rarity\": \"Common\",
         * \"boxName\": \"Common Box\"},\"UncommonBox\": {\"unlockType\": \"coins\",\"unlockAmount\": 10000,\"rarity\": \"Uncommon\",\"boxName\": \"Uncommon Box\"},\"RareBox\": {\"unlockType\": \"coins\",\"unlockAmount\": 15000,\"rarity\": \"Rare\",\"boxName\": \"Rare Box\"},\"LegendaryBox\": {\"unlockType\": \"coins\",\"unlockAmount\": 20000,\"rarity\": \"Legendary\",\"boxName\": \"Legendary Box\"}}"}},"status":200}]}
         */
        var response = (Dictionary<string, object>)JsonReader.Deserialize(jsonResponse);
        var data = (Dictionary<string, object>)response["data"];
        var mysteryBoxInfo = (Dictionary<string, object>)data["MysteryBoxInfo"];
        string innerJson = (string)mysteryBoxInfo["value"];
        var lootboxes = (Dictionary<string, object>)JsonReader.Deserialize(innerJson);
        var listOfBoxInfo =  new List<MysteryBoxInfo>();

        foreach (var keyValuePair in lootboxes)
        {
            var boxDict = (Dictionary<string, object>)keyValuePair.Value;
            
            MysteryBoxInfo boxInfo = new MysteryBoxInfo();
            boxInfo.Rarity = boxDict["rarity"] as string;
            boxInfo.BoxName = boxInfo.Rarity + " Box";
            boxInfo.UnlockType = Enum.Parse<UnlockTypes>((string)boxDict["unlockType"]);
            boxInfo.UnlockAmount = (int)boxDict["unlockAmount"];
            
            listOfBoxInfo.Add(boxInfo);
        }
        GameManager.Instance.MysteryBoxes = listOfBoxInfo;
    }
    
    private void OnGetChildAccounts(string jsonResponse)
    {
        /*
        {"packetId":1,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"evaluateTime":162916,"scriptSize":6845,"renderTime":17},
        "response":{"childEntityData":[{"childData":{"profileName":"bob","profileId":"4e046b26-7508-4b74-964c-96a7216f0136","appId":"49162"},
        "entityData4":{"createdAt":1755280006783,"data":{"rarity":"rare","coinMultiplier":1.5,"coinPerHour":100,"maxCoinCapacity":1000,"buddyId":"Buddy03"},
        "entityType":"buddies","_serverTime":1755280065784,"entityId":"7e67c09d-62a5-4457-b7a9-d99f54759f3c","acl":{"other":0},"version":1,
        "updatedAt":1755280006783}}]},"success":true,"reasonCode":null},"status":200}]}
         */
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data =  packet["data"] as Dictionary<string, object>;
        var response = data["response"] as Dictionary<string, object>;
        var getChildAccountObject = response["getChildProfiles"] as Dictionary<string, object>;
        var data2 = getChildAccountObject["data"] as Dictionary<string, object>;
        var children = data2["children"] as Dictionary<string, object>[];
        var appChildrenInfos = new List<AppChildrenInfo>();
        if(children == null || children.Length == 0)
        {
            StateManager.Instance.RefreshScreen();
            _isProcessing = false;
            return;
        }
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
                //Get Summary data
                dataInfo.rarity = summaryFriendData["rarity"] as string;
                dataInfo.buddyType = BuddyType.Buddy01;
                var multiplier = summaryFriendData["coinMultiplier"] as double?;
                if(multiplier != null)
                {
                    dataInfo.coinMultiplier = (float) multiplier;
                }
                else
                {
                    dataInfo.coinMultiplier = 1.0f;
                }
                dataInfo.coinPerHour = (int) summaryFriendData["coinPerHour"];
                dataInfo.maxCoinCapacity = (int) summaryFriendData["maxCoinCapacity"];   
            }
            
            appChildrenInfos.Add(dataInfo);
        }

        
        if (appChildrenInfos.Count == 0 || appChildrenInfos[0].profileId.IsNullOrEmpty())
        {
            //Debug.LogError("Child Profile ID is missing. Cant fetch data.");
            //return;
        }
        
        _childInfoIndex = 0;
        GameManager.Instance.AppChildrenInfos = appChildrenInfos;
        if(appChildrenInfos.Count > 0)
        {
            GetChildStatsAndCurrencyData();   
        }
        else
        {
            CompletedGettingCurrencies();
        }
    }
    
    private void GetChildStatsAndCurrencyData()
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object>
        {
            {"childAppId", BrainCloudConsts.APP_CHILD_ID},
            {"childProfileId", GameManager.Instance.AppChildrenInfos[_childInfoIndex].profileId}
        };
        
        //Get data from cloud code scripts
        Wrapper.ScriptService.RunScript
        (
            BrainCloudConsts.GET_STATS_SCRIPT_NAME, 
            scriptData.Serialize(), 
            HandleSuccess("Stats Retrieved", OnGetStatsSuccess), 
            HandleFailure("Getting Stats Failed", OnFailureCallback)
        );
            
        Wrapper.ScriptService.RunScript
        (
            BrainCloudConsts.GET_CURRENCIES_SCRIPT_NAME,
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
        
        var childStatsResponse = response["childStats"] as Dictionary<string, object>;
        var childStatistics =  childStatsResponse["statistics"] as Dictionary<string, object>;
        GameManager.Instance.AppChildrenInfos[_childInfoIndex].buddyLove =  (int)childStatistics["Love"];
        
        if(_childInfoIndex < GameManager.Instance.AppChildrenInfos.Count - 1)
        {
            if(_statsRetrieved && _currencyRetrieved)
            {
                _childInfoIndex++;
                GetChildStatsAndCurrencyData();   
            }
        }
        if(UserInfo.Coins > 0)
        {
            CompletedGettingCurrencies();
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
        if(UserInfo.Level > 0)
        {
            CompletedGettingCurrencies();
        }
    }
    
    private void CompletedGettingCurrencies()
    {
        _isProcessing = false;
        StateManager.Instance.RefreshScreen();
    }
    
    public void RewardCoinsToParent(int in_coins)
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object> {{"increaseAmount", in_coins}};
        Wrapper.ScriptService.RunScript
        (
            BrainCloudConsts.AWARD_COINS_SCRIPT_NAME,
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
        var coins = currencyMap["Coins"] as Dictionary<string, object>;
        UserInfo.UpdateCoins((int) coins["balance"]);
        StateManager.Instance.RefreshScreen();
    }
    
    public void RewardGemsToParent(int in_gems)
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object> {{"increaseAmount", in_gems}};
        Wrapper.ScriptService.RunScript
        (
            BrainCloudConsts.AWARD_GEMS_SCRIPT_NAME,
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
        var gems = currencyMap["Gems"] as Dictionary<string, object>;
        UserInfo.UpdateGems((int) gems["balance"]);
        StateManager.Instance.RefreshScreen();
    }
    
    public void LevelUpParent()
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object> {{"Level", 1}};
        Wrapper.PlayerStatisticsService.IncrementUserStats
        (
            scriptData.Serialize(), 
            HandleSuccess("LevelUpParent Success", OnLevelUpParent),
            HandleFailure("LevelUpParent Failed", OnFailureCallback)
        );
    }
    
    private void OnLevelUpParent(string jsonResponse, object cbObject)
    {
        /*
         * {"packetId":2,"responses":[{"data":{"rewardDetails":{},"currency":{},"rewards":{},"statistics":{"Level":3}},"status":200}]}
         */
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var firstData =  packet["data"] as Dictionary<string, object>;
        var statistics = firstData["statistics"] as Dictionary<string, object>;
        UserInfo.UpdateLevel((int) statistics["Level"]);
        StateManager.Instance.RefreshScreen();
    }
    
    public void AddBuddy()
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object> {{"childAppId", BrainCloudConsts.APP_CHILD_ID}};
        Wrapper.ScriptService.RunScript
        (
            BrainCloudConsts.ADD_CHILD_ACCOUNT_SCRIPT_NAME,
            scriptData.Serialize(),
            HandleSuccess("Add Buddy Success", OnAddBuddy),
            HandleFailure("Add Buddy Failed", OnFailureCallback)
        );
    }
    
    private void OnAddBuddy(string jsonResponse)
    {
        
    }
    
    public void AwardBlingToChild(int in_amount)
    {
        //Params for AwardBlingToChild(childAppId, profileId, increaseAmount)
        Dictionary<string, object> scriptData = new Dictionary<string, object>
        {
            {"childAppId", BrainCloudConsts.APP_CHILD_ID},
            {"profileId", GameManager.Instance.SelectedAppChildrenInfo.profileId},
            {"increaseAmount", in_amount}
        };
        Wrapper.ScriptService.RunScript
        (   
            BrainCloudConsts.AWARD_BLING_TO_CHILD_SCRIPT_NAME,
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
    
    }
    
    public void UpdateChildProfileName(string in_newName, string in_profileId)
    {
        if(_isProcessing) return;
        _isProcessing = true;
        Dictionary<string, object> scriptData = new Dictionary<string, object>
        {
            {"childAppId", BrainCloudConsts.APP_CHILD_ID},
            {"newName", in_newName},
            {"profileId", in_profileId},
        };
        Wrapper.ScriptService.RunScript
        (
            BrainCloudConsts.UPDATE_CHILD_PROFILE_NAME_SCRIPT_NAME,
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
        Destroy(FindAnyObjectByType<MysteryBoxPanelUI>().gameObject);
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
    }
    
    // public void AddRandomChildProfile(string in_childName, Rarity in_selectedLootbox)
    // {
    //     Dictionary<string, object> scriptData = new Dictionary<string, object>
    //     {
    //         {"childAppId", BrainCloudConsts.APP_CHILD_ID},
    //         {"lootboxType", in_selectedLootbox},
    //         {"customName", in_childName}
    //     };
    //     
    //     Wrapper.ScriptService.RunScript
    //     (
    //         BrainCloudConsts.AWARD_RANDOM_LOOTBOX_SCRIPT_NAME,
    //         scriptData.Serialize(),
    //         HandleSuccess("Add Child Profile Success", OnAddRandomChildProfile),
    //         HandleFailure("Add Child Profile Failed", OnFailureCallback)
    //     );
    // }
    
    public void OnAddRandomChildProfile(string jsonResponse)
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
        for(int i = 0; i < profileChildren.Length; i++)
        {
            var summaryData = profileChildren[i]["summaryFriendData"] as Dictionary<string, object>;
             var dataInfo = new AppChildrenInfo();
             //Get Child data
             dataInfo.profileName = profileChildren[i]["profileName"] as string;
             dataInfo.profileId = profileChildren[i]["profileId"] as string;
            
             if(summaryData != null)
             {
                 //Get Entity data
                 dataInfo.rarity = summaryData["rarity"] as string;
                 dataInfo.buddyType = BuddyType.Buddy01;
                 var multiplier = summaryData["coinMultiplier"] as double?;
                 if(multiplier != null)
                 {
                     dataInfo.coinMultiplier = (float) multiplier;
                 }
                 else
                 {
                     dataInfo.coinMultiplier = 1.0f;
                 }
                 dataInfo.coinPerHour = (int) summaryData["coinPerHour"];
                 dataInfo.maxCoinCapacity = (int) summaryData["maxCoinCapacity"];
                 dataInfo.buddyLevel = (int) summaryData["level"];
             }
            
            appChildrenInfos.Add(dataInfo);
        }

        
        if (appChildrenInfos.Count == 0 || appChildrenInfos[0].profileId.IsNullOrEmpty())
        {
            Debug.LogError("Child Profile ID is missing. Cant fetch data.");
            return;
        }
        
        _childInfoIndex = 0;
        GameManager.Instance.AppChildrenInfos = appChildrenInfos;
        GetChildStatsAndCurrencyData();
    }
    
        public void OnAddBasicChildProfile(string jsonResponse)
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
        var getProfileResult = response["getProfileResult"] as Dictionary<string, object>;
        var profileChildren = getProfileResult["childEntityData"] as Dictionary<string, object>[];
        var appChildrenInfos = new List<AppChildrenInfo>();
        for(int i = 0; i < profileChildren.Length; i++)
        {
            var childData =  profileChildren[i]["childData"] as  Dictionary<string, object>;
            var entityDataObject = profileChildren[i]["entityData"] as Dictionary<string, object>;
             var dataInfo = new AppChildrenInfo();
             if(childData != null)
             {
                 //Get Child data
                 dataInfo.profileName = childData["profileName"] as string;
                 dataInfo.profileId = childData["profileId"] as string;   
             }
            
             if(entityDataObject != null)
             {
                 var entityData = entityDataObject["data"] as Dictionary<string, object>;
                 if(entityData != null)
                 {
                     //Get Entity data
                     dataInfo.rarity = entityData["rarity"] as string;
                     dataInfo.buddyType = Enum.Parse<BuddyType>(entityData["buddyId"] as string);
                     var multiplier = entityData["coinMultiplier"] as double?;
                     if(multiplier != null)
                     {
                         dataInfo.coinMultiplier = (float) multiplier;
                     }
                     else
                     {
                         dataInfo.coinMultiplier = 1.0f;
                     }
                     dataInfo.coinPerHour = (int) entityData["coinPerHour"];
                     dataInfo.maxCoinCapacity = (int) entityData["maxCoinCapacity"];   
                 }
             }
            
            appChildrenInfos.Add(dataInfo);
        }

        
        if (appChildrenInfos.Count == 0 || appChildrenInfos[0].profileId.IsNullOrEmpty())
        {
            Debug.LogError("Child Profile ID is missing. Cant fetch data.");
            return;
        }
        
        _childInfoIndex = 0;
        GameManager.Instance.AppChildrenInfos = appChildrenInfos;
        GetChildStatsAndCurrencyData();
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
