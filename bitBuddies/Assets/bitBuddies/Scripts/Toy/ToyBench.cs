using System.Collections;
using System.Collections.Generic;
using BrainCloud.JSONHelper;
using Gameframework;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ToyBench : MonoBehaviour
{
	public string BenchId
	{
		get => _toyBenchInfo.BenchId;
	}
	
	[SerializeField] private Transform RewardSpawnPoint;
	[SerializeField] private RewardPickup RewardPickupPrefab;
	[SerializeField] private GameObject ReadyIcon;
	[SerializeField] private GameObject SpinnerIcon;
	[SerializeField] private Button AddToyButton;
 
	private int _rewardSpawnNumber;	//used to determine how many rewards are spawned in level	

	private ToyBenchInfo _toyBenchInfo;
	
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
		AddToyButton.onClick.AddListener(OnAddButton);
		SpinnerIcon.gameObject.SetActive(false);
		_moveBuddyAnimation = FindFirstObjectByType<MoveBuddyAnimation>();
		_buddyTargetPosition = gameObject.GetComponent<RectTransform>();

	}
	
	public void SetUpToyBench(ToyBenchInfo in_toyBenchInfo)
	{
		_toyBenchInfo = in_toyBenchInfo;
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
		AddToyButton.gameObject.SetActive(false);
	}
	
	public void DisableBench()
	{
		SpinnerIcon.gameObject.SetActive(false);
		ReadyIcon.gameObject.SetActive(false);
		if(!_benchButton)
			_benchButton = GetComponent<Button>();
		_benchButton.interactable = false;
		AddToyButton.gameObject.SetActive(true);
	}
	
	public void MoveBuddyToReward(Vector2 in_position)
	{
		_moveBuddyAnimation.MoveBuddyToPosition(in_position);		
	}
	
	private void OnAddButton()
	{
		var title = "Are you sure?";
		var body = $"Buy {_toyBenchInfo.DisplayName} for {_toyBenchInfo.UnlockCost}?<br>(Must be level {_toyBenchInfo.LevelRequirement} or higher to acquire)";
		if(_toyBenchInfo.UnlockCost == 0)
		{
			body = $"Get the {_toyBenchInfo.DisplayName} for free?";
		}
		bool canBuyToy = BrainCloudManager.Instance.UserInfo.Coins >= _toyBenchInfo.UnlockCost;
		if(canBuyToy)
		{
			canBuyToy = GameManager.Instance.SelectedAppChildrenInfo.buddyLevel >= _toyBenchInfo.LevelRequirement;
		}
		StateManager.Instance.OpenConfirmPopUp(title, body, BuyToy, canBuyToy);
	}
	
	private void BuyToy()
	{
		ToyManager.Instance.ObtainToy(_toyBenchInfo.BenchId, BenchIsObtained);
	}
	
	private void BenchIsObtained()
	{
		StateManager.Instance.OpenInfoPopUp("Toy Bench Acquired!", $"You have obtained {_toyBenchInfo.DisplayName}");
		EnableBench();
	}
	
	private void MoveBuddyToBench()
	{
		var position =_buddyTargetPosition.localPosition; 
		position = new Vector2(_buddyTargetPosition.localPosition.x, _buddyTargetPosition.localPosition.y + _buddyTargetPositionOffsetY);
		_moveBuddyAnimation.MoveBuddyToBench(position, RequestConsumeToy);
	}

	private void RequestConsumeToy()
	{
		var scriptData = new Dictionary<string, object>();
		scriptData.Add("toyId", BenchId);
		scriptData.Add("childProfileId", GameManager.Instance.SelectedAppChildrenInfo.profileId);
		scriptData.Add("childAppId", BitBuddiesConsts.APP_CHILD_ID);
		BrainCloudManager.Wrapper.ScriptService.RunScript
		(
			BitBuddiesConsts.CONSUME_TOY_SCRIPT_NAME,
			scriptData.Serialize(),
			BrainCloudManager.HandleSuccess("Consume Toy Success", OnConsumeToySuccess),
			BrainCloudManager.HandleFailure("Consume Toy Failure", OnConsumeToyFailure)
		);
	}
	
	private void OnConsumeToySuccess(string jsonResponse)
	{
		_rewardSpawnNumber = 0;
		var data = jsonResponse.Deserialize("data");
		
		if(data.TryGetValue("response", out object response))
		{
			var responseDict = response as Dictionary<string, object>;
			var dropInfo = responseDict["dropInfo"] as Dictionary<string, object>;
			_toyBenchInfo.CoinRewardAmount = (int) dropInfo["coinPayout"];
			_toyBenchInfo.LoveRewardAmount = (int) dropInfo["lovePayout"];
			_toyBenchInfo.BuddyBlingRewardAmount = (int) dropInfo["blingPayout"];
			_toyBenchInfo.Cooldown = (int) dropInfo["cooldown"];
			var entityId = dropInfo["entityId"] as string;
			ToyManager.Instance.SetRewardEntityId(entityId);
		}
		
		SpawnReward(CurrencyTypes.Coins, _toyBenchInfo.CoinRewardAmount, _toyBenchInfo.CoinSpawnAmount);
		
		SpawnReward(CurrencyTypes.Love, _toyBenchInfo.LoveRewardAmount, _toyBenchInfo.LoveSpawnAmount);
		
		SpawnReward(CurrencyTypes.BuddyBling, _toyBenchInfo.BuddyBlingRewardAmount, _toyBenchInfo.BuddyBlingSpawnAmount);
		ToyManager.Instance.IncrementRewardSpawnCount(_rewardSpawnNumber);

		StartCoroutine(CooldownOnBench());
	}
	
	private void OnConsumeToyFailure()
	{
		StateManager.Instance.OpenInfoPopUp(BitBuddiesConsts.CONSUME_TOY_FAILED_TITLE,BitBuddiesConsts.CONSUME_TOY_FAILED_MESSAGE);
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
			reward.SetUpPickup(in_currencyType, rewardValue, MoveBuddyToReward, this);
		}
		_rewardSpawnNumber += in_rewardSpawnNumber;
	}
	
	IEnumerator CooldownOnBench()
	{
		_benchButton.interactable = false;
		ReadyIcon.gameObject.SetActive(false);
		SpinnerIcon.gameObject.SetActive(true);
		yield return new WaitForSeconds(_toyBenchInfo.Cooldown);
		_benchButton.interactable = true;
		ReadyIcon.gameObject.SetActive(true);
		SpinnerIcon.gameObject.SetActive(false);
	}
}
