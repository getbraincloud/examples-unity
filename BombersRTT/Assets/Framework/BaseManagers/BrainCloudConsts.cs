
namespace Gameframework
{
    public class BrainCloudConsts
    {
        #region Public Events Consts
        public const string JSON_EVENT_CREATED_AT = "createdAt";
        public const string JSON_EVENT_EV_ID = "evId";
        public const string JSON_EVENT_EVENT_DATA = "eventData";
        public const string JSON_EVENT_EVENT_ID = "eventId";
        public const string JSON_EVENT_EVENT_TYPE = "eventType";
        public const string JSON_EVENT_FROM_PLAYER_ID = "fromPlayerId";
        public const string JSON_EVENT_INCOMING_EVENTS = "incoming_events";
        public const string JSON_EVENT_LEADERBOARD_ID = "leaderboardId";
        public const string JSON_EVENT_SYSTEM_TOURNAMENT_COMPLETE = "SYSTEM_TOURNAMENT_COMPLETE";
        public const string JSON_EVENT_TO_PLAYER_ID = "toPlayerId";
        public const string JSON_EVENT_VERSION_ID = "versionId";
        #endregion

        #region Public BC Status Consts
        public const string JSON_SERVER_TIME = "server_time";
        #endregion

        #region Public Data Consts
        public const string JSON_ACTIVITY = "activity";
        public const string JSON_CLAIM_REWARD_REWARD_DETAILS = "rewardDetails";
        public const string JSON_CLAIM_REWARD_TOURNAMENTS = "tournaments";
        public const string JSON_DATA = "data";
        public const string JSON_TRANSACTION_SUMMARY = "transactionSummary";
        public const string JSON_RESULT_CODE = "resultCode";
        public const string JSON_ERROR_MESSAGE = "errorMessage";
        public const string JSON_TRANSACTION_ID = "transactionId";
        public const string JSON_TRANS_ID = "transId";
        public const string JSON_ITEM_ID = "itemId";
        public const string JSON_LANGUAGE = "language";
        public const string JSON_VALUE = "value";
        public const string JSON_ENTITY_ENTITY_INDEXED_ID = "entityIndexedId";
        public const string JSON_ENTITY_ENTITY_TYPE = "entityType";
        public const string JSON_ENTITY_LIST = "entityList";
        public const string JSON_ENTRY_COUNT = "entryCount";
        public const string JSON_EXTERNAL_DATA = "externalData";
        public const string JSON_EXTERNAL_ID = "externalId";
        public const string JSON_FRIENDS = "friends";
        public const string JSON_FOUND_COUNT = "foundCount";
        public const string JSON_GLN_DATA = "GLNdata";
        public const string JSON_MATCHES = "matches";
        public const string JSON_MAX_RESULTS = "maxResults";
        public const string JSON_MORE_AFTER = "moreAfter";
        public const string JSON_MORE_BEFORE = "moreBefore";
        public const string JSON_ONLINE = "online";
        public const string JSON_PAYOUT_RULES = "payoutRules";
        public const string JSON_PAYOUT_RULES_RANK = "rank";
        public const string JSON_PAYOUT_RULES_RANK_ABS = "rankAbs";
        public const string JSON_PAYOUT_RULES_RANK_REMAINDER = "rankRemainder";
        public const string JSON_PAYOUT_RULES_RANK_TO_PERCENT = "rankToPercent";
        public const string JSON_PAYOUT_RULES_RANK_UP_TO = "rankUpTo";
        public const string JSON_PAYOUT_RULES_REWARD = "reward";
        public const string JSON_RESPONSE = "response";
        public const string JSON_SEARCH_TEXT = "searchText";
        public const string JSON_SUMMARY_FRIEND_DATA = "summaryFriendData";
        public const string JSON_STATISTICS = "statistics";
        public const string JSON_TIME_BEFORE_RESET = "timeBeforeReset";
        public const string JSON_TOURNAMENT_CONFIGS = "tournamentConfigs";
        public const string JSON_TOURNAMENT_RANK = "tRank";
        public const string JSON_TOURNAMENT_REWARDS = "rewards";
        public const string JSON_TOURNAMENT_REWARDS_CLAIMED = "tClaimedAt";
        public const string JSON_USER = "user";
        public const string JSON_USER_NAME = "userName";
        public const string VIRTUAL_CURRENCY_BALANCE = "balance";
        public const string JSON_LEADERBOARD = "leaderboard";
        public const string JSON_LEADERBOARD_ID = "leaderboardId";
        public const string JSON_LEADERBOARDS_IDS = "leaderboardIds";
        public const string JSON_LEADERBOARDS_TITLES = "leaderboardTitles";
        public const string JSON_CUSTOM_KEY = "custom";
        public const string JSON_FROM_KEY = "from";
        public const string JSON_CONTENT_KEY = "content";
        public const string JSON_MESSAGES_KEY = "messages";
        #endregion

