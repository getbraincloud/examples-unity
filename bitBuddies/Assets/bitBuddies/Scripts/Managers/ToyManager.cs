using System;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
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
	
	
	private string _selectedToyId;
	public override void Awake()
	{
		SetUpToyBenches();
		CheckForAvailableBenches();
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
		if(getUserUnlockedBenches.Count > 0)
		{
			foreach (ToyBench toyBench in ToyBenches)
			{
				foreach(string benchId in getUserUnlockedBenches)
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
