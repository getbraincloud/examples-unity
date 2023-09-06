using BrainCloud;
using BrainCloud.JsonFx.Json;
using Gameframework;
using System.Collections.Generic;
using UnityEngine;

namespace BrainCloudUNETExample.Connection
{
    public class BrainCloudStats : MonoBehaviour
    {
        public static BrainCloudStats Instance
        {
            get { return s_instance; }
        }

        //Player Properties
        private int m_playerLevel = 0;
        private int m_playerExperience = 0;
        private int m_statBombsDropped = 0;
        private int m_statPlanesDestroyed = 0;
        private int m_statShotsFired = 0;
        private int m_statGamesPlayed = 0;
        private int m_statCarriersDestroyed = 0;
        private int m_statTimesDestroyed = 0;
        private int m_statGamesWon = 0;
        private int m_statBombsHit = 0;

        public string m_previousGameName = "";

        public List<AchievementData> m_achievements;
        public List<MilestoneData> m_milestones;

        public bool m_leaderboardReady = false;

        public string[] m_playerLevelTitles;

        public Dictionary<string, object> m_leaderboardData;

        public static BrainCloudStats s_instance;
        void Awake()
        {
            if (s_instance)
                DestroyImmediate(gameObject);
            else
                s_instance = this;

            m_achievements = new List<AchievementData>();
            m_milestones = new List<MilestoneData>();
        }

        public void GetLeaderboard(string aLeaderboardID, SuccessCallback success = null, FailureCallback failure = null)
        {
            m_leaderboardReady = false;
            GetLeaderboardPage(aLeaderboardID, 0, 100, success, failure);
        }

        public void GetLeaderboardPage(string aLeaderboardID, int aIndex, int aSecondIndex, SuccessCallback success = null, FailureCallback failure = null)
        {
            m_leaderboardReady = false;
            GCore.Wrapper.Client.SocialLeaderboardService.GetGlobalLeaderboardPage(
                aLeaderboardID,
                BrainCloudSocialLeaderboard.SortOrder.HIGH_TO_LOW,
                aIndex,
                aSecondIndex,
                LeaderboardSuccess_Callback + success,
                LeaderboardFailure_Callback + failure,
                null);
        }

        public class Stat
        {
            public string Name { get; set; }
            public int Value { get; set; }
            public string Key { get; set; }

            public Stat(string aName, int aValue, string aKey)
            {
                Name = aName;
                Value = aValue;
                Key = aKey;
            }
        }

        ////////////////////////////////////////////
        // BrainCloud integration code
        ////////////////////////////////////////////

        public void ReadStatistics()
        {
            // Ask brainCloud for statistics
            GCore.Wrapper.Client.PlayerStatisticsService.ReadAllUserStats(StatsSuccess_Callback, StatsFailure_Callback, null);
            GCore.Wrapper.Client.PlayerStateService.ReadUserState(StateSuccess_Callback, StateFailure_Callback, null);
            GCore.Wrapper.Client.GamificationService.ReadXpLevelsMetaData(LevelsSuccess_Callback, LevelsFailure_Callback, null);
            GCore.Wrapper.Client.GamificationService.ReadAchievements(true, AchievementSuccess_Callback, AchievementFailure_Callback, null);
            GCore.Wrapper.Client.GamificationService.ReadMilestones(true, ReadMilestonesSuccess_Callback, null);
        }

