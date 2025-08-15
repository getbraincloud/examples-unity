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

public enum Rarity
{
	common,
	uncommon,
	rare,
	legendary,
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
	public Rarity rarity { get; set; }
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
}
