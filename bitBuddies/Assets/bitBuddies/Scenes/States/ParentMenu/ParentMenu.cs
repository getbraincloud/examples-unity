using System;
using System.Collections;
using System.Collections.Generic;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ParentMenu : ContentUIBehaviour
{
    [SerializeField] private Button OpenSettingsButton;
    [SerializeField] private TextMeshProUGUI UsernameText;
    [SerializeField] private TextMeshProUGUI LevelText;
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

    private float textGoldSpawnOffset = 40f;
    private float textLevelSpawnOffset = -40f;
    //Debug Buttons
    [SerializeField] private Button IncreaseCoinsButton;
    [SerializeField] private Button IncreaseGemsButton;
    [SerializeField] private Button IncreaseLevelButton;
    [SerializeField] private GameObject DebugButtonGroup;
    private bool isWaitingForResponse = false;
    private float checkForCoinsInterval = 60;
    private List<AppChildrenInfo> _appChildrenInfos;
    private List<BuddyHouseInfo> _listOfBuddies;
    private AppChildrenInfo _newAppChildrenInfo;
    public AppChildrenInfo NewAppChildrenInfo
    {
        get { return _newAppChildrenInfo; }
        set { _newAppChildrenInfo = value; }
    }

    private const string CHILD_COUNT_TEXT = "Buddy Count: ";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Awake()
    {
        InitializeUI();
        OpenSettingsButton.onClick.AddListener(OpenSettingsButtonOnClick);
        BuddyHouseInfo.OnCoinsCollected += UpdateValueText;
        BuddyHouseInfo.OnCoinsCollected += SpawnCurrencyAddedAnimation;
        StartCoroutine(LoopCheckCoins());
        base.Awake();
    }
    
    IEnumerator LoopCheckCoins()
    {
        while(true)
        {
            CheckAllBuddiesCoinEarnings();
            yield return new WaitForSeconds(checkForCoinsInterval);
        }
    }
    
    private void CheckAllBuddiesCoinEarnings()
    {
        for (int i = 0; i < _listOfBuddies.Count; i++)
        {
            _listOfBuddies[i].HouseInfo.CheckCoinsEarned();
            _listOfBuddies[i].CheckCoinsButton();
        }
    }

    protected override void InitializeUI()
    {
        UserInfo userInfo = BrainCloudManager.Instance.UserInfo;
        if(userInfo.Username.IsNullOrEmpty())
        {
            UsernameText.text = "New User";
        }
        else
        {
            if(userInfo.Username.Length > 9)
            {
                UsernameText.text = userInfo.Username.Substring(0, 9) + "...";
            }
            else
            {
                UsernameText.text = userInfo.Username;
            }
        }
        LevelText.text = $"Lv. {userInfo.Level}";
        LevelSlider.minValue = userInfo.PreviousLevelUp;
        LevelSlider.maxValue = userInfo.NextLevelUp;
        LevelSlider.value = userInfo.CurrentXP;
        //We're max level, so just have the bar filled.
        if(LevelSlider.minValue == 0 && LevelSlider.maxValue == 0)
        {
            LevelSlider.minValue = 1;
            LevelSlider.maxValue = 2;
            LevelSlider.value = 2;
        }
        CoinsText.text = userInfo.Coins.ToString();
        GemsText.text = userInfo.Gems.ToString();
        GameVersionText.text = $"Game Version: {Application.version}";
        BcClientVersionText.text = $"BC Client Version: {BrainCloud.Version.GetVersion()}";

        bool debug = GameManager.Instance.Debug;
        if(debug)
        {
            IncreaseCoinsButton.onClick.AddListener(OnIncreaseCoins);
            IncreaseGemsButton.onClick.AddListener(OnIncreaseGems);
            IncreaseLevelButton.onClick.AddListener(OnIncreaseLevel);
        }
        DebugButtonGroup.SetActive(debug);
        _appChildrenInfos = GameManager.Instance.AppChildrenInfos;
        SetupHouses();
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
        
        Instantiate(MoveInPrefab, BuddySpawnTransform);
    }
    
    private void OpenSettingsButtonOnClick()
    {
        // what do other than open ?
        // Settings page shows: Volume slider, connect an email (attach email to anonymous account), about the app and log out
        Instantiate(SettingsPanelUIPrefab, transform);
    }
    
    public void OpenMysteryBoxPanel()
    {
        Instantiate(MysteryBoxPanelPrefab, transform);
    }
    
    public void OpenConfirmDemolishPanel()
    {
        
    }
    
    public void SpawnCurrencyAddedAnimation(int amount, int typeIndex)
    {
        RectTransform mainTextPosition = new RectTransform();
        Transform parent = new RectTransform();
        switch (typeIndex)
        {
            //Coins
            case 0:
                mainTextPosition = CoinsText.rectTransform;
                parent = CoinsText.transform.parent;
                break;
            //Gems
            case 1:
                mainTextPosition = GemsText.rectTransform;
                parent = GemsText.transform.parent;
                break;
        }
        //Set up animation
        var textAnimation = Instantiate(AddedValueTextAnimationPrefab, parent);
        textAnimation.TextRectTransform.localPosition = mainTextPosition.localPosition + new Vector3(mainTextPosition.rect.width - textGoldSpawnOffset, 0f);
        textAnimation.SetUpPositiveNumberText(amount);
        textAnimation.PlayBounce();
        
        SpawnLevelIncreaseAnimation();
    }
    
    public void SpawnLevelIncreaseAnimation()
    {
        if(Mathf.Approximately(LevelSlider.minValue, 1f) && Mathf.Approximately(LevelSlider.maxValue, 2f)) return; //No level increase animation (already max level)
        
        var amount = GameManager.Instance.XpAcquiredAmount;
        if(amount == 0) return;
        GameManager.Instance.XpAcquiredAmount = 0;
        RectTransform mainTextPosition = new RectTransform();
        Transform parent = new RectTransform();
        mainTextPosition = LevelText.rectTransform;
        parent = LevelText.transform.parent;
        
        //Set up animation
        var textAnimation = Instantiate(AddedValueTextAnimationPrefab, parent);
        textAnimation.TextRectTransform.localPosition = mainTextPosition.localPosition + new Vector3(mainTextPosition.rect.width - textLevelSpawnOffset, 0f);
        textAnimation.SetUpPositiveNumberText(amount);
        textAnimation.PlayBounce();
    }
    
    private void UpdateValueText(int amount, int typeIndex)
    {
        switch (typeIndex)
        {
            //Coins
            case 0:
                CoinsText.text = BrainCloudManager.Instance.UserInfo.Coins.ToString();
                break;
            //Gems
            case 1:
                GemsText.text = BrainCloudManager.Instance.UserInfo.Gems.ToString();
                break;
        }
    }
    
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
