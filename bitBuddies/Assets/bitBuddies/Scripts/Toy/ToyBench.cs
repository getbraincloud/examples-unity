using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ToyBench : MonoBehaviour
{
	public string BenchId
	{
		get => _benchId;
		set => _benchId = value;
	}
	private string _benchId;
	[SerializeField] private Transform RewardSpawnPoint;
	[SerializeField] private RewardPickup RewardPickupPrefab;
	[SerializeField] private GameObject ReadyIcon;
	[SerializeField] private GameObject SpinnerIcon;
	
	[SerializeField] private int CoinRewardsToSpawn;
	[SerializeField] private int LoveRewardsToSpawn;
	[SerializeField] private int BuddyBlingRewardsToSpawn;
	
	private float _cooldownTime;
	private int _coinRewardValueFromBench;
	private int _loveRewardValueFromBench;
	private int _buddyBlingRewardValueFromBench;
	
	private Vector2 _rewardSpawnRangeX = new Vector2(-440, 440);
	private Vector2 _rewardSpawnRangeY = new Vector2(-125, 125);
	private Button _benchButton;
	private bool _isEnabled;
	private MoveBuddyAnimation _moveBuddyAnimation;
	private RectTransform _buddyTargetPosition;
	private int _buddyTargetPositionOffsetY = 250;
	private void Awake()
	{
		_benchButton = GetComponent<Button>();
		_benchButton.onClick.AddListener(MoveBuddyToBench);
		SpinnerIcon.gameObject.SetActive(false);
		_moveBuddyAnimation = FindFirstObjectByType<MoveBuddyAnimation>();
		_buddyTargetPosition = gameObject.GetComponent<RectTransform>();

	}
	
	public void SetUpToyBench(ToyBenchInfo in_toyBenchInfo)
	{
		_benchId = in_toyBenchInfo.BenchId;
		_cooldownTime = in_toyBenchInfo.Cooldown;
		_coinRewardValueFromBench = in_toyBenchInfo.CoinPayout;
		_loveRewardValueFromBench = in_toyBenchInfo.LovePayout;
		_buddyBlingRewardValueFromBench = in_toyBenchInfo.BuddyBlingPayout;
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}
	
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
	
	public void MoveBuddyToReward(Vector2 in_position)
	{
		_moveBuddyAnimation.MoveBuddyToPosition(in_position);		
	}
	
	private void MoveBuddyToBench()
	{
		var position =_buddyTargetPosition.localPosition; 
		position = new Vector2(_buddyTargetPosition.localPosition.x, _buddyTargetPosition.localPosition.y + _buddyTargetPositionOffsetY);
		_moveBuddyAnimation.MoveBuddyToBench(position, SpawnAllRewards);
	}

	private void SpawnAllRewards()
	{
		SpawnReward(CurrencyTypes.Coins, _coinRewardValueFromBench, CoinRewardsToSpawn);
		
		SpawnReward(CurrencyTypes.Love, _loveRewardValueFromBench, LoveRewardsToSpawn);
		
		SpawnReward(CurrencyTypes.BuddyBling, _buddyBlingRewardValueFromBench, BuddyBlingRewardsToSpawn);

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
			//Moving this to a cloud code script
			// int rewardValue;
			// if(in_rewardSpawnNumber > 1)
			// {
			// 	rewardValue = in_rewardValue/in_rewardSpawnNumber;
			// }
			// else
			// {
			// 	rewardValue = in_rewardValue;
			// }
			reward.SetUpPickup(in_currencyType, MoveBuddyToReward, this);
		}
	}
	
	IEnumerator CooldownOnBench()
	{
		_benchButton.interactable = false;
		ReadyIcon.gameObject.SetActive(false);
		SpinnerIcon.gameObject.SetActive(true);
		yield return new WaitForSeconds(_cooldownTime);
		_benchButton.interactable = true;
		ReadyIcon.gameObject.SetActive(true);
		SpinnerIcon.gameObject.SetActive(false);
	}
}
