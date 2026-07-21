using Gameframework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameManager : SingletonBehaviour<GameManager>
{
    [Tooltip("Debug"), SerializeField] public bool Debug;

    private float resetTime = 0.0f;

    private List<AppChildrenInfo> _appChildrenInfos = new List<AppChildrenInfo>();

    public List<AppChildrenInfo> AppChildrenInfos
    {
        get { return _appChildrenInfos; }
        set { _appChildrenInfos = value; }
    }
    private List<MysteryBoxInfo> _mysteryBoxes;
    public List<MysteryBoxInfo> MysteryBoxes
    {
        get => _mysteryBoxes;
        set => _mysteryBoxes = value;
    }
    private AppChildrenInfo _selectedAppChildrenInfo;
    public AppChildrenInfo SelectedAppChildrenInfo
    {
        get { return _selectedAppChildrenInfo; }
        set
        {
            _selectedAppChildrenInfo = value;
            UpdateSelectedAppChildrenInfo();
        }
    }

    private List<ToyBenchInfo> _toyBenchInfos;
    public List<ToyBenchInfo> ToyBenchInfos
    {
        get => _toyBenchInfos;
        set => _toyBenchInfos = value;
    }

    private float _xpAcquiredAmount;
    public float XpAcquiredAmount
    {
        get => _xpAcquiredAmount;
        set => _xpAcquiredAmount = value;
    }

    private float _rewardPickupDuration;
    public float RewardPickupDuration
    {
        get => _rewardPickupDuration;
        set
        {
            if (value > 0)
                _rewardPickupDuration = value;
            else
                _rewardPickupDuration = 10f;
        }
    }

    private int _childCountMaximum;
    public int ChildCountMaximum
    {
        get => _childCountMaximum;
        set => _childCountMaximum = value;
    }

    private List<QuestInfo> _bitBuddiesQuests;
    public List<QuestInfo> BitBuddiesQuests
    {
        get => _bitBuddiesQuests;
        set => _bitBuddiesQuests = value;
    }

    private List<QuestInfo> _bitBlingQuests;
    public List<QuestInfo> BitBlingQuests
    {
        get => _bitBlingQuests;
        set => _bitBlingQuests = value;
    }

    private List<QuestInfo> _generalQuests;
    public List<QuestInfo> GeneralQuests
    {
        get => _generalQuests;
        set => _generalQuests = value;
    }

    private List<ShopInfo> _parentShopInfos;
    public List<ShopInfo> ParentShopInfos
    {
        get => _parentShopInfos;
        set => _parentShopInfos = value;
    }

    private List<ShopInfo> _childShopInfos;
    public List<ShopInfo> ChildShopInfos
    {
        get => _childShopInfos;
        set => _childShopInfos = value;
    }

    private long _freebieItemCooldownUntil;
    public long FreebieItemCooldownUntil
    {
        get => _freebieItemCooldownUntil;
        set => _freebieItemCooldownUntil = value;
    }

    private int _coinsCollectedViaVisit;

    public int CoinsCollectedViaVisit
    {
        get => _coinsCollectedViaVisit;
        set => _coinsCollectedViaVisit = value;
    }

    private List<float> _buddyMoveSpeeds;
    public List<float> BuddyMoveSpeeds
    {
        get => _buddyMoveSpeeds;
        set => _buddyMoveSpeeds = value;
    }

    private bool _claimQuestAvailable;
    public bool ClaimQuestAvailable
    {
        get => _claimQuestAvailable;
        set => _claimQuestAvailable = value;
    }

    public float GetBuddyMoveSpeed()
    {
        if (_buddyMoveSpeeds == null || _buddyMoveSpeeds.Count == 0) return 0;

        return _buddyMoveSpeeds[(int)_selectedAppChildrenInfo.rarity];
    }

    public void SetQuestsLists(List<QuestInfo> listOfQuests)
    {
        _bitBlingQuests = new List<QuestInfo>();
        _bitBuddiesQuests = new List<QuestInfo>();
        _generalQuests = new List<QuestInfo>();

        for (int i = 0; i < listOfQuests.Count; i++)
        {

            switch (listOfQuests[i].QuestId)
            {
                case BitBuddiesConsts.BITBUDDIES_QUESTLINEID:
                    BitBuddiesQuests.Add(listOfQuests[i]);
                    break;
                case BitBuddiesConsts.BITBLING_QUESTLINEID:
                    BitBlingQuests.Add(listOfQuests[i]);
                    break;
                case BitBuddiesConsts.GENERAL_QUESTLINEID:
                    GeneralQuests.Add(listOfQuests[i]);
                    break;
            }
        }
    }

    public override void Awake()
    {
        _selectedAppChildrenInfo = new AppChildrenInfo();
        //_eventSystem = EventSystem.current;
        base.Awake();
    }

#if UNITY_STANDALONE
    private void Update()
    {
        // Quit app
        if (Input.GetKeyDown(KeyCode.Escape) &&
            !BrainCloudManager.Instance.IsProcessingRequest &&
            FindFirstObjectByType<PopUpUI>() == null &&
            FindFirstObjectByType<LoadingScreen>() == null)
        {
            PopUpUI.Show("Quit bitBuddies", false)
                   .AddBodyText("Are you sure you want to quit the game?")
                   .AddButton("Back", PopUpUI.ButtonColor.Blue, null)
                   .AddButton("Quit", PopUpUI.ButtonColor.Red, Application.Quit);
        }

        // Clear app data
        if (Input.GetKey(KeyCode.F12))
        {
            resetTime += Time.deltaTime;
            if (resetTime >= 3.0f)
            {
                static IEnumerator reset()
                {
                    yield return new WaitForFixedUpdate();

                    PlayerPrefs.DeleteAll();

                    yield return new WaitForFixedUpdate();

                    PlayerPrefs.Save();

                    yield return new WaitForFixedUpdate();

                    Application.Quit();
                }

                var eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                foreach (var system in eventSystems)
                {
                    system.gameObject.SetActive(false);
                }

                StartCoroutine(reset());
                return;
            }
        }
        else
        {
            resetTime = 0.0f;
        }
    }
#endif

    public void OnDeleteBuddySuccess()
    {
        /*
         * Update list to remove selected child info
         * Refresh screen to display the current
         */
        _appChildrenInfos.Remove(_selectedAppChildrenInfo);
        StateManager.Instance.RefreshScreen();
    }

    public void ClearDataForLogout()
    {
        _appChildrenInfos.Clear();
        _selectedAppChildrenInfo = null;

    }

    public void UpdateChildAppInfo(AppChildrenInfo in_appChildrenInfo)
    {
        var index = _appChildrenInfos.FindIndex(x => x.profileId == in_appChildrenInfo.profileId);
        if (index != -1)
        {
            _appChildrenInfos[index] = in_appChildrenInfo;
        }
    }

    public void UpdateSelectedAppChildrenInfo()
    {
        if (_appChildrenInfos == null || _appChildrenInfos.Count == 0) return;
        if (SelectedAppChildrenInfo == null) return;

        for (int i = 0; i < _appChildrenInfos.Count; i++)
        {
            if (_appChildrenInfos[i].profileId.Equals(SelectedAppChildrenInfo.profileId, StringComparison.OrdinalIgnoreCase))
            {
                _appChildrenInfos[i] = SelectedAppChildrenInfo;
            }
        }
    }

    public static bool CanBuyItem(CurrencyTypes in_currencyType, int in_itemCost)
    {
        bool canBuy = false;
        switch (in_currencyType)
        {
            case CurrencyTypes.Coins:
                canBuy = BrainCloudManager.Instance.CurrentUserInfo.Coins >= in_itemCost;
                break;
            case CurrencyTypes.Gems:
                canBuy = BrainCloudManager.Instance.CurrentUserInfo.Gems >= in_itemCost;
                break;
            case CurrencyTypes.BuddyBling:
                canBuy = Instance.SelectedAppChildrenInfo.buddyBling >= in_itemCost;
                break;
        }

        return canBuy;
    }

    public void UpdateAchievementAwarded(string[] in_achievementId)
    {
        if (SelectedAppChildrenInfo == null) return;

        ChildAchievementInfo childAchievementInfo = new ChildAchievementInfo();
        for (int i = 0; i < SelectedAppChildrenInfo.childAchievements.Count; i++)
        {
            for (int x = 0; x < in_achievementId.Length; x++)
            {
                if (SelectedAppChildrenInfo.childAchievements[i].AchievementId.Equals(in_achievementId[x], StringComparison.OrdinalIgnoreCase))
                {
                    childAchievementInfo = SelectedAppChildrenInfo.childAchievements[i];
                    childAchievementInfo.Status = "AWARDED";
                    SelectedAppChildrenInfo.childAchievements[i] = childAchievementInfo;
                }
            }
        }
    }

    public void UpdateSelectedAppChildrenInfo(AppChildrenInfo in_appChildrenInfo)
    {
        for (int i = 0; i < _appChildrenInfos.Count; i++)
        {
            if (_appChildrenInfos[i].profileId.Equals(in_appChildrenInfo.profileId, StringComparison.OrdinalIgnoreCase))
            {
                _appChildrenInfos[i] = in_appChildrenInfo;
                SelectedAppChildrenInfo = in_appChildrenInfo;
            }
        }
    }

    public string GetChildItemDisplayName(string itemId)
    {
        for (int i = 0; i < _childShopInfos.Count; i++)
        {
            if (_childShopInfos[i].ShopId.Equals(itemId))
            {
                return _childShopInfos[i].DisplayName;
            }
        }
        return "";
    }

    public Sprite GetCurrencySprite(CurrencyTypes in_currency)
    {
        switch (in_currency)
        {
            case CurrencyTypes.BuddyBling:
                return Resources.Load<Sprite>(BitBuddiesConsts.BIT_BLING_SPRITE_PATH);
            case CurrencyTypes.Gems:
                return Resources.Load<Sprite>(BitBuddiesConsts.GEM_SPRITE_PATH);
            case CurrencyTypes.FakeDollars:
                return Resources.Load<Sprite>(BitBuddiesConsts.FAKE_MONEY_SPRITE_PATH);
            case CurrencyTypes.Love:
                return Resources.Load<Sprite>(BitBuddiesConsts.LOVE_SPRITE_PATH);
            case CurrencyTypes.Level:
                return Resources.Load<Sprite>(BitBuddiesConsts.LEVEL_SPRITE_PATH);
            case CurrencyTypes.Coins:
            default:
                return Resources.Load<Sprite>(BitBuddiesConsts.COIN_SPRITE_PATH);
        }
    }
}
