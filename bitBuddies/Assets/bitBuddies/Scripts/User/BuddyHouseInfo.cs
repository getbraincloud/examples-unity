using System;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
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
	[SerializeField] private UnityEngine.UI.Button _collectCoinsButton;
	private Transform _parentTransform;
	private int enableCollectCoinsButtonMinValue = 1;
	
	public static event Action<int, int> OnCoinsCollected;
	
	public void SetUpHouse()
	{
		_collectCoinsButton.onClick.AddListener(OnCollectCoinsButton);
		_visitButton.onClick.AddListener(OnVisitButton);
		_deleteButton.onClick.AddListener(OnDeleteButton);
		_parentTransform = FindAnyObjectByType<ParentMenu>().transform;
		_buddySprite.sprite = AssetLoader.LoadSprite(HouseInfo.buddySpritePath); 
		_buddyNameText.text = HouseInfo.profileName.IsNullOrEmpty() ? "Missing Name" : HouseInfo.profileName + "'s Home";
		
		CheckCoinsButton();
	}
	
	public void CheckCoinsButton()
	{
		if(HouseInfo.coinsEarnedInHolding >= enableCollectCoinsButtonMinValue)
		{
			_collectCoinsButton.gameObject.SetActive(true);
		}
		else
		{
			_collectCoinsButton.gameObject.SetActive(false);
		}
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
	
	public void OnCollectCoinsButton()
	{
		Dictionary<string, object> scriptData = new Dictionary<string, object>();
		scriptData.Add("childAppId", BitBuddiesConsts.APP_CHILD_ID);
		scriptData.Add("profileId", HouseInfo.profileId);
		scriptData.Add("summaryFriendData", HouseInfo.summaryFriendData);
		BrainCloudManager.Wrapper.ScriptService.RunScript
		(
			BitBuddiesConsts.UPDATE_CHILD_COINS_COLLECTED_SCRIPT_NAME, 
			scriptData.Serialize(), 
			BrainCloudManager.HandleSuccess("Update Child Coin Timestamp Success",OnUpdateSummaryDataSuccess),
			BrainCloudManager.HandleFailure("Update Child Coin Timestamp Failed", OnUpdateSummaryDataFailure)
		);
	}
	
	private void OnUpdateSummaryDataSuccess(string jsonResponse)
	{
	/*
		{"packetId":2,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"compileTime":20636,"scriptSize":11419,"renderTime":52,
		"executeTime":243461},"response":{"currencyMap":{"gems":{"consumed":0,"balance":0,"purchased":0,"awarded":0,"revoked":0},
		"coins":{"consumed":2000,"balance":3197,"purchased":0,"awarded":5197,"revoked":0}},
		"xpResult":{"increaseXpResult":{"experiencePoints":48,"rewardDetails":{},"currency":{},"xpCapped":false,"experienceLevel":1,"rewards":{}}},
		"summaryData":{"coinMultiplier":1,"coinPerHour":40,"maxCoinCapacity":100,"buddySpritePath":"BuddySprites/buddy-1","rarity":"starter",
		"level":1,"experiencePoints":0,"lastIdleTimestamp":1.762372115799E12,"nextLevelUpXP":5},
		"statResult":{"data":{"rewardDetails":{},"currency":{},"rewards":{},"statistics":{"CoinsGainedForParent":197}},"status":200}},
		"success":true,"reasonCode":null},"status":200}]}
	 */
		var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
		var data =  packet["data"] as Dictionary<string, object>;
		var response = data["response"] as Dictionary<string, object>;
		
		var currencyMap = response["currencyMap"] as Dictionary<string, object>;
		var coinsObj = currencyMap["coins"] as Dictionary<string, object>;
		var currentBalance = (int) coinsObj["balance"];
		var coinsAdded = currentBalance - BrainCloudManager.Instance.UserInfo.Coins;
		BrainCloudManager.Instance.UserInfo.Coins = currentBalance;
				
		var summaryData = response["summaryData"] as Dictionary<string, object>;
		
		
		HouseInfo.lastIdleTimestamp = DateTimeOffset.FromUnixTimeMilliseconds((long) summaryData["lastIdleTimestamp"]).UtcDateTime;
		HouseInfo.coinsEarnedInHolding = 0;
		CheckCoinsButton();
		
		var statResult = response["statResult"] as Dictionary<string, object>;
		var statData = statResult["data"] as Dictionary<string, object>;
		var statistics = statData["statistics"] as Dictionary<string, object>;
		
		HouseInfo.coinsEarnedInLifetime = (int) statistics["CoinsGainedForParent"];
		if(statistics.ContainsKey("LoveEarned"))
		{
			HouseInfo.loveEarnedInLifetime = (int) statistics["LoveEarned"];
		}
		
		//Fire UI event
		if(OnCoinsCollected != null)
		{
			OnCoinsCollected.Invoke(coinsAdded, 0);
		}

	}
	
	private void OnUpdateSummaryDataFailure()
	{
		//Check to see if its an error saying its empty,
		//If so then create the entity now.
	}
}
