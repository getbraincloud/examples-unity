
using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum ItemType
{
    Freebie,
    Item,
    Bundle,
    Multiplier,
    Product
}
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
    public int TotalXP;
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
}
public class AuthResponse
{
    public int ExperienceLevel { get; set; }
    public string PlayerName { get; set; }
    public string PictureUrl { get; set; }
    public CurrencyData Currency { get; set; }
}

public class ItemSlot
{
    public string slotName;
    public string equippedItemId;
}

public class CurrencyData
{
    public int Coins { get; set; }
    public int Gems { get; set; }

}

public class CoinMultiplierStatus
{
    public long ActiveUntil { get; set; }
    public bool isActive { get; set; }
    public int multiplierAmount { get; set; }
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

[Serializable]
public class LocalImageOverride
{
    // Matches the catalog "image" value (filename, case-insensitive, extension optional)
    // for items whose art ships with the app instead of being hosted at a URL.
    public string key;
    public Sprite sprite;
}
