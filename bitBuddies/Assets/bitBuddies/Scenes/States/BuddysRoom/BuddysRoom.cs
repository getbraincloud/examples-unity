using TMPro;
using UnityEngine;

public class BuddysRoom : ContentUIBehaviour
{
    [SerializeField] private TMP_Text _profileNameText;
    [SerializeField] private TMP_Text _loveText;
    [SerializeField] private TMP_Text _buddyBlingText;
    [SerializeField] private TMP_Text _parentCoinText;
    
    private AppChildrenInfo _appChildrenInfo;
 
    protected override void Awake()
    {
        InitializeUI();
        base.Awake();
    }

    protected override void InitializeUI()
    {
        _appChildrenInfo = GameManager.Instance.AppChildrenInfo;
        
        _profileNameText.text = _appChildrenInfo.profileName;
        _parentCoinText.text = BrainCloudManager.Instance.UserInfo.Coins.ToString();
        //_buddyBlingText.text =
        //_loveText.text =  
    }

}
