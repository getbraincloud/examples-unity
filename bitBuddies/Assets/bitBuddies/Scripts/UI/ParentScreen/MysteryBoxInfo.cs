using System;
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
