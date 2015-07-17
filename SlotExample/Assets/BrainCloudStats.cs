using UnityEngine;
using System.Collections;
using LitJson;
using System.Collections.Generic;
using BrainCloudSlots.Lobby;

namespace BrainCloudSlots.Connection
{
    public class BrainCloudStats : MonoBehaviour
    {
        public int m_credits = 0;
        public string m_userName = "";
        //public int m_gems = 0;
        public JsonData m_slotsData = null;
        public JsonData m_slotsDataEggies = null;
        public int m_progressiveJackpot = 0;
        public bool m_readyToPlay = false;
        public bool m_showOffersPage = false;

        public JsonData m_userData = null;

        public JsonData m_productData = null;

        public string m_termsConditionsString = "";

        /*public bool m_leaderboardReady = false;

        public JsonData m_leaderboardData;

        public void GetLeaderboardPage(string aLeaderboardID, int aIndex, int aSecondIndex)
        {
            m_leaderboardReady = false;
            BrainCloudWrapper.GetBC().SocialLeaderboardService.GetGlobalLeaderboardPage(aLeaderboardID, BrainCloud.BrainCloudSocialLeaderboard.SortOrder.HIGH_TO_LOW, aIndex, aSecondIndex, true, LeaderboardSuccess_Callback, LeaderboardFailure_Callback, null);
        }
        
        public void LeaderboardSuccess_Callback(string responseData, object cbObject)
        {
            m_leaderboardReady = true;
            m_leaderboardData = JsonMapper.ToObject(responseData)["data"];
        }
        
        public void SubmitLeaderboardData(int aKills, int aBombHits, int aDeaths)
        {
            BrainCloudWrapper.GetBC().SocialLeaderboardService.PostScoreToLeaderboard("KDR", (ulong)killScore, "");
            BrainCloudWrapper.GetBC().SocialLeaderboardService.PostScoreToLeaderboard("BDR", (ulong)bombScore, "");
            ReadStatistics();
        }
        */

        public void ReadSlotsData()
        {
            BrainCloudWrapper.GetBC().GlobalEntityService.GetList("{ \"entityType\" : \"rapaNuiSlotsData\" }", "", 1, SaveSlotsData, SaveSlotsFailure, null);
            BrainCloudWrapper.GetBC().GlobalEntityService.GetList("{ \"entityType\" : \"eggiesSlotsData\" }", "", 1, SaveSlots2Data, SaveSlotsFailure, null);
            BrainCloudWrapper.GetBC().GlobalStatisticsService.ReadAllGlobalStats(SaveJackpotData, SaveSlotsFailure, null);
            BrainCloudWrapper.GetBC().ProductService.GetSalesInventory("facebook", "USD", SaveProductData, SaveSlotsFailure, null);
        }

        public void SaveProductData(string responseData, object cbObject)
        {
            JsonData response = JsonMapper.ToObject(responseData);
            m_productData = response["data"]["product_inventory"];
            GameObject.Find("GameLobby").GetComponent<GameLobby>().UpdateProductButtons(m_productData);
        }

        public void ReadJackpotData()
        {
            BrainCloudWrapper.GetBC().GlobalStatisticsService.ReadAllGlobalStats(SaveJackpotData, SaveSlotsFailure, null);
        }

        public void SaveSlots2Data(string responseData, object cbObject)
        {
            m_readyToPlay = true;
            JsonData response = JsonMapper.ToObject(responseData);
            m_slotsDataEggies = response["data"]["entityList"][0]["data"];
        }

        public void SaveSlotsData(string responseData, object cbObject)
        {
            m_readyToPlay = true;
            JsonData response = JsonMapper.ToObject(responseData);
            m_slotsData = response["data"]["entityList"][0]["data"];
        }

        public void SaveJackpotData(string responseData, object cbObject)
        {
            JsonData response = JsonMapper.ToObject(responseData);
            m_progressiveJackpot = int.Parse(response["data"]["statistics"]["progressiveJackpot"].ToString());
        }

        public void SaveSlotsFailure(int a, int b, string responseData, object cbObject)
        {
            Debug.Log(a);
            Debug.Log(b);
            Debug.Log(responseData);
        }

        public void ReadGlobalProperties()
        {
            BrainCloudWrapper.GetBC().GlobalAppService.ReadProperties(PropertiesSuccess_Callback, PropertiesFailure_Callback, null);
        }

        public void ReadStatistics()
        {
            // Ask brainCloud for statistics
            BrainCloudWrapper.GetBC().PlayerStateService.ReadPlayerState(StateSuccess_Callback, StateFailure_Callback, null);
        }
        
        private void StateSuccess_Callback(string responseData, object cbObject)
        {
            JsonData jsonData = JsonMapper.ToObject(responseData);
            JsonData entries = jsonData["data"]["currency"];

            m_credits = int.Parse(entries["Credits"]["balance"].ToString());
            //m_gems = int.Parse(entries["Gems"]["balance"].ToString());
        }

        private void StateFailure_Callback(int a, int b, string responseData, object cbObject)
        {
            Debug.LogError(responseData);
        }

        void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void PropertiesSuccess_Callback(string responseData, object cbObject)
        {
            JsonData jsonData = JsonMapper.ToObject(responseData);
            JsonData entries = jsonData["data"];
        }

        private void PropertiesFailure_Callback(int a, int b, string responseData, object cbObject)
        {
            Debug.LogError(responseData);
        }
    }
}