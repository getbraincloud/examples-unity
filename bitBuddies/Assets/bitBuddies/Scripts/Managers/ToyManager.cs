using System.Collections.Generic;
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
		CheckForAvailableBenches();
	}
	
	private void CheckForAvailableBenches()
	{
		var getUserUnlockedBenches = GameManager.Instance.SelectedAppChildrenInfo.ownedToys;
		foreach (ToyBench toyBench in ToyBenches)
		{
			foreach (string unlockedBench in getUserUnlockedBenches)
			{
				if(toyBench.BenchId.ToLower().Equals(unlockedBench.ToLower()))
				{
					toyBench.EnableBench();
				}
				else
				{
					toyBench.DisableBench();
				}
			}
		}
	}

	public void ObtainToy(string in_benchId)
	{
		if (in_benchId.Equals(""))
			return;
		
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
		foreach (ToyBench toyBench in ToyBenches)
		{
			if(toyBench.BenchId.Equals(_selectedToyId))
			{
				toyBench.EnableBench();
			}
		}
	}
	
	private void OnObtainToyFailure()
	{
		
	}
	
	private ToyBench GetToyBench(string in_benchId)
	{
		foreach (ToyBench toyBench in ToyBenches)
		{
			if(in_benchId.Equals(toyBench.BenchId))
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
			if(in_benchId.Equals(toyBenchInfo.BenchId))
			{
				return toyBenchInfo;
			}
		}

		return new ToyBenchInfo();
	}
}
