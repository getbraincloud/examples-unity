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
