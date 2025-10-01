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
    [SerializeField] private TMP_Text UsernameText;
    [SerializeField] private TMP_Text LevelText;
    [SerializeField] private TMP_Text CoinsText;
    [SerializeField] private TMP_Text GemsText;
    [SerializeField] private TMP_Text ChildCountText;
    [SerializeField] private Transform BuddySpawnTransform;
    [SerializeField] private BuddyHouseInfo BuddyPrefab;
    [SerializeField] private GameObject MoveInPrefab;
    [SerializeField] private MysteryBoxPanelUI MysteryBoxPanelPrefab;
    [SerializeField] private SettingsPanelUI SettingsPanelUIPrefab;
    [SerializeField] private TMP_Text GameVersionText;
    [SerializeField] private TMP_Text BcClientVersionText;
    [SerializeField] private Slider LevelSlider;
    
    //Debug Buttons
    [SerializeField] private Button IncreaseCoinsButton;
    [SerializeField] private Button IncreaseGemsButton;
    [SerializeField] private Button IncreaseLevelButton;
    [SerializeField] private GameObject DebugButtonGroup;
    private bool isWaitingForResponse = false;
    private List<AppChildrenInfo> _appChildrenInfos;
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
        base.Awake();
    }

    protected override void InitializeUI()
    {
        UserInfo userInfo = BrainCloudManager.Instance.UserInfo;
        UsernameText.text = userInfo.Username.IsNullOrEmpty() ? "New User" : userInfo.Username;
        LevelText.text = userInfo.Level.ToString();
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
        ChildCountText.text = CHILD_COUNT_TEXT + _appChildrenInfos.Count;
        SetupHouses();
    }

    private void OnDisable()
    {
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

        foreach (AppChildrenInfo buddyHouse in _appChildrenInfos)
        {
            BuddyHouseInfo buddyHouseInfo = Instantiate(BuddyPrefab, BuddySpawnTransform);
            buddyHouseInfo.HouseInfo = buddyHouse;
            buddyHouseInfo.SetUpHouse();
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
