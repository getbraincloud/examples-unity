using Gameframework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SingletonBehaviour<GameManager>
{
    [Tooltip("Debug")] public bool Debug;

    public List<AppChildrenInfo> AppChildrenInfos { get; set; } = new();
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

    public List<MysteryBoxInfo> MysteryBoxes { get; set; }
    public List<ToyBenchInfo> ToyBenchInfos { get; set; }
    public float XpAcquiredAmount { get; set; }
    public int ChildCountMaximum { get; set; }
    public List<QuestInfo> BitBuddiesQuests { get; set; }
    public List<QuestInfo> BitBlingQuests { get; set; }
    public List<QuestInfo> GeneralQuests { get; set; }
    public List<ShopInfo> ParentShopInfos { get; set; }
    public List<ShopInfo> ChildShopInfos { get; set; }
    public long FreebieItemCooldownUntil { get; set; }
    public List<float> BuddyMoveSpeeds { get; set; }
    public bool ClaimQuestAvailable { get; set; }
    public Dictionary<string, object> HomeRewards { get; set; }

    public override void Awake()
    {
        _selectedAppChildrenInfo = new();
        HomeRewards = new();

        base.Awake();
    }

#if UNITY_STANDALONE
    private float resetTime = 0.0f;

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

                var eventSystems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

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

    public float GetBuddyMoveSpeed()
    {
        if (BuddyMoveSpeeds == null || BuddyMoveSpeeds.Count == 0) return 0;

        return BuddyMoveSpeeds[(int)_selectedAppChildrenInfo.rarity];
    }

    public void SetQuestsLists(List<QuestInfo> listOfQuests)
    {
        BitBlingQuests = new List<QuestInfo>();
        BitBuddiesQuests = new List<QuestInfo>();
        GeneralQuests = new List<QuestInfo>();

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

    public void OnDeleteBuddySuccess()
    {
        /*
         * Update list to remove selected child info
         * Refresh screen to display the current
         */
        AppChildrenInfos.Remove(_selectedAppChildrenInfo);
        StateManager.Instance.RefreshScreen();
    }

    public void ClearDataForLogout()
    {
        AppChildrenInfos.Clear();
        _selectedAppChildrenInfo = null;

    }

    public void UpdateChildAppInfo(AppChildrenInfo in_appChildrenInfo)
    {
        var index = AppChildrenInfos.FindIndex(x => x.profileId == in_appChildrenInfo.profileId);
        if (index != -1)
        {
            AppChildrenInfos[index] = in_appChildrenInfo;
        }
    }

    public void UpdateSelectedAppChildrenInfo()
    {
        if (AppChildrenInfos == null || AppChildrenInfos.Count == 0) return;
        if (SelectedAppChildrenInfo == null) return;

        for (int i = 0; i < AppChildrenInfos.Count; i++)
        {
            if (AppChildrenInfos[i].profileId.Equals(SelectedAppChildrenInfo.profileId, StringComparison.OrdinalIgnoreCase))
            {
                AppChildrenInfos[i] = SelectedAppChildrenInfo;
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
            case CurrencyTypes.FakeDollars:
                canBuy = BrainCloudManager.Instance.CurrentUserInfo.FakeMoney >= in_itemCost;
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
        for (int i = 0; i < AppChildrenInfos.Count; i++)
        {
            if (AppChildrenInfos[i].profileId.Equals(in_appChildrenInfo.profileId, StringComparison.OrdinalIgnoreCase))
            {
                AppChildrenInfos[i] = in_appChildrenInfo;
                SelectedAppChildrenInfo = in_appChildrenInfo;
            }
        }
    }

    public string GetChildItemDisplayName(string itemId)
    {
        for (int i = 0; i < ChildShopInfos.Count; i++)
        {
            if (ChildShopInfos[i].ShopId.Equals(itemId))
            {
                return ChildShopInfos[i].DisplayName;
            }
        }
        return "";
    }

    public void ShowFailurePopUp(string header = "Something went wrong",
                                 string body = "Please try again or restart the application.",
                                 Action onOKButton = null)
    {
        PopUpUI.Show(header, false)
               .AddBodyText(body)
               .AddButton("OK", PopUpUI.ButtonColor.Green, onOKButton, true, true);
    }

    public static string FormatCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Insert a space before each uppercase letter (except the first)
        string result = System.Text.RegularExpressions.Regex.Replace(input, "(?<!^)([A-Z])", " $1");

        // Capitalize the first letter
        return char.ToUpper(result[0]) + result.Substring(1);
    }
}
