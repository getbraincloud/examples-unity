using System.Collections.Generic;
using BrainCloud.Entity;
using UnityEngine;

public class BestScores : GameScene
{
    protected const int MAX_ENTRIES = 10;

    protected int _newScore;
    protected int _newScoreIndex;

    public GUIText[] EntriesText;

    public Spinner Spinner;

    public virtual void postNewScore(int in_score)
    {
        _newScore = in_score;
        _newScoreIndex = -1; // -1 means it's not in the top 10
        Spinner.gameObject.SetActive(true);

        foreach (var entry in EntriesText) entry.text = "";

        // Save 10 last best scores into a player entity in brainCloud


        // Fetch existing entities for this player
        App.Bc.PlayerStateService.ReadUserState((responseData, cbObject) =>
        {
            // Construct a list of all entities present for this player
            var entityFactory = new BCEntityFactory(App.Bc.EntityService);
            var entities = entityFactory.NewUserEntitiesFromReadPlayerState(responseData);

            // Search for our "BestScores" entity
            BCUserEntity bestScoresEntity = null;
            foreach (var entity in entities)
                if (entity.EntityType == "BestScores")
                {
                    // We found it
                    bestScoresEntity = entity;
                    break;
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
                var newEntries = new List<int>(MAX_ENTRIES);
                for (var i = 0; i < MAX_ENTRIES; ++i) newEntries.Add(0);
                bestScoresEntity["entries"] = newEntries;
            }

            // Get our best score entries from our Entity
            var entries = bestScoresEntity.Get<IList<int>>("entries");

            // Insert our new score in it
            for (var i = 0; i < MAX_ENTRIES; ++i)
            {
                var previous = entries[i];
                if (_newScore > previous)
                {
                    // Shift previous entries back
                    for (var j = MAX_ENTRIES - 1; j > i; --j) entries[j] = entries[j - 1];

                    // Insert new one here
                    entries[i] = _newScore;
                    _newScoreIndex = i;
                    break;
                }
            }

            // Store our entity into the cloud
            bestScoresEntity.StoreAsync();

            // Ok done, update our UI
            for (var i = 0; i < MAX_ENTRIES; ++i)
            {
                var score = entries[i];
                if (score == 0)
                    EntriesText[i].text = "---"; // No entry here
                else
                    EntriesText[i].text = score.ToString();

                // Show our new score if it's high enough, with little arrows --> score <--
                if (_newScoreIndex == i) EntriesText[i].text = "--> " + EntriesText[i].text + " <--";
            }

            Spinner.gameObject.SetActive(false);
        }, null, null);
    }
}