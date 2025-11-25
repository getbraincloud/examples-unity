
namespace Gameframework
{
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
        public const string INCREASE_XP_SCRIPT_NAME = "child/increaseChildBuddyExperience";
        public const string UPDATE_CHILD_COINS_COLLECTED_SCRIPT_NAME = "child/updateChildCoinCollected";
        public const string OBTAIN_TOY_SCRIPT_NAME = "child/obtainToy";
        public const string TOY_REWARD_RECEIVED_SCRIPT_NAME = "child/toyRewardsReceived";
        public const string GET_CHILD_ITEM_CATALOG_SCRIPT_NAME = "child/getChildItemCatalog";


        //Stat Names
        public const string PLAYER_STAT_LEVEL_NAME = "Level";

        public const string APP_CHILD_ID = "50974";

        //Player Prefs Keys
        public const string VOLUME_SLIDER_KEY = "volume"; 
        
        //Default sprite path for buddies
        public const string DEFAULT_SPRITE_PATH_FOR_BUDDY = "Assets/Resources/BuddySprites/buddy-1.png";
        
        //Pop Up messages
            //Buddys Room
        public const string GO_BUDDYS_ROOM_TITLE = "Enter Buddy's Room?";
        public const string GO_BUDDYS_ROOM_MESSAGE = "Would you like to enter buddy's room?";
        public const string DELETE_BUDDYS_ROOM_TITLE = "Delete Buddy's Room?";
        public const string DELETE_BUDDYS_ROOM_MESSAGE =  "Would you like to delete buddy's room?";
	
        public const string DELETE_BUDDYS_ROOM_SUCCESS_TITLE = "Buddys Room Deleted";
        public const string DELETE_BUDDYS_ROOM_SUCCESS_MESSAGE = "The requested buddy's room was deleted";
        public const string DELETE_BUDDYS_ROOM_FAILED_TITLE = "Something went wrong";
        public const string DELETE_BUDDYES_ROOM_FAILED_MESSAGE = "There was an error while attempting to delete the requested buddy's room, please try again later";
        
            //Settings
        public const string ATTACH_EMAIL_SUCCESS_TITLE = "Attach Email Successful";
        public const string ATTACH_EMAIL_SUCCESS_MESSAGE = "Email address entered is now attached to this account.";
        
        public const string ATTACH_EMAIL_FAILURE_TITLE = "Attach Email Failed";
        public const string ATTACH_EMAIL_FAILURE_MESSAGE = "Email address entered is failed to attach to this account.";

        public const string ARE_YOU_SURE_LOGOUT_TITLE = "Are you sure ?";
        public const string ARE_YOU_SURE_LOGOUT_MESSAGE = "Are you sure you want to logout?";


    }
}