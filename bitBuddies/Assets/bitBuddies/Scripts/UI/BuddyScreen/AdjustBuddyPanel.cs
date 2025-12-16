using System;
using System.Collections.Generic;
using BrainCloud.JSONHelper;
using Gameframework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdjustBuddyPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField BuddyInputField;
    [SerializeField] private Button EnableInputFieldButton;
    [SerializeField] private Button DoneButton;
    [SerializeField] private Button ExitButton;
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
        if(BuddyInputField.text.Length > 12)
        {
            StateManager.Instance.OpenInfoPopUp
            ("Name is too long", 
            "Please enter a shorter name that's under 12 characters");
            return;
        }
        
        StateManager.Instance.OpenConfirmPopUp("Are you sure?", $"Are you sure you want to change {_appChildrenInfo.profileName} to {BuddyInputField.text}?", OnConfirm);
        
        BuddyInputField.interactable = false;
        DoneButton.gameObject.SetActive(false);
    }
    
    private void OnConfirm()
    {
        //Update brainCloud with new name
        Dictionary<string, object> scriptData = new Dictionary<string, object>();
        scriptData.Add("childAppId", BitBuddiesConsts.APP_CHILD_ID);
        scriptData.Add("profileId", _appChildrenInfo.profileId);
        scriptData.Add("newName", BuddyInputField.text);
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
        StateManager.Instance.OpenInfoPopUp("Name updated successfully", $"{BuddyInputField.text} has been updated to your profile");
    }
}
