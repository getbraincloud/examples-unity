
namespace Gameframework
{
    public class BrainCloudConsts
    {
        public const string JSON_DATA = "data";
        public const string JSON_ENTITY_LIST = "entityList";
        
        
        //Scene Names
        public const string LOGIN_SCENE_NAME = "LoginScreen";
        public const string GAME_SCENE_NAME =  "BuddysRoom";
        public const string PARENT_SCENE_NAME = "ParentMenu";
        public const string LOADING_SCREEN_SCENE_NAME = "LoadingScreen";
        
        //Script Names
        public const string GET_STATS_SCRIPT_NAME = "child/fetchStats";
        public const string GET_CURRENCIES_SCRIPT_NAME = "child/fetchCurrencies";
        public const string AWARD_COINS_SCRIPT_NAME = "AwardCoinsToUser";
        public const string AWARD_GEMS_SCRIPT_NAME = "AwardGemsToUser";
        public const string AWARD_BLING_TO_CHILD_SCRIPT_NAME = "AwardBlingToChild";
        public const string GET_CHILD_ACCOUNTS_SCRIPT_NAME = "child/getChildProfiles";
        public const string ADD_CHILD_ACCOUNT_SCRIPT_NAME = "child/addChildAccount";
        public const string AWARD_RANDOM_LOOTBOX_SCRIPT_NAME = "child/addRandomChildAccount";
        public const string UPDATE_CHILD_PROFILE_NAME_SCRIPT_NAME = "child/updateChildAccountName";
        public const string DELETE_CHILD_PROFILE_SCRIPT_NAME = "/child/deleteChildProfile";

        //Stat Names
        public const string PLAYER_STAT_LEVEL_NAME = "Level";

        public const string APP_CHILD_ID = "49162";
    }
}