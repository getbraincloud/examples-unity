using System;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using Gameframework;
using UnityEngine;


[Serializable]
public class UserInfo
{
    public int Level;
    public int CurrentXP;
    public int PreviousLevelUp;
    public int NextLevelUp;
    public string Username;
    public string Email;
    public int Coins;
    public int Gems;
    
    public void UpdateLevel(int in_level)
    {
        Level = in_level;
    }
    
    public void UpdateXP(int in_xp)
    {
        if(in_xp == 0)
        {
            in_xp = 1;
        }
        CurrentXP = in_xp;
    }
    
    public void UpdateNextLevelUp(int in_nextLevel)
    {
        NextLevelUp = in_nextLevel;
    }
    
    public void UpdateUsername(string in_username)
    {
        Username = in_username;
    }
    
    public void UpdateEmail(string in_email)
    {
        Email = in_email;
    }
    
    public void UpdateCoins(int in_coins)
    {
        Coins = in_coins;
    }
    
    public void UpdateGems(int in_gems)
    {
        Gems = in_gems;
    }
    
    public void UpdateStats(Dictionary<string, object> in_jsonForStats)
    {
        StatTracker.Instance.ResetAllStats();
        foreach (KeyValuePair<string,object> stat in in_jsonForStats)
        {
            StatTracker.Instance.IncrementStat(stat.Key, (int) stat.Value);
        }
    }
}
