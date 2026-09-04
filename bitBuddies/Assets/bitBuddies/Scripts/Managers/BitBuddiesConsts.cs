using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Gameframework
{
    public enum QUEST_STATUS { LOCKED, UNLOCKED, IN_PROGRESS, SATISFIED }

    public class BitBuddiesConsts
    {
        #region SpriteAtlas Buddy Configs

        public static readonly Dictionary<string, BitBuddy> BUDDY_TYPE_TO_ENUM = new()
        {
            { "BunBun", BitBuddy.BitBunny_BunBun }, { "BunBunWhite", BitBuddy.BitBunny_BunBunWhite }, { "PinkE",      BitBuddy.BitBunny_PinkE      },
            { "Grizz",  BitBuddy.BitBear_Grizz   }, { "GrizzBlack",  BitBuddy.BitBear_GrizzBlack   }, { "GrizzWhite", BitBuddy.BitBear_GrizzWhite  },
            { "TabE",   BitBuddy.BitCat_TabE     }, { "Tux",         BitBuddy.BitCat_Tux           }, { "Snowball",   BitBuddy.BitCat_Snowball     },
            { "Eli",    BitBuddy.BitElephant_Eli }, { "EliWhite",    BitBuddy.BitElephant_EliWhite }, { "EliPink",    BitBuddy.BitElephant_EliPink },
            { "PandE",  BitBuddy.BitPanda_PandE  }, { "PandEYellow", BitBuddy.BitPanda_PandEYellow }, { "PandERed",   BitBuddy.BitPanda_PandERed   }
        };

        public static readonly Dictionary<BitBuddy, string> BUDDY_ENUM_TO_SPRITE_NAME = new()
        {
            { BitBuddy.BitBunny_BunBun, "BitBuddy_BunBun_Large_Front" }, { BitBuddy.BitBunny_BunBunWhite, "BitBuddy_BunBunWhite_Large_Front" }, { BitBuddy.BitBunny_PinkE,      "BitBuddy_PinkE_Large_Front"      },
            { BitBuddy.BitBear_Grizz,   "BitBuddy_Grizz_Large_Front"  }, { BitBuddy.BitBear_GrizzBlack,   "BitBuddy_GrizzBlack_Large_Front"  }, { BitBuddy.BitBear_GrizzWhite,  "BitBuddy_GrizzWhite_Large_Front" },
            { BitBuddy.BitCat_TabE,     "BitBuddy_TabE_Large_Front"   }, { BitBuddy.BitCat_Tux,           "BitBuddy_Tux_Large_Front"         }, { BitBuddy.BitCat_Snowball,     "BitBuddy_Snowball_Large_Front"   },
            { BitBuddy.BitElephant_Eli, "BitBuddy_Eli_Large_Front"    }, { BitBuddy.BitElephant_EliWhite, "BitBuddy_EliWhite_Large_Front"    }, { BitBuddy.BitElephant_EliPink, "BitBuddy_EliPink_Large_Front"    },
            { BitBuddy.BitPanda_PandE,  "BitBuddy_PandE_Large_Front"  }, { BitBuddy.BitPanda_PandEYellow, "BitBuddy_PandEYellow_Large_Front" }, { BitBuddy.BitPanda_PandERed,   "BitBuddy_PandERed_Large_Front"   }
        };

        public static readonly Dictionary<BitBuddy, string> BUDDY_ENUM_TO_HOUSE_IMAGE_NAME = new()
        {
            { BitBuddy.BitBunny_BunBun, "BunBun_House" }, { BitBuddy.BitBunny_BunBunWhite, "BunBunWhite_House" }, { BitBuddy.BitBunny_PinkE,      "PinkE_House"      },
            { BitBuddy.BitBear_Grizz,   "Grizz_House"  }, { BitBuddy.BitBear_GrizzBlack,   "GrizzBlack_House"  }, { BitBuddy.BitBear_GrizzWhite,  "GrizzWhite_House" },
            { BitBuddy.BitCat_TabE,     "TabE_House"   }, { BitBuddy.BitCat_Tux,           "Tux_House"         }, { BitBuddy.BitCat_Snowball,     "Snowball_House"   },
            { BitBuddy.BitElephant_Eli, "Eli_House"    }, { BitBuddy.BitElephant_EliWhite, "EliWhite_House"    }, { BitBuddy.BitElephant_EliPink, "EliPink_House"    },
            { BitBuddy.BitPanda_PandE,  "PandE_House"  }, { BitBuddy.BitPanda_PandEYellow, "PandEYellow_House" }, { BitBuddy.BitPanda_PandERed,   "PandERed_House"   }
        };

        public static readonly Dictionary<BitBuddy, string> BUDDY_ENUM_TO_NAMESIGN_IMAGE_NAME = new()
        {
            { BitBuddy.BitBunny_BunBun, "BunBun_NameSign" }, { BitBuddy.BitBunny_BunBunWhite, "BunBunWhite_NameSign" }, { BitBuddy.BitBunny_PinkE,      "PinkE_NameSign"      },
            { BitBuddy.BitBear_Grizz,   "Grizz_NameSign"  }, { BitBuddy.BitBear_GrizzBlack,   "GrizzBlack_NameSign"  }, { BitBuddy.BitBear_GrizzWhite,  "GrizzWhite_NameSign" },
            { BitBuddy.BitCat_TabE,     "TabE_NameSign"   }, { BitBuddy.BitCat_Tux,           "Tux_NameSign"         }, { BitBuddy.BitCat_Snowball,     "Snowball_NameSign"   },
            { BitBuddy.BitElephant_Eli, "Eli_NameSign"    }, { BitBuddy.BitElephant_EliWhite, "EliWhite_NameSign"    }, { BitBuddy.BitElephant_EliPink, "EliPink_NameSign"    },
            { BitBuddy.BitPanda_PandE,  "PandE_NameSign"  }, { BitBuddy.BitPanda_PandEYellow, "PandEYellow_NameSign" }, { BitBuddy.BitPanda_PandERed,   "PandERed_NameSign"   }
        };

        public static readonly Dictionary<BitBuddy, string> BUDDY_ENUM_TO_BACKGROUND_IMAGE_NAME = new()
        {
            { BitBuddy.BitBunny_BunBun, "BunBun_Background" }, { BitBuddy.BitBunny_BunBunWhite, "BunBunWhite_Background" }, { BitBuddy.BitBunny_PinkE,      "PinkE_Background"      },
            { BitBuddy.BitBear_Grizz,   "Grizz_Background"  }, { BitBuddy.BitBear_GrizzBlack,   "GrizzBlack_Background"  }, { BitBuddy.BitBear_GrizzWhite,  "GrizzWhite_Background" },
            { BitBuddy.BitCat_TabE,     "TabE_Background"   }, { BitBuddy.BitCat_Tux,           "Tux_Background"         }, { BitBuddy.BitCat_Snowball,     "Snowball_Background"   },
            { BitBuddy.BitElephant_Eli, "Eli_Background"    }, { BitBuddy.BitElephant_EliWhite, "EliWhite_Background"    }, { BitBuddy.BitElephant_EliPink, "EliPink_Background"    },
            { BitBuddy.BitPanda_PandE,  "PandE_Background"  }, { BitBuddy.BitPanda_PandEYellow, "PandEYellow_Background" }, { BitBuddy.BitPanda_PandERed,   "PandERed_Background"   }
        };

        #endregion

        // ???
        public const string JSON_DATA = "data";
        public const string JSON_ENTITY_LIST = "entityList";

        // Scene Names
        public const string LOGIN_SCENE_NAME = "LoginScreen";
        public const string GAME_SCENE_NAME = "BuddysRoom";
        public const string PARENT_SCENE_NAME = "ParentMenu";
        public const string LOADING_SCREEN_SCENE_NAME = "LoadingScreen";

        // Script Names
        public const string CONSUME_PARENT_COINS_SCRIPT_NAME = "ConsumeCoinsForUser";
        public const string AWARD_COINS_SCRIPT_NAME = "AwardCoinsToUser";
        public const string AWARD_GEMS_SCRIPT_NAME = "AwardGemsToUser";
        public const string AWARD_BLING_TO_CHILD_SCRIPT_NAME = "AwardBlingToChild";
        public const string GET_QUEST_INFO_SCRIPT_NAME = "GetQuestInfo";
        public const string CLAIM_QUEST_SCRIPT_NAME = "ClaimQuestReward";
        public const string GET_SHOP_CATALOG_SCRIPT_NAME = "GetParentShopCatalog";
        public const string AWARD_MONEY_SCRIPT_NAME = "AwardMoneyToUser";
        public const string CLAIM_ITEM_SCRIPT_NAME = "ClaimParentShopItem";

        // Child Scripts
        public const string GET_STATS_SCRIPT_NAME = "child/fetchStats";
        public const string GET_CURRENCIES_SCRIPT_NAME = "child/fetchCurrencies";
        public const string GET_CHILD_ACCOUNTS_SCRIPT_NAME = "child/getChildProfiles";
        public const string ADD_CHILD_ACCOUNT_SCRIPT_NAME = "child/addChildAccount";
        public const string AWARD_RANDOM_LOOTBOX_SCRIPT_NAME = "child/addRandomChildAccount";
        public const string AWARD_STARTER_BUDDY_SCRIPT_NAME = "child/lootboxes/addStarterChildAccount";
        public const string AWARD_BASIC_LOOTBOX_SCRIPT_NAME = "child/lootboxes/addBasicChildAccount";
        public const string AWARD_UNCOMMON_LOOTBOX_SCRIPT_NAME = "child/lootboxes/addUncommonChildAccount";
        public const string AWARD_RARE_LOOTBOX_SCRIPT_NAME = "child/lootboxes/addRareChildAccount";
        public const string AWARD_LEGENDARY_LOOTBOX_SCRIPT_NAME = "child/lootboxes/addLegendaryChildAccount";
        public const string AWARD_MYTHIC_LOOTBOX_SCRIPT_NAME = "child/lootboxes/addMythicChildAccount";
        public const string UPDATE_CHILD_PROFILE_NAME_SCRIPT_NAME = "child/updateChildAccountName";
        public const string DELETE_CHILD_PROFILE_SCRIPT_NAME = "child/deleteChildProfile";
        public const string INCREASE_XP_FOR_CHILD_SCRIPT_NAME = "child/increaseChildBuddyExperience";
        public const string UPDATE_CHILD_COINS_COLLECTED_SCRIPT_NAME = "child/updateChildCoinCollected";
        public const string OBTAIN_TOY_SCRIPT_NAME = "child/obtainToy";
        public const string TOY_REWARD_RECEIVED_SCRIPT_NAME = "child/consumeCurrencyFromToy";
        public const string GET_CHILD_ITEM_CATALOG_SCRIPT_NAME = "child/getChildItemCatalog";
        public const string CONSUME_TOY_SCRIPT_NAME = "child/consumeToy";
        public const string CLAIM_CHILD_ITEM_SCRIPT_NAME = "child/claimMouseMerchantItem";
        public const string CLAIM_LOVE_BOOSTER_SCRIPT_NAME = "child/claimLoveBooster";

        public const string JSON_DAILY_LOVE_BOOSTER_ITEM = "dailyLoveBooster";

        // Quest Line Names
        public const string BITBUDDIES_QUESTLINEID = "bitBuddiesQuestTier";
        public const string GENERAL_QUESTLINEID = "generalQuestTier";
        public const string BITBLING_QUESTLINEID = "bitBlingQuestTier";

        // Stat Names
        public const string PLAYER_STAT_LEVEL_NAME = "Level";
        public const string TRASHED_BUDDIES_STAT_NAME = "trashBuddies";
        public const string BUDDIES_OWNED_STAT_NAME = "bitBuddiesOwned";
        public const string BUDDIES_LEVELED_UP_STAT_NAME = "buddiesLeveledUp";
        public const string BOUGHT_LEVELUP_PROMOS_STAT_NAME = "boughtLevelUpPromo";
        public const string BOUGHT_COINS_WITH_GEMS_STAT_NAME = "boughtCoinsWithGems";
        public const string BOUGHT_GEMS_WITH_COINS_STAT_NAME = "boughtGemsWithCash";
        public const string HATS_BOUGHT_STAT_NAME = "hatsBought";
        public const string SUNGLASSES_BOUGHT_STAT_NAME = "sunglassesBought";
        public const string CHAINS_BOUGHT_STAT_NAME = "chainNecklacesBought";
        public const string USER_NAME_CHANGED_STAT_NAME = "userChangedName";
        public const string LEVEL5_BUDDIES_STAT_NAME = "level5Buddies";
        public const string VISIT_BUDDIES_STAT_NAME = "visitBitBuddy";
        public const string TOYS_BOUGHT_STAT_NAME = "toysBought";
        public const string SCIENCE_KITS_BOUGHT_STAT_NAME = "scienceKitObtained";
        public const string LOGIN_COUNT_STAT_NAME = "loginCount";

        public const string APP_CHILD_ID = "50974";

        // Player Prefs Keys
        public const string VOLUME_SLIDER_KEY = "volume";

        // Sprite Paths
        public const string DEFAULT_SPRITE_PATH_FOR_BUDDY = "BuddySprites/buddy-1";
        public const string GEM_SPRITE_PATH = "RewardIcons/IconGem";
        public const string COIN_SPRITE_PATH = "RewardIcons/IconCoin_Gold";
        public const string LOVE_SPRITE_PATH = "RewardIcons/IconHeart";
        public const string BIT_BLING_SPRITE_PATH = "RewardIcons/BuddyBling";
        public const string FAKE_MONEY_SPRITE_PATH = "RewardIcons/FakeMoneyStack";
        public const string LEVEL_SPRITE_PATH = "RewardIcons/IconStar";
        public const string DISABLE_BUTTON_SPRITE_PATH = "Buttons/ButtonWide1_Disabled";
        public const string ENABLE_BUTTON_SPRITE_PATH = "Buttons/ButtonWide1_Green";

        // Pop Up Messages
        // Buddys Room
        public const string GO_BUDDYS_ROOM_TITLE = "Enter ";
        public const string GO_BUDDYS_ROOM_MESSAGE = "Would you like to enter ";
        public const string DELETE_BUDDYS_ROOM_TITLE = "Delete ";
        public const string DELETE_BUDDYS_ROOM_MESSAGE = "Would you like to demolish ";

        public const string DELETE_BUDDYS_ROOM_FAILED_TITLE = "Something went wrong";
        public const string DELETE_BUDDYES_ROOM_FAILED_MESSAGE = "There was an error while attempting to delete the requested buddy's room, please try again later";
        public const string DEFAULT_BUDDY_NAME = "MyBuddy";

        // Settings
        public const string ATTACH_EMAIL_SUCCESS_TITLE = "Attach Email Successful";
        public const string ATTACH_EMAIL_SUCCESS_MESSAGE = "Email address entered is now attached to this account.";

        public const string ATTACH_EMAIL_FAILURE_TITLE = "Attach Email Failed";
        public const string ATTACH_EMAIL_FAILURE_MESSAGE = "Email address entered is failed to attach to this account.";

        public const string ARE_YOU_SURE_LOGOUT_TITLE = "Are you sure?";
        public const string ARE_YOU_SURE_LOGOUT_MESSAGE = "Are you sure you want to logout?";

        public const string CONSUME_TOY_FAILED_TITLE = "Cannot consume toy";
        public const string CONSUME_TOY_FAILED_MESSAGE = "Something went wrong with the toy consumption.";

        public const string SOMETHING_WENT_WRONG_TITLE = "Something went wrong";
        public const string SOMETHING_WENT_WRONG_MESSAGE = "Something went wrong, please try again later.";

        // Parent Screen
        public const string CANT_DELETE_BUDDY_TITLE = "Can't Delete Buddy";
        public const string CANT_DELETE_BUDDY_MESSAGE = "You can't have zero buddies, try again later when you have more than 1 buddy.";

        // Screen Titles
        public const string LIST_BOXES_TEXT_TITLE = "Pick a mystery box";
        public const string OPEN_BOX_TEXT_TITLE = "Open your Mystery Box";
        public const string NEW_BUDDY_TEXT_TITLE = "New bitBuddy!";

        // Buddy Info
        public const string COIN_PAYOUT_TEXT = "Coin Payouts ";
        public const string COIN_GAIN_TEXT = "Idle Coin Gains ";
        public const string COIN_PER_HOUR_TEXT = "/hr";
        public const string COIN_CAPACITY_TEXT = "Idle Coins Capacity ";
    }
}
