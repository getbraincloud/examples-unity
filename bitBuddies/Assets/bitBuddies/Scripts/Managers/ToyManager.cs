using System;
using System.Collections;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using UnityEngine;

public class ToyManager : SingletonBehaviour<ToyManager>
{
	/*
	 * Manages what toys are locked or unlocked
	 *	- How the heck am I saving that data ?
	 *		- I think this has to be a User Entity, cause I dont want to add more data into Summary Friend Data when
	 *			the data might not be used. Get the user entity when the user visits the player, ensure the loading screen
	 *			waits until the response is completed
	 * Logic for saving picked up currencies
	 *	- not sure to send a request for:
		 * This sounds expensive for # of calls to be billed - each pick up 
		 *	Probably this one -> wait 5 seconds to send bunches of picked up items... 
	 * If the user leaves while having rewards still on the floor, then the manager will pick it up for them
	 * 
	 */

	[SerializeField] private List<ToyBench> ToyBenches;
	
	private const float CHECK_FOR_REWARDS_INTERVAL = 1f;
	private string _selectedToyId;
	private List<RewardPickup> _rewardPickups = new List<RewardPickup>();
	
	public static event Action<int> OnCoinsTaken;
	public override void Awake()
	{
		base.Awake();
		SetUpToyBenches();
		CheckForAvailableBenches();
		StartCoroutine(LoopCheckRewardsToSend());
	}

	private void OnDisable()
	{
		StopAllCoroutines();
		CheckForLeftoverRewards();
	}
	
	IEnumerator LoopCheckRewardsToSend()
	{
		while(true)
		{
			CheckForSendingRewards();
			yield return new WaitForSeconds(CHECK_FOR_REWARDS_INTERVAL);
		}
	}
	
	private void CheckForLeftoverRewards()
	{
		int amountOfLoveToReward = 0;
		int amountOfCoinsToReward = 0;
		int amountOfBuddyBlingToReward = 0;
		
		//Grab what is left in the scene
		var listOfRewards = FindObjectsByType<RewardPickup>(FindObjectsSortMode.None);
		if(listOfRewards != null && listOfRewards.Length > 0)
		{
			for (int i = 0; i < listOfRewards.Length; i++)
			{
				switch (listOfRewards[i].CurrencyType)
				{
					case CurrencyTypes.Coins:
						amountOfCoinsToReward += listOfRewards[i].RewardAmount;
						break;
					case CurrencyTypes.Love:
						amountOfLoveToReward += listOfRewards[i].RewardAmount;
						break;
					case CurrencyTypes.BuddyBling:
						amountOfBuddyBlingToReward += listOfRewards[i].RewardAmount;
						break;
				}
			}
		}
		
		if(_rewardPickups != null && _rewardPickups.Count > 0)
		{
			for (int i = 0; i < _rewardPickups.Count; i++)
			{
				switch (_rewardPickups[i].CurrencyType)
				{
					case CurrencyTypes.Coins:
						amountOfCoinsToReward += _rewardPickups[i].RewardAmount;
						break;
					case CurrencyTypes.Love:
						amountOfLoveToReward += _rewardPickups[i].RewardAmount;
						break;
					case CurrencyTypes.BuddyBling:
						amountOfBuddyBlingToReward += _rewardPickups[i].RewardAmount;
						break;
				}
			}
			_rewardPickups.Clear();
		}

		Dictionary<string, object> scriptData = new Dictionary<string, object>();
		scriptData.Add("childAppId", BitBuddiesConsts.APP_CHILD_ID);
		scriptData.Add("profileId", GameManager.Instance.SelectedAppChildrenInfo.profileId);
		scriptData.Add("amountOfCoinsToGive", amountOfCoinsToReward);
		scriptData.Add("amountOfLoveToGive", amountOfLoveToReward);
		scriptData.Add("amountOfBuddyBlingToGive", amountOfBuddyBlingToReward);
		BrainCloudManager.Wrapper.ScriptService.RunScript(BitBuddiesConsts.TOY_REWARD_RECEIVED_SCRIPT_NAME, scriptData.Serialize(), BrainCloudManager.HandleSuccess("Toy Reward Received Success", OnRewardsReceived));
	}
	
