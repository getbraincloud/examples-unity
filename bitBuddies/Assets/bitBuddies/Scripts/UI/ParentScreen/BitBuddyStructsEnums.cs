using System;
using System.Collections.Generic;
using UnityEngine;

public enum CurrencyTypes
{
    Coins,
    Love,
    BuddyBling,
    Level,
    Gems,
    FakeDollars
}

public enum Rarity
{
    starter,
    basic,
    rare,
    superRare,
    legendary
}

public enum QuestTypes
{
    BitBuddies,
    BitBling,
    General
}

[Serializable]
public struct MysteryBoxInfo
{
    public string BoxName;
    public CurrencyTypes currencyType;
    public int UnlockAmount;
    public int LevelRequirement;
    public string Rarity;
    public Rarity RarityEnum;

}

//Keeping the currency kinda hidden behind ambiguous fields for the rewards to avoid hacking
[Serializable]
public struct RewardInfo
{
    public string entityId;
    public float z;	//AmountOfLoveToReward
    public int x;	//AmountOfCoinsToReward
    public int y;	//AmountOfBuddyBlingToReward
}

[Serializable]
public struct ToyBenchInfo
{
    public string BenchId;
    public string DisplayName;
    public int LevelRequirement;
    public int UnlockCost;
    public int Cooldown;
    public int CoinRewardAmount;
    public int LoveRewardAmount;
    public int BuddyBlingRewardAmount;
    public int CoinSpawnAmount;
    public int LoveSpawnAmount;
    public int BuddyBlingSpawnAmount;
}

[Serializable]
public struct ShopInfo
{
    public string ShopId;
    public string DisplayName;
    public string ItemDescription;
    public int BuyCost;
    public int RewardAmount;
    public CurrencyTypes BuyCurrency;
    public CurrencyTypes RewardCurrencyType;
    public Sprite ItemIcon;
}

[Serializable]
public struct ChildAchievementInfo
{
    public string AchievementId;
    public string DisplayName;
    public string Status;
    public int LevelRequirement;
}

[Serializable]
public class AppChildrenInfo
{
    public string profileName { get; set; }
    public string profileId { get; set; }
    public int buddyBling { get; set; }
    public float coinMultiplier { get; set; }
    public int coinPerHour { get; set; }
    public int maxCoinCapacity { get; set; }
    public string buddySpritePath { get; set; }
    public Rarity rarity { get; set; }
    public int buddyLevel { get; set; }
    public int currentXP { get; set; }
    public DateTime lastIdleTimestamp { get; set; }
    public int coinsEarnedInHolding { get; set; }
    //Love is only earned through Toy interaction in Buddys Room.
    //Aka used as current XP for profile. 
    public int coinsEarnedInLifetime { get; set; }

    private float _hourInSeconds = 3600;

    public List<string> ownedToys { get; set; } = new List<string>();
    public List<string> ownedShopItems { get; set; }
    public List<ChildAchievementInfo> childAchievements { get; set; }
    public long dailyCooldownUntil { get; set; }
    public long dailyBoosterExpiryUntil { get; set; }
    public float loveMultiplier { get; set; }

    public static int MinLevel { get; private set; } = int.MaxValue;
    public static int MaxLevel { get; private set; } = int.MinValue;
    public static Dictionary<int, int> LevelUpInfo { get; private set; } = new();

    public void CheckCoinsEarned()
    {
        TimeSpan timeDifference = DateTime.UtcNow - lastIdleTimestamp;
        float coinsPerSecond = coinPerHour / _hourInSeconds;
        int coinsEarned = Mathf.FloorToInt(coinsPerSecond * (float)timeDifference.TotalSeconds);
        if (coinsEarned > 0)
        {
            if (coinsEarned < maxCoinCapacity)
            {
                coinsEarnedInHolding = coinsEarned;
            }
            else
            {
                coinsEarnedInHolding = maxCoinCapacity;
            }
        }
    }

    public Sprite GetBuddySprite()
    {
        return Resources.Load<Sprite>(buddySpritePath);
    }

    public void UpdateLoveBoosterInfo(int in_loveMultiplier, long in_expiryTime, long in_dailyCooldownTime)
    {
        loveMultiplier = in_loveMultiplier;
        dailyBoosterExpiryUntil = in_expiryTime;
        dailyCooldownUntil = in_dailyCooldownTime;
    }

    public void UpdateCoinBoosterInfo(int in_coinMultiplier, long in_expiryTime, long in_dailyCooldownTime)
    {
        coinMultiplier = in_coinMultiplier;
    }

    public static void UpdateLevelUpInfo(Dictionary<string, object>[] xplevels)
    {
        LevelUpInfo.Clear();

        foreach (Dictionary<string, object> levelData in xplevels)
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
        return buddyLevel < MinLevel ? 0 : LevelUpInfo[buddyLevel];
    }

    public int GetNextLevelExperience()
    {
        int next = buddyLevel + 1;
        return next > MaxLevel ? 0 : LevelUpInfo[next];
    }
}
