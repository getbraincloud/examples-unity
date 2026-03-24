using System;
using System.Collections.Generic;
using BrainCloud.Plugin;
using Gameframework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
	
	private List<ToyBenchInfo> _toyBenchInfos;
	public List<ToyBenchInfo> ToyBenchInfos
	{
		get => _toyBenchInfos;
		set => _toyBenchInfos = value;
	}

	private float _xpAcquiredAmount;
	public float XpAcquiredAmount
	{
		get => _xpAcquiredAmount;
		set => _xpAcquiredAmount = value;
	}

	private float _rewardPickupDuration;
	public float RewardPickupDuration
	{
		get => _rewardPickupDuration;
		set
		{
			if(value > 0)
				_rewardPickupDuration = value;
			else
				_rewardPickupDuration = 10f;
		}
	}

	private int _childCountMaximum;
	public int ChildCountMaximum
	{
		get => _childCountMaximum;
		set => _childCountMaximum = value;
	}
	
	public List<QuestInfo> BitBuddiesQuests = new List<QuestInfo>();
	public List<QuestInfo> BitBlingQuests = new List<QuestInfo>();
	public List<QuestInfo> GeneralQuests = new List<QuestInfo>();
	
	private List<ParentShopInfo> _parentShopInfos;
	public List<ParentShopInfo> ParentShopInfos
	{
		get => _parentShopInfos;
		set => _parentShopInfos = value;
	}

	private long _freebieItemCooldownUntil;
	public long FreebieItemCooldownUntil
	{
		get => _freebieItemCooldownUntil;
		set => _freebieItemCooldownUntil = value;
	}
	
	public void SetQuestsLists(List<QuestInfo> listOfQuests)
	{
		for (int i = 0; i < listOfQuests.Count; i++)
		{
			switch (listOfQuests[i].QuestId)
			{
				case BitBuddiesConsts.BITBUDDIES_QUESTLINEID:
					BitBuddiesQuests.Add(listOfQuests[i]);
					break;
				case BitBuddiesConsts.BITBLING_QUESTLINEID:
					BitBlingQuests.Add(listOfQuests[i]);
					break;
				case BitBuddiesConsts.GENERAL_QUESTLINEID:
					GeneralQuests.Add(listOfQuests[i]);
					break;
			}
		}
	}
	
	public override void Awake()
	{
		_selectedAppChildrenInfo = new AppChildrenInfo();
		//_eventSystem = EventSystem.current;
		base.Awake();
	}
	
	// private void Update()
	// {
	// 	if (Input.GetKeyDown(KeyCode.Tab) && _eventSystem.currentSelectedGameObject != null)
	// 	{
	// 		Selectable next = _eventSystem.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
 //         
	// 		if (next != null)
	// 		{
	// 			InputField inputfield = next.GetComponent<InputField>();
	// 			if (inputfield != null)
	// 			{
	// 				//if it's an input field, also set the text caret
	// 				inputfield.OnPointerClick(new PointerEventData(_eventSystem));
	// 			}
	// 			_eventSystem.SetSelectedGameObject(next.gameObject, new BaseEventData(_eventSystem));
	// 		}
	// 	}
	// }
	
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
	
	public void UpdateSelectedAppChildrenInfo()
	{
		for (int i = 0; i < appChildrenInfos.Count; i++)
		{
			if(appChildrenInfos[i].profileId.Equals(SelectedAppChildrenInfo.profileId, StringComparison.OrdinalIgnoreCase))
			{
				appChildrenInfos[i] = SelectedAppChildrenInfo;
			}
		}
	}
}
