using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToyManager : SingletonBehaviour<ToyManager>
{
    /*
     * Manages what toys are locked or unlocked
     *	- How the heck am I saving that data?
     * Logic for saving picked up currencies
     *	- not sure to send a request for:
         * This sounds expensive for # of calls to be billed - each pick up 
         *	Probably this one -> wait 5 seconds to send bunches of picked up items... 
     * If the user leaves while having rewards still on the floor, then the manager will pick it up for them
     * 
     */
    [SerializeField] private Button MoveAreaButton;
    [SerializeField] private List<ToyBench> ToyBenches;

    private MoveBuddyAnimation _moveBuddyAnimation;
    private const float CHECK_FOR_REWARDS_INTERVAL = 7.5f;
    private string _selectedToyId;
    private List<RewardPickup> _rewardPickups = new List<RewardPickup>();
    private bool _timerStarted;
    private int _currentRewardSpawnAmount;
    private RectTransform _buttonRectTransform;
    private Canvas canvas;
    private float _loveRewardMultiplier = 1.0f;
    private long _loveMultiplierDuration = 1;
    private const float MULTIPLIER_DEFAULT = 1.0f;
    private CountdownTimer _multiplierCountdownTimer;

    public static event Action<int> OnBuddyLeveledUp;
    public static event Action<int> OnCoinsTaken;

    private bool _isRunning = false;

    public override void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
            name = GetType().Name;
        }
        else
        {
            Destroy(gameObject);
        }

        SetUpToyBenches();
        CheckForAvailableBenches();
        _moveBuddyAnimation = FindFirstObjectByType<MoveBuddyAnimation>();
        _buttonRectTransform = MoveAreaButton.GetComponent<RectTransform>();
        MoveAreaButton.onClick.AddListener(MoveToPosition);
        if (canvas == null)
            canvas = MoveAreaButton.GetComponentInParent<Canvas>();
        var appInfo = GameManager.Instance.SelectedAppChildrenInfo;
        _multiplierCountdownTimer = GetComponent<CountdownTimer>();
        if (appInfo.loveMultiplier > MULTIPLIER_DEFAULT)
        {
            StartLoveMultiplierCountdown(appInfo.loveMultiplier, appInfo.dailyBoosterExpiryUntil);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public void CheckForUnlockedBenches()
    {
        foreach (ToyBench bench in ToyBenches)
        {
            bench.CheckIfBenchIsAvailable();
        }
    }

    private void MoveToPosition()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle
        (
            _buttonRectTransform,
            Input.mousePosition,
            canvas.worldCamera,
            out Vector2 position
        );

        _moveBuddyAnimation.MoveBuddyToPosition(position);
    }

    public void MoveToPositionWithCallback(Action callback)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle
        (
            _buttonRectTransform,
            Input.mousePosition,
            canvas.worldCamera,
            out Vector2 position
        );

        _moveBuddyAnimation.MoveBuddyToPosition(position, callback);
    }

    public static void ReportBuddyLeveledUp(int levelsGained)
    {
        if (levelsGained < 1)
        {
            levelsGained = 1;
        }

        OnBuddyLeveledUp?.Invoke(levelsGained);
    }

    // We use the xpRewards payload to count the levels (Buddy levels are tied to achievements)
    public static void ReportBuddyLeveledUp(Dictionary<string, object> increaseXpResult)
    {
        int levelsGained = 1;
        var xpRewards = increaseXpResult?.GetJSONObject("rewards");
        if (xpRewards != null &&
            xpRewards.TryGetValue("experienceLevels", out object xpLevelsObj) &&
            xpLevelsObj is System.Collections.IList xpLevelsList &&
            xpLevelsList.Count > 0)
        {
            levelsGained = xpLevelsList.Count;
        }

        ReportBuddyLeveledUp(levelsGained);
    }

    public void IncrementRewardSpawnCount(int in_amount)
    {
        _currentRewardSpawnAmount += in_amount;
    }

    public void StartLoveMultiplierCountdown(float multiplier, long duration)
    {
        if (_isRunning) return;
        _isRunning = true;
        _loveMultiplierDuration = duration;
        _loveRewardMultiplier = multiplier;

        _multiplierCountdownTimer.StartCountdown(_loveMultiplierDuration);

        StartCoroutine(LoveMultiplierCountdown());
    }

    IEnumerator LoveMultiplierCountdown()
    {
        var timeSpan = CountdownTimer.GetRemainingTime(_loveMultiplierDuration);
        float seconds = (float)timeSpan.TotalSeconds;

        yield return new WaitForSeconds(seconds);
        _loveRewardMultiplier = MULTIPLIER_DEFAULT;
        _isRunning = false;
    }

    public void DecrementRewardSpawnCount()
    {
        _currentRewardSpawnAmount--;
    }

    public void OnRewardExpired()
    {
        DecrementRewardSpawnCount();
        if (_currentRewardSpawnAmount <= 0)
        {
            CheckForSendingRewards();
        }
    }

    public void FlushPendingRewards()
    {
        CheckForSendingRewards();
    }

    IEnumerator LoopCheckRewardsToSend()
    {
        yield return new WaitForSecondsRealtime(CHECK_FOR_REWARDS_INTERVAL);
        CheckForSendingRewards();
        _timerStarted = false;
    }

    private void OnRewardsReceived(string jsonResponse)
    {
        /*
            {"packetId":2,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"compileTime":40583,"scriptSize":12594,"renderTime":44,"executeTime":244216},"
            response":{"incrementCoinsResult":{"consumed":221000,"balance":47142,"purchased":0,"awarded":268142,"revoked":0},
            "statResult":{"data":{"rewardDetails":{},"currency":{},"rewards":{},
            "statistics":{"CoinsGainedForParent":1837,"LoveEarned":12}},"status":200},
            "xpLoveResult":{"data":{"experiencePoints":1325,"rewardDetails":{},"currency":{},"xpCapped":false,"experienceLevel":10,"rewards":{}},"status":200},
            "blingResult":null},"success":true,"reasonCode":null},"status":200}]}
         */

        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = packet["data"] as Dictionary<string, object>;
        var response = data["response"] as Dictionary<string, object>;
        var beforeAmount = BrainCloudManager.Instance.CurrentUserInfo.Coins;
        var selectedAppChildInfo = GameManager.Instance.SelectedAppChildrenInfo;
        if (response != null)
        {
            if (response.TryGetValue("incrementCoinsResult", out var value))
            {
                var incrementCoinsResult = value as Dictionary<string, object>;
                if (incrementCoinsResult != null)
                {
                    BrainCloudManager.Instance.CurrentUserInfo.UpdateCoins((int)incrementCoinsResult["balance"]);
                }
            }

            if (response.TryGetValue("statResult", out var statValue))
            {
                var statResult = statValue as Dictionary<string, object>;
                var statData = statResult["data"] as Dictionary<string, object>;
                var statistics = statData["statistics"] as Dictionary<string, object>;
                if (statistics != null)
                {
                    selectedAppChildInfo.coinsEarnedInLifetime = (int)statistics["CoinsGainedForParent"];
                }
            }

            if (response.TryGetValue("playerAchievements", out var achievementsValue))
            {
                var achievements = achievementsValue as string[];
                if (achievements != null && achievements.Length > 0)
                {
                    GameManager.Instance.UpdateAchievementAwarded(achievements);
                }
            }

            if (response.TryGetValue("loveResult", out var xpValue))
            {
                var xpResult = xpValue as Dictionary<string, object>;
                var xpData = xpResult["increaseXpResult"] as Dictionary<string, object>;
                if (xpData != null)
                {
                    selectedAppChildInfo.currentXP = (int)xpData["experiencePoints"];
                    selectedAppChildInfo.buddyLevel = (int)xpData["experienceLevel"];

                    bool didBuddyLevelUp = xpResult.GetValue<bool>("buddyLeveledUp");
                    if (didBuddyLevelUp)
                    {
                        StatTracker.Instance.IncrementStat(BitBuddiesConsts.BUDDIES_LEVELED_UP_STAT_NAME);

                        // A single love batch can cross more than one level; this
                        // reports the total so subscribers act once.
                        ReportBuddyLeveledUp(xpData);
                    }

                    bool didBuddyLevelUpTo5 = xpResult.GetValue<bool>("didCatchLevel5");
                    if (didBuddyLevelUpTo5)
                    {
                        StatTracker.Instance.IncrementStat(BitBuddiesConsts.LEVEL5_BUDDIES_STAT_NAME);
                    }
                }
            }


            if (response.TryGetValue("blingResult", out var blingValue))
            {
                if (blingValue != null)
                {
                    var blingResult = blingValue as Dictionary<string, object>;
                    var blingData = blingResult["data"] as Dictionary<string, object>;
                    var currencyMap = blingData["currencyMap"] as Dictionary<string, object>;
                    var buddyBlingData = currencyMap["buddyBling"] as Dictionary<string, object>;

                    selectedAppChildInfo.buddyBling = (int)buddyBlingData["balance"];
                }
            }

            if (response.TryGetValue("increaseStatResult", out var increaseStatValue))
            {
                if (increaseStatValue != null)
                {
                    var increaseStatResult = increaseStatValue as Dictionary<string, object>;
                    var increaseStatData = increaseStatResult["data"] as Dictionary<string, object>;
                    var statistics = increaseStatData["statistics"] as Dictionary<string, object>;
                    if (statistics != null)
                    {
                        selectedAppChildInfo.coinsEarnedInLifetime = (int)statistics["CoinsGainedForParent"];
                    }
                }
            }

            if (response.TryGetValue("coinResult", out var coinValue))
            {
                if (coinValue != null)
                {
                    var coinResult = coinValue as Dictionary<string, object>;
                    var coinData = coinResult["data"] as Dictionary<string, object>;
                    var currencyMap = coinData["currencyMap"] as Dictionary<string, object>;
                    var coinsData = currencyMap["coins"] as Dictionary<string, object>;

                    BrainCloudManager.Instance.CurrentUserInfo.UpdateCoins((int)coinsData["balance"]);
                }
            }
        }


        var totalDifference = beforeAmount - BrainCloudManager.Instance.CurrentUserInfo.Coins;

        //ToDo Add animations for
        /*
         * Coins
         * Buddy Bling
         * Love aka xp
         */

        StateManager.Instance.RefreshScreen();
    }

    private void CheckForSendingRewards()
    {
        // Nothing has been collected since the last send
        if (_rewardPickups == null || _rewardPickups.Count == 0)
        {
            return;
        }
        var rewardsByEntity = new Dictionary<string, RewardInfo>();
        for (int i = 0; i < _rewardPickups.Count; i++)
        {
            RewardPickup pickup = _rewardPickups[i];
            if (pickup is null || string.IsNullOrEmpty(pickup.EntityId))
            {
                continue;
            }

            if (!rewardsByEntity.TryGetValue(pickup.EntityId, out RewardInfo info))
            {
                info = new RewardInfo { entityId = pickup.EntityId };
            }

            switch (pickup.CurrencyType)
            {
                case CurrencyTypes.Coins:
                    info.x += pickup.RewardAmount;
                    break;
                case CurrencyTypes.Love:
                    info.z += pickup.RewardAmount * (int)_loveRewardMultiplier;
                    break;
                case CurrencyTypes.BuddyBling:
                    info.y += pickup.RewardAmount;
                    break;
            }

            rewardsByEntity[pickup.EntityId] = info;
        }

        // Everything collected has now been captured, so clear tracking
        _rewardPickups.Clear();

        if (rewardsByEntity.Count == 0)
        {
            return;
        }

        RewardInfo[] listOfRewardInfo = new RewardInfo[rewardsByEntity.Count];
        rewardsByEntity.Values.CopyTo(listOfRewardInfo, 0);

        Dictionary<string, object> scriptData = new Dictionary<string, object>();
        scriptData.Add("childAppId", BitBuddiesConsts.APP_CHILD_ID);
        scriptData.Add("profileId", GameManager.Instance.SelectedAppChildrenInfo.profileId);
        scriptData.Add("rewards", listOfRewardInfo.Serialize());
        BrainCloudManager.Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.TOY_REWARD_RECEIVED_SCRIPT_NAME,
            scriptData.Serialize(),
            BrainCloudManager.HandleSuccess("Toy Reward Received Success", OnRewardsReceived)
        );
    }

    public void AddRewardPickup(RewardPickup in_rewardPickup)
    {
        _rewardPickups.Add(in_rewardPickup);
        switch (in_rewardPickup.CurrencyType)
        {
            case CurrencyTypes.Coins:
                UserInfo userInfo = BrainCloudManager.Instance.CurrentUserInfo;
                var amount = userInfo.Coins + in_rewardPickup.RewardAmount;
                userInfo.UpdateCoins(amount);
                break;
            case CurrencyTypes.Love:
                AppChildrenInfo childInfo = GameManager.Instance.SelectedAppChildrenInfo;
                childInfo.currentXP += in_rewardPickup.RewardAmount * (int)_loveRewardMultiplier;
                childInfo.PredictLevelFromXP(); // Check if we need to show the Buddy's leveled up
                break;
            case CurrencyTypes.BuddyBling:
                GameManager.Instance.SelectedAppChildrenInfo.buddyBling += in_rewardPickup.RewardAmount;
                break;
        }
        StateManager.Instance.RefreshScreen();

        //We've picked up everything on the floor
        if (_currentRewardSpawnAmount <= 0)
        {
            CheckForSendingRewards();
        }
        else if (!_timerStarted) // Otherwise just start the timer to send reward info after timer is up
        {
            _timerStarted = true;
            StartCoroutine(LoopCheckRewardsToSend());
        }
    }

    private void SetUpToyBenches()
    {
        var listOfInfo = GameManager.Instance.ToyBenchInfos;
        for (int i = 0; i < ToyBenches.Count; i++)
        {
            ToyBenches[i].SetUpToyBench(listOfInfo[i]);
        }
    }

    private void CheckForAvailableBenches()
    {
        var getUserUnlockedBenches = GameManager.Instance.SelectedAppChildrenInfo.ownedToys;
        if (getUserUnlockedBenches != null && getUserUnlockedBenches.Count > 0)
        {
            foreach (ToyBench toyBench in ToyBenches)
            {
                foreach (string benchId in getUserUnlockedBenches)
                {
                    if (!benchId.IsNullOrEmpty() && toyBench != null && !toyBench.BenchId.IsNullOrEmpty())
                    {
                        if (toyBench.BenchId.Equals(benchId, System.StringComparison.OrdinalIgnoreCase))
                        {
                            toyBench.EnableBench();
                            break;
                        }

                        toyBench.DisableBench();
                    }
                }
            }
        }
        else
        {
            foreach (ToyBench toyBench in ToyBenches)
            {
                toyBench.DisableBench();
            }
        }
    }

    private Action _toyBenchUIRefreshCallback;
    public void ObtainToy(string in_benchId, Action uiCallback)
    {
        if (in_benchId.Equals(""))
            return;
        if (GameManager.Instance.SelectedAppChildrenInfo.ownedToys.Contains(in_benchId))
            return;

        _toyBenchUIRefreshCallback = uiCallback;
        _selectedToyId = in_benchId;
        var scriptData = new Dictionary<string, object>();
        scriptData.Add("toyId", in_benchId);
        scriptData.Add("childAppId", BitBuddiesConsts.APP_CHILD_ID);
        scriptData.Add("profileId", GameManager.Instance.SelectedAppChildrenInfo.profileId);
        BrainCloudManager.Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.OBTAIN_TOY_SCRIPT_NAME,
            scriptData.Serialize(),
            BrainCloudManager.HandleSuccess("Obtain Toy Success", OnObtainToySuccess),
            BrainCloudManager.HandleFailure("Obtain Toy Failure", OnObtainToyFailure)
        );
    }

    private void OnObtainToySuccess(string jsonResponse)
    {
        /*
         * {"packetId":10,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"scriptSize":12672,"executeTime":117410},
         * "response":{"consumeResult":{"data":{"currencyMap":{"gems":{"consumed":0,"balance":0,"purchased":0,"awarded":0,"revoked":0},
         * "coins":{"consumed":31000,"balance":6619,"purchased":0,"awarded":37619,"revoked":0}}},"status":200}}
         * ,"success":true,"reasonCode":null},"status":200}]}
         */
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = packet["data"] as Dictionary<string, object>;
        var response = data["response"] as Dictionary<string, object>;
        var beforeAmount = BrainCloudManager.Instance.CurrentUserInfo.Coins;

        if (response != null && response.TryGetValue("consumeCurrencyResult", out var value))
        {
            var consumeResult = value as Dictionary<string, object>;
            var currencyData = consumeResult["data"] as Dictionary<string, object>;
            var currencyMap = currencyData["currencyMap"] as Dictionary<string, object>;
            var coins = currencyMap["coins"] as Dictionary<string, object>;
            BrainCloudManager.Instance.CurrentUserInfo.UpdateCoins((int)coins["balance"]);
        }

        StatTracker.Instance.IncrementStat(BitBuddiesConsts.TOYS_BOUGHT_STAT_NAME);

        if (_selectedToyId.Equals("scienceTable"))
        {
            StatTracker.Instance.IncrementStat(BitBuddiesConsts.SCIENCE_KITS_BOUGHT_STAT_NAME);
        }

        GameManager.Instance.SelectedAppChildrenInfo.ownedToys.Add(_selectedToyId);

        CheckForAvailableBenches();

        _toyBenchUIRefreshCallback?.Invoke();
        var totalDifference = beforeAmount - BrainCloudManager.Instance.CurrentUserInfo.Coins;
        if (totalDifference > 0)
        {
            OnCoinsTaken?.Invoke(totalDifference);
        }
    }

    private void OnObtainToyFailure()
    {
        PopUpUI.Show(BitBuddiesConsts.SOMETHING_WENT_WRONG_TITLE)
               .AddBodyText(BitBuddiesConsts.SOMETHING_WENT_WRONG_MESSAGE);
    }

    private ToyBench GetToyBench(string in_benchId)
    {
        foreach (ToyBench toyBench in ToyBenches)
        {
            if (in_benchId.Equals(toyBench.BenchId, System.StringComparison.OrdinalIgnoreCase))
            {
                //That means the user owns this bench and can enable it.
                return toyBench;
            }
        }

        return null;
    }

    private ToyBenchInfo GetToyBenchInfo(string in_benchId)
    {
        var benchInfo = GameManager.Instance.ToyBenchInfos;
        foreach (ToyBenchInfo toyBenchInfo in benchInfo)
        {
            if (in_benchId.Equals(toyBenchInfo.BenchId, System.StringComparison.OrdinalIgnoreCase))
            {
                return toyBenchInfo;
            }
        }

        return new ToyBenchInfo();
    }
}