        public void ReadMilestonesSuccess_Callback(string responseData, object cbObject)
        {
            GDebug.Log(string.Format("Success | {0}", responseData));

            Dictionary<string, object> response = (Dictionary<string, object>)BrainCloud.JsonFx.Json.JsonReader.Deserialize(responseData);
            Dictionary<string, object> data = (Dictionary<string, object>)response[BrainCloudConsts.JSON_DATA];

            if (data.ContainsKey(BrainCloudConsts.JSON_MILESTONES))
            {
                object[] milestones = ((object[])data[BrainCloudConsts.JSON_MILESTONES]);
                if (milestones.Length > 0)
                {
                    m_milestones.Clear();
                    MilestoneData milestoneData = null;
                    for (int i = 0; i < milestones.Length; ++i)
                    {
                        Dictionary<string, object> milestone = (Dictionary<string, object>)milestones[i];
                        Dictionary<string, object> thresholds = (Dictionary<string, object>)milestone[BrainCloudConsts.JSON_MILESTONES_THRESHOLDS];
                        Dictionary<string, object> rewards = milestone.ContainsKey(BrainCloudConsts.JSON_MILESTONES_REWARDS) ? 
                                                            (Dictionary<string, object>)milestone[BrainCloudConsts.JSON_MILESTONES_REWARDS] : null;

                        milestoneData = new MilestoneData(milestone[BrainCloudConsts.JSON_MILESTONES_TITLE].ToString(),
                                              milestone[BrainCloudConsts.JSON_MILESTONES_ID].ToString(),
                                              milestone[BrainCloudConsts.JSON_MILESTONES_DESCRIPTION] == null ? "" : milestone[BrainCloudConsts.JSON_MILESTONES_DESCRIPTION].ToString(),
                                              milestone[BrainCloudConsts.JSON_MILESTONES_STATUS].ToString(),
                                              milestone[BrainCloudConsts.JSON_MILESTONES_CATEGORY].ToString(),
                                              milestone[BrainCloudConsts.JSON_MILESTONES_GAMEID].ToString(),
                                              milestone[BrainCloudConsts.JSON_MILESTONES_QUESTID] == null ? "" : milestone[BrainCloudConsts.JSON_MILESTONES_QUESTID].ToString(),
                                              milestone[BrainCloudConsts.JSON_MILESTONES_EXTRA_DATA] == null ? "" : milestone[BrainCloudConsts.JSON_MILESTONES_EXTRA_DATA].ToString(),
                                              thresholds,
                                              rewards);
                        m_milestones.Add(milestoneData);
                    }
                }
            }
        }

        public void AchievementSuccess_Callback(string responseData, object cbObject)
        {
            var data = JsonReader.Deserialize<Dictionary<string, object>>(responseData)["data"] as Dictionary<string, object>;
            var achievementData = data["achievements"] as Dictionary<string, object>[];
            m_achievements.Clear();
            AchievementData achievement = null;
            for (int i = 0; i < achievementData.Length; i++)
            {
                achievement = new AchievementData(achievementData[i]["title"].ToString(),
                                                  achievementData[i]["id"].ToString(),
                                                  achievementData[i]["description"].ToString(),
                                                  achievementData[i]["status"].ToString() == "AWARDED",
                                                  achievementData[i]["imageUrl"].ToString());
                m_achievements.Add(achievement);
            }
            /*
            if (m_statPlanesDestroyed >= 50)
            {
                for (int i = 0; i < m_achievements.Count; i++)
                {
                    if (m_achievements[i].m_id == "2")
                    {
                        if (!m_achievements[i].m_achieved)
                        {
                            m_achievements[i].m_achieved = true;
                            GCore.Wrapper.Client.GamificationService.AwardAchievements(m_achievements[i].m_id, null, null, null);
                            GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayAchievement(int.Parse(m_achievements[i].m_id), m_achievements[i].m_name, m_achievements[i].m_description);
                        }
                        break;
                    }
                }
            }

            if (m_statCarriersDestroyed >= 10)
            {
                for (int i = 0; i < m_achievements.Count; i++)
                {
                    if (m_achievements[i].m_id == "1")
                    {
                        if (!m_achievements[i].m_achieved)
                        {
                            m_achievements[i].m_achieved = true;
                            GCore.Wrapper.Client.GamificationService.AwardAchievements(m_achievements[i].m_id, AwardSuccess_Callback, AwardFailure_Callback, null);
                            GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayAchievement(int.Parse(m_achievements[i].m_id), m_achievements[i].m_name, m_achievements[i].m_description);
                        }
                        break;
                    }
                }
            }*/
        }

        public void AwardSuccess_Callback(string responseData, object cbObject)
        {

        }

        public void AwardFailure_Callback(int a, int b, string responseData, object cbObject)
        {
            Debug.LogError(responseData);
        }

        public void Get5KillsAchievement()
        {
            for (int i = 0; i < m_achievements.Count; i++)
            {
                if (m_achievements[i].Id == "0")
                {
                    if (!m_achievements[i].Achieved)
                    {
                        m_achievements[i].SetAchieved(true);
                        string[] achArray = new string[] { m_achievements[i].Id };
                        GCore.Wrapper.Client.GamificationService.AwardAchievements(achArray, null, null, null);
                        //GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayAchievement(int.Parse(m_achievements[i].m_id), m_achievements[i].m_name, m_achievements[i].m_description);
                    }
                    break;
                }
            }
        }

