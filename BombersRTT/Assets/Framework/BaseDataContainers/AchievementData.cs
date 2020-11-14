using System.Collections.Generic;

namespace Gameframework
{
    public class AchievementData
    {
        #region public
        public AchievementData()
        {
            Name = "";
            Id = "";
            Description = "";
            ImageURL = "";
            Achieved = false;
        }

        public AchievementData(string aName, string aID, string aDesc, bool aAchieved, string aImageURL)
        {
            Name = aName;
            Id = aID;
            Description = aDesc;
            ImageURL = aImageURL;
            Achieved = aAchieved;
        }

        public void SetAchieved(bool in_isAchieved)
        {
            Achieved = in_isAchieved;
        }

        public void SetThresholds(Dictionary<string, object> in_thresholds)
        {
            Thresholds = in_thresholds;
        }
        #endregion

        #region Public Accessors
        public string Name
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
        public string ImageURL
        {
            get; private set;
        }
        public bool Achieved
        {
            get; private set;
        }
        public Dictionary<string, object> Thresholds
        {
            get; private set;
        }
        #endregion
    }
}