using Gameframework;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;
using System.Linq;

namespace Gameframework
{
    public class GLevelManager : SingletonBehaviour<GLevelManager>
    {
        #region Public Variables
        public List<LevelUpRewards> Rewards
        {
            get
            {
                return m_rewards;
            }
            private set { m_rewards = value; }
        }
        #endregion

        #region Public Constants
        public const string XP_LEVELS = "xp_levels";
        public const string LEVEL_PREFIX = "Level_";
        public const string PACK = "PACK";
        public const string UNLOCK_LIST = "UnlockList";
        public const string PROMO = "PROMO";

        // some unlock keys that we could add off of
        public const string SEASON_UNLOCK = "SEASON PLAY";
        #endregion

        #region Public Functions
        public void PopulateRewardsList()
        {
            GCore.Wrapper.Client.GamificationService.ReadXpLevelsMetaData(onReadMetaDataSuccess);
        }

        public LevelUpRewards GetRewardAtLevel(int in_iLevel)
        {
            LevelUpRewards rewardToReturn = null;
            for (int i = 0; i < Rewards.Count; i++)
            {
                if (Rewards[i].LevelUnlocked == in_iLevel)
                {
                    rewardToReturn = Rewards[i];
                    break;
                }
            }
            return rewardToReturn;
        }

        public int GetRequiredXPForLevel(int in_iLevel)
        {
            LevelUpRewards reward = GetRewardAtLevel(in_iLevel);
            if (reward != null)
            {
                return reward.Experience;
            }
            return 0;
        }

        public void ResetCachedLevel()
        {
            m_iCachedPlayerLevel = 0;
        }

        public void OnPreExperienceGain()
        {
            m_iCachedPlayerLevel = GPlayerMgr.Instance.PlayerData.PlayerXPData.CurrentLevel;
        }

        public void CheckForRequireLevelupDialogue()
        {
            if (m_iCachedPlayerLevel != 0 && m_iCachedPlayerLevel < GPlayerMgr.Instance.PlayerData.PlayerXPData.CurrentLevel)
            {
                LevelUpRewards currentReward = GetRewardAtLevel(GPlayerMgr.Instance.PlayerData.PlayerXPData.CurrentLevel);
                if (currentReward != null)
                {
                    // TODO
                }
            }
        }
        #endregion

        #region Private Functions
        private void onReadMetaDataSuccess(string in_sJsonResponse, object in_cbObject)
        {
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_sJsonResponse);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
            if (jsonData.ContainsKey(XP_LEVELS))
            {
                try
                {
                    Dictionary<string, object>[] jsonLevels = (Dictionary<string, object>[])jsonData[XP_LEVELS];

                    LevelUpRewards reward;
                    for (int i = 0; i < jsonLevels.Length; i++)
                    {
                        reward = new LevelUpRewards(jsonLevels[i]);
                        Rewards.Add(reward);
                    }
                    GCore.Wrapper.GlobalEntityService.GetListByIndexedId("playerLevelUp", 1, onGetEntitySuccess);
                }
                catch (System.Exception) { GEventManager.TriggerEvent(GEventManager.ON_PLAYER_DATA_UPDATED);  }
            }
        }
        
        private void onGetEntitySuccess(string in_jsonString, object in_object)
        {
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_jsonString);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
            try
            {
                Dictionary<string, object> jsonArray = (Dictionary<string, object>)(((object[])jsonData[BrainCloudConsts.JSON_ENTITY_LIST])[0]);
                Dictionary<string, object> jsonLevelPacks = (Dictionary<string, object>)jsonArray[BrainCloudConsts.JSON_DATA];

                for (int i = 2; i <= jsonLevelPacks.Count + 1; ++i)
                {
                    parseLevelPackData(jsonLevelPacks, i);
                }

                GEventManager.TriggerEvent(GEventManager.ON_PLAYER_DATA_UPDATED);
            }
            catch (System.Exception) { }
            
        }

        private void parseLevelPackData(Dictionary<string, object> in_levelPacks, int in_iLevel)
        {
            string key = LEVEL_PREFIX + in_iLevel.ToString();
            if (in_levelPacks.ContainsKey(key))
            {
                LevelUpRewards reward = GetRewardAtLevel(in_iLevel);
                if (reward != null)
                {
                    Dictionary<string, object> rewardValues = (Dictionary<string, object>)in_levelPacks[key];
                    
                    /*
                    // TODO: make this generic!
                    reward.CurrencyRewards[BrainCloudUNETExample.GiftRewardData.AWARD_XP_BOOST] = HudHelper.GetLongValue(rewardValues, BrainCloudUNETExample.GiftRewardData.AWARD_XP_BOOST);
                    reward.CurrencyRewards[BrainCloudUNETExample.GiftRewardData.AWARD_XP_POINTS] = HudHelper.GetLongValue(rewardValues, BrainCloudUNETExample.GiftRewardData.AWARD_XP_POINTS);
                    */
                }
            }
        }
        #endregion

        #region Private Variables
        private int m_iCachedPlayerLevel = 0;

        private List<LevelUpRewards> m_rewards = new List<LevelUpRewards>();
        #endregion
    }
}