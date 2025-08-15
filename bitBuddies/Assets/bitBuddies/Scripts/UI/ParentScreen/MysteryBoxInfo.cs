using System;
using UnityEngine;
using UnityEngine.UI;

public enum UnlockTypes
{
	Coins,
	Love,
	Level
}

[Serializable]
public struct MysteryBoxInfo
{
	public string BoxName;
	public UnlockTypes UnlockType;
	public int UnlockAmount;
	public Rarity Rarity;

}
