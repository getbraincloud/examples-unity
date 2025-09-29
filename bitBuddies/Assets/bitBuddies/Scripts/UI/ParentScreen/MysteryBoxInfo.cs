using System;
using UnityEngine;
using UnityEngine.UI;

public enum UnlockTypes
{
	Coins,
	Love,
	Level
}

public enum Rarity
{
	Basic,
	Rare,
	SuperRare,
	Legendary
}

[Serializable]
public struct MysteryBoxInfo
{
	public string BoxName;
	public UnlockTypes UnlockType;
	public int UnlockAmount;
	public string Rarity;
	public Rarity RarityEnum;

}
