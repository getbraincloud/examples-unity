using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ParentMenu : ContentUIBehaviour
{
    private const float TEXT_GOLD_SPAWN_OFFSET = 40.0f;
    private const float TEXT_LEVEL_SPAWN_OFFSET = -300.0f;
    private const float COIN_CHECK_INTERVAL = 60.0f;
    private const string CHILD_COUNT_TEXT = "Buddy Count: ";

    [SerializeField] private Button OpenSettingsButton;
    [SerializeField] private TextMeshProUGUI UsernameText;
    [SerializeField] private TextMeshProUGUI LevelText;
    [SerializeField] private TextMeshProUGUI LevelFillText;
    [SerializeField] private TextMeshProUGUI CoinsText;
    [SerializeField] private TextMeshProUGUI GemsText;
    [SerializeField] private Transform BuddySpawnTransform;
    [SerializeField] private BuddyHouseInfo BuddyPrefab;
    [SerializeField] private GameObject MoveInPrefab;
    [SerializeField] private MysteryBoxPanelUI MysteryBoxPanelPrefab;
    [SerializeField] private SettingsPanelUI SettingsPanelUIPrefab;
    [SerializeField] private TextMeshProUGUI GameVersionText;
    [SerializeField] private TextMeshProUGUI BcClientVersionText;
    [SerializeField] private Slider LevelSlider;
    [SerializeField] private ValueAddedAnimation AddedValueTextAnimationPrefab;
    [SerializeField] private QuestPanel QuestPanelPrefab;
    [SerializeField] private Button OpenQuestPanelButton;
    [SerializeField] private ParentShop ParentShopPrefab;
    [SerializeField] private Button OpenParentShopButton;
    [SerializeField] private Image FreebieIcon;

    [Header("Debug")]
    [SerializeField] private Button IncreaseCoinsButton;
    [SerializeField] private Button IncreaseGemsButton;
    [SerializeField] private Button IncreaseLevelButton;
    [SerializeField] private GameObject DebugButtonGroup;

    private bool isWaitingForResponse = false;
    private List<AppChildrenInfo> _appChildrenInfos;
    private List<BuddyHouseInfo> _listOfBuddies;

    public RectTransform CanvasRectTransform { get; private set; }
    public AppChildrenInfo NewAppChildrenInfo { get; set; }

    protected override void Awake()
    {
        InitializeUI();

        OpenSettingsButton.onClick.AddListener(OpenSettingsButtonOnClick);
        OpenQuestPanelButton.onClick.AddListener(OpenQuestPanel);
        OpenParentShopButton.onClick.AddListener(OpenParentShop);
        BuddyHouseInfo.OnCoinsCollected += UpdateValueText;
        BuddyHouseInfo.OnCoinsCollected += SpawnCurrencyAddedAnimation;
        StartCoroutine(LoopCheckCoins());
        CanvasRectTransform = (RectTransform)transform;

        base.Awake();
    }

    public RectTransform GetCurrencyTextRectTransform(CurrencyTypes in_type)
    {
        switch (in_type)
        {
            case CurrencyTypes.Coins:
                return CoinsText.rectTransform;
            case CurrencyTypes.Gems:
                return GemsText.rectTransform;
            case CurrencyTypes.Level:
                return LevelFillText.rectTransform;
            default:
                return null;
        }
    }

    IEnumerator LoopCheckCoins()
    {
        while (true)
        {
            if (!BrainCloudManager.Instance.IsProcessingRequest)
            {
                CheckAllBuddiesCoinEarnings();
            }
            
            yield return new WaitForSeconds(COIN_CHECK_INTERVAL);
        }
    }

    private void CheckAllBuddiesCoinEarnings()
    {
        for (int i = 0; i < _listOfBuddies.Count; i++)
        {
            _listOfBuddies[i].CheckCoinsButton();
        }
    }

    protected override void InitializeUI()
    {
        UserInfo userInfo = BrainCloudManager.Instance.CurrentUserInfo;
        if (userInfo.Username.IsNullOrEmpty())
        {
            UsernameText.text = "New User";
        }
        else
        {
            if (userInfo.Username.Length > 9)
            {
                UsernameText.text = userInfo.Username[..9] + "...";
            }
            else
            {
                UsernameText.text = userInfo.Username;
            }
        }

        LevelText.text = $"{userInfo.Level}";

        int prevXP = userInfo.GetPreviousLevelExperience();
        int nextXP = userInfo.GetNextLevelExperience();
        int adjustedCurrentXP = userInfo.CurrentXP - prevXP;
        int adjustedNextLevelXP = nextXP - prevXP;
        if (nextXP == 0)
        {
            LevelFillText.text = "MAX";
            LevelSlider.minValue = 0;
            LevelSlider.maxValue = 1;
            LevelSlider.value = 1;
        }
        else
        {
            LevelFillText.text = $"{adjustedCurrentXP:N0}/{adjustedNextLevelXP:N0}";
            LevelSlider.minValue = 0;
            LevelSlider.maxValue = adjustedNextLevelXP;
            LevelSlider.value = adjustedCurrentXP;
        }

        var homeRewards = GameManager.Instance.HomeRewards;
        CoinsText.text  = (homeRewards.GetValue<int>("coins") is int coins && coins > 0 ? userInfo.Coins - coins : userInfo.Coins).ToString("N0");
        GemsText.text   = (homeRewards.GetValue<int>("gems")  is int gems  && gems > 0  ? userInfo.Gems  - gems  : userInfo.Gems).ToString("N0");

        GameVersionText.text = $"Game Version: {Application.version}";
        BcClientVersionText.text = $"BC Client Version: {BrainCloud.Version.GetVersion()}";

        bool debug = GameManager.Instance.Debug;
        if (debug)
        {
            IncreaseCoinsButton.onClick.AddListener(OnIncreaseCoins);
            IncreaseGemsButton.onClick.AddListener(OnIncreaseGems);
            IncreaseLevelButton.onClick.AddListener(OnIncreaseLevel);
        }

        FreebieIcon.gameObject.SetActive(GameManager.Instance.FreebieItemCooldownUntil == 0);

        DebugButtonGroup.SetActive(debug);
        _appChildrenInfos = GameManager.Instance.AppChildrenInfos;

        SetupHouses();
        ShowRewardsPopUp();
    }

    private void OnDisable()
    {
        BuddyHouseInfo.OnCoinsCollected -= SpawnCurrencyAddedAnimation;
        BuddyHouseInfo.OnCoinsCollected -= UpdateValueText;
        StopAllCoroutines();
        IncreaseCoinsButton.onClick.RemoveAllListeners();
        IncreaseGemsButton.onClick.RemoveAllListeners();
        IncreaseLevelButton.onClick.RemoveAllListeners();
        OpenSettingsButton.onClick.RemoveAllListeners();
        OpenQuestPanelButton.onClick.RemoveAllListeners();
        OpenParentShopButton.onClick.RemoveAllListeners();
    }

    public void SetupHouses()
    {
        // Clear existing houses...
        for (int i = 0; i < BuddySpawnTransform.transform.childCount; i++)
        {
            Destroy(BuddySpawnTransform.transform.GetChild(i).gameObject);
        }
        _listOfBuddies = new List<BuddyHouseInfo>();

        foreach (AppChildrenInfo buddyHouse in _appChildrenInfos)
        {
            BuddyHouseInfo buddyHouseInfo = Instantiate(BuddyPrefab, BuddySpawnTransform);
            buddyHouseInfo.HouseInfo = buddyHouse;
            buddyHouseInfo.SetUpHouse();
            _listOfBuddies.Add(buddyHouseInfo);
        }
        if (_listOfBuddies.Count < GameManager.Instance.ChildCountMaximum)
        {
            Instantiate(MoveInPrefab, BuddySpawnTransform);
        }
    }

    private void ShowRewardsPopUp()
    {
        var homeRewards = GameManager.Instance.HomeRewards;
        int coinReward = homeRewards.GetValue<int>("coins");
        int gemReward = homeRewards.GetValue<int>("gems");

        if (coinReward > 0 || gemReward > 0)
        {
            var popup = PopUpUI.Show("You Leveled Up!", false)
                               .AddButton("OK!", PopUpUI.ButtonColor.Green, RefreshScreen);

            if (coinReward > 0)
            {
                popup.AddRewardItem("Coins", GameManager.Instance.GetCurrencySprite(CurrencyTypes.Coins), coinReward, "You recieved");
            }

            if (gemReward > 0)
            {
                popup.AddRewardItem("Gems", GameManager.Instance.GetCurrencySprite(CurrencyTypes.Gems), gemReward);
            }

            homeRewards["coins"] = 0;
            homeRewards["gems"] = 0;
        }
    }

    private void OpenSettingsButtonOnClick()
    {
        // what do other than open ?
        // Settings page shows: Volume slider, connect an email (attach email to anonymous account), about the app and log out
        Instantiate(SettingsPanelUIPrefab, transform);
    }

    private void OpenQuestPanel()
    {
        var questPanel = Instantiate(QuestPanelPrefab, transform);
        questPanel.SetUpPanel();
    }

    private void OpenParentShop()
    {
        var parentShopPanel = Instantiate(ParentShopPrefab, transform);
        parentShopPanel.SetupShop();
    }

    public void OpenMysteryBoxPanel()
    {
        Instantiate(MysteryBoxPanelPrefab, transform);
    }

    public void SpawnCurrencyAddedAnimation(int amount, int typeIndex)
    {
        RectTransform mainTextPosition;
        Transform parent;
        switch (typeIndex)
        {
            // Coins
            case 0:
                mainTextPosition = CoinsText.rectTransform;
                parent = CoinsText.transform.parent;
                break;
            // Gems
            case 1:
                mainTextPosition = GemsText.rectTransform;
                parent = GemsText.transform.parent;
                break;
            default:
                throw new System.Exception("Unknown typeIndex, cannot play animation!");
        }

        // Set up animation
        var textAnimation = Instantiate(AddedValueTextAnimationPrefab, parent);
        textAnimation.TextRectTransform.localPosition = mainTextPosition.localPosition + new Vector3(mainTextPosition.rect.width - TEXT_GOLD_SPAWN_OFFSET, 0f);
        if (amount > 1)
        {
            textAnimation.SetUpPositiveNumberText(amount);
        }
        else
        {
            textAnimation.SetUpPositiveNumberText(1);
        }
        textAnimation.PlayBounce();

        SpawnLevelIncreaseAnimation();
    }

    public void SpawnLevelIncreaseAnimation()
    {
        if (Mathf.Approximately(LevelSlider.minValue, 1f) && Mathf.Approximately(LevelSlider.maxValue, 2f)) return; //No level increase animation (already max level)

        var amount = GameManager.Instance.XpAcquiredAmount;
        if (amount == 0) return;
        GameManager.Instance.XpAcquiredAmount = 0;
        RectTransform mainTextPosition = new RectTransform();
        Transform parent = new RectTransform();
        mainTextPosition = LevelText.rectTransform;
        parent = LevelText.transform.parent;

        //Set up animation
        var textAnimation = Instantiate(AddedValueTextAnimationPrefab, parent);
        textAnimation.TextRectTransform.localPosition = mainTextPosition.localPosition + new Vector3(mainTextPosition.rect.width - TEXT_LEVEL_SPAWN_OFFSET, 0f);
        if (amount > 1)
        {
            textAnimation.SetUpPositiveNumberText((int)amount);
        }
        else
        {
            textAnimation.SetUpPositiveNumberText(1);
        }

        textAnimation.PlayBounce();
    }

    private void UpdateValueText(int amount, int typeIndex)
    {
        switch (typeIndex)
        {
            //Coins
            case 0:
                CoinsText.text = BrainCloudManager.Instance.CurrentUserInfo.Coins.ToString();
                break;
            //Gems
            case 1:
                GemsText.text = BrainCloudManager.Instance.CurrentUserInfo.Gems.ToString();
                break;
        }
    }

    //ToDo: Remove Debug Buttons before release
    private void OnIncreaseCoins()
    {
        if (isWaitingForResponse) return;
        BrainCloudManager.Instance.RewardCoinsToParent(1000);
        StartCoroutine(WaitAbitForResponse());
    }

    private void OnIncreaseGems()
    {
        if (isWaitingForResponse) return;
        BrainCloudManager.Instance.RewardGemsToParent(100);
        StartCoroutine(WaitAbitForResponse());
    }

    private void OnIncreaseLevel()
    {
        if (isWaitingForResponse) return;
        BrainCloudManager.Instance.LevelUpParent();
        StartCoroutine(WaitAbitForResponse());
    }

    IEnumerator WaitAbitForResponse()
    {
        isWaitingForResponse = true;
        yield return new WaitForSeconds(0.5f);
        isWaitingForResponse = false;
    }
}
