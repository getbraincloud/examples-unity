using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BuddysRoom : ContentUIBehaviour
{
    [SerializeField] private TMP_Text _profileNameText;
    [SerializeField] private TMP_Text _loveLevelText;
    [SerializeField] private TMP_Text _buddyBlingText;
    [SerializeField] private TMP_Text _parentCoinText;
    [SerializeField] private Image _buddySprite;
    [SerializeField] private TMP_Text _gameVersionText;
    [SerializeField] private TMP_Text _bcClientVersionText;
    [SerializeField] private Slider _loveSlider;

    [SerializeField] private Button _exitButton;
    [SerializeField] private Button _shopButton;
    [SerializeField] private Button _statsButton;

    private int _increaseXpAmount;
    private AppChildrenInfo _appChildrenInfo;
 
    protected override void Awake()
    {
        InitializeUI();
        base.Awake();
    }

    protected override void InitializeUI()
    {
        _exitButton.onClick.AddListener(OnExitButton);
        _shopButton.onClick.AddListener(OnShopButton);
        _statsButton.onClick.AddListener(OnStatsButton);
        
        _gameVersionText.text = $"Game Version: {Application.version}";
        _bcClientVersionText.text = $"BC Client Version: {BrainCloud.Version.GetVersion()}";
        _appChildrenInfo = GameManager.Instance.SelectedAppChildrenInfo;
        
        _profileNameText.text = _appChildrenInfo.profileName;
        _parentCoinText.text = BrainCloudManager.Instance.UserInfo.Coins.ToString();
        _buddyBlingText.text = _appChildrenInfo.buddyBling.ToString();

        _loveLevelText.text = $"Lv. {_appChildrenInfo.buddyLevel}";
        _loveSlider.value = _appChildrenInfo.currentXP;
        _loveSlider.maxValue = _appChildrenInfo.nextLevelUp;
        
        //_buddySprite.sprite = Resources.Load<Sprite>(_appChildrenInfo.buddySpritePath.IsNullOrEmpty() ? BitBuddiesConsts.DEFAULT_SPRITE_PATH_FOR_BUDDY : _appChildrenInfo.buddySpritePath);
        _buddySprite.sprite = AssetLoader.LoadSprite(_appChildrenInfo.buddySpritePath);
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
        
    }
    
    private void OnStatsButton()
    {
        
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
        var currentXP = (int) data["experiencePoints"];
        if(currentXP != 0)
        {
            _appChildrenInfo.currentXP = currentXP;
        }
        
        var currentLevel = (int) data["experienceLevel"];
        if(currentLevel != 0)
        {
            _appChildrenInfo.buddyLevel = currentLevel;
        }

        var nextLevelUp = (int) data["nextLevelUpXP"];
        if(nextLevelUp != 0)
        {
            _appChildrenInfo.nextLevelUp = nextLevelUp;
        }
        
        var currency = data["currency"] as Dictionary<string, object>;
        if(data.ContainsValue(currency))
        {
            //get the money
            var gems = currency["coins"] as Dictionary<string, object>;
            var balance = (int) gems["balance"];
            _appChildrenInfo.buddyBling = balance;
            _buddyBlingText.text = _appChildrenInfo.buddyBling.ToString();
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
    }
}
