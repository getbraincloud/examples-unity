using System;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuddysRoom : ContentUIBehaviour
{
    [SerializeField] private TMP_Text ProfileNameText;
    [SerializeField] private TMP_Text LoveLevelText;
    [SerializeField] private TMP_Text BuddyBlingText;
    [SerializeField] private TMP_Text ParentCoinText;
    [SerializeField] private Image BuddySprite;
    [SerializeField] private TMP_Text GameVersionText;
    [SerializeField] private TMP_Text BcClientVersionText;
    [SerializeField] private Slider LoveSlider;
    [SerializeField] private TMP_Text TimestampText;

    [SerializeField] private Button ExitButton;
    [SerializeField] private Button ShopButton;
    [SerializeField] private Button StatsButton;
    [SerializeField] private ToyShop ToyShop;
    [SerializeField] private ValueAddedAnimation AddedValueTextAnimationPrefab;
    
    private float _textSpawnOffset = 135f;
    private int _increaseXpAmount;
    private bool _isShopOpen;
    private AppChildrenInfo _appChildrenInfo;
    public AppChildrenInfo AppChildrenInfo { get { return _appChildrenInfo; } }
 
    protected override void Awake()
    {
        InitializeUI();
        base.Awake();
        
        ToyManager.OnCoinsTaken += SpawnValueSubtractedAnimation;
    }

    protected override void InitializeUI()
    {
        ExitButton.onClick.AddListener(OnExitButton);
        ShopButton.onClick.AddListener(OnShopButton);
        StatsButton.onClick.AddListener(OnStatsButton);
        
        GameVersionText.text = $"Game Version: {Application.version}";
        BcClientVersionText.text = $"BC Client Version: {BrainCloud.Version.GetVersion()}";
        _appChildrenInfo = GameManager.Instance.SelectedAppChildrenInfo;
        
        ProfileNameText.text = _appChildrenInfo.profileName;
        ParentCoinText.text = BrainCloudManager.Instance.UserInfo.Coins.ToString();
        BuddyBlingText.text = _appChildrenInfo.buddyBling.ToString();

        LoveLevelText.text = $"Lv. {_appChildrenInfo.buddyLevel}";
        if(_appChildrenInfo.nextLevelUp == 0)
        {
            LoveSlider.maxValue = 1;
            LoveSlider.value = 1;
        }
        else
        {
            LoveSlider.value = _appChildrenInfo.currentXP;
            LoveSlider.maxValue = _appChildrenInfo.nextLevelUp;
        }

        TimestampText.text = _appChildrenInfo.lastIdleTimestamp.ToString();
        
        //_buddySprite.sprite = Resources.Load<Sprite>(_appChildrenInfo.buddySpritePath.IsNullOrEmpty() ? BitBuddiesConsts.DEFAULT_SPRITE_PATH_FOR_BUDDY : _appChildrenInfo.buddySpritePath);
        BuddySprite.sprite = AssetLoader.LoadSprite(_appChildrenInfo.buddySpritePath);
        if(_appChildrenInfo.buddySpritePath.IsNullOrEmpty())
        {
            Debug.LogWarning("Buddy sprite was missing for: "+ _appChildrenInfo.profileName + " child");
        }
    }

    private void OnExitButton()
    {
        StateManager.Instance.GoToParent();
    }
    
    private void OnShopButton()
    {
        if(_isShopOpen) return;
        _isShopOpen = true;
        Instantiate(ToyShop, transform);
    }
    
    public void ShopClosed()
    {
        _isShopOpen = false;
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
        
        ParentCoinText.text = BrainCloudManager.Instance.UserInfo.Coins.ToString();
    }
    
    public void IncreaseXP(int xpAmount)
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object>();
        scriptData["incrementAmount"] =  xpAmount;
        scriptData["profileId"]  = _appChildrenInfo.profileId;
        scriptData["childAppId"] = BitBuddiesConsts.APP_CHILD_ID;
        BrainCloudManager.Wrapper.ScriptService.RunScript(BitBuddiesConsts.INCREASE_XP_SCRIPT_NAME, scriptData.Serialize(), BrainCloudManager.HandleSuccess("IncreaseXP Success", OnIncreaseXP));
    }
    
    private void OnIncreaseXP(string jsonResponse)
    {
        var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data =  packet["data"] as Dictionary<string, object>;
        var response =  data["response"] as Dictionary<string, object>;
        var update =  response["update"] as Dictionary<string, object>;
        var increaseXP =  update["increaseXpResult"] as Dictionary<string, object>;
        
        if(update.ContainsKey("nextLevelUpXP"))
        {
            var nextLevelUp = (int) update["nextLevelUpXP"];
            if(nextLevelUp != 0)
            {
                _appChildrenInfo.nextLevelUp = nextLevelUp;
                LoveSlider.maxValue = nextLevelUp;
            }            
        }
        
        var currentXP = (int) increaseXP["experiencePoints"];
        if(currentXP != 0)
        {
            _appChildrenInfo.currentXP = currentXP;
            LoveSlider.value = currentXP;
        }
        
        var currentLevel = (int) increaseXP["experienceLevel"];
        if(currentLevel != 0)
        {
            _appChildrenInfo.buddyLevel = currentLevel;
        }

        
        if(data.ContainsKey("currency"))
        {
            var currency = data["currency"] as Dictionary<string, object>;
            if(data.ContainsValue(currency))
            {
                if(currency.ContainsKey("coins"))
                {
                    //get the money
                    var gems = currency["coins"] as Dictionary<string, object>;
                    var balance = (int) gems["balance"];
                    _appChildrenInfo.buddyBling = balance;
                    BuddyBlingText.text = _appChildrenInfo.buddyBling.ToString();
                }
            }
        }
        
        //grab app child reference from game manager and assign new values
        var listOfApps = GameManager.Instance.AppChildrenInfos;
        var appIndex = 0;
        for (int i = 0; i < listOfApps.Count; i++)
        {
            if(_appChildrenInfo == listOfApps[i])
            {
                listOfApps[i] = _appChildrenInfo;
            }
        }
        
        //save the new values 
        GameManager.Instance.AppChildrenInfos = listOfApps;
        GameManager.Instance.SelectedAppChildrenInfo = _appChildrenInfo;
        StateManager.Instance.RefreshScreen();
    }
}
