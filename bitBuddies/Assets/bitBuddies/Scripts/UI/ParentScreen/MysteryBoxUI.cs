using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MysteryBoxUI : ContentUIBehaviour
{
    [Header("Main UI")]
    [SerializeField] private TMP_Text BoxNameText;
    [SerializeField] private TMP_Text UnlockAmountText;
    [SerializeField] private Button OpenBoxButton;
    [SerializeField] private Image UnlockTypeImage;
    
    [Header("References")]
    [SerializeField] private Sprite[] UnlockTypeSprites;  //0 = coins, 1 = love, 2 = level
    private MysteryBoxPanelUI _mysteryBoxPanelUI;
    //Data
    /*[SerializeField]*/ private MysteryBoxInfo _mysteryBoxInfo;
    public MysteryBoxInfo MysteryBoxInfo
    {
        get { return _mysteryBoxInfo; }
        set { _mysteryBoxInfo = value; }
    }

    public void Init()
    {
        InitializeUI();
    }

    protected override void InitializeUI()
    {
        OpenBoxButton.onClick.AddListener(OnOpenBox);
        BoxNameText.text = _mysteryBoxInfo.BoxName;
        _mysteryBoxPanelUI = FindAnyObjectByType<MysteryBoxPanelUI>();
        switch (_mysteryBoxInfo.UnlockType)
        {
            case UnlockTypes.Coins:
                UnlockAmountText.text = _mysteryBoxInfo.UnlockAmount.ToString("#,#");    //#,# adds commas to the string when using ints
                UnlockTypeImage.sprite = UnlockTypeSprites[(int)UnlockTypes.Coins];
                break;
            case UnlockTypes.Love:
                UnlockAmountText.text = "Needs Lvl." + _mysteryBoxInfo.UnlockAmount;
                UnlockTypeImage.sprite = UnlockTypeSprites[(int)UnlockTypes.Love];
                break;
        }
    }
    
    private void OnOpenBox()
    {
        //Open another screen where we Animate the box opening
        // After box is opened, we show another screen where the user 
        // picks the name of buddy
        _mysteryBoxPanelUI.MysteryBoxInfo = _mysteryBoxInfo;
        _mysteryBoxPanelUI.NextPage();
    }
}
