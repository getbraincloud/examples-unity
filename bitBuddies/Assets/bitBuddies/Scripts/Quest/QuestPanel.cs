using System;
using System.Collections.Generic;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using UnityEngine;


public class QuestPanel : MonoBehaviour
{
	[SerializeField] private QuestCard QuestCardPrefab;
	[SerializeField] private Transform BitBuddiesSpawnPoint;
	[SerializeField] private Transform BitBlingSpawnPoint;
	[SerializeField] private Transform GeneralSpawnPoint;
	
	private QuestCard _activeBitBuddyCard;
	private QuestCard _activeBitBlingCard;
	private QuestCard _activeGeneralCard;
	
	private QuestCard _lockedBitBuddyCard;
	private QuestCard _lockedBitBlingCard;
	private QuestCard _lockedGeneralCard;
	
	public void SetUpPanel()
	{
		GameManager gameManager = GameManager.Instance;
		
		SetUpQuestCard(gameManager.GeneralQuestsActive, gameManager.GeneralQuestsLocked, GeneralSpawnPoint);		
		SetUpQuestCard(gameManager.BitBuddiesQuestsActive, gameManager.BitBuddiesQuestsLocked, BitBuddiesSpawnPoint);
		SetUpQuestCard(gameManager.BitBlingQuestsActive, gameManager.BitBlingQuestsLocked, BitBlingSpawnPoint);
	}
	
	private void SetUpQuestCard(List<QuestInfo> listOfActiveQuests, List<QuestInfo> listOfLockedQuests, Transform spawnParent)
	{
		QuestInfo nextQuestInLine = GetNextLockedQuestInLine(listOfLockedQuests, listOfActiveQuests[0]);
		QuestInfo activeQuest = GetLatestActiveQuestInLine(listOfActiveQuests);
		
		if(activeQuest.QuestStatToTrack.IsNullOrEmpty())
		{
			Debug.Log("Quest stat to track is null or empty");
			return;
		}
		QuestCard activeQuestCard = Instantiate(QuestCardPrefab, spawnParent);
		activeQuestCard.SetupCard(activeQuest);
		
		if(nextQuestInLine.QuestStatToTrack.IsNullOrEmpty())
		{
			Debug.Log("Next quest in line is null or empty");
			return;
		}
		QuestCard lockedQuestCard = Instantiate(QuestCardPrefab, spawnParent);
		lockedQuestCard.SetupCard(nextQuestInLine);
	}
	
	private QuestInfo GetNextLockedQuestInLine(List<QuestInfo> listOfLockedQuests, QuestInfo currentActiveQuest)
	{
		for (int i = 0; i < listOfLockedQuests.Count; ++i)
		{
			if(listOfLockedQuests[i].QuestLineIndex == currentActiveQuest.QuestLineIndex + 1)
			{
				return listOfLockedQuests[i];
			}
		}

		return new QuestInfo();
	}
	
	private QuestInfo GetLatestActiveQuestInLine(List<QuestInfo> listOfActiveQuests)
	{
		var questInfo = listOfActiveQuests[0];
		for (int i = 0; i < listOfActiveQuests.Count; ++i)
		{
			if(listOfActiveQuests[i].QuestStatus == QUEST_STATUS.UNLOCKED || 
			   listOfActiveQuests[i].QuestStatus == QUEST_STATUS.IN_PROGRESS)
			{
				if(questInfo.QuestLineIndex < listOfActiveQuests[i].QuestLineIndex)
				{
					questInfo = listOfActiveQuests[i];
				}
			}
		}

		return questInfo;
	}
}
