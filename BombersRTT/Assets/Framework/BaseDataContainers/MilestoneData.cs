using System.Collections.Generic;

namespace Gameframework
{
    public class MilestoneData
    {
        #region public
        public MilestoneData()
        {
            Title = "";
            Id = "";
            Description = "";
            Status = "";
            Category = "";
            GameId = "";
            QuestId = "";
            ExtraData = "";
            Thresholds = null;
            Rewards = null;
        }

        public MilestoneData(string aTitle, string aID, string aDesc, string aStatus, string aCategory, string aGameId, string aQuestId, string aExtraData, Dictionary<string, object> aThresholds, Dictionary<string, object> aRewards)
        {
            Title = aTitle;
            Id = aID;
            Description = aDesc;
            Status = aStatus;
            Category = aCategory;
            GameId = aGameId;
            QuestId = aQuestId;
            ExtraData = aExtraData;
            Thresholds = aThresholds;
            Rewards = aRewards;
        }
        #endregion

        #region Public Accessors
        public string Title
        {
            get; private set;
        }
        public string Id
        {
            get; private set;
        }
        public string Description
        {
            get; private set;
        }
        public string Status
        {
            get; private set;
        }
        public string Category
        {
            get; private set;
        }
        public string GameId
        {
            get; private set;
        }
        public string QuestId
        {
            get; private set;
        }
        public string ExtraData
        {
            get; private set;
        }
        public Dictionary<string, object> Thresholds
        {
            get; private set;
        }
        public Dictionary<string, object> Rewards
        {
            get; private set;
        }
        #endregion
    }
}