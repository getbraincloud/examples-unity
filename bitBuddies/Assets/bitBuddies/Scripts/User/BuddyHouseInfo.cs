using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BuddyHouseInfo : MonoBehaviour
{
    public AppChildrenInfo HouseInfo;
    [SerializeField] private UnityEngine.UI.Button _visitButton;
    [SerializeField] private UnityEngine.UI.Button _secondVisitButton;
    [SerializeField] private UnityEngine.UI.Button _deleteButton;
    [SerializeField] private PopUpUI PopUpPrefab;
    [SerializeField] private TMP_Text _buddyNameText;
    [SerializeField] private UnityEngine.UI.Image _buddySprite;
    [SerializeField] private UnityEngine.UI.Button _collectCoinsButton;
    private Transform _parentTransform;
    private int enableCollectCoinsButtonMinValue = 1;

    //This is specifically for when the user visits a buddy without collecting any coins.
    private bool _saveCoinsCollected;
    private RectTransform _coinButtonRectTransform;
    private RectTransform _spawnRectTransform;
    private ParentMenu _parentMenu;
    public static event Action<int, int> OnCoinsCollected;

    public void SetUpHouse()
    {
        _collectCoinsButton.onClick.AddListener(OnCollectCoinsButton);
        _visitButton.onClick.AddListener(GoToBuddysRoom);
        _secondVisitButton.onClick.AddListener(GoToBuddysRoom);
        _deleteButton.onClick.AddListener(OnDeleteButton);
        _parentTransform = FindAnyObjectByType<ParentMenu>().transform;
        _buddySprite.sprite = AssetLoader.LoadBuddySprite(HouseInfo.buddySpritePath);
        _buddyNameText.text = HouseInfo.profileName.IsNullOrEmpty() ? "Missing Name" : HouseInfo.profileName;
        _parentMenu = FindAnyObjectByType<ParentMenu>();
        _coinButtonRectTransform = _collectCoinsButton.GetComponent<RectTransform>();
        CheckCoinsButton();
    }

    private void OnDestroy()
    {
        _collectCoinsButton.onClick.RemoveAllListeners();
        _visitButton.onClick.RemoveAllListeners();
        _secondVisitButton.onClick.RemoveAllListeners();
        _deleteButton.onClick.RemoveAllListeners();
    }

    public void CheckCoinsButton()
    {
        if (HouseInfo.coinsEarnedInHolding >= enableCollectCoinsButtonMinValue)
        {
            _collectCoinsButton.gameObject.SetActive(true);
        }
        else
        {
            _collectCoinsButton.gameObject.SetActive(false);
        }
    }

    private void GoToBuddysRoom()
    {
        if (HouseInfo.coinsEarnedInHolding >= enableCollectCoinsButtonMinValue)
        {
            BrainCloudManager.Instance.IsProcessingRequest = true;
            _saveCoinsCollected = true;
            OnCollectCoinsButton();
        }
        GameManager.Instance.SelectedAppChildrenInfo = HouseInfo;
        StateManager.Instance.GoToBuddysRoom();

        //Increment stat for visited buddies
        var statData = new Dictionary<string, object>();
        statData.Add(BitBuddiesConsts.VISIT_BUDDIES_STAT_NAME, 1);
        BrainCloudManager.Client.PlayerStatisticsService.IncrementUserStats(statData.Serialize());
        StatTracker.Instance.IncrementStat(BitBuddiesConsts.VISIT_BUDDIES_STAT_NAME);

    }

    private void OnDeleteButton()
    {
        if (GameManager.Instance.AppChildrenInfos.Count <= 1)
        {
            PopUpUI.Show(BitBuddiesConsts.CANT_DELETE_BUDDY_TITLE)
                   .AddBodyText(BitBuddiesConsts.CANT_DELETE_BUDDY_MESSAGE);
        }
        else
        {
            PopUpUI.Show(GetDeleteTitleMessage(), false)
                   .AddBodyText(GetDeleteBodyMessage())
                   .AddButton("Close", PopUpUI.ButtonColor.Blue, null)
                   .AddButton("Confirm", PopUpUI.ButtonColor.Green, DeleteBuddyRoom);
        }
    }

    private string GetDeleteTitleMessage()
    {
        if (HouseInfo.profileName.IsNullOrEmpty())
        {
            return BitBuddiesConsts.DELETE_BUDDYS_ROOM_TITLE + BitBuddiesConsts.DEFAULT_BUDDY_NAME + "'s home?";
        }

        return BitBuddiesConsts.DELETE_BUDDYS_ROOM_TITLE + HouseInfo.profileName + "'s?";
    }

    private string GetDeleteBodyMessage()
    {
        if (HouseInfo.profileName.IsNullOrEmpty())
        {
            return BitBuddiesConsts.DELETE_BUDDYS_ROOM_MESSAGE + BitBuddiesConsts.DEFAULT_BUDDY_NAME + "'s home?";
        }

        return BitBuddiesConsts.DELETE_BUDDYS_ROOM_MESSAGE + HouseInfo.profileName + "'s home?";
    }

    private string GetVisitTitleMessage()
    {
        if (HouseInfo.profileName.IsNullOrEmpty())
        {
            return BitBuddiesConsts.GO_BUDDYS_ROOM_TITLE + BitBuddiesConsts.DEFAULT_BUDDY_NAME + "'s home?";
        }

        return BitBuddiesConsts.GO_BUDDYS_ROOM_TITLE + HouseInfo.profileName + "'s home?";
    }

    private string GetVisitBodyMessage()
    {
        if (HouseInfo.profileName.IsNullOrEmpty())
        {
            return BitBuddiesConsts.GO_BUDDYS_ROOM_MESSAGE + BitBuddiesConsts.DEFAULT_BUDDY_NAME + "'s home and collect any available coins?";
        }

        return BitBuddiesConsts.GO_BUDDYS_ROOM_MESSAGE + HouseInfo.profileName + "'s home and collect any available coins?";
    }

    private void DeleteBuddyRoom()
    {
        GameManager.Instance.SelectedAppChildrenInfo = HouseInfo;
        Dictionary<string, object> scriptData = new Dictionary<string, object>
        {
            {"childAppId", BitBuddiesConsts.APP_CHILD_ID},
            {"childProfileId", HouseInfo.profileId}
        };
        BrainCloudManager.Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.DELETE_CHILD_PROFILE_SCRIPT_NAME,
            scriptData.Serialize(),
            BrainCloudManager.HandleSuccess("Delete Child Profile Success", OnDeleteBuddySuccess),
            BrainCloudManager.HandleFailure("Delete Child Profile Failure", OnDeleteBuddyFailure)
        );
    }

    private void OnDeleteBuddySuccess()
    {
        var popUp = Instantiate(PopUpPrefab, _parentTransform);
        popUp.SetUpInfoPopup(BitBuddiesConsts.DELETE_BUDDYS_ROOM_SUCCESS_TITLE, BitBuddiesConsts.DELETE_BUDDYS_ROOM_SUCCESS_MESSAGE);
        StatTracker.Instance.IncrementStat(BitBuddiesConsts.TRASHED_BUDDIES_STAT_NAME);
        GameManager.Instance.OnDeleteBuddySuccess();
    }

    private void OnDeleteBuddyFailure()
    {
        var popUp = Instantiate(PopUpPrefab, _parentTransform);
        popUp.SetUpInfoPopup(BitBuddiesConsts.DELETE_BUDDYS_ROOM_FAILED_TITLE, BitBuddiesConsts.DELETE_BUDDYES_ROOM_FAILED_MESSAGE);
    }

    public void OnCollectCoinsButton()
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object>();
        scriptData.Add("childAppId", BitBuddiesConsts.APP_CHILD_ID);
        scriptData.Add("profileId", HouseInfo.profileId);
        scriptData.Add("summaryFriendData", HouseInfo.summaryFriendData);
        BrainCloudManager.Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.UPDATE_CHILD_COINS_COLLECTED_SCRIPT_NAME,
            scriptData.Serialize(),
            BrainCloudManager.HandleSuccess("Update Child Coin Timestamp Success", OnUpdateSummaryDataSuccess),
            BrainCloudManager.HandleFailure("Update Child Coin Timestamp Failed", OnUpdateSummaryDataFailure)
        );
    }

    private void OnUpdateSummaryDataSuccess(string jsonResponse)
    {
        /*
            {"packetId":4,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"scriptSize":16649,"executeTime":198940},
            "response":{"currencyMap":{"gems":{"consumed":12200,"balance":730,"purchased":0,"awarded":12930,"revoked":0},
            "coins":{"consumed":272000,"balance":109252,"purchased":0,"awarded":381252,"revoked":0},"fakeDollars":
            {"consumed":0,"balance":200,"purchased":0,"awarded":200,"revoked":0}},"xpAwarded":452.0,
            "increaseXpResult":{"experiencePoints":609,"rewardDetails":{"xp":{"experienceLevels":[{"level":3,"rewards":{"currency":{"gems":50}}},
            {"level":4,"rewards":{"currency":{"coins":7000}}},{"level":5,"rewards":{"currency":{"coins":8000}}},
            {"level":6,"rewards":{"currency":{"gems":100}}}]}},
            "currency":{"gems":{"consumed":12200,"balance":880,"purchased":0,"awarded":13080,"revoked":0},
            "coins":{"consumed":272000,"balance":124252,"purchased":0,"awarded":396252,"revoked":0},
            "fakeDollars":{"consumed":0,"balance":200,"purchased":0,"awarded":200,"revoked":0}},"xpCapped":false,"experienceLevel":6,
            "rewards":{"experienceLevels":[3,4,5,6],"currency":{"gems":150,"coins":15000}}},"nextLevelUpXP":750,"summaryData":{"coinMultiplier":4,
            "coinPerHour":300,"maxCoinCapacity":3600,"buddySpritePath":"BuddySprites/buddy-4","rarity":"legendary","level":7,"experiencePoints":750,
            "lastIdleTimestamp":1.777569123054E12,"nextLevelUpXP":910},"statResult":{"data":{"rewardDetails":{},"currency":{},"rewards":{},
            "statistics":{"CoinsGainedForParent":8356,"LoveEarned":0}},"status":200}},"success":true,"reasonCode":null},"status":200}]}
         */
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = packet["data"] as Dictionary<string, object>;
        var response = data["response"] as Dictionary<string, object>;
        var currentUser = BrainCloudManager.Instance.CurrentUserInfo;

        int amountToReward = response.GetValue<int>("amountToReward");

        var currencyMap = response["currencyMap"] as Dictionary<string, object>;
        var coinsObj = currencyMap["coins"] as Dictionary<string, object>;
        currentUser.UpdateCoins(coinsObj.GetValue<int>("balance"));

        GameManager.Instance.CoinsCollectedViaVisit = _saveCoinsCollected ? amountToReward : 0;
        _saveCoinsCollected = false;
        BrainCloudManager.Instance.IsProcessingRequest = false;

        var summaryData = response["summaryData"] as Dictionary<string, object>;

        HouseInfo.lastIdleTimestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)summaryData["lastIdleTimestamp"]).UtcDateTime;
        HouseInfo.coinsEarnedInHolding = 0;
        if (_collectCoinsButton != null)
        {
            CheckCoinsButton();
        }

        var statResult = response["statResult"] as Dictionary<string, object>;
        var statData = statResult["data"] as Dictionary<string, object>;
        var statistics = statData["statistics"] as Dictionary<string, object>;

        HouseInfo.coinsEarnedInLifetime = (int)statistics["CoinsGainedForParent"];

        //Fire UI event for floating coins and level animation
        OnCoinsCollected?.Invoke(amountToReward, 0);

        if (_spawnRectTransform == null && _parentMenu != null && _parentMenu.transform != null)
        {
            _spawnRectTransform = _parentMenu.transform.GetChild(1).GetComponent<RectTransform>();

            RectTransform target = _parentMenu.GetCurrencyTextRectTransform(CurrencyTypes.Coins);
            _coinButtonRectTransform.position -= Vector3.left * 50;
            StateManager.Instance.PlayCurrencyAnimationWorld(_coinButtonRectTransform, target, CurrencyTypes.Coins, _parentMenu.CanvasRectTransform, _spawnRectTransform);
        }

        BrainCloudManager.Instance.CurrentUserInfo = currentUser;
        StateManager.Instance.RefreshScreen();
    }

    private void OnUpdateSummaryDataFailure()
    {
        //Check to see if its an error saying its empty,
        //If so then create the entity now.
    }
}
