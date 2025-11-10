using System;
using System.Collections.Generic;
using BrainCloud.Plugin;
using Gameframework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class AppChildrenInfo
{
	public string profileName { get; set; }
	public string profileId { get; set; }
	public Dictionary<string, object> summaryFriendData { get; set; }
	public int buddyBling { get; set; }
	public float coinMultiplier { get; set; }
	public int coinPerHour { get; set; }
	public int maxCoinCapacity {get; set;}
	public string buddySpritePath { get; set; }
	public string rarity { get; set; }
	public int buddyLevel { get; set; }
	public int nextLevelUp { get; set; }
	public int currentXP { get; set; }
	public DateTime lastIdleTimestamp { get; set; }
	public int coinsEarnedInHolding { get; set; }
	//Love is only earned through Toy interaction in Buddys Room.
	//Aka used as current XP for profile. 
	public int loveEarnedInLifetime {get; set;}
	public int coinsEarnedInLifetime {get; set;}
	
	private float _hourInSeconds = 3600;
	
	public void CheckCoinsEarned()
	{
		TimeSpan timeDifference = DateTime.UtcNow - lastIdleTimestamp;
		float coinsPerSecond = coinPerHour / _hourInSeconds;
		int coinsEarned = Mathf.FloorToInt(coinsPerSecond * (float)timeDifference.TotalSeconds);
		if(coinsEarned > 0)
		{
			if(coinsEarned < maxCoinCapacity)
			{
				coinsEarnedInHolding = coinsEarned;
			}
			else
			{
				coinsEarnedInHolding = maxCoinCapacity;
			}			
		}
	}
}


public class GameManager : SingletonBehaviour<GameManager>
{
	[Tooltip("Debug")]
	[SerializeField] public bool Debug;
	private EventSystem _eventSystem;
	[Tooltip("App Info")]
	private List<AppChildrenInfo> appChildrenInfos = new List<AppChildrenInfo>();
	public List<AppChildrenInfo> AppChildrenInfos
	{
		get { return appChildrenInfos; }
		set { appChildrenInfos = value; }
	}
	public Sprite[] BuddySprites;
	private List<MysteryBoxInfo> _mysteryBoxes;
	public List<MysteryBoxInfo> MysteryBoxes
	{
		get => _mysteryBoxes;
		set => _mysteryBoxes = value;
	}
	private AppChildrenInfo _selectedAppChildrenInfo;
	public AppChildrenInfo SelectedAppChildrenInfo
	{
		get { return _selectedAppChildrenInfo; }
		set { _selectedAppChildrenInfo = value; }
	}

	public override void Awake()
	{
		_selectedAppChildrenInfo = new AppChildrenInfo();
		_eventSystem = EventSystem.current;
		base.Awake();
	}
	
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Tab) && _eventSystem.currentSelectedGameObject != null)
		{
			Selectable next = _eventSystem.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
         
			if (next != null)
			{
				InputField inputfield = next.GetComponent<InputField>();
				if (inputfield != null)
				{
					//if it's an input field, also set the text caret
					inputfield.OnPointerClick(new PointerEventData(_eventSystem));
				}
				_eventSystem.SetSelectedGameObject(next.gameObject, new BaseEventData(_eventSystem));
			}
		}
	}
	
	public void OnDeleteBuddySuccess()
	{
		/*
		 * Update list to remove selected child info
		 * Refresh screen to display the current
		 */
		appChildrenInfos.Remove(_selectedAppChildrenInfo);
		StateManager.Instance.RefreshScreen();
	}
	
	public void ClearDataForLogout()
	{
		appChildrenInfos.Clear();
		_selectedAppChildrenInfo = null;
	}
	
	public void UpdateChildAppInfo(AppChildrenInfo in_appChildrenInfo)
	{
		var index = appChildrenInfos.FindIndex(x => x.profileId == in_appChildrenInfo.profileId);
		if(index != -1)
		{
			appChildrenInfos[index] = in_appChildrenInfo;
		}
	}
	
}