        public void AchievementFailure_Callback(int a, int b, string responseData, object cbObject)
        {
            Debug.LogError(responseData);
        }

        public void LeaderboardSuccess_Callback(string responseData, object cbObject)
        {
            m_leaderboardReady = true;
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)BrainCloud.JsonFx.Json.JsonReader.Deserialize(responseData);
            m_leaderboardData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
        }

        public void LeaderboardFailure_Callback(int a, int b, string responseData, object cbObject)
        {
            Debug.LogError(responseData);
        }

        public void SubmitLeaderboardData(int aKills, int aBombHits, int aDeaths)
        {
            int kills = aKills + m_statPlanesDestroyed;

            if (kills == 0)
                kills = 1;

            int bombs = aBombHits + m_statBombsHit;

            if (bombs == 0)
                bombs = 1;

            int killScore = (kills) * 10000 - (m_statTimesDestroyed + aDeaths);
            int bombScore = (bombs) * 10000 - (m_statTimesDestroyed + aDeaths);
            GCore.Wrapper.Client.SocialLeaderboardService.PostScoreToLeaderboard(LeaderboardSubState.JSON_KDR, killScore, "{\"rank\":\"" + m_playerLevelTitles[m_playerLevel - 1] + "\", \"level\":\"" + m_playerLevel + "\"}");
            GCore.Wrapper.Client.SocialLeaderboardService.PostScoreToLeaderboard(LeaderboardSubState.JSON_BDR, bombScore, "{\"rank\":\"" + m_playerLevelTitles[m_playerLevel - 1] + "\", \"level\":\"" + m_playerLevel + "\"}");
            ReadStatistics();
        }

        public void IncrementStatisticsToBrainCloud(int gamesPlayed, int gamesWon = 0, int timesDestroyed = 0,
                                                int shotsFired = 0, int bombsDropped = 0, int planesDestroyed = 0,
                                                int carriersDestroyed = 0, int bombsHit = 0)
        {
            // Build the statistics name/inc value dictionary
            Dictionary<string, object> stats = new Dictionary<string, object> {
            {"gamesPlayed", gamesPlayed},
            {"gamesWon", gamesWon},
            {"timesDestroyed", timesDestroyed},
            {"shotsFired", shotsFired},
            {"bombsDropped", bombsDropped},
            {"planesDestroyed", planesDestroyed},
            {"carriersDestroyed", carriersDestroyed},
            {"bombsHit", bombsHit}
        };

            // Send to the cloud
            GCore.Wrapper.Client.PlayerStatisticsService.IncrementUserStats(
                stats, StatsSuccess_Callback, StatsFailure_Callback, null);
        }

        public void IncrementExperienceToBrainCloud(int aExperience)
        {

            GCore.Wrapper.Client.PlayerStatisticsService.IncrementExperiencePoints(aExperience, StateSuccess_Callback, StateFailure_Callback, null);
        }

        private void StateSuccess_Callback(string responseData, object cbObject)
        {
            var entries = JsonReader.Deserialize<Dictionary<string, object>>(responseData)["data"] as Dictionary<string, object>;

            m_playerLevel = int.Parse(entries["experienceLevel"].ToString());
            m_playerExperience = int.Parse(entries["experiencePoints"].ToString());

            if (entries.ContainsKey("rewardDetails"))
            {
                var rewardDetails = entries["rewardDetails"] as Dictionary<string, object>;
                if (rewardDetails.ContainsKey("xp"))
                {
                    var xp = rewardDetails["xp"] as Dictionary<string, object>;

                    if (xp.ContainsKey("experienceLevels"))
                    {
                        var levels = xp["experienceLevels"] as Dictionary<string, object>[];
                        if (levels.Length > 0)
                        {
                            //var dialogDisplay = GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>();
                            //dialogDisplay.DisplayRankUp(int.Parse(levels[0]["level"].ToString()));
                        }
                    }
                }
            }

            if (entries.ContainsKey("entities"))
            {
                var entities = entries["entities"] as Dictionary<string, object>[];
                for (int i = 0; i < entities.Length; ++i)
                {
                    var data = entities[i]["data"] as Dictionary<string, object>;
                    if (data.ContainsKey("gameName"))
                    {
                        m_previousGameName = data["gameName"].ToString();
                    }
                    else
                    {
                        GFriendsManager.Instance.OnReadRecentlyViewedEntitySuccess(JsonWriter.Serialize(data), cbObject);
                    }
                }
            }
        }

