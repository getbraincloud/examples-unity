using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum CurrencyTypes
{
	Coins,
	Love,
	BuddyBling,
	Level
}

public enum Rarity
{
	starter,
	basic,
	rare,
	superRare,
	legendary
}

[Serializable]
public struct MysteryBoxInfo
{
	public string BoxName;
	[FormerlySerializedAs("UnlockType")] public CurrencyTypes currencyType;
	public int UnlockAmount;
	public string Rarity;
	public Rarity RarityEnum;

}

[Serializable]
public struct ToyBenchInfo
{
	public string BenchId;
	public int LevelRequirement;
	public int UnlockCost;
	public int Cooldown;
	public int CoinPayout;
	public int LovePayout;
	public int BuddyBlingPayout;
}

[Serializable]
public class AppChildrenInfo
{
	public string profileName { get; set; }
	public string profileId { get; set; }
	public Dictionary<string, object> summaryFriendData { get; set; }
	public int buddyBling { get; set; }
	public float coinMultiplier { get; set; }
	public int coinPerHour { get; set; }
	public int maxCoinCapacity {get; set;}
	public string buddySpritePath { get; set; }
	public string rarity { get; set; }
	public int buddyLevel { get; set; }
	public int nextLevelUp { get; set; }
	public int currentXP { get; set; }
	public DateTime lastIdleTimestamp { get; set; }
	public int coinsEarnedInHolding { get; set; }
	//Love is only earned through Toy interaction in Buddys Room.
	//Aka used as current XP for profile. 
	public int loveEarnedInLifetime {get; set;}
	public int coinsEarnedInLifetime {get; set;}
	
	private float _hourInSeconds = 3600;
	
	public List<string> ownedToys { get; set; }
	
	public void CheckCoinsEarned()
	{
		TimeSpan timeDifference = DateTime.UtcNow - lastIdleTimestamp;
		float coinsPerSecond = coinPerHour / _hourInSeconds;
		int coinsEarned = Mathf.FloorToInt(coinsPerSecond * (float)timeDifference.TotalSeconds);
		if(coinsEarned > 0)
		{
			if(coinsEarned < maxCoinCapacity)
			{
				coinsEarnedInHolding = coinsEarned;
			}
			else
			{
				coinsEarnedInHolding = maxCoinCapacity;
			}			
		}
	}
}