using System;
using System.Collections.Generic;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using UnityEngine;
using UnityEngine.UI;


public class QuestPanel : MonoBehaviour
{
	[SerializeField] private QuestCard QuestCardPrefab;
	[SerializeField] private Transform BitBuddiesSpawnPoint;
	[SerializeField] private Transform BitBlingSpawnPoint;
	[SerializeField] private Transform GeneralSpawnPoint;
	[SerializeField] private Button NextPageButton;
	[SerializeField] private Button PreviousPageButton;
	[SerializeField] private Button CloseButton;
	
	private QuestCard _firstRowBitBuddyCard;
	private QuestCard _firstRowBitBlingCard;
	private QuestCard _firstRowGeneralCard;
	
	private QuestCard _secondRowBitBuddyCard;
	private QuestCard _secondRowBitBlingCard;
	private QuestCard _secondRowGeneralCard;

	private int _pageIndex;
	
	public void SetUpPanel()
	{
		GameManager gameManager = GameManager.Instance;
		NextPageButton.onClick.AddListener(OnNextPageButton);
		PreviousPageButton.onClick.AddListener(OnPreviousPageButton);
		CloseButton.onClick.AddListener(OnCloseButtonPressed);
		_pageIndex = 0;
		SetUpQuestCard(gameManager.GeneralQuests, GeneralSpawnPoint);		
		SetUpQuestCard(gameManager.BitBuddiesQuests, BitBuddiesSpawnPoint);
		SetUpQuestCard(gameManager.BitBlingQuests, BitBlingSpawnPoint);
	}
	
	private void UpdatePanel()
	{
		foreach (Transform child in GeneralSpawnPoint)
		{
			Destroy(child.gameObject);
		}
		foreach (Transform child in BitBuddiesSpawnPoint)
		{
			Destroy(child.gameObject);
		}
		foreach (Transform child in BitBlingSpawnPoint)
		{
			 Destroy(child.gameObject);
		}
	
		GameManager gameManager = GameManager.Instance;
		SetUpQuestCard(gameManager.GeneralQuests, GeneralSpawnPoint);		
		SetUpQuestCard(gameManager.BitBuddiesQuests, BitBuddiesSpawnPoint);
		SetUpQuestCard(gameManager.BitBlingQuests, BitBlingSpawnPoint);
	}

	private void OnDestroy()
	{
		NextPageButton.onClick.RemoveAllListeners();
		PreviousPageButton.onClick.RemoveAllListeners();
		CloseButton.onClick.RemoveAllListeners();
	}

	private void OnNextPageButton()
	{
		_pageIndex++;
		if(_pageIndex > 2)
		{
			_pageIndex = 0;
		}
		UpdatePanel();
	}
	
	private void OnPreviousPageButton()
	{
		_pageIndex--;
		if(_pageIndex < 0)
		{
			_pageIndex = 2;
		}
		UpdatePanel();
	}
	
	
	private void SetUpQuestCard(List<QuestInfo> listOfQuests, Transform spawnParent)
	{
		QuestInfo firstRowQuest = GetFirstRow(listOfQuests);
		QuestInfo secondRowQuest = GetSecondRow(listOfQuests);
		
		if(secondRowQuest.QuestStatToTrack.IsNullOrEmpty())
		{
			Debug.Log("Quest stat to track is null or empty");
			return;
		}
		QuestCard activeQuestCard = Instantiate(QuestCardPrefab, spawnParent);
		activeQuestCard.SetupCard(firstRowQuest);
		
		if(firstRowQuest.QuestStatToTrack.IsNullOrEmpty())
		{
			Debug.Log("Next quest in line is null or empty");
			return;
		}
		QuestCard lockedQuestCard = Instantiate(QuestCardPrefab, spawnParent);
		lockedQuestCard.SetupCard(secondRowQuest);
	}
	
	private QuestInfo GetFirstRow(List<QuestInfo> listOfQuests)
	{
		int questIndex;
		if(_pageIndex == 1)
		{
			questIndex = 2;
		}
		else if(_pageIndex == 2)
		{
			questIndex = 4;
		}
		else
		{
			questIndex = 0;
		}

		for (int i = 0; i < listOfQuests.Count; i++)
		{
			if(listOfQuests[i].QuestLineIndex == questIndex)
			{
				return listOfQuests[i];
			}
		}

		return new QuestInfo();
	}
	
	private QuestInfo GetSecondRow(List<QuestInfo> listOfQuests)
	{
		int questIndex;
		if(_pageIndex == 1)
		{
			questIndex = 3;
		}
		else if(_pageIndex == 2)
		{
			questIndex = 5;
		}
		else
		{
			questIndex = 1;
		}

		for (int i = 0; i < listOfQuests.Count; i++)
		{
			if(listOfQuests[i].QuestLineIndex == questIndex)
			{
				return listOfQuests[i];
			}
		}

		return new QuestInfo();
	}
	
	private void OnCloseButtonPressed()
	{
		Destroy(gameObject);
		
	}
}
