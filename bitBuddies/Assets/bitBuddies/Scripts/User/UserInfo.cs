using System;
using UnityEngine;

[Serializable]
public struct UserInfo
{
    public int Level;
    public string Username;
    public int Coins;
    public int Gems;
    
    public void UpdateLevel(int in_level)
    {
        Level = in_level;
    }
    
    public void  UpdateUsername(string in_username)
    {
        Username = in_username;
    }
    
    public void UpdateCoins(int in_coins)
    {
        Coins = in_coins;
    }
    
    public void UpdateGems(int in_gems)
    {
        Gems = in_gems;
    }
}
