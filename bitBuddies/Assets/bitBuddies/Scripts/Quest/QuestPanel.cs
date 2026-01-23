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

	private const string BITBUDDIES_QUESTID = "bitBuddyQuestLine";
	
	public void SetUpPanel()
	{
		var listOfActiveQuests = GameManager.Instance.ActiveQuests;
		var listOfLockedQuests = GameManager.Instance.LockedQuests;
		
		for (int i = 0; i < listOfActiveQuests.Count; i++)
		{
			if(listOfActiveQuests[i].QuestId == BITBUDDIES_QUESTID)
			{
				
			}
		}
	}
}