        #region Public Player Consts
        public const string JSON_CURRENT_LEVEL = "currentLevel";
        public const string JSON_EXPERIENCE = "experience";
        public const string JSON_EXPERIENCE_POINTS = "experiencePoints";
        public const string JSON_ID = "id";
        public const string JSON_IDENTITIES = "identities";
        public const string JSON_IDENTITY_EMAIL = "Email";
        public const string JSON_IDENTITY_FACEBOOK = "Facebook";
        public const string JSON_IDENTITY_UNIVERSAL = "Universal";
        public const string JSON_IDENTITY_STEAM = "Steam";
        public const string JSON_IN_STRING = "in_string";
        public const string JSON_IS_TESTER = "isTester";
        public const string JSON_LEVEL = "level";
        public const string JSON_LOCATION = "LOCATION";
        public const string JSON_LOGIN_COUNT = "loginCount";
        public const string JSON_NAME = "name";
        public const string JSON_NEW_USER = "newUser";
        public const string JSON_NEXT_THRESHOLD = "nextThreshold";
        public const string JSON_PIC = "pic";
        public const string JSON_PLAYER_EMAIL = "emailAddress";
        public const string JSON_PLAYER_ID = "playerId";
        public const string JSON_PLAYER_NAME = "playerName";
        public const string JSON_PLAYER_RATING = "playerRating";
        public const string JSON_PLAYER_PICTURE_URL = "pictureUrl";
        public const string JSON_PRESENCE = "presence";
        public const string JSON_PREV_THRESHOLD = "prevThreshold";
        public const string JSON_SCORE = "score";
        public const string JSON_STATUS = "STATUS";
        public const string JSON_VC_PURCHASED = "vcPurchased";
        public const string JSON_XP_CAPPED = "xpCapped";
        public const string JSON_PARENT_PROFILE_ID = "parentProfileId";
        public const string JSON_PEER_PROFILE_ID = "peerProfileIds";
        public const string JSON_RANK = "rank";
        public const string JSON_INDEX = "index";
        public const string JSON_ENTRY_FEE = "entryFee";
        public const string JSON_PEER_REWARDS = "peerRewards";
        public const string JSON_PROFILE_ID = "profileId";
        public const string JSON_LAST_CONNECTION_ID = "lastConnectionId";
        public const string JSON_PROFILE_NAME = "profileName";
        public const string JSON_UPGRADE_APP_ID = "upgradeAppId";

        public const string JSON_LOBBY_ID = "LOBBY_ID";
        #endregion

        #region Public Currency Consts
        public const string JSON_CURRENCY = "currency";
        public const string JSON_CURRENCY_AMOUNT = "amount";
        public const string JSON_CURRENCY_MAP = "currencyMap";
        public const string JSON_CURRENCY_TYPE = "type";
        public const string JSON_PLAYER_CURRENCY = "playerCurrency"; // IAP Cash in receipt
        public const string JSON_AWARD_CURRENCY_TYPE = "awardType";
        public const string JSON_AWARD_CURRENCY_AMOUNT = "awardAmount";
        public const string JSON_CONSUME_CURRENCY_TYPE = "consumeType";
        public const string JSON_CONSUME_CURRENCY_AMOUNT = "consumeAmount";
        public const string JSON_PEER_CURRENCY = "peerCurrency";
        public const string JSON_PARENT_CURRENCY = "parentCurrency";

        #endregion
    }
}