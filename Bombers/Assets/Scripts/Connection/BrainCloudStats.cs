using UnityEngine;
using LitJson;
using System.Collections.Generic;

namespace BrainCloudPhotonExample.Connection
{
    public class BrainCloudStats : MonoBehaviour
    {
        public class Achievement
        {
            public string m_name = "";
            public string m_id = "";
            public string m_description = "";

            public bool m_achieved = false;

            public Achievement(string aName, string aID, string aDesc, bool aAchieved)
            {
                m_name = aName;
                m_id = aID;
                m_description = aDesc;
                m_achieved = aAchieved;
            }
        }

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

        ///Global Properties
        public float m_defaultGameTime = 0;
        public float m_bulletLifeTime = 0;
        public float m_fireRateDelay = 0;
        public float m_bulletSpeed = 0;
        public float m_planeTurnSpeed = 0;
        public float m_planeAcceleration = 0;
        public int m_basePlaneHealth = 0;
        public float m_maxPlaneSpeedMultiplier = 0;
        public int m_expForKill = 0;
        public int m_pointsForShipDestruction = 0;
        public int m_pointsForWeakpointDestruction = 0;
        public int m_maxBombCapacity = 0;
        public float m_shipIntensity = 0;
        public float m_weakpointIntensity = 0;
        public float m_shakeTime = 0;
        public float m_multiShotDelay = 0;
        public int m_multiShotAmount = 0;
        public float m_multiShotBurstDelay = 0;
        public float m_fastModeFireRateDelay = 0;
        public float m_bombPickupLifetime = 0;
        public float m_flareLifetime = 0;
        public float m_flareCooldown = 0;
        public string m_previousGameName = "";

        public List<Achievement> m_achievements;

        public bool m_leaderboardReady = false;

        public string[] m_playerLevelTitles;

        public JsonData m_leaderboardData;

        public static BrainCloudStats s_instance;

        void Awake()
        {
            if (s_instance)
                DestroyImmediate(gameObject);
            else
                s_instance = this;

            m_achievements = new List<Achievement>();
        }

        public void GetLeaderboard(string aLeaderboardID)
        {
            m_leaderboardReady = false;
            GetLeaderboardPage(aLeaderboardID, 0, 100);
        }

        public void GetLeaderboardPage(string aLeaderboardID, int aIndex, int aSecondIndex)
        {
            m_leaderboardReady = false;
            BrainCloudWrapper.GetBC().SocialLeaderboardService.GetGlobalLeaderboardPage(aLeaderboardID, BrainCloud.BrainCloudSocialLeaderboard.SortOrder.HIGH_TO_LOW, aIndex, aSecondIndex, true, LeaderboardSuccess_Callback, LeaderboardFailure_Callback, null);
        }

        public class Stat
        {
            public string m_statName { get; set; }
            public int m_statValue { get; set; }

            public Stat(string aName, int aValue)
            {
                m_statName = aName;
                m_statValue = aValue;
            }
        }

        ////////////////////////////////////////////
        // BrainCloud integration code
        ////////////////////////////////////////////

        public void ReadGlobalProperties()
        {
            BrainCloudWrapper.GetBC().GlobalAppService.ReadProperties(PropertiesSuccess_Callback, PropertiesFailure_Callback, null);
        }

        public void ReadStatistics()
        {
            // Ask brainCloud for statistics
            BrainCloudWrapper.GetBC().PlayerStatisticsService.ReadAllPlayerStats(StatsSuccess_Callback, StatsFailure_Callback, null);
            BrainCloudWrapper.GetBC().PlayerStateService.ReadPlayerState(StateSuccess_Callback, StateFailure_Callback, null);
            BrainCloudWrapper.GetBC().GamificationService.ReadXpLevelsMetaData(LevelsSuccess_Callback, LevelsFailure_Callback, null);
            BrainCloudWrapper.GetBC().GamificationService.ReadAchievements(true, AchievementSuccess_Callback, AchievementFailure_Callback, null);
        }

