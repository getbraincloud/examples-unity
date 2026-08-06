using System;
using System.Collections.Generic;

[Serializable]
public class UserInfo
{
    public int Level;
    public int CurrentXP;
    public string Username;
    public string Email;
    public int Coins;
    public int Gems;
    public int FakeMoney;

    public int MinLevel { get; private set; } = int.MaxValue;
    public int MaxLevel { get; private set; } = int.MinValue;
    public Dictionary<int, int> LevelUpInfo { get; private set; } = new();

    public void UpdateLevel(int in_level)
    {
        Level = in_level;
    }

    public void UpdateXP(int in_xp)
    {
        if (in_xp < 0)
        {
            in_xp = 0;
        }

        CurrentXP = in_xp;
    }

    public void UpdateLevelUpInfo(Dictionary<string, object>[] xp_levels)
    {
        LevelUpInfo.Clear();

        foreach (Dictionary<string, object> levelData in xp_levels)
        {
            if (levelData.ContainsKey("level") && levelData.ContainsKey("experience") &&
                levelData["level"] is int level && levelData["experience"] is int experience)
            {
                LevelUpInfo.Add(level, experience);
                MinLevel = level < MinLevel ? level : MinLevel;
                MaxLevel = level > MaxLevel ? level : MaxLevel;
            }
        }
    }

    public int GetPreviousLevelExperience()
    {
        return Level < MinLevel ? 0 : LevelUpInfo[Level];
    }

    public int GetNextLevelExperience()
    {
        int next = Level + 1;
        return next > MaxLevel ? 0 : LevelUpInfo[next];
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

    public void UpdateFakeMoney(int in_fakeMoney)
    {
        FakeMoney = in_fakeMoney;
    }

    public void UpdateStats(Dictionary<string, object> in_jsonForStats)
    {
        StatTracker.Instance.ResetAllStats();
        foreach (KeyValuePair<string, object> stat in in_jsonForStats)
        {
            StatTracker.Instance.IncrementStat(stat.Key, (int)stat.Value);
        }
    }
}