        private void StateFailure_Callback(int a, int b, string responseData, object cbObject)
        {
            Debug.LogError(responseData);
        }

        private void LevelsSuccess_Callback(string responseData, object cbObject)
        {
            var data = JsonReader.Deserialize<Dictionary<string, object>>(responseData)["data"] as Dictionary<string, object>;
            var entries = data["xp_levels"] as Dictionary<string, object>[];

            m_playerLevelTitles = new string[entries.Length];

            for (int i = 0; i < entries.Length; i++)
            {
                m_playerLevelTitles[i] = entries[i]["statusTitle"].ToString();
            }
        }

        private void LevelsFailure_Callback(int a, int b, string responseData, object cbObject)
        {
            Debug.LogError(responseData);
        }

        private void StatsSuccess_Callback(string responseData, object cbObject)
        {
            // Read the json and update our values
            var data = JsonReader.Deserialize<Dictionary<string, object>>(responseData)["data"] as Dictionary<string, object>;
            var entries = data["statistics"] as Dictionary<string, object>;

            m_statBombsDropped = int.Parse(entries["bombsDropped"].ToString());
            m_statPlanesDestroyed = int.Parse(entries["planesDestroyed"].ToString());
            m_statShotsFired = int.Parse(entries["shotsFired"].ToString());
            m_statGamesPlayed = int.Parse(entries["gamesPlayed"].ToString());
            m_statCarriersDestroyed = int.Parse(entries["carriersDestroyed"].ToString());
            m_statTimesDestroyed = int.Parse(entries["timesDestroyed"].ToString());
            m_statGamesWon = int.Parse(entries["gamesWon"].ToString());
            m_statBombsHit = int.Parse(entries["bombsHit"].ToString());

            if (entries.ContainsKey("rewardDetails"))
            {
                var rewardDetails = entries["rewardDetails"] as Dictionary<string, object>;
                if (rewardDetails.ContainsKey("milestones"))
                {
                    var milestones = rewardDetails["milestones"] as Dictionary<string, object>[];
                    if (milestones.Length > 0)
                    {
                        var rewards = milestones[0]["rewards"] as Dictionary<string, object>;

                        if (rewards.Count > 0 && rewards.ContainsKey("achievement"))
                        {
                            //assuming the player received an achievement
                            int achievementID = int.Parse(rewards["achievement"].ToString());

                            for (int i = 0; i < m_achievements.Count; i++)
                            {
                                if (m_achievements[i].Id == achievementID.ToString())
                                {
                                    //var dialogDisplay = GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>();
                                    //dialogDisplay.DisplayAchievement(achievementID, m_achievements[i].m_name, m_achievements[i].m_description);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void StatsFailure_Callback(int a, int b, string responseData, object cbObject)
        {
            Debug.LogError(responseData);
        }

        void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        public List<Stat> GetStats()
        {
            return new List<Stat>()
        {
            {new Stat("Rank", m_playerLevel, "experienceLevel")},
            {new Stat("Experience", m_playerExperience, "experiencePoints")},
            {new Stat("Games Played", m_statGamesPlayed, "gamesPlayed")},
            {new Stat("Games Won", m_statGamesWon, "gamesWon")},
            {new Stat("Shots Fired", m_statShotsFired, "shotsFired")},
            {new Stat("Bombs Dropped", m_statBombsDropped, "bombsDropped")},
            {new Stat("Bombs Hit", m_statBombsHit, "bombsHit")},
            {new Stat("Planes Destroyed", m_statPlanesDestroyed, "planesDestroyed")},
            {new Stat("Carriers Destroyed", m_statCarriersDestroyed, "carriersDestroyed")},
            {new Stat("Times Destroyed", m_statTimesDestroyed, "timesDestroyed")},
        };
        }
    }
}
