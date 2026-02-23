
using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum CurrencyType
{
    None = 0,
    Coins,
    Gems,
    Stars,
    Cash
}
public class UserData
{
    public int Coins;
    public int Gems;
    public int CurrentXP;
    public int Level;
    public int XPToNextLevel;
    public bool XPCapped;
    public string LevelStatusName;
    public string PlayerName;
    public string PictureUrl;

    public void UpdateFromAuth(AuthResponse response)
    {
        Coins = response.Currency.Coins;
        Gems = response.Currency.Gems;
        Level = response.ExperienceLevel;
        PlayerName = response.PlayerName;
        PictureUrl = response.PictureUrl;
    }

    public void UpdateFromGamification(GamificationResponse response)
    {
        XPCapped = response.XPCapped;
        CurrentXP = response.CurrentXP;
        LevelStatusName = response.LevelStatusName;
    }

    public void UpdateXPToNextLevel(int XPToNextLevel)
    {
        this.XPToNextLevel = XPToNextLevel;
    }
}
public class AuthResponse
{
    public int ExperienceLevel { get; set; }
    public string PlayerName { get; set; }
    public string PictureUrl { get; set; }
    public CurrencyData Currency { get; set; }
}


public class CurrencyData
{
    public int Coins { get; set; }
    public int Gems { get; set; }

}

public class GamificationResponse
{
    public bool XPCapped { get; set; }
    public string LevelStatusName { get; set; }
    public int CurrentXP { get; set; }
}

[Serializable]
public class GetUserItemsContext
{
    public GetUserItemsPagination pagination;
    public GetUserItemsSearchCriteria searchCriteria;
    public GetUserItemsSortCriteria sortCriteria;
}

[Serializable]
public class GetUserItemsPagination
{
    public int rowsPerPage;
    public int pageNumber;
}

[Serializable]
public class GetUserItemsSearchCriteria
{
    public string defId;
}

public class GetUserItemsSortCriteria
{
    public int createdAt;
    public int updatedAt;
}

[Serializable]
public class CurrencySprite
{
    public CurrencyType type;
    public Sprite sprite;
}


[Serializable]
public class ItemSectionSprite
{
    public string sectionName;
    public Sprite sprite;
}
