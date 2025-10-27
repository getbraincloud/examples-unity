using System;
using System.Collections.Generic;
using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using TMPro;
using UnityEditor;
using UnityEngine;

public class BuddyHouseInfo : MonoBehaviour
{
	public AppChildrenInfo HouseInfo;
	[SerializeField]private UnityEngine.UI.Button _visitButton;
	[SerializeField]private UnityEngine.UI.Button _deleteButton;
	[SerializeField] private PopUpUI PopUpPrefab;
	[SerializeField] private TMP_Text _buddyNameText;
	[SerializeField] private UnityEngine.UI.Image _buddySprite;
	private Transform _parentTransform;
	
	public void SetUpHouse()
	{
		_visitButton.onClick.AddListener(OnVisitButton);
		_deleteButton.onClick.AddListener(OnDeleteButton);
		_parentTransform = FindAnyObjectByType<ParentMenu>().transform;
		_buddySprite.sprite = AssetLoader.LoadSprite(HouseInfo.buddySpritePath); 
		_buddyNameText.text = HouseInfo.profileName.IsNullOrEmpty() ? "Missing Name" : HouseInfo.profileName + "'s Home";
	}
	
	private void OnVisitButton()
	{
		var popUp = Instantiate(PopUpPrefab,  _parentTransform);
		popUp.SetupConfirmPopup(BitBuddiesConsts.GO_BUDDYS_ROOM_TITLE, BitBuddiesConsts.GO_BUDDYS_ROOM_MESSAGE, GoToBuddysRoom);
	}
	
	private void GoToBuddysRoom()
	{
		GameManager.Instance.SelectedAppChildrenInfo = HouseInfo;
		StateManager.Instance.GoToBuddysRoom();		
	}
	
	private void OnDeleteButton()
	{
		var popUp = Instantiate(PopUpPrefab,  _parentTransform);
		popUp.SetupConfirmPopup(BitBuddiesConsts.DELETE_BUDDYS_ROOM_TITLE, BitBuddiesConsts.DELETE_BUDDYS_ROOM_MESSAGE, DeleteBuddyRoom);
	}
	
	private void DeleteBuddyRoom()
	{
		GameManager.Instance.SelectedAppChildrenInfo  = HouseInfo;
		Dictionary<string, object> scriptData = new Dictionary<string, object>
		{
			{"childAppId", BitBuddiesConsts.APP_CHILD_ID},
			{"childProfileId", HouseInfo.profileId}
		};
		BrainCloudManager.Wrapper.ScriptService.RunScript
		(
			BitBuddiesConsts.DELETE_CHILD_PROFILE_SCRIPT_NAME,
			scriptData.Serialize(),
			BrainCloudManager.HandleSuccess("Delete Child Profile Success", OnDeleteBuddySuccess),
			BrainCloudManager.HandleFailure("Delete Child Profile Failure", OnDeleteBuddyFailure)			
		);
	}
	
	private void OnDeleteBuddySuccess()
	{
		var popUp = Instantiate(PopUpPrefab,  _parentTransform);
		popUp.SetUpInfoPopup(BitBuddiesConsts.DELETE_BUDDYS_ROOM_SUCCESS_TITLE, BitBuddiesConsts.DELETE_BUDDYS_ROOM_SUCCESS_MESSAGE);
		GameManager.Instance.OnDeleteBuddySuccess();
	}
	
	private void OnDeleteBuddyFailure()
	{
		var popUp = Instantiate(PopUpPrefab,  _parentTransform);
		popUp.SetUpInfoPopup(BitBuddiesConsts.DELETE_BUDDYS_ROOM_FAILED_TITLE, BitBuddiesConsts.DELETE_BUDDYES_ROOM_FAILED_MESSAGE);
	}
}
