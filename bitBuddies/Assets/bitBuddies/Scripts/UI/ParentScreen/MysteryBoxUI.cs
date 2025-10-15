using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using Gameframework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.Serialization;

//Mystery box template to then be displayed with different rarities and cost.
public class MysteryBoxUI : ContentUIBehaviour
{
    [Header("Main UI")]
    [SerializeField] private TMP_Text BoxNameText;
    [SerializeField] private TMP_Text UnlockAmountText;
    [SerializeField] private Button OpenBoxButton;
    [SerializeField] private Image UnlockTypeImage;
    [SerializeField] private Image LockIconImage;
    [SerializeField] private Image BoxSpriteImage;
    [Header("References")]
    [SerializeField] private Sprite[] UnlockTypeSprites;  //0 = coins, 1 = love, 2 = level

    [SerializeField] private Sprite[] OpenBoxTypeSprites;
    [SerializeField] private Sprite[] ClosedBoxTypeSprites;
    private MysteryBoxPanelUI _mysteryBoxPanelUI;
    //Data
    private MysteryBoxInfo _mysteryBoxInfo;

    public void Init(MysteryBoxInfo in_mysteryBoxInfo)
    {
        _mysteryBoxInfo = in_mysteryBoxInfo;
        InitializeUI();
    }

    protected override void InitializeUI()
    {
        switch (_mysteryBoxInfo.UnlockType)
        {
            case UnlockTypes.Coins:
                UnlockAmountText.text = _mysteryBoxInfo.UnlockAmount.ToString("#,#");    //#,# adds commas to the string when using ints
                UnlockTypeImage.sprite = UnlockTypeSprites[(int)UnlockTypes.Coins];

                var usersCoins = BrainCloudManager.Instance.UserInfo.Coins;
                if(_mysteryBoxInfo.UnlockAmount > usersCoins)
                {
                    LockIconImage.gameObject.SetActive(true);
                    OpenBoxButton.interactable = false;
                    BoxSpriteImage.sprite = ClosedBoxTypeSprites[(int) _mysteryBoxInfo.RarityEnum];
                }
                else
                {
                    BoxSpriteImage.sprite = OpenBoxTypeSprites[(int) _mysteryBoxInfo.RarityEnum];
                    LockIconImage.gameObject.SetActive(false);
                    OpenBoxButton.interactable = true;
                    OpenBoxButton.onClick.AddListener(OnOpenBox);
                }
                break;
            case UnlockTypes.Love:
                UnlockAmountText.text = "Needs Lvl." + _mysteryBoxInfo.UnlockAmount;
                UnlockTypeImage.sprite = UnlockTypeSprites[(int)UnlockTypes.Love];
                //ToDo: how do I determine this value...?
                break;
        }
        
        
        BoxNameText.text = _mysteryBoxInfo.BoxName;
        _mysteryBoxPanelUI = FindAnyObjectByType<MysteryBoxPanelUI>();
    }
    
    private void OnOpenBox()
    {
        //Goal: Open another screen where we Animate the box opening
        // After box is opened, we show another screen where the user 
        // picks the name of buddy
        Dictionary<string, object> scriptData =  new Dictionary<string, object> {{"amountToConsume", _mysteryBoxInfo.UnlockAmount}};
        BrainCloudManager.Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.CONSUME_PARENT_COINS_SCRIPT_NAME, 
            scriptData.Serialize(), 
            BrainCloudManager.HandleSuccess("Consume Coins Success", BrainCloudManager.Instance.OnConsumeCoins),
            BrainCloudManager.HandleFailure("Consume Coins Failure", OnFailureCallback)
        );
            
        _mysteryBoxPanelUI.MysteryBoxInfo = _mysteryBoxInfo;
        _mysteryBoxPanelUI.NextPage();
    }
    
    private void OnFailureCallback()
    {
        
    }
}
