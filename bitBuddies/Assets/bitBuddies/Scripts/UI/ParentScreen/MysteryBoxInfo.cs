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
	public UnlockTypes UnlockType;
	public int UnlockAmount;
	public string Rarity;
	public Rarity RarityEnum;

}
