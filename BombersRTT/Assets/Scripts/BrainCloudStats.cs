using UnityEngine;
using BrainCloud.LitJson;
using System.Collections.Generic;
using Gameframework;
using BrainCloud;

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

            public Stat(string aName, int aValue)
            {
                Name = aName;
                Value = aValue;
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
        }

        public void AchievementSuccess_Callback(string responseData, object cbObject)
        {
            JsonData achievementData = JsonMapper.ToObject(responseData);
            achievementData = achievementData["data"]["achievements"];
            m_achievements.Clear();
            AchievementData achievement = null;
            for (int i = 0; i < achievementData.Count; i++)
            {
                achievement = new AchievementData(achievementData[i]["title"].ToString(), achievementData[i]["id"].ToString(), achievementData[i]["description"].ToString(), achievementData[i]["status"].ToString() == "AWARDED", achievementData[i]["imageUrl"].ToString());
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
            JsonData jsonData = JsonMapper.ToObject(responseData);
            JsonData entries = jsonData["data"];

            m_playerLevel = int.Parse(entries["experienceLevel"].ToString());
            m_playerExperience = int.Parse(entries["experiencePoints"].ToString());

            if (entries.Keys.Contains("rewardDetails"))
            {
                JsonData rewardDetails = entries["rewardDetails"];
                if (rewardDetails.Keys.Contains("xp"))
                {
                    var xp = rewardDetails["xp"];

                    if (xp.Keys.Contains("experienceLevels"))
                    {
                        var levels = xp["experienceLevels"];
                        if (levels.Count > 0)
                        {
                            //var dialogDisplay = GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>();
                            //dialogDisplay.DisplayRankUp(int.Parse(rewardDetails["xp"]["experienceLevels"][0]["level"].ToString()));
                        }
                    }
                }
            }

            if (entries.Keys.Contains("entities"))
            {
                var entities = entries["entities"];
                for (int i = 0; i < entities.Count; ++i)
                {
                    if (entities[i]["data"].Keys.Contains("gameName"))
                    {
                        m_previousGameName = entities[i]["data"]["gameName"].ToString();
                    }
                    else
                    {
                        GFriendsManager.Instance.OnReadRecentlyViewedEntitySuccess(entities[i].ToJson(), cbObject);
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
            JsonData jsonData = JsonMapper.ToObject(responseData);
            JsonData entries = jsonData["data"]["xp_levels"];

            m_playerLevelTitles = new string[entries.Count];

            for (int i = 0; i < entries.Count; i++)
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
            JsonData jsonData = JsonMapper.ToObject(responseData);
            JsonData entries = jsonData["data"]["statistics"];

            m_statBombsDropped = int.Parse(entries["bombsDropped"].ToString());
            m_statPlanesDestroyed = int.Parse(entries["planesDestroyed"].ToString());
            m_statShotsFired = int.Parse(entries["shotsFired"].ToString());
            m_statGamesPlayed = int.Parse(entries["gamesPlayed"].ToString());
            m_statCarriersDestroyed = int.Parse(entries["carriersDestroyed"].ToString());
            m_statTimesDestroyed = int.Parse(entries["timesDestroyed"].ToString());
            m_statGamesWon = int.Parse(entries["gamesWon"].ToString());
            m_statBombsHit = int.Parse(entries["bombsHit"].ToString());

            if (entries.Keys.Contains("rewardDetails"))
            {
                JsonData rewardDetails = entries["rewardDetails"];
                if (rewardDetails.Keys.Contains("milestones"))
                {
                    var milestones = rewardDetails["milestones"];
                    if (milestones.Count > 0)
                    {
                        var rewards = rewardDetails["milestones"][0]["rewards"];

                        if (rewards.Count > 0 && rewards.Keys.Contains("achievement"))
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
            {new Stat("Rank", m_playerLevel)},
            {new Stat("Experience", m_playerExperience)},
            {new Stat("Games Played", m_statGamesPlayed)},
            {new Stat("Games Won", m_statGamesWon)},
            {new Stat("Shots Fired", m_statShotsFired)},
            {new Stat("Bombs Dropped", m_statBombsDropped)},
            {new Stat("Bombs Hit", m_statBombsHit)},
            {new Stat("Planes Destroyed", m_statPlanesDestroyed)},
            {new Stat("Carriers Destroyed", m_statCarriersDestroyed)},
            {new Stat("Times Destroyed", m_statTimesDestroyed)},
        };
        }
    }
}