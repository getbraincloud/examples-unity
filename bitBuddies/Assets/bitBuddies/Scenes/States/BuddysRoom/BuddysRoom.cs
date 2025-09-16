using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuddysRoom : ContentUIBehaviour
{
    [SerializeField] private TMP_Text _profileNameText;
    [SerializeField] private TMP_Text _loveText;
    [SerializeField] private TMP_Text _buddyBlingText;
    [SerializeField] private TMP_Text _parentCoinText;
    [SerializeField] private Image _buddySprite;

    [SerializeField] private Button _exitButton;
    [SerializeField] private Button _shopButton;
    [SerializeField] private Button _statsButton;
    
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
        
        _appChildrenInfo = GameManager.Instance.SelectedAppChildrenInfo;
        
        _profileNameText.text = _appChildrenInfo.profileName;
        _parentCoinText.text = BrainCloudManager.Instance.UserInfo.Coins.ToString();
        _buddyBlingText.text = _appChildrenInfo.buddyBling.ToString();
        _loveText.text =  _appChildrenInfo.buddyLove.ToString();
        _buddySprite.sprite = Resources.Load<Sprite>(_appChildrenInfo.buddySpritePath.IsNullOrEmpty() ? BitBuddiesConsts.DEFAULT_SPRITE_PATH_FOR_BUDDY : _appChildrenInfo.buddySpritePath);
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

}
