
namespace Gameframework
{
    public enum QUEST_STATUS {LOCKED, UNLOCKED, IN_PROGRESS, SATISFIED}
    public class BitBuddiesConsts
    {
        public const string JSON_DATA = "data";
        public const string JSON_ENTITY_LIST = "entityList";
        
        //Scene Names
        public const string LOGIN_SCENE_NAME = "LoginScreen";
        public const string GAME_SCENE_NAME =  "BuddysRoom";
        public const string PARENT_SCENE_NAME = "ParentMenu";
        public const string LOADING_SCREEN_SCENE_NAME = "LoadingScreen";
        
        //Script Names
        public const string CONSUME_PARENT_COINS_SCRIPT_NAME = "ConsumeCoinsForUser";
        public const string AWARD_COINS_SCRIPT_NAME = "AwardCoinsToUser";
        public const string AWARD_GEMS_SCRIPT_NAME = "AwardGemsToUser";
        public const string AWARD_BLING_TO_CHILD_SCRIPT_NAME = "AwardBlingToChild";
        public const string INCREASE_XP_FOR_PARENT_SCRIPT_NAME = "IncreaseXPForParent";
        public const string GET_QUEST_INFO_SCRIPT_NAME = "GetQuestInfo";
        public const string CLAIM_QUEST_SCRIPT_NAME = "ClaimQuestReward";
        public const string GET_SHOP_CATALOG_SCRIPT_NAME = "GetParentShopCatalog";
        public const string AWARD_MONEY_SCRIPT_NAME = "AwardMoneyToUser";
        public const string CLAIM_ITEM_SCRIPT_NAME = "ClaimParentShopItem";


        
        public const string GET_STATS_SCRIPT_NAME = "child/fetchStats";
        public const string GET_CURRENCIES_SCRIPT_NAME = "child/fetchCurrencies";
        public const string GET_CHILD_ACCOUNTS_SCRIPT_NAME = "child/getChildProfiles";
        public const string ADD_CHILD_ACCOUNT_SCRIPT_NAME = "child/addChildAccount";
        public const string AWARD_RANDOM_LOOTBOX_SCRIPT_NAME = "child/addRandomChildAccount";
        public const string AWARD_STARTER_BUDDY_SCRIPT_NAME = "child/lootboxes/addStarterChildAccount";
        public const string AWARD_BASIC_LOOTBOX_SCRIPT_NAME = "child/lootboxes/addBasicChildAccount";
        public const string AWARD_RARE_LOOTBOX_SCRIPT_NAME = "child/lootboxes/addRareChildAccount";
        public const string AWARD_SUPER_RARE_LOOTBOX_SCRIPT_NAME = "child/lootboxes/addSuperRareChildAccount";
        public const string AWARD_LEGENDARY_LOOTBOX_SCRIPT_NAME = "child/lootboxes/addLegendaryChildAccount";
        public const string UPDATE_CHILD_PROFILE_NAME_SCRIPT_NAME = "child/updateChildAccountName";
        public const string DELETE_CHILD_PROFILE_SCRIPT_NAME = "child/deleteChildProfile";
        public const string INCREASE_XP_FOR_CHILD_SCRIPT_NAME = "child/increaseChildBuddyExperience";
        public const string UPDATE_CHILD_COINS_COLLECTED_SCRIPT_NAME = "child/updateChildCoinCollected";
        public const string OBTAIN_TOY_SCRIPT_NAME = "child/obtainToy";
        public const string TOY_REWARD_RECEIVED_SCRIPT_NAME = "child/consumeCurrencyFromToy";
        public const string GET_CHILD_ITEM_CATALOG_SCRIPT_NAME = "child/getChildItemCatalog";
        public const string CONSUME_TOY_SCRIPT_NAME = "child/consumeToy";

        //Quest Line names
        public const string BITBUDDIES_QUESTLINEID = "bitBuddiesQuestTier";
        public const string GENERAL_QUESTLINEID = "generalQuestTier";
        public const string BITBLING_QUESTLINEID = "bitBlingQuestTier";

