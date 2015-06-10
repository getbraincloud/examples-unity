using UnityEngine;
using System.Collections;
using LitJson;
using System.Collections.Generic;

namespace BrainCloudPhotonExample.Connection
{
    public class BrainCloudStats : MonoBehaviour
    {

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

        public bool m_leaderboardReady = false;

        public JsonData m_leaderboardData;

        public void GetLeaderboard(string aLeaderboardID)
        {
            m_leaderboardReady = false;
            BrainCloudWrapper.GetBC().SocialLeaderboardService.GetGlobalLeaderboard(aLeaderboardID, BrainCloud.BrainCloudSocialLeaderboard.FetchType.HIGHEST_RANKED, 100, LeaderboardSuccess_Callback, LeaderboardFailure_Callback, null);
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
            BrainCloudWrapper.GetBC().SocialLeaderboardService.PostScoreToLeaderboard("KDR", (ulong)killScore, "");
            BrainCloudWrapper.GetBC().SocialLeaderboardService.PostScoreToLeaderboard("BDR", (ulong)bombScore, "");
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
        }

        private void StateFailure_Callback(int a, int b, string responseData, object cbObject)
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

        }

        private void PropertiesFailure_Callback(int a, int b, string responseData, object cbObject)
        {
            Debug.LogError(responseData);
        }
    }
}