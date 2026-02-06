using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ToyBench : MonoBehaviour
{
	[SerializeField] private Transform RewardSpawnPoint;
	[SerializeField] private RewardPickup RewardPickupPrefab;
	[SerializeField] private GameObject ReadyIcon;
	[SerializeField] private GameObject SpinnerIcon;
	[SerializeField] private float CooldownTime;
	[SerializeField] private int CoinRewardsToSpawn;
	[SerializeField] private int LoveRewardsToSpawn;
	[SerializeField] private int BuddyBlingRewardsToSpawn;
	[SerializeField] private int CoinRewardValueFromBench;
	[SerializeField] private int LoveRewardValueFromBench;
	[SerializeField] private int BuddyBlingRewardValueFromBench;
	
	
	private Vector2 _rewardSpawnRangeX = new Vector2(-440, 440);
	private Vector2 _rewardSpawnRangeY = new Vector2(-125, 125);
	private Button _benchButton;
	private void Awake()
	{
		_benchButton = GetComponent<Button>();
		_benchButton.onClick.AddListener(OnBenchButton);
		ReadyIcon.gameObject.SetActive(false);
		SpinnerIcon.gameObject.SetActive(false);
		//ToDo uncomment when Toy Manager enables benches
		//_benchButton.interactable = false;
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}
	
	//ToDo: Called from Toy Manager when it determines what benches are available
	public void EnableBench()
	{
		ReadyIcon.gameObject.SetActive(true);
		_benchButton.interactable = true;
	}

	private void OnBenchButton()
	{
		for (int i = 0; i < CoinRewardsToSpawn; ++i)
		{
			Vector2 spawnPos = new Vector2(
								Random.Range(_rewardSpawnRangeX.x, _rewardSpawnRangeX.y), 
								Random.Range(_rewardSpawnRangeY.x, _rewardSpawnRangeY.y));
			var reward = Instantiate(RewardPickupPrefab, RewardSpawnPoint);
			reward.transform.localPosition = spawnPos;
			int rewardValue;
			if(CoinRewardsToSpawn > 1)
			{
				rewardValue = CoinRewardValueFromBench/CoinRewardsToSpawn;
			}
			else
			{
				rewardValue = CoinRewardValueFromBench;
			}
			reward.SetUpPickup(CurrencyTypes.Coins, rewardValue);
		}
		
		
		for (int i = 0; i < LoveRewardsToSpawn; ++i)
		{
			Vector2 spawnPos = new Vector2(
								Random.Range(_rewardSpawnRangeX.x, _rewardSpawnRangeX.y), 
								Random.Range(_rewardSpawnRangeY.x, _rewardSpawnRangeY.y));
			var reward = Instantiate(RewardPickupPrefab, RewardSpawnPoint);
			reward.transform.localPosition = spawnPos;
			int rewardValue;
			if(LoveRewardsToSpawn > 1)
			{
				rewardValue = LoveRewardValueFromBench/LoveRewardsToSpawn;
			}
			else
			{
				rewardValue = LoveRewardValueFromBench;
			}
			reward.SetUpPickup(CurrencyTypes.Love, rewardValue);
		}
		
		
		for (int i = 0; i < BuddyBlingRewardsToSpawn; ++i)
		{
			Vector2 spawnPos = new Vector2(
				Random.Range(_rewardSpawnRangeX.x, _rewardSpawnRangeX.y), 
				Random.Range(_rewardSpawnRangeY.x, _rewardSpawnRangeY.y));
			var reward = Instantiate(RewardPickupPrefab, RewardSpawnPoint);
			reward.transform.localPosition = spawnPos;
			int rewardValue;
			if(BuddyBlingRewardsToSpawn > 1)
			{
				rewardValue = BuddyBlingRewardValueFromBench/BuddyBlingRewardsToSpawn;
			}
			else
			{
				rewardValue = BuddyBlingRewardValueFromBench;
			}
			reward.SetUpPickup(CurrencyTypes.BuddyBling, rewardValue);
		}

		StartCoroutine(CooldownOnBench());
	}
	
	IEnumerator CooldownOnBench()
	{
		_benchButton.interactable = false;
		ReadyIcon.gameObject.SetActive(false);
		SpinnerIcon.gameObject.SetActive(true);
		yield return new WaitForSeconds(CooldownTime);
		_benchButton.interactable = true;
		ReadyIcon.gameObject.SetActive(true);
		SpinnerIcon.gameObject.SetActive(false);
	}
}
