using System.Collections.Generic;
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
		//ToDo: Not sure why but a quest from bitBling line is coming back null, not sure if its lock or active
		QuestInfo nextQuestInLine = GetNextQuestInLine(listOfLockedQuests, listOfActiveQuests[0]);
		QuestInfo activeQuest = listOfActiveQuests[0];
		
		QuestCard activeQuestCard = Instantiate(QuestCardPrefab, spawnParent);
		activeQuestCard.SetupCard(activeQuest);
		
		QuestCard lockedQuestCard = Instantiate(QuestCardPrefab, spawnParent);
		lockedQuestCard.SetupCard(nextQuestInLine);
	}
	
	private QuestInfo GetNextQuestInLine(List<QuestInfo> listOfLockedQuests, QuestInfo currentActiveQuest)
	{
		for (int i = 0; i < listOfLockedQuests.Count; i++)
		{
			if(listOfLockedQuests[i].QuestLineIndex == currentActiveQuest.QuestLineIndex + 1)
			{
				return listOfLockedQuests[i];
			}
		}

		return new QuestInfo();
	}
}
