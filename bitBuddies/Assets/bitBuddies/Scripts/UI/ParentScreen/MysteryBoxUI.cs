using BrainCloud.JSONHelper;
using Gameframework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private GameObject LevelRequirementObject;
    [SerializeField] private GameObject PriceRequirementObject;
    [SerializeField] private TextMeshProUGUI LevelRequirementText;

    [Header("References")]
    [SerializeField] private Sprite[] UnlockTypeSprites;  //0 = coins, 1 = love, 2 = level
    [SerializeField] private Sprite[] OpenBoxTypeSprites;
    [SerializeField] private Sprite[] ClosedBoxTypeSprites;

    private MysteryBoxPanelUI _mysteryBoxPanelUI;
    private MysteryBoxInfo _mysteryBoxInfo; // Data

    public void Init(MysteryBoxInfo in_mysteryBoxInfo)
    {
        _mysteryBoxInfo = in_mysteryBoxInfo;
        InitializeUI();
    }

    public Sprite GetBoxSpriteImage() => BoxSpriteImage.sprite;

    protected override void InitializeUI()
    {
        UnlockAmountText.text = _mysteryBoxInfo.UnlockAmount.ToString("#,#");    //#,# adds commas to the string when using ints
        UnlockTypeImage.sprite = UnlockTypeSprites[(int)CurrencyTypes.Coins];
        var userInfo = BrainCloudManager.Instance.CurrentUserInfo;
        if (userInfo.Level >= _mysteryBoxInfo.LevelRequirement)
        {
            LevelRequirementObject.SetActive(false);
            PriceRequirementObject.SetActive(true);

            var usersCoins = BrainCloudManager.Instance.CurrentUserInfo.Coins;
            if (_mysteryBoxInfo.UnlockAmount > usersCoins)
            {
                LockIconImage.gameObject.SetActive(true);
                OpenBoxButton.interactable = false;
                BoxSpriteImage.sprite = ClosedBoxTypeSprites[(int)_mysteryBoxInfo.RarityEnum];
            }
            else
            {
                BoxSpriteImage.sprite = OpenBoxTypeSprites[(int)_mysteryBoxInfo.RarityEnum];
                LockIconImage.gameObject.SetActive(false);
                OpenBoxButton.interactable = true;
                OpenBoxButton.onClick.AddListener(OnOpenBox);
            }
        }
        else
        {
            LevelRequirementObject.SetActive(true);
            PriceRequirementObject.SetActive(false);
            LevelRequirementText.text = $"Lvl. {_mysteryBoxInfo.LevelRequirement}";
            OpenBoxButton.interactable = false;
            LockIconImage.gameObject.SetActive(true);
            BoxSpriteImage.sprite = ClosedBoxTypeSprites[(int)_mysteryBoxInfo.RarityEnum];
        }

        BoxNameText.text = _mysteryBoxInfo.BoxName;
        _mysteryBoxPanelUI = FindAnyObjectByType<MysteryBoxPanelUI>();
    }

    private void OnOpenBox()
    {
        //Goal: Open another screen where we Animate the box opening
        // After box is opened, we show another screen where the user 
        // picks the name of buddy
        Dictionary<string, object> scriptData = new Dictionary<string, object> { { "amountToConsume", _mysteryBoxInfo.UnlockAmount } };
        BrainCloudManager.Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.CONSUME_PARENT_COINS_SCRIPT_NAME,
            scriptData.Serialize(),
            BrainCloudManager.HandleSuccess("Consume Coins Success", BrainCloudManager.Instance.OnConsumeCoins),
            BrainCloudManager.HandleFailure("Consume Coins Failure", OnFailureCallback)
        );

        _mysteryBoxPanelUI.MysteryBoxInfo = _mysteryBoxInfo;
        _mysteryBoxPanelUI.OpenBoxImageSprite = OpenBoxTypeSprites[(int)_mysteryBoxInfo.RarityEnum];
        _mysteryBoxPanelUI.NextPage();
    }

    private void OnFailureCallback()
    {

    }
}
