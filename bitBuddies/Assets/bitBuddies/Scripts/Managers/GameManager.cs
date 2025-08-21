using System;
using System.Collections.Generic;
using BrainCloud.Plugin;
using Gameframework;
using UnityEngine;

public enum BuddyType
{
	Buddy01,
	Buddy02,
	Buddy03,
	Buddy04,
}

[Serializable]
public class AppChildrenInfo
{
	public string profileName { get; set; }
	public string profileId { get; set; }
	public Dictionary<string, object> summaryFriendData { get; set; }
	public int buddyBling { get; set; }
	public int buddyLove { get; set; }
	public int buddySpriteIndex { get; set; }
	public float coinMultiplier { get; set; }
	public int coinPerHour { get; set; }
	public int maxCoinCapacity {get; set;}
	public BuddyType buddyType { get; set; }
	public string rarity { get; set; }
	public int buddyLevel { get; set; }
}

public class GameManager : SingletonBehaviour<GameManager>
{
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
		base.Awake();
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
	
	
}
