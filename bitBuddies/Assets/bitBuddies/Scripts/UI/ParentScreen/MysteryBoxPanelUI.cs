using System;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MysteryBoxPanelUI : ContentUIBehaviour
{
	[SerializeField] private TMP_Text TitleText;
    [SerializeField] private Button CloseButton;
    [SerializeField] private Transform MysteryBoxSpawnPoint;
	[SerializeField] private List<MysteryBoxInfo> MysteryBoxes;
	[SerializeField] private MysteryBoxUI MysteryBoxPrefab;
	[SerializeField] private Button DoneButton; //for page 3, closes the whole panel but captures the data input
	[SerializeField] private TMP_InputField NameBuddyInput;	// for page 3
	[SerializeField] private Button OpenBoxButton;	// for page 2
	[SerializeField] private List<GameObject> _mysteryScreens;   //0 = selection, 1 = open box, 2 = name buddy,display stats etc

	[SerializeField] private TMP_Text CoinMultiplierText;
	[SerializeField] private TMP_Text CoinPerHourText;
	[SerializeField] private TMP_Text CoinCapacityText;
	[SerializeField] private TMP_Text RarityText;
	[SerializeField] private TMP_Text BuddyTypeNameText;
	[SerializeField] private Image BuddyImage;
	private MysteryBoxInfo _mysteryBoxInfo;
	public MysteryBoxInfo MysteryBoxInfo
	{
		set {_mysteryBoxInfo = value;}
	}
	private int _screenIndex;
	private ParentMenu _parentMenu;
	//Screen Titles
	private const string DEFAULT_BUDDY_NAME = "MyBuddy";
	private const string LIST_BOXES_TEXT_TITLE = "Pick a mystery box";
	private const string OPEN_BOX_TEXT_TITLE = "Open your Mystery Box";
	private const string NEW_BUDDY_TEXT_TITLE = "New BitBuddy!";
	
	//screen 3 text presets
	private const string COIN_PAYOUT_TEXT = "Coin Payouts ";
	private const string COIN_GAIN_TEXT = "Idle Coin Gains ";
	private const string COIN_PER_HOUR_TEXT = "/hr";
	private const string COIN_CAPACITY_TEXT = "Idle Coins Capacity ";
	

	protected override void Awake()
	{
		InitializeUI();
		for (int i = 0; i < _mysteryScreens.Count; i++)
		{
			_mysteryScreens[i].SetActive(false);
		}
		_mysteryScreens[0].SetActive(true);
		base.Awake();
	}

	protected override void InitializeUI()
    {
	    foreach (MysteryBoxInfo mysteryBoxInfo in MysteryBoxes)
	    {
			var box = Instantiate(MysteryBoxPrefab, MysteryBoxSpawnPoint);
			box.MysteryBoxInfo = mysteryBoxInfo;    
			box.Init();
	    }
	    _parentMenu = FindAnyObjectByType<ParentMenu>();
	    OpenBoxButton.onClick.AddListener(OnOpenBox);
	    CloseButton.onClick.AddListener(OnCloseButton);
	    DoneButton.onClick.AddListener(OnDoneButton);
	    TitleText.text = LIST_BOXES_TEXT_TITLE;
	    _screenIndex = 0;
    }
    
	private void OnOpenBox()
	{
		//Open another screen where we Animate the box opening
		// After box is opened, we show another screen where the user 
		// picks the name of buddy
		Dictionary<string, object> scriptData = new Dictionary<string, object>
		{
			{"childAppId", BrainCloudConsts.APP_CHILD_ID},
			{"lootboxType", _mysteryBoxInfo.Rarity}
		};
		BrainCloudManager.Wrapper.ScriptService.RunScript
		(
			BrainCloudConsts.AWARD_RANDOM_LOOTBOX_SCRIPT_NAME,
			scriptData.Serialize(),
			BrainCloudManager.HandleSuccess("Award new buddy Success", OnGetLootboxInfo),
			BrainCloudManager.HandleFailure("Award new buddy Failure", OnFailureCallback)
		);
		
	}
	
	private void OnGetLootboxInfo(string jsonResponse)
	{
		BrainCloudManager.Instance.OnAddRandomChildProfile(jsonResponse);
		var listOfBuddies = GameManager.Instance.AppChildrenInfos;
		if(listOfBuddies.Count > 0)
		{
			var recentBuddyAdded = listOfBuddies[listOfBuddies.Count - 1];
			_parentMenu.NewAppChildrenInfo = new AppChildrenInfo();
			_parentMenu.NewAppChildrenInfo.profileId = recentBuddyAdded.profileId;
			
			//Extract entity data from response
			var packet = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
			var data =  packet["data"] as Dictionary<string, object>;
			var response = data["response"] as Dictionary<string, object>;
			var getProfileResult = response["getProfileResult"] as Dictionary<string, object>;
			var profileChildren = getProfileResult["childEntityData"] as Dictionary<string, object>[];
			if(profileChildren != null)
			{
				for (int i = 0; i < profileChildren.Length; i++)
				{
					var childData = profileChildren[i]["childData"] as Dictionary<string, object>;
					var childName = childData["profileName"] as string;
					if(childName.IsNullOrEmpty())
					{
						var entityData = profileChildren[i]["entityData"] as Dictionary<string, object>;
						var entityInfo = entityData["data"] as Dictionary<string, object>;
						_parentMenu.NewAppChildrenInfo.rarity = Enum.Parse<Rarity>(entityInfo["rarity"] as string);
						_parentMenu.NewAppChildrenInfo.coinPerHour = (int) entityInfo["coinPerHour"];
						_parentMenu.NewAppChildrenInfo.maxCoinCapacity = (int) entityInfo["maxCoinCapacity"];
						var multiplier = entityInfo["coinMultiplier"] as double?;
						if(multiplier.HasValue)
						{
							_parentMenu.NewAppChildrenInfo.coinMultiplier = (float) multiplier;	
						}
						else
						{
							_parentMenu.NewAppChildrenInfo.coinMultiplier = 1.0f;
						}
						
						SetupBuddyDataDisplay();
					}
				}	
			}
		}
		NextPage();
	}
    
	private void OnFailureCallback()
	{
        
	}
    
    public void NextPage()
    {
	    if (_screenIndex < _mysteryScreens.Count - 1)
	    {
		    _mysteryScreens[_screenIndex].SetActive(false);
		    _screenIndex++;
		    _mysteryScreens[_screenIndex].SetActive(true);
		    if (_screenIndex == 1)
		    {
			    TitleText.text = OPEN_BOX_TEXT_TITLE;
		    }
		    else
		    {
			    TitleText.text = NEW_BUDDY_TEXT_TITLE;
		    }
	    }
	    else
	    {
		    gameObject.SetActive(false);
	    }
    }
    
    private void OnDoneButton()
    {
		DoneButton.onClick.RemoveAllListeners();
		DoneButton.interactable = false;
	    string nameValue = NameBuddyInput.text.IsNullOrEmpty() ? "bob": NameBuddyInput.text;
	    BrainCloudManager.Instance.UpdateChildProfileName(nameValue, _parentMenu.NewAppChildrenInfo.profileId);
    }
    
    private void SetupBuddyDataDisplay()
    {
	    var childAppInfo = _parentMenu.NewAppChildrenInfo;
	    CoinMultiplierText.text = COIN_PAYOUT_TEXT + childAppInfo.coinMultiplier + "x";
	    CoinPerHourText.text = COIN_GAIN_TEXT + childAppInfo.coinPerHour + COIN_PER_HOUR_TEXT;
	    CoinCapacityText.text = COIN_CAPACITY_TEXT + childAppInfo.maxCoinCapacity;
	    RarityText.text = childAppInfo.rarity.ToString();
	    BuddyTypeNameText.text = childAppInfo.buddyType.ToString();
	    int buddyIndex = (int)childAppInfo.buddyType;
	    BuddyImage.sprite = GameManager.Instance.BuddySprites[buddyIndex];
    }
    
}