	private void OnRewardsReceived(string jsonResponse)
	{
		/*
			{"packetId":2,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"compileTime":40583,"scriptSize":12594,"renderTime":44,"executeTime":244216},"
			response":{"incrementCoinsResult":{"consumed":221000,"balance":47142,"purchased":0,"awarded":268142,"revoked":0},
			"statResult":{"data":{"rewardDetails":{},"currency":{},"rewards":{},
			"statistics":{"CoinsGainedForParent":1837,"LoveEarned":12}},"status":200},
			"xpLoveResult":{"data":{"experiencePoints":1325,"rewardDetails":{},"currency":{},"xpCapped":false,"experienceLevel":10,"rewards":{}},"status":200},
			"blingResult":null},"success":true,"reasonCode":null},"status":200}]}
		 */
		 
		var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
		var data =  packet["data"] as Dictionary<string, object>;
		var response = data["response"] as Dictionary<string, object>;
		var beforeAmount = BrainCloudManager.Instance.UserInfo.Coins;
		var selectedAppChildInfo = GameManager.Instance.SelectedAppChildrenInfo;
		if(response != null)
		{
			if(response.TryGetValue("incrementCoinsResult", out var value))
			{
				var incrementCoinsResult = value as Dictionary<string, object>;
				if(incrementCoinsResult != null)
				{
					BrainCloudManager.Instance.UserInfo.UpdateCoins((int) incrementCoinsResult["balance"]);
				}
			}
			
			if(response.TryGetValue("statResult", out var statValue))
			{
				var statResult = statValue as Dictionary<string, object>;
				var statData = statResult["data"] as Dictionary<string, object>;
				var statistics = statData["statistics"] as Dictionary<string, object>;
				if(statistics != null)
				{
					selectedAppChildInfo.coinsEarnedInLifetime = (int) statistics["CoinsGainedForParent"];
					selectedAppChildInfo.loveEarnedInLifetime = (int) statistics["LoveEarned"];	
				}
			}
			
			if(response.TryGetValue("xpLoveResult", out var xpValue))
			{
				var xpResult = xpValue as Dictionary<string, object>;
				var xpData = xpResult["data"] as Dictionary<string, object>;
				if(xpData != null)
				{
					selectedAppChildInfo.currentXP = (int) xpData["experiencePoints"];
					selectedAppChildInfo.buddyLevel = (int) xpData["experienceLevel"];	
				}
			}
			
			if(response.ContainsKey("nextLevelUpXP"))
			{
				var nextLevelUp = (int) response["nextLevelUpXP"];
				selectedAppChildInfo.nextLevelUp = nextLevelUp;
			}
			
			if(response.TryGetValue("blingResult", out var blingValue))
			{
				if(blingValue != null)
				{
					var blingResult = blingValue as Dictionary<string, object>;
					selectedAppChildInfo.buddyBling = (int) blingResult["balance"];	
				}
			}
		}

		
		var totalDifference = beforeAmount - BrainCloudManager.Instance.UserInfo.Coins;
		//ToDo Add animations for
		/*
		 * Coins
		 * Buddy Bling
		 * Love aka xp
		 */
		
		GameManager.Instance.SelectedAppChildrenInfo = selectedAppChildInfo;
		GameManager.Instance.UpdateSelectedAppChildrenInfo();
		StateManager.Instance.RefreshScreen();
	}
	
	private void CheckForSendingRewards()
	{
		//Checks if we have more than 1 reward to send since last check.
		if(_rewardPickups != null && _rewardPickups.Count > 0)
		{
			int amountOfLoveToReward = 0;
			int amountOfCoinsToReward = 0;
			int amountOfBuddyBlingToReward = 0;
			for (int i = 0; i < _rewardPickups.Count; i++)
			{
				switch (_rewardPickups[i].CurrencyType)
				{
					case CurrencyTypes.Coins:
						amountOfCoinsToReward += _rewardPickups[i].RewardAmount;
						break;
					case CurrencyTypes.Love:
						amountOfLoveToReward += _rewardPickups[i].RewardAmount;
						break;
					case CurrencyTypes.BuddyBling:
						amountOfBuddyBlingToReward += _rewardPickups[i].RewardAmount;
						break;
				}
			}
			_rewardPickups.Clear();
			Dictionary<string, object> scriptData = new Dictionary<string, object>();
			scriptData.Add("childAppId", BitBuddiesConsts.APP_CHILD_ID);
			scriptData.Add("profileId", GameManager.Instance.SelectedAppChildrenInfo.profileId);
			scriptData.Add("amountOfCoinsToGive", amountOfCoinsToReward);
			scriptData.Add("amountOfLoveToGive", amountOfLoveToReward);
			scriptData.Add("amountOfBuddyBlingToGive", amountOfBuddyBlingToReward);
			BrainCloudManager.Wrapper.ScriptService.RunScript(BitBuddiesConsts.TOY_REWARD_RECEIVED_SCRIPT_NAME, scriptData.Serialize(), BrainCloudManager.HandleSuccess("Toy Reward Received Success", OnRewardsReceived));
		}
	}
	
	public void AddRewardPickup(RewardPickup in_rewardPickup)
	{
		_rewardPickups.Add(in_rewardPickup);
	}
	
