using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuddysRoom : ContentUIBehaviour
{
    [SerializeField] private TMP_Text ProfileNameText;
    [SerializeField] private TMP_Text LoveLevelText;
    [SerializeField] private TMP_Text LoveFillText;
    [SerializeField] private TMP_Text BuddyBlingText;
    [SerializeField] private TMP_Text ParentCoinText;
    [SerializeField] private Image BuddySprite;
    [SerializeField] private TMP_Text GameVersionText;
    [SerializeField] private TMP_Text BcClientVersionText;
    [SerializeField] private Slider LoveSlider;
    [SerializeField] private TMP_Text TimestampText;
    [SerializeField] private TMP_Text HighestAchievementText;
    [SerializeField] private Image FreebieIcon;

    [SerializeField] private Button ExitDoorButton;
    [SerializeField] private Button ExitCornerButton;
    [SerializeField] private Button ShopButton;
    [SerializeField] private Button StatsButton;
    [SerializeField] private Shop shop;
    [SerializeField] private ValueAddedAnimation AddedValueTextAnimationPrefab;
    [SerializeField] private Button BuddyOverviewButton;
    [SerializeField] private AdjustBuddyPanel AdjustBuddyPanelPrefab;

    private float _textSpawnOffset = 135f;
    private int _increaseXpAmount;
    private AppChildrenInfo _appChildrenInfo;
    private ChildAchievementInfo _highestAchievementInfo;

    private RectTransform _canvasRectTransform;
    public RectTransform CanvasRectTransform { get { return _canvasRectTransform; } }

    protected override void Awake()
    {
        ExitDoorButton.onClick.AddListener(OnExitButton);
        ExitCornerButton.onClick.AddListener(OnExitButton);
        BuddyOverviewButton.onClick.AddListener(OnBuddyOverviewButton);
        ShopButton.onClick.AddListener(OnShopButton);
        StatsButton.onClick.AddListener(OnStatsButton);

        _canvasRectTransform = GetComponent<RectTransform>();
        InitializeUI();
        base.Awake();

        ToyManager.OnCoinsTaken += SpawnValueSubtractedAnimation;
    }

    protected override void Start()
    {
        base.Start();
        if (GameManager.Instance.CoinsCollectedViaVisit > 0)
        {
            SpawnValueAddedAnimation(GameManager.Instance.CoinsCollectedViaVisit);
            GameManager.Instance.CoinsCollectedViaVisit = 0;
            ParentCoinText.text = BrainCloudManager.Instance.CurrentUserInfo.Coins.ToString("N0");
        }
    }

    public RectTransform GetCurrencyTextRectTransform(CurrencyTypes in_type)
    {
        switch (in_type)
        {
            case CurrencyTypes.Coins:
                return ParentCoinText.rectTransform;
            case CurrencyTypes.BuddyBling:
                return BuddyBlingText.rectTransform;
            case CurrencyTypes.Love:
                return LoveFillText.rectTransform;
            default:
                return null;
        }
    }

    private void OnDisable()
    {
        ExitCornerButton.onClick.RemoveAllListeners();
        ExitDoorButton.onClick.RemoveAllListeners();
        BuddyOverviewButton.onClick.RemoveAllListeners();
        ShopButton.onClick.RemoveAllListeners();
        StatsButton.onClick.RemoveAllListeners();
        ToyManager.OnCoinsTaken -= SpawnValueSubtractedAnimation;
    }

    protected override void InitializeUI()
    {
        GameVersionText.text = $"Game Version: {Application.version}";
        BcClientVersionText.text = $"BC Client Version: {BrainCloud.Version.GetVersion()}";
        _appChildrenInfo = GameManager.Instance.SelectedAppChildrenInfo;

        ProfileNameText.text = _appChildrenInfo.profileName.IsNullOrEmpty() ? BitBuddiesConsts.DEFAULT_BUDDY_NAME : _appChildrenInfo.profileName;

        ParentCoinText.text = BrainCloudManager.Instance.CurrentUserInfo.Coins.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        BuddyBlingText.text = _appChildrenInfo.buddyBling.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

        FreebieIcon.gameObject.SetActive(_appChildrenInfo.dailyCooldownUntil == 0);

        LoveLevelText.text = $"{_appChildrenInfo.buddyLevel}";

        int adjustedCurrentXP = _appChildrenInfo.currentXP - _appChildrenInfo.previousLevelUp;
        int adjustedNextLevelXP = _appChildrenInfo.nextLevelUp - _appChildrenInfo.previousLevelUp;
        if (_appChildrenInfo.nextLevelUp == 0)
        {
            LoveFillText.text = "MAX";
            LoveSlider.minValue = 0;
            LoveSlider.maxValue = 1;
            LoveSlider.value = 1;
        }
        else
        {
            LoveFillText.text = $"{adjustedCurrentXP:N0}/{adjustedNextLevelXP:N0}";
            LoveSlider.minValue = 0;
            LoveSlider.maxValue = adjustedNextLevelXP;
            LoveSlider.value = adjustedCurrentXP;
        }

        // Check if Toy Benches have become available
        if (FindFirstObjectByType<ToyManager>(FindObjectsInactive.Exclude) != null &&
            FindObjectsByType<ToyBench>(FindObjectsInactive.Include, FindObjectsSortMode.None) is var benches &&
            benches != null && benches.Length > 0)
        {
            for (int i = 0; i < benches.Length; i++)
            {
                benches[i].CheckIfBenchIsAvailable();
            }
        }

        TimestampText.text = _appChildrenInfo.lastIdleTimestamp.ToString();

        //_buddySprite.sprite = Resources.Load<Sprite>(_appChildrenInfo.buddySpritePath.IsNullOrEmpty() ? BitBuddiesConsts.DEFAULT_SPRITE_PATH_FOR_BUDDY : _appChildrenInfo.buddySpritePath);
        BuddySprite.sprite = _appChildrenInfo.GetBuddySprite();
        if (_appChildrenInfo.buddySpritePath.IsNullOrEmpty())
        {
            Debug.LogWarning("Buddy sprite was missing for: " + _appChildrenInfo.profileName + " child");
        }

        if (_appChildrenInfo.childAchievements != null && _appChildrenInfo.childAchievements.Count > 0)
        {
            for (int i = 0; i < _appChildrenInfo.childAchievements.Count; i++)
            {
                if (_appChildrenInfo.childAchievements[i].Status.Equals("AWARDED") &&
                   _appChildrenInfo.childAchievements[i].LevelRequirement > _highestAchievementInfo.LevelRequirement)
                {
                    _highestAchievementInfo = _appChildrenInfo.childAchievements[i];
                }
            }
            if (_highestAchievementInfo.DisplayName.IsNullOrEmpty())
            {
                HighestAchievementText.text = "Baby bitBuddy";
            }
            else
            {
                HighestAchievementText.text = _highestAchievementInfo.DisplayName;
            }
        }
        else
        {
            HighestAchievementText.text = "Baby bitBuddy";
        }

    }

    private void OnExitButton()
    {
        PopUpUI.Show("Are you sure?", false)
               .AddBodyText("Exit to parent screen?")
               .AddButton("Close", PopUpUI.ButtonColor.Blue, null)
               .AddButton("Confirm", PopUpUI.ButtonColor.Green, GoToParentMenu);
    }

    private void GoToParentMenu()
    {
        StateManager.Instance.GoToParent();
    }

    private void OnBuddyOverviewButton()
    {
        Instantiate(AdjustBuddyPanelPrefab, transform);

    }

    private void OnShopButton()
    {
        ToyManager.Instance.MoveToPositionWithCallback(OnMoveToComplete);
    }

    private void OnMoveToComplete()
    {
        Instantiate(shop, transform);
    }

    private void OnStatsButton()
    {

    }

    public void SpawnValueSubtractedAnimation(int amount)
    {
        RectTransform mainTextPosition = new RectTransform();
        Transform parent = new RectTransform();
        mainTextPosition = ParentCoinText.rectTransform;
        parent = ParentCoinText.transform.parent;

        //Set up animation
        var textAnimation = Instantiate(AddedValueTextAnimationPrefab, parent);
        textAnimation.TextRectTransform.localPosition = mainTextPosition.localPosition + new Vector3(mainTextPosition.rect.width - _textSpawnOffset, 0f);
        textAnimation.SetUpNegativeNumberText(amount);
        textAnimation.PlayBounce();

        ParentCoinText.text = BrainCloudManager.Instance.CurrentUserInfo.Coins.ToString("N0");
    }

    public void SpawnValueAddedAnimation(int amount)
    {
        RectTransform mainTextPosition = new RectTransform();
        Transform parent = new RectTransform();
        mainTextPosition = ParentCoinText.rectTransform;
        parent = ParentCoinText.transform.parent;

        //Set up animation
        var textAnimation = Instantiate(AddedValueTextAnimationPrefab, parent);
        textAnimation.TextRectTransform.localPosition = mainTextPosition.localPosition + new Vector3(mainTextPosition.rect.width - _textSpawnOffset, 0f);
        textAnimation.SetUpPositiveNumberText(amount);
        textAnimation.PlayBounce();

        ParentCoinText.text = BrainCloudManager.Instance.CurrentUserInfo.Coins.ToString("N0");
    }

    //Tester function for cloud code script
    public void IncreaseXP(int xpAmount)
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object>();
        scriptData["incrementAmount"] = xpAmount;
        scriptData["profileId"] = _appChildrenInfo.profileId;
        scriptData["childAppId"] = BitBuddiesConsts.APP_CHILD_ID;
        BrainCloudManager.Wrapper.ScriptService.RunScript(BitBuddiesConsts.INCREASE_XP_FOR_CHILD_SCRIPT_NAME, scriptData.Serialize(), BrainCloudManager.HandleSuccess("IncreaseXP Success", OnIncreaseXP));
    }

    //Tester response function for cloud code script
    private void OnIncreaseXP(string jsonResponse)
    {
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = packet["data"] as Dictionary<string, object>;
        var response = data["response"] as Dictionary<string, object>;
        var update = response["update"] as Dictionary<string, object>;
        var increaseXP = update["increaseXpResult"] as Dictionary<string, object>;

        if (update.ContainsKey("nextLevelUpXP"))
        {
            var nextLevelUp = (int)update["nextLevelUpXP"];
            if (nextLevelUp != 0)
            {
                _appChildrenInfo.nextLevelUp = nextLevelUp;
                LoveSlider.maxValue = nextLevelUp;
            }
        }

        var currentXP = (int)increaseXP["experiencePoints"];
        if (currentXP != 0)
        {
            _appChildrenInfo.currentXP = currentXP;
            LoveSlider.value = currentXP;
        }

        var currentLevel = (int)increaseXP["experienceLevel"];
        if (currentLevel != 0)
        {
            _appChildrenInfo.buddyLevel = currentLevel;
        }


        if (data.ContainsKey("currency"))
        {
            var currency = data["currency"] as Dictionary<string, object>;
            if (data.ContainsValue(currency))
            {
                if (currency.ContainsKey("coins"))
                {
                    //get the money
                    var gems = currency["coins"] as Dictionary<string, object>;
                    var balance = (int)gems["balance"];
                    _appChildrenInfo.buddyBling = balance;
                    BuddyBlingText.text = _appChildrenInfo.buddyBling.ToString("N0");
                }
            }
        }

        //grab app child reference from game manager and assign new values
        var listOfApps = GameManager.Instance.AppChildrenInfos;
        for (int i = 0; i < listOfApps.Count; i++)
        {
            if (_appChildrenInfo.profileId.Equals(listOfApps[i].profileId))
            {
                listOfApps[i] = _appChildrenInfo;
            }
        }

        //save the new values 
        GameManager.Instance.AppChildrenInfos = listOfApps;
        GameManager.Instance.SelectedAppChildrenInfo = _appChildrenInfo;
        StateManager.Instance.RefreshScreen();
    }

    private void OnCoinUpdate()
    {

    }
}
