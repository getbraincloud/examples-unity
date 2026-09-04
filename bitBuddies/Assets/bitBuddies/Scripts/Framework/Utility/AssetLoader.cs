using Gameframework;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public static class AssetLoader
{
    private static SpriteAtlas BITBUDDY_SPRITES = null;
    private static SpriteAtlas PARENTMENU_IMAGES = null;
    private static SpriteAtlas BUDDYSROOM_IMAGES = null;

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
        if (BITBUDDY_SPRITES == null)
        {
            BITBUDDY_SPRITES = Resources.Load<SpriteAtlas>("BitBuddySprites");
        }

        if (!BitBuddiesConsts.BUDDY_ENUM_TO_SPRITE_NAME.ContainsKey(buddy))
        {
            Debug.LogError($"Unknown BitBuddy Key: {buddy}; Using Default Sprite.");
            buddy = BitBuddy.Default;
        }

        return BITBUDDY_SPRITES.GetSprite(BitBuddiesConsts.BUDDY_ENUM_TO_SPRITE_NAME[buddy]);
    }

    public static void GetBuddyParentMenuSprites(BitBuddy buddy, ref Image houseImage, ref Image nameSignImage)
    {
        if (PARENTMENU_IMAGES == null)
        {
            PARENTMENU_IMAGES = Resources.Load<SpriteAtlas>("ParentMenu");
        }

        if (!BitBuddiesConsts.BUDDY_ENUM_TO_HOUSE_IMAGE_NAME.ContainsKey(buddy) ||
            !BitBuddiesConsts.BUDDY_ENUM_TO_NAMESIGN_IMAGE_NAME.ContainsKey(buddy))
        {
            Debug.LogError($"Unknown BitBuddy Key: {buddy}; Using Default Sprite.");
            buddy = BitBuddy.Default;
        }

        houseImage.sprite = PARENTMENU_IMAGES.GetSprite(BitBuddiesConsts.BUDDY_ENUM_TO_HOUSE_IMAGE_NAME[buddy]);
        nameSignImage.sprite = PARENTMENU_IMAGES.GetSprite(BitBuddiesConsts.BUDDY_ENUM_TO_NAMESIGN_IMAGE_NAME[buddy]);
    }

    public static void GetBuddyBackground(BitBuddy buddy, ref Image houseBackground)
    {
        if (BUDDYSROOM_IMAGES == null)
        {
            BUDDYSROOM_IMAGES = Resources.Load<SpriteAtlas>("BuddysRoom");
        }

        if (!BitBuddiesConsts.BUDDY_ENUM_TO_BACKGROUND_IMAGE_NAME.ContainsKey(buddy))
        {
            Debug.LogError($"Unknown BitBuddy Key: {buddy}; Using Default Sprite.");
            buddy = BitBuddy.Default;
        }

        houseBackground.sprite = BUDDYSROOM_IMAGES.GetSprite(BitBuddiesConsts.BUDDY_ENUM_TO_BACKGROUND_IMAGE_NAME[buddy]);
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
