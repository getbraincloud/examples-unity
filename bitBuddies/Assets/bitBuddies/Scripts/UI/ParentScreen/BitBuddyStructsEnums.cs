using Gameframework;
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
    Starter,
    Basic,
    Uncommon,
    Rare,
    Legendary,
    Mythic
}

public enum QuestTypes
{
    BitBuddies,
    BitBling,
    General
}

public enum BitBuddy
{
    BitBunny_BunBun,
    BitBunny_BunBunWhite,
    BitBunny_PinkE,
    BitBear_Grizz,
    BitBear_GrizzBlack,
    BitBear_GrizzWhite,
    BitCat_TabE,
    BitCat_Tux,
    BitCat_Snowball,
    BitElephant_Eli,
    BitElephant_EliWhite,
    BitElephant_EliPink,
    BitPanda_PandE,
    BitPanda_PandEYellow,
    BitPanda_PandERed,
    Default = BitBunny_BunBun
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
    public string buddyName { get; set; }
    public int buddyBling { get; set; }
    public float coinMultiplier { get; set; }
    public float loveMultiplier { get; set; }
    public float blingMultiplier { get; set; }
    public int coinPerHour { get; set; }
    public int maxCoinCapacity { get; set; }
    public string buddyType { get; set; }
    public Rarity rarity { get; set; }
    public int buddyLevel { get; set; }
    public int currentXP { get; set; }
    public DateTime lastIdleTimestamp { get; set; }
    public int coinsEarnedInLifetime { get; set; }
    public List<string> ownedToys { get; set; } = new List<string>();
    public List<string> ownedShopItems { get; set; }
    public List<ChildAchievementInfo> childAchievements { get; set; }
    public long dailyCooldownUntil { get; set; }
    public long dailyBoosterExpiryUntil { get; set; }
    public float dailyBoosterMultiplier { get; set; }

    public static int MinLevel { get; private set; } = int.MaxValue;
    public static int MaxLevel { get; private set; } = int.MinValue;
    public static Dictionary<int, int> LevelUpInfo { get; private set; } = new();

    public int GetCoinsEarned()
    {
        const float HOURS_IN_SECONDS = 3600.0f;
        const float MS_IN_SECONDS = 1000.0f;

        float timeDifference = (float)(DateTime.UtcNow - lastIdleTimestamp).TotalMilliseconds / MS_IN_SECONDS;
        float coinsPerSecond = coinPerHour / HOURS_IN_SECONDS;
        int coinsEarned = Mathf.FloorToInt(coinsPerSecond * timeDifference);

        return coinsEarned > maxCoinCapacity ? maxCoinCapacity
                                             : coinsEarned > 0 ? coinsEarned : 0;
    }

    public BitBuddy GetBuddyEnum()
    {
        BitBuddy buddy = BitBuddy.Default;
        if (BitBuddiesConsts.BUDDY_TYPE_TO_ENUM.ContainsKey(buddyType))
        {
            buddy = BitBuddiesConsts.BUDDY_TYPE_TO_ENUM[buddyType];
        }
        else
        {
            Debug.LogError($"Unknown BitBuddy Key: {buddyType}; Using Default Sprite.");
        }

        return buddy;
    }

    public Sprite GetBuddySprite()
    {
        return AssetLoader.GetBuddySprite(GetBuddyEnum());
    }

    public void UpdateLoveBoosterInfo(int loveMultiplier, long expiryTime, long cooldownTime)
    {
        dailyBoosterMultiplier = loveMultiplier;
        dailyBoosterExpiryUntil = expiryTime;
        dailyCooldownUntil = cooldownTime;
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

    public static bool HasLevelUpInfo => LevelUpInfo.Count > 0;

    public int GetPreviousLevelExperience()
    {
        if (!HasLevelUpInfo || buddyLevel < MinLevel)
        {
            return 0;
        }

        return LevelUpInfo.TryGetValue(buddyLevel, out int experience) ? experience : 0;
    }

    public int GetNextLevelExperience()
    {
        if (!HasLevelUpInfo)
        {
            return -1;
        }

        int next = buddyLevel + 1;
        if (next > MaxLevel)
        {
            return 0;
        }

        return LevelUpInfo.TryGetValue(next, out int experience) ? experience : 0;
    }

    public void PredictLevelFromXP()
    {
        while (buddyLevel < MaxLevel &&
               LevelUpInfo.ContainsKey(buddyLevel + 1) &&
               currentXP >= LevelUpInfo[buddyLevel + 1])
        {
            buddyLevel++;
        }
    }
}
