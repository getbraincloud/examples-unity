using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuddyHouseInfo : MonoBehaviour
{
	public AppChildrenInfo HouseInfo;
	[SerializeField] private Button _visitButton;
	[SerializeField] private Button _deleteButton;

	private void Awake()
	{
		_visitButton.onClick.AddListener(OnVisitButton);
		_deleteButton.onClick.AddListener(OnDeleteButton);
	}
	
	private void OnVisitButton()
	{
		//ToDo: Go to buddys room
		GameManager.Instance.SelectedAppChildrenInfo = HouseInfo;
		StateManager.Instance.GoToBuddysRoom();
	}
	
	private void OnDeleteButton()
	{
		//ToDo: Delete this shit
	}
}