        public void AchievementSuccess_Callback(string responseData, object cbObject)
        {
            JsonData achievementData = JsonMapper.ToObject(responseData);
            achievementData = achievementData["data"]["achievements"];
            m_achievements.Clear();
            for (int i = 0; i < achievementData.Count; i++)
            {
                Achievement achievement = new Achievement(achievementData[i]["title"].ToString(), achievementData[i]["id"].ToString(), achievementData[i]["description"].ToString(), achievementData[i]["status"].ToString() == "AWARDED");
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
                            BrainCloudWrapper.GetBC().GamificationService.AwardAchievements(m_achievements[i].m_id, null, null, null);
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
                            BrainCloudWrapper.GetBC().GamificationService.AwardAchievements(m_achievements[i].m_id, AwardSuccess_Callback, AwardFailure_Callback, null);
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
                if (m_achievements[i].m_id == "0")
                {
                    if (!m_achievements[i].m_achieved)
                    {
                        m_achievements[i].m_achieved = true;
                        string[] achArray = new string[] { m_achievements[i].m_id };
                        BrainCloudWrapper.GetBC().GamificationService.AwardAchievements(achArray, null, null, null);
                        GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayAchievement(int.Parse(m_achievements[i].m_id), m_achievements[i].m_name, m_achievements[i].m_description);
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
            m_leaderboardData = JsonMapper.ToObject(responseData)["data"];
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
            BrainCloudWrapper.GetBC().SocialLeaderboardService.PostScoreToLeaderboard("KDR", killScore, "{\"rank\":\"" + m_playerLevelTitles[m_playerLevel - 1] + "\", \"level\":\"" + m_playerLevel + "\"}");
            BrainCloudWrapper.GetBC().SocialLeaderboardService.PostScoreToLeaderboard("BDR", bombScore, "{\"rank\":\"" + m_playerLevelTitles[m_playerLevel - 1] + "\", \"level\":\"" + m_playerLevel + "\"}");
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
            BrainCloudWrapper.GetBC().PlayerStatisticsService.IncrementPlayerStats(
                stats, StatsSuccess_Callback, StatsFailure_Callback, null);
        }

        public void IncrementExperienceToBrainCloud(int aExperience)
        {

            BrainCloudWrapper.GetBC().PlayerStatisticsService.IncrementExperiencePoints(aExperience, StateSuccess_Callback, StateFailure_Callback, null);
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
                            var dialogDisplay = GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>();
                            dialogDisplay.DisplayRankUp(int.Parse(rewardDetails["xp"]["experienceLevels"][0]["level"].ToString()));
                        }
                    }
                }
            }

            if (entries.Keys.Contains("entities"))
            {
                var entities = entries["entities"];
                if (entities.Count > 0)
                    m_previousGameName = entries["entities"][0]["data"]["gameName"].ToString();
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
                                if (m_achievements[i].m_id == achievementID.ToString())
                                {
                                    var dialogDisplay = GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>();
                                    dialogDisplay.DisplayAchievement(achievementID, m_achievements[i].m_name, m_achievements[i].m_description);
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
            {new Stat("Games Won", m_statGamesWon)},
            {new Stat("Games Played", m_statGamesPlayed)},
            {new Stat("Shots Fired", m_statShotsFired)},
            {new Stat("Bombs Dropped", m_statBombsDropped)},
            {new Stat("Bombs Hit", m_statBombsHit)},
            {new Stat("Planes Destroyed", m_statPlanesDestroyed)},
            {new Stat("Carriers Destroyed", m_statCarriersDestroyed)},
            {new Stat("Times Destroyed", m_statTimesDestroyed)},
        };
        }

        private void PropertiesSuccess_Callback(string responseData, object cbObject)
        {
            // Read the json and update our values
            JsonData jsonData = JsonMapper.ToObject(responseData);
            JsonData entries = jsonData["data"];

            m_defaultGameTime = float.Parse(entries["DefaultGameTime"]["value"].ToString());
            m_bulletLifeTime = float.Parse(entries["BulletLifeTime"]["value"].ToString());
            m_fireRateDelay = float.Parse(entries["FireRateDelay"]["value"].ToString());
            m_bulletSpeed = float.Parse(entries["BulletSpeed"]["value"].ToString());
            m_planeTurnSpeed = float.Parse(entries["TurnSpeed"]["value"].ToString());
            m_planeAcceleration = float.Parse(entries["PlaneAcceleration"]["value"].ToString());
            m_basePlaneHealth = int.Parse(entries["BasePlaneHealth"]["value"].ToString());
            m_maxPlaneSpeedMultiplier = float.Parse(entries["MaxSpeedMultiplier"]["value"].ToString());
            m_expForKill = int.Parse(entries["ExpForKill"]["value"].ToString());
            m_pointsForShipDestruction = int.Parse(entries["ScoreForShipDestruction"]["value"].ToString());
            m_pointsForWeakpointDestruction = int.Parse(entries["ScoreForWeakpointDestruction"]["value"].ToString());
            m_maxBombCapacity = int.Parse(entries["MaxBombCapacity"]["value"].ToString());
            m_shipIntensity = float.Parse(entries["ShipDestructionShakeIntensity"]["value"].ToString());
            m_weakpointIntensity = float.Parse(entries["WeakpointDestructionShakeIntensity"]["value"].ToString());
            m_shakeTime = float.Parse(entries["ScreenShakeTime"]["value"].ToString());
            m_multiShotDelay = float.Parse(entries["MultishotDelay"]["value"].ToString());
            m_multiShotAmount = int.Parse(entries["MultishotAmount"]["value"].ToString());
            m_multiShotBurstDelay = float.Parse(entries["MultishotBurstDelay"]["value"].ToString());
            m_fastModeFireRateDelay = float.Parse(entries["FastModeFireRateDelay"]["value"].ToString());
            m_bombPickupLifetime = float.Parse(entries["BombPickupLifeTime"]["value"].ToString());
            m_flareLifetime = float.Parse(entries["FlareLifeTime"]["value"].ToString());
            m_flareCooldown = float.Parse(entries["FlareCooldown"]["value"].ToString());


        }

        private void PropertiesFailure_Callback(int a, int b, string responseData, object cbObject)
        {
            Debug.LogError(responseData);
        }
    }
}