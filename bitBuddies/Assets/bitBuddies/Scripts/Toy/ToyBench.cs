using System;
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
	[SerializeField] private Button AddToyButton;
	[SerializeField] private Sprite AvailableBenchSprite;
	[SerializeField] private Sprite UnavailableBenchSprite;
	[SerializeField] private Sprite CooldownSprite;
	[SerializeField] private Sprite ReadySprite;
 
	private int _rewardSpawnNumber;	//used to determine how many rewards are spawned in level	
	private Image _readyStatusImage;
	private ToyBenchInfo _toyBenchInfo;
	private Image _toyBenchImage;
	private Vector2 _rewardSpawnRangeX = new Vector2(-440, 440);
	private Vector2 _rewardSpawnRangeY = new Vector2(-125, 125);
	private Button _benchButton;
	private bool _isEnabled;
	private MoveBuddyAnimation _moveBuddyAnimation;
	private RectTransform _buddyTargetPosition;
	private int _buddyTargetPositionOffsetY = 300;
	private float _currentCooldown;
	
	private void Awake()
	{
		_benchButton = GetComponent<Button>();
		_benchButton.onClick.AddListener(MoveBuddyToBench);
		AddToyButton.onClick.AddListener(OnAddButton);
		_moveBuddyAnimation = FindFirstObjectByType<MoveBuddyAnimation>();
		_buddyTargetPosition = gameObject.GetComponent<RectTransform>();
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}

	public void SetUpToyBench(ToyBenchInfo in_toyBenchInfo)
	{
		_toyBenchInfo = in_toyBenchInfo;
		_toyBenchImage = GetComponent<Image>();
		_readyStatusImage = ReadyIcon.GetComponent<Image>();
		_readyStatusImage.fillMethod = Image.FillMethod.Vertical;
		_readyStatusImage.type = Image.Type.Filled;
		if(!_benchButton)
			_benchButton = GetComponent<Button>();
	}
	
	public void EnableBench()
	{
		ReadyIcon.gameObject.SetActive(true);
		_readyStatusImage.sprite = ReadySprite;
		_benchButton.interactable = true;
		AddToyButton.gameObject.SetActive(false);
		_toyBenchImage.sprite = AvailableBenchSprite;
	}
	
	public void DisableBench()
	{
		ReadyIcon.gameObject.SetActive(false);
		_benchButton.interactable = false;
		AddToyButton.gameObject.SetActive(true);
		_toyBenchImage.sprite = UnavailableBenchSprite;
	}
	
	private void OnAddButton()
	{
		var position = new Vector2(_buddyTargetPosition.localPosition.x, _buddyTargetPosition.localPosition.y + _buddyTargetPositionOffsetY);
		_moveBuddyAnimation.MoveBuddyToPosition(position, OnBuddyArrivedAtBench);
	}

	private void OnBuddyArrivedAtBench()
	{
		var title = "Are you sure?";
		var body = $"Buy {_toyBenchInfo.DisplayName} for {_toyBenchInfo.UnlockCost}?<br>(Must be level {_toyBenchInfo.LevelRequirement} or higher to acquire)";
		if(_toyBenchInfo.UnlockCost == 0)
		{
			body = $"Get the {_toyBenchInfo.DisplayName} for free?";
		}
		bool canBuyToy = BrainCloudManager.Instance.CurrentUserInfo.Coins >= _toyBenchInfo.UnlockCost;
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
		var position = new Vector2(_buddyTargetPosition.localPosition.x, _buddyTargetPosition.localPosition.y + _buddyTargetPositionOffsetY);
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
		
		if(_toyBenchInfo.Cooldown > 0)
		{
			StartCoroutine(CooldownBench());
		}
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
			reward.transform.localPosition = transform.localPosition + new Vector3(0, 50, 0);

			int rewardValue;
			if(in_rewardSpawnNumber > 1)
			{
				rewardValue = in_rewardValue/in_rewardSpawnNumber;
			}
			else
			{
				rewardValue = in_rewardValue;
			}
			reward.SetUpPickup(in_currencyType, rewardValue, this, spawnPos);
		}
		_rewardSpawnNumber += in_rewardSpawnNumber;
	}
	
	private IEnumerator CooldownBench()
	{
		_benchButton.interactable = false;
		_readyStatusImage.sprite = CooldownSprite;
		_readyStatusImage.fillAmount = 0f;
		float elapsed = 0f;
		while(elapsed < _toyBenchInfo.Cooldown)
		{
			elapsed += Time.deltaTime;
			float fillAmount = elapsed / _toyBenchInfo.Cooldown;
			_readyStatusImage.fillAmount = Mathf.Clamp01(fillAmount);
			yield return null;
		}
		_readyStatusImage.sprite = ReadySprite;
		_benchButton.interactable = true;
	}
}
