using System;
using System.Collections.Generic;
using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuddyHouseInfo : MonoBehaviour
{
	public AppChildrenInfo HouseInfo;
	[SerializeField] private Button _visitButton;
	[SerializeField] private Button _deleteButton;
	[SerializeField] private PopUpUI PopUpPrefab;
	[SerializeField] private TMP_Text _buddyNameText;
	[SerializeField] private Image _buddySprite;
	private Transform _parentTransform;

	private const string GO_BUDDYS_ROOM_TITLE = "Enter Buddy's Room?";
	private const string GO_BUDDYS_ROOM_MESSAGE = "Would you like to enter buddy's room?";
	private const string DELETE_BUDDYS_ROOM_TITLE = "Delete Buddy's Room?";
	private const string DELETE_BUDDYS_ROOM_MESSAGE =  "Would you like to delete buddy's room?";
	
	private const string DELETE_BUDDYS_ROOM_SUCCESS_TITLE = "Buddys Room Deleted";
	private const string DELETE_BUDDYS_ROOM_SUCCESS_MESSAGE = "The requested buddy's room was deleted";
	private const string DELETE_BUDDYS_ROOM_FAILED_TITLE = "Something went wrong";
	private const string DELETE_BUDDYES_ROOM_FAILED_MESSAGE = "There was an error while attempting to delete the requested buddy's room, please try again later";
	
	public void SetUpHouse()
	{
		_visitButton.onClick.AddListener(OnVisitButton);
		_deleteButton.onClick.AddListener(OnDeleteButton);
		_parentTransform = FindAnyObjectByType<ParentMenu>().transform;
		_buddySprite.sprite = GameManager.Instance.BuddySprites[(int)HouseInfo.buddyType];
		_buddyNameText.text = HouseInfo.profileName.IsNullOrEmpty() ? "Missing Name" : HouseInfo.profileName + "'s Home";
	}
	
	private void OnVisitButton()
	{
		var popUp = Instantiate(PopUpPrefab,  _parentTransform);
		popUp.SetupConfirmPopup(GO_BUDDYS_ROOM_TITLE, GO_BUDDYS_ROOM_MESSAGE, GoToBuddysRoom);
	}
	
	private void GoToBuddysRoom()
	{
		GameManager.Instance.SelectedAppChildrenInfo = HouseInfo;
		StateManager.Instance.GoToBuddysRoom();		
	}
	
	private void OnDeleteButton()
	{
		var popUp = Instantiate(PopUpPrefab,  _parentTransform);
		popUp.SetupConfirmPopup(DELETE_BUDDYS_ROOM_TITLE, DELETE_BUDDYS_ROOM_MESSAGE, DeleteBuddyRoom);
	}
	
	private void DeleteBuddyRoom()
	{
		GameManager.Instance.SelectedAppChildrenInfo  = HouseInfo;
		Dictionary<string, object> scriptData = new Dictionary<string, object>
		{
			{"childAppId", BrainCloudConsts.APP_CHILD_ID},
			{"childProfileId", HouseInfo.profileId}
		};
		BrainCloudManager.Wrapper.ScriptService.RunScript
		(
			BrainCloudConsts.DELETE_CHILD_PROFILE_SCRIPT_NAME,
			scriptData.Serialize(),
			BrainCloudManager.HandleSuccess("Delete Child Profile Success", OnDeleteBuddySuccess),
			BrainCloudManager.HandleFailure("Delete Child Profile Failure", OnDeleteBuddyFailure)			
		);
	}
	
	private void OnDeleteBuddySuccess()
	{
		var popUp = Instantiate(PopUpPrefab,  _parentTransform);
		popUp.SetUpInfoPopup(DELETE_BUDDYS_ROOM_SUCCESS_TITLE, DELETE_BUDDYS_ROOM_SUCCESS_MESSAGE);
		GameManager.Instance.OnDeleteBuddySuccess();
	}
	
	private void OnDeleteBuddyFailure()
	{
		var popUp = Instantiate(PopUpPrefab,  _parentTransform);
		popUp.SetUpInfoPopup(DELETE_BUDDYS_ROOM_FAILED_TITLE, DELETE_BUDDYES_ROOM_FAILED_MESSAGE);
	}
}
