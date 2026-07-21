using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuddyHouseInfo : MonoBehaviour
{
    private const int MINIMUM_COINS_FOR_COLLECTION = 1;

    [SerializeField] private Button _visitButton;
    [SerializeField] private Button _secondVisitButton;
    [SerializeField] private Button _deleteButton;
    [SerializeField] private TMP_Text _buddyNameText;
    [SerializeField] private Image _buddySprite;
    [SerializeField] private Button _collectCoinsButton;

    public AppChildrenInfo HouseInfo { get; set; }

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
        _collectCoinsButton.gameObject.SetActive(HouseInfo.GetCoinsEarned() >= MINIMUM_COINS_FOR_COLLECTION);
    }

    private void GoToBuddysRoom()
    {
        GameManager.Instance.SelectedAppChildrenInfo = HouseInfo;
        StateManager.Instance.GoToBuddysRoom();

        //Increment stat for visited buddies
        var statData = new Dictionary<string, object>
        {
            { BitBuddiesConsts.VISIT_BUDDIES_STAT_NAME, 1 }
        };

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
        PopUpUI.Show(BitBuddiesConsts.DELETE_BUDDYS_ROOM_SUCCESS_TITLE, true)
               .AddBodyText(BitBuddiesConsts.DELETE_BUDDYS_ROOM_SUCCESS_MESSAGE);

        StatTracker.Instance.IncrementStat(BitBuddiesConsts.TRASHED_BUDDIES_STAT_NAME);
        GameManager.Instance.OnDeleteBuddySuccess();
    }

    private void OnDeleteBuddyFailure()
    {
        PopUpUI.Show(BitBuddiesConsts.DELETE_BUDDYS_ROOM_FAILED_TITLE, true)
               .AddBodyText(BitBuddiesConsts.DELETE_BUDDYES_ROOM_FAILED_MESSAGE);
    }

    public void OnCollectCoinsButton()
    {
        if (HouseInfo.GetCoinsEarned() >= MINIMUM_COINS_FOR_COLLECTION)
        {
            _collectCoinsButton.gameObject.SetActive(false);

            Dictionary<string, object> scriptData = new()
            {
                { "childAppId", BitBuddiesConsts.APP_CHILD_ID },
                { "profileId", HouseInfo.profileId }
            };

            BrainCloudManager.Wrapper.ScriptService.RunScript
            (
                BitBuddiesConsts.UPDATE_CHILD_COINS_COLLECTED_SCRIPT_NAME,
                scriptData.Serialize(),
                BrainCloudManager.HandleSuccess("Update Child Coin Timestamp Success", OnCoinsCollectedSuccess),
                BrainCloudManager.HandleFailure("Update Child Coin Timestamp Failed", OnCoinsCollectedFailure)
            );
        }
    }

    private void OnCoinsCollectedSuccess(string jsonResponse)
    {
        var data = jsonResponse.Deserialize("data", "response");
        var currentUser = BrainCloudManager.Instance.CurrentUserInfo;

        int amountToReward = data.GetValue<int>("amountToReward");

        currentUser.UpdateCoins(data.GetJSONObject("currencyMap")
                                   ?.GetJSONObject("coins")
                                   ?.GetValue<int>("balance") is int coins && coins > currentUser.Coins ? coins : currentUser.Coins);

        GameManager.Instance.CoinsCollectedViaVisit = _saveCoinsCollected ? amountToReward : 0;
        _saveCoinsCollected = false;
        BrainCloudManager.Instance.IsProcessingRequest = false;

        HouseInfo.lastIdleTimestamp = data.GetDateTime("newLastIdleTimestamp");
        HouseInfo.coinsEarnedInLifetime = data.GetJSONObject("statResult")
                                             ?.GetJSONObject("data")
                                             ?.GetJSONObject("statistics")
                                             ?.GetValue<int>("CoinsGainedForParent") is int gained && gained > HouseInfo.coinsEarnedInLifetime ? gained : HouseInfo.coinsEarnedInLifetime;

        // Fire UI event for floating coins and level animation
        if (amountToReward > 0)
        {
            OnCoinsCollected?.Invoke(amountToReward, 0);
        }

        if (_spawnRectTransform == null && _parentMenu != null && _parentMenu.transform != null)
        {
            _spawnRectTransform = _parentMenu.transform.GetChild(0).GetComponent<RectTransform>();

            RectTransform target = _parentMenu.GetCurrencyTextRectTransform(CurrencyTypes.Coins);
            _coinButtonRectTransform.position -= Vector3.left * 50;
            StateManager.Instance.PlayCurrencyAnimationWorld(_coinButtonRectTransform, target, CurrencyTypes.Coins, _parentMenu.CanvasRectTransform, _spawnRectTransform);
        }

        BrainCloudManager.Instance.CurrentUserInfo = currentUser;
        StateManager.Instance.RefreshScreen();
    }

    private void OnCoinsCollectedFailure()
    {
        //Check to see if its an error saying its empty,
        //If so then create the entity now.
    }
}
