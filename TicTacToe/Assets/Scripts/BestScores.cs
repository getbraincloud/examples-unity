using UnityEngine;
using System.Collections;
using BrainCloud.Entity;
using System.Collections.Generic;
using LitJson;

public class BestScores : MonoBehaviour {

	public Spinner spinner;
	public GUIText[] entriesText;

	protected int m_newScore;
	protected int m_newScoreIndex;

	protected const int MAX_ENTRIES = 10;

	// Use this for initialization
	void Start () 
	{
	
	}

	public virtual void postNewScore(int in_score)
	{
		m_newScore = in_score;
		m_newScoreIndex = -1; // -1 means it's not in the top 10
		spinner.gameObject.SetActive(true);

		foreach (GUIText entry in entriesText)
		{
			entry.text = "";
		}

		// Save 10 last best scores into a player entity in brainCloud

		// Get our existing "BestScores" entity (if it exists)
		//TODO: Call not supported yet by brainCloud
	/*	BrainCloudWrapper.GetBC().GetEntityService().GetEntitiesByType(
			"BestScores",
			(string responseData, object cbObject) => {
				int tmp;
				tmp = 5;
			},
			(string responseData, object cbObject) => {
				int tmp;
				tmp = 5;
			},
			null);*/

		// Fetch existing entities for this player
		BrainCloudWrapper.GetBC().GetPlayerStateService().ReadPlayerState((string responseData, object cbObject) => {

			// Construct a list of all entities present for this player
			BCEntityFactory entityFactory = new BCEntityFactory(BrainCloudWrapper.GetBC().GetEntityService());
			IList<BCUserEntity> entities = entityFactory.NewUserEntitiesFromReadPlayerState(responseData);

			// Search for our "BestScores" entity
			BCUserEntity bestScoresEntity = null;
			foreach (BCUserEntity entity in entities)
			{
				if (entity.EntityType == "BestScores")
				{
					// We found it
					bestScoresEntity = entity;
					break;
				}
			}

			// [dsl] Note: all previous lines could be simplified by a new BC call: 
			// BrainCloudWrapper.GetBC().GetEntityService().GetEntitiesByType("BestScores");

			// Check if our entity exists or not
			if (bestScoresEntity == null)
			{
				// Entity was not created. This will happend if we
				// do this for the first time for this player.
				// We create it
				bestScoresEntity = entityFactory.NewUserEntity("BestScores");

				// Fill it with empty list of leaderboard entries
				List<int> newEntries = new List<int>(MAX_ENTRIES);
				for (int i = 0; i < MAX_ENTRIES; ++i)
				{
					newEntries.Add(0);
				}
				bestScoresEntity["entries"] = newEntries;
			}

			// Get our best score entries from our Entity
			IList<int> entries = bestScoresEntity.Get<IList<int>>("entries");

			// Insert our new score in it
			for (int i = 0; i < MAX_ENTRIES; ++i)
			{
				int previous = entries[i];
				if (m_newScore > previous)
				{
					// Shift previous entries back
					for (int j = MAX_ENTRIES - 1; j > i; --j)
					{
						entries[j] = entries[j - 1];
					}

					// Insert new one here
					entries[i] = m_newScore;
					m_newScoreIndex = i;
					break;
				}
			}

			// Store our entity into the cloud
			bestScoresEntity.StoreAsync(null, null);

			// Ok done, update our UI
			for (int i = 0; i < MAX_ENTRIES; ++i)
			{
				int score = entries[i];
				if (score == 0)
				{
					entriesText[i].text = "---"; // No entry here
				}
				else 
				{
					entriesText[i].text = score.ToString();
				}

				// Show our new score if it's high enough, with little arrows --> score <--
				if (m_newScoreIndex == i)
				{
					entriesText[i].text = "--> " + entriesText[i].text + " <--";
				}
			}
			spinner.gameObject.SetActive(false);
		}, null, null);
	}
}