        //Stat Names
        public const string PLAYER_STAT_LEVEL_NAME = "Level";
        public const string TRASHED_BUDDIES_STAT_NAME = "trashBuddies";
        public const string BUDDIES_OWNED_STAT_NAME = "bitBuddiesOwned";
        public const string BUDDIES_LEVELED_UP_STAT_NAME = "buddiesLeveledUp";
        public const string BOUGHT_LEVELUP_PROMOS_STAT_NAME = "boughtLevelUpPromo";
        public const string BOUGHT_COINS_WITH_GEMS_STAT_NAME = "boughtCoinsWithGems";
        public const string BOUGHT_GEMS_WITH_COINS_STAT_NAME = "boughtGemsWithCash";
        public const string USER_NAME_CHANGED_STAT_NAME = "userChangedName";
        public const string LEVEL5_BUDDIES_STAT_NAME = "level5Buddies";
        public const string VISIT_BUDDIES_STAT_NAME = "visitBitBuddy";
        public const string TOYS_BOUGHT_STAT_NAME = "toysBought";
        public const string SCIENCE_KITS_BOUGHT_STAT_NAME = "scienceKitBought";

        public const string APP_CHILD_ID = "50974";

        //Player Prefs Keys
        public const string VOLUME_SLIDER_KEY = "volume"; 
        
        //Sprite Paths
        public const string DEFAULT_SPRITE_PATH_FOR_BUDDY = "BuddySprites/buddy-1";
        public const string GEM_SPRITE_PATH = "RewardIcons/IconGem";
        public const string COIN_SPRITE_PATH = "RewardIcons/IconCoin_Gold";
        public const string LOVE_SPRITE_PATH = "RewardIcons/IconHeart";
        public const string BIT_BLING_SPRITE_PATH = "RewardIcons/BuddyBling";
        public const string FAKE_MONEY_SPRITE_PATH = "RewardIcons/FakeMoneyStack";
        
        //Pop Up messages
            //Buddys Room
        public const string GO_BUDDYS_ROOM_TITLE = "Enter ";
        public const string GO_BUDDYS_ROOM_MESSAGE = "Would you like to enter ";
        public const string DELETE_BUDDYS_ROOM_TITLE = "Delete ";
        public const string DELETE_BUDDYS_ROOM_MESSAGE =  "Would you like to demolish ";
	
        public const string DELETE_BUDDYS_ROOM_SUCCESS_TITLE = "Buddys Room Deleted";
        public const string DELETE_BUDDYS_ROOM_SUCCESS_MESSAGE = "The requested buddy's room was deleted";
        public const string DELETE_BUDDYS_ROOM_FAILED_TITLE = "Something went wrong";
        public const string DELETE_BUDDYES_ROOM_FAILED_MESSAGE = "There was an error while attempting to delete the requested buddy's room, please try again later";
        public const string DEFAULT_BUDDY_NAME = "MyBuddy";
        
            //Settings
        public const string ATTACH_EMAIL_SUCCESS_TITLE = "Attach Email Successful";
        public const string ATTACH_EMAIL_SUCCESS_MESSAGE = "Email address entered is now attached to this account.";
        
        public const string ATTACH_EMAIL_FAILURE_TITLE = "Attach Email Failed";
        public const string ATTACH_EMAIL_FAILURE_MESSAGE = "Email address entered is failed to attach to this account.";

        public const string ARE_YOU_SURE_LOGOUT_TITLE = "Are you sure ?";
        public const string ARE_YOU_SURE_LOGOUT_MESSAGE = "Are you sure you want to logout?";

        public const string CONSUME_TOY_FAILED_TITLE = "Cannot consume toy";
        public const string CONSUME_TOY_FAILED_MESSAGE = "Something went wrong with the toy consumption.";

        public const string SOMETHING_WENT_WRONG_TITLE = "Something went wrong";
        public const string SOMETHING_WENT_WRONG_MESSAGE = "Something went wrong, please try again later.";
        
            //Parent Screen
        public const string CANT_DELETE_BUDDY_TITLE = "Cant Delete Buddy";
        public const string CANT_DELETE_BUDDY_MESSAGE = "You cant have zero buddies, try again later when you have more than 1 buddy";
    }
}