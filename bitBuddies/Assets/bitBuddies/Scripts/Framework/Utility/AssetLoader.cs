using Gameframework;
using UnityEngine;
using UnityEngine.U2D;

public static class AssetLoader
{
    private static SpriteAtlas BIT_BUDDY_SPRITES = null;

    public static Sprite LoadSprite(string path)
    {
        Sprite result = Resources.Load<Sprite>(path);

        if (result == null)
        {
            Debug.LogError($"Couldn't load sprite: {path}; Returning null.");
        }

        return result;
    }

    public static Sprite GetBuddySprite(BitBuddy buddy)
    {
        if (BIT_BUDDY_SPRITES == null)
        {
            BIT_BUDDY_SPRITES = Resources.Load<SpriteAtlas>("BitBuddySprites");
        }

        if (!BitBuddiesConsts.BUDDY_ENUM_TO_SPRITE_NAME.ContainsKey(buddy))
        {
            Debug.LogError($"Unknown BitBuddy Key: {buddy}; Using Default Sprite.");
            buddy = BitBuddy.Default;
        }

        return BIT_BUDDY_SPRITES.GetSprite(BitBuddiesConsts.BUDDY_ENUM_TO_SPRITE_NAME[buddy]);
    }

    public static Sprite GetCurrencySprite(CurrencyTypes in_currency)
    {
        return in_currency switch
        {
            CurrencyTypes.BuddyBling => Resources.Load<Sprite>(BitBuddiesConsts.BIT_BLING_SPRITE_PATH),
            CurrencyTypes.Gems => Resources.Load<Sprite>(BitBuddiesConsts.GEM_SPRITE_PATH),
            CurrencyTypes.FakeDollars => Resources.Load<Sprite>(BitBuddiesConsts.FAKE_MONEY_SPRITE_PATH),
            CurrencyTypes.Love => Resources.Load<Sprite>(BitBuddiesConsts.LOVE_SPRITE_PATH),
            CurrencyTypes.Level => Resources.Load<Sprite>(BitBuddiesConsts.LEVEL_SPRITE_PATH),
            _ => Resources.Load<Sprite>(BitBuddiesConsts.COIN_SPRITE_PATH),
        };
    }
}
