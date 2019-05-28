using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameframework
{
    public class LevelUpRewards
    {
        public LevelUpRewards(Dictionary<string, object> in_jsonData)
        {
            LevelUnlocked = (int)(in_jsonData[BrainCloudConsts.JSON_LEVEL]);
            Experience = (int)(in_jsonData[BrainCloudConsts.JSON_EXPERIENCE]);

            if (in_jsonData.ContainsKey(BrainCloudConsts.JSON_PAYOUT_RULES_REWARD))
            {
                Dictionary<string, object> rewards = (Dictionary<string, object>)in_jsonData[BrainCloudConsts.JSON_PAYOUT_RULES_REWARD];
                if (rewards.ContainsKey(BrainCloudConsts.JSON_CURRENCY))
                {
                    CurrencyRewards = (Dictionary<string, object>)rewards[BrainCloudConsts.JSON_CURRENCY];
                }
            }
        }

        public long GetCurrencyRewardValue(string in_currencyType)
        {
            long ToReturn = HudHelper.GetLongValue(CurrencyRewards, in_currencyType);
            return ToReturn;
        }

        public int LevelUnlocked { get; private set; }
        public int Experience { get; private set; }
        public Dictionary<string, object> CurrencyRewards = new Dictionary<string, object>();
    }
}