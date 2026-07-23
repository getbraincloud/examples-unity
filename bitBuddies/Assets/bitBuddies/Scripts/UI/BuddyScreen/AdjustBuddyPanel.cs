using BrainCloud.JSONHelper;
using Gameframework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdjustBuddyPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField BuddyInputField;
    [SerializeField] private Button EnableInputFieldButton;
    [SerializeField] private Button DoneButton;
    [SerializeField] private Button ExitButton;

    [Header("Buddy Info")]
    [SerializeField] private TMP_Text CoinMultiplierText;
    [SerializeField] private TMP_Text CoinPerHourText;
    [SerializeField] private TMP_Text CoinCapacityText;
    [SerializeField] private TMP_Text RarityText;
    [SerializeField] private TMP_Text BuddyTypeNameText;
    [SerializeField] private Image BuddySprite;

    private TextMeshProUGUI _buddyPlaceholderText;
    private AppChildrenInfo _appChildrenInfo;

    private void Awake()
    {
        _appChildrenInfo = GameManager.Instance.SelectedAppChildrenInfo;

        ExitButton.onClick.AddListener(OnExitButton);
        DoneButton.onClick.AddListener(OnDoneButton);
        EnableInputFieldButton.onClick.AddListener(OnEnableInputFieldButton);

        BuddyInputField.interactable = false;
        BuddyInputField.text = _appChildrenInfo.profileName;
        _buddyPlaceholderText = BuddyInputField.placeholder.GetComponent<TextMeshProUGUI>();
        _buddyPlaceholderText.text = _appChildrenInfo.profileName;
        DoneButton.gameObject.SetActive(false);

        CoinMultiplierText.text = BitBuddiesConsts.COIN_PAYOUT_TEXT + _appChildrenInfo.coinMultiplier + "x";
        CoinPerHourText.text = BitBuddiesConsts.COIN_GAIN_TEXT + _appChildrenInfo.coinPerHour + BitBuddiesConsts.COIN_PER_HOUR_TEXT;
        CoinCapacityText.text = BitBuddiesConsts.COIN_CAPACITY_TEXT + _appChildrenInfo.maxCoinCapacity;
        RarityText.text = GameManager.FormatCamelCase(_appChildrenInfo.rarity.ToString());
        BuddyTypeNameText.text = _appChildrenInfo.buddyType;
        BuddySprite.sprite = _appChildrenInfo.GetBuddySprite();
    }

    private void OnDisable()
    {
        ExitButton.onClick.RemoveAllListeners();
    }

    private void OnEnableInputFieldButton()
    {
        BuddyInputField.interactable = !BuddyInputField.interactable;
        DoneButton.gameObject.SetActive(BuddyInputField.interactable);
    }

    private void OnDoneButton()
    {
        if (BuddyInputField.text.Length > 12)
        {
            PopUpUI.Show("Name is too long")
                   .AddBodyText("Please enter a shorter name that's under 12 characters");
            return;
        }

        PopUpUI.Show("Are you sure?", false)
               .AddBodyText($"Are you sure you want to change {_appChildrenInfo.profileName} to {BuddyInputField.text}?")
               .AddButton("Cancel", PopUpUI.ButtonColor.Blue, null)
               .AddButton("Confirm", PopUpUI.ButtonColor.Green, OnConfirm);

        BuddyInputField.interactable = false;
        DoneButton.gameObject.SetActive(false);
    }

    private void OnConfirm()
    {
        // Update brainCloud with new name
        var scriptData = new Dictionary<string, object>
        {
            { "childAppId", BitBuddiesConsts.APP_CHILD_ID },
            { "profileId", _appChildrenInfo.profileId },
            { "newName", BuddyInputField.text }
        };

        BrainCloudManager.Wrapper.ScriptService.RunScript
        (
            BitBuddiesConsts.UPDATE_CHILD_PROFILE_NAME_SCRIPT_NAME,
            scriptData.Serialize(),
            BrainCloudManager.HandleSuccess("Updated child name success", OnUpdateNameSuccess)
        );
    }

    private void OnExitButton()
    {
        Destroy(gameObject);
    }

    private void OnUpdateNameSuccess(string jsonResponse)
    {
        PopUpUI.Show("Name updated successfully")
               .AddBodyText($"{BuddyInputField.text} has been updated to your profile");
        GameManager.Instance.SelectedAppChildrenInfo.profileName = BuddyInputField.text;
        GameManager.Instance.UpdateChildAppInfo(GameManager.Instance.SelectedAppChildrenInfo);
        StatTracker.Instance.IncrementStat(BitBuddiesConsts.USER_NAME_CHANGED_STAT_NAME);
        StateManager.Instance.RefreshScreen();
    }
}