	private void SetUpToyBenches()
	{
		var listOfInfo = GameManager.Instance.ToyBenchInfos;
		for (int i = 0; i < ToyBenches.Count; i++)
		{
			ToyBenches[i].SetUpToyBench(listOfInfo[i]);
		}
	}
	
	private void CheckForAvailableBenches()
	{
		var getUserUnlockedBenches = GameManager.Instance.SelectedAppChildrenInfo.ownedToys;
		if(getUserUnlockedBenches != null && getUserUnlockedBenches.Count > 0)
		{
			foreach (ToyBench toyBench in ToyBenches)
			{
				foreach(string benchId in getUserUnlockedBenches)
				{
					if(!benchId.IsNullOrEmpty() && toyBench != null && !toyBench.BenchId.IsNullOrEmpty())
					{
						if(toyBench.BenchId.Equals(benchId, System.StringComparison.OrdinalIgnoreCase))
						{
							toyBench.EnableBench();
							break;
						}
					
						toyBench.DisableBench();	
					}
				}
			}
		}
		else
		{
			foreach (ToyBench toyBench in ToyBenches)
			{
				toyBench.DisableBench();
			}
		}
	}

	private Action _toyBenchUIRefreshCallback;
	public void ObtainToy(string in_benchId, Action uiCallback)
	{
		if (in_benchId.Equals(""))
			return;
		if(GameManager.Instance.SelectedAppChildrenInfo.ownedToys.Contains(in_benchId))
			return;
		_toyBenchUIRefreshCallback = uiCallback;
		_selectedToyId = in_benchId;
		var scriptData = new Dictionary<string, object>();
		scriptData.Add("toyId", in_benchId);
		scriptData.Add("childAppId", BitBuddiesConsts.APP_CHILD_ID);
		scriptData.Add("profileId", GameManager.Instance.SelectedAppChildrenInfo.profileId);
		BrainCloudManager.Wrapper.ScriptService.RunScript
		(
			BitBuddiesConsts.OBTAIN_TOY_SCRIPT_NAME, 
			scriptData.Serialize(),
			BrainCloudManager.HandleSuccess("Obtain Toy Success", OnObtainToySuccess),
			BrainCloudManager.HandleFailure("Obtain Toy Failure", OnObtainToyFailure)
		);
	}
	
	private void OnObtainToySuccess(string jsonResponse)
	{
		/*
		 * {"packetId":10,"responses":[{"data":{"runTimeData":{"hasIncludes":true,"scriptSize":12672,"executeTime":117410},
		 * "response":{"consumeResult":{"data":{"currencyMap":{"gems":{"consumed":0,"balance":0,"purchased":0,"awarded":0,"revoked":0},
		 * "coins":{"consumed":31000,"balance":6619,"purchased":0,"awarded":37619,"revoked":0}}},"status":200}}
		 * ,"success":true,"reasonCode":null},"status":200}]}
		 */
		var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
		var data =  packet["data"] as Dictionary<string, object>;
		var response = data["response"] as Dictionary<string, object>;
		var beforeAmount = BrainCloudManager.Instance.UserInfo.Coins;
		
		if(response != null && response.TryGetValue("consumeResult", out var value))
		{
			var consumeResult = value as Dictionary<string, object>;
			var currencyData = consumeResult["data"] as Dictionary<string, object>;
			var currencyMap = currencyData["currencyMap"] as Dictionary<string, object>;
			var coins = currencyMap["coins"] as Dictionary<string, object>;
			BrainCloudManager.Instance.UserInfo.UpdateCoins((int) coins["balance"]);
		}
		
		GameManager.Instance.SelectedAppChildrenInfo.ownedToys.Add(_selectedToyId);

		CheckForAvailableBenches();
		
		_toyBenchUIRefreshCallback?.Invoke();
		var totalDifference = beforeAmount - BrainCloudManager.Instance.UserInfo.Coins;
		OnCoinsTaken?.Invoke(totalDifference);
	}
	
	private void OnObtainToyFailure()
	{
		
	}
	
	private ToyBench GetToyBench(string in_benchId)
	{
		foreach (ToyBench toyBench in ToyBenches)
		{
			if(in_benchId.Equals(toyBench.BenchId, System.StringComparison.OrdinalIgnoreCase))
			{
				//That means the user owns this bench and can enable it.
				return toyBench;
			}
		}

		return null;
	}
	
	private ToyBenchInfo GetToyBenchInfo(string in_benchId)
	{
		var benchInfo = GameManager.Instance.ToyBenchInfos;
		foreach (ToyBenchInfo toyBenchInfo in benchInfo)
		{
			if(in_benchId.Equals(toyBenchInfo.BenchId, System.StringComparison.OrdinalIgnoreCase))
			{
				return toyBenchInfo;
			}
		}

		return new ToyBenchInfo();
	}
}
