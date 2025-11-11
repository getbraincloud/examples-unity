using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ToyBench : MonoBehaviour
{
	public string BenchId;
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
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}
	
	//ToDo: Called from Toy Manager when it determines what benches are available
	public void EnableBench()
	{
		ReadyIcon.gameObject.SetActive(true);
		if(!_benchButton)
			_benchButton = GetComponent<Button>();
		_benchButton.interactable = true;
	}
	
	public void DisableBench()
	{
		SpinnerIcon.gameObject.SetActive(false);
		ReadyIcon.gameObject.SetActive(false);
		if(!_benchButton)
			_benchButton = GetComponent<Button>();
		_benchButton.interactable = false;
	}

	private void OnBenchButton()
	{
		SpawnReward(CurrencyTypes.Coins, CoinRewardValueFromBench, CoinRewardsToSpawn);
		
		SpawnReward(CurrencyTypes.Love, LoveRewardValueFromBench, LoveRewardsToSpawn);
		
		SpawnReward(CurrencyTypes.BuddyBling, BuddyBlingRewardValueFromBench, BuddyBlingRewardsToSpawn);

		StartCoroutine(CooldownOnBench());
	}
	
	private void SpawnReward(CurrencyTypes in_currencyType, int in_rewardValue, int in_rewardSpawnNumber)
	{
		for (int i = 0; i < in_rewardSpawnNumber; ++i)
		{
			Vector2 spawnPos = new Vector2(
				Random.Range(_rewardSpawnRangeX.x, _rewardSpawnRangeX.y), 
				Random.Range(_rewardSpawnRangeY.x, _rewardSpawnRangeY.y));
			var reward = Instantiate(RewardPickupPrefab, RewardSpawnPoint);
			reward.transform.localPosition = spawnPos;
			int rewardValue;
			if(in_rewardSpawnNumber > 1)
			{
				rewardValue = in_rewardValue/in_rewardSpawnNumber;
			}
			else
			{
				rewardValue = in_rewardValue;
			}
			reward.SetUpPickup(in_currencyType, rewardValue);
		}
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
