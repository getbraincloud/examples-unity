using System.Collections.Generic;

namespace Gameframework
{
    public class PlayerData
    {
        public PlayerData()
        {
            PlayerName = "";
            ProfileId = "";
            PlayerEmail = "";
            PlayerPictureUrl = "";
            OpponentPlayerData = null;

            MatchMakingEnabled = false;
            IsTester = false;

            PlayerXPData = new XPData();
            Presence = new PresenceData();
        }

        #region Public Accessors
        public PlayerData OpponentPlayerData
        {
            get; private set;
        }

        public string PlayerName
        {
            get; set;
        }

        public string ProfileId
        {
            get; set;
        }

        public string FacebookUserId
        {
            get; set;
        }

        public string UniversalId
        {
            get; set;
        }

        public string SteamId
        {
            get; set;
        }

        public string PlayerEmail
        {
            get; set;
        }

        public int PlayerRating
        {
            get; set;
        }

        public int PlayerRank
        {
            get; set;
        }

        public int PlayerVCPurchased
        {
            get; set;
        }

        public bool MatchMakingEnabled
        {
            get; set;
        }

        public bool IsTester
        {
            get; set;
        }

        public string PlayerPictureUrl
        {
            get; set;
        }

        public bool IsNewUser
        {
            get; set;
        }

        private bool m_bIsPeerConnected = false;
        public bool IsPeerConnected
        {
            get
            {
                return m_bIsPeerConnected || GPlayerMgr.Instance.PeerCurrencyMap.Count > 0;
            }
            set
            {
                m_bIsPeerConnected = value;
            }
        }

        public bool IsParentConnected
        {
            get; set;
        }

        public Dictionary<string, object> SummaryFriendData
        {
            get; private set;
        }

        public PresenceData Presence
        {
            get; set;
        }

        public string ExternalData
        {
            get; private set;
        }

        public XPData PlayerXPData { get; set; }
        #endregion

        #region Public function
        public void SetOpponentPlayerData(PlayerData in_playerData)
        {
            OpponentPlayerData = in_playerData;
        }

        public void SetupOpponentData(object in_object)
        {
            Dictionary<string, object> opponentInfo = (Dictionary<string, object>)in_object;
            OpponentPlayerData = new PlayerData();

            if (opponentInfo.ContainsKey(BrainCloudConsts.JSON_PLAYER_NAME))
            {
                OpponentPlayerData.PlayerName = (string)opponentInfo[BrainCloudConsts.JSON_PLAYER_NAME];
            }

            if (opponentInfo.ContainsKey(BrainCloudConsts.JSON_PLAYER_RATING))
            {
                OpponentPlayerData.PlayerRating = (int)opponentInfo[BrainCloudConsts.JSON_PLAYER_RATING];
            }

            if (opponentInfo.ContainsKey(BrainCloudConsts.JSON_PLAYER_ID))
            {
                OpponentPlayerData.ProfileId = (string)opponentInfo[BrainCloudConsts.JSON_PLAYER_ID];
            }

            if (opponentInfo.ContainsKey(BrainCloudConsts.JSON_PLAYER_PICTURE_URL))
            {
                OpponentPlayerData.PlayerPictureUrl = (string)opponentInfo[BrainCloudConsts.JSON_PLAYER_PICTURE_URL];
            }

            // Summary data, may create team data
        }

        public void ReadLeaderboardPlayerData(object in_object)
        {
            Dictionary<string, object> leaderboardInfo = (Dictionary<string, object>)in_object;

            if (leaderboardInfo.ContainsKey(BrainCloudConsts.JSON_NAME))
            {
                PlayerName = (string)leaderboardInfo[BrainCloudConsts.JSON_NAME];
            }

            if (leaderboardInfo.ContainsKey(BrainCloudConsts.JSON_PLAYER_ID))
            {
                ProfileId = (string)leaderboardInfo[BrainCloudConsts.JSON_PLAYER_ID];
            }

            if (leaderboardInfo.ContainsKey(BrainCloudConsts.JSON_SCORE))
            {
                PlayerRating = (int)leaderboardInfo[BrainCloudConsts.JSON_SCORE];
            }

            if (leaderboardInfo.ContainsKey(BrainCloudConsts.JSON_RANK))
            {
                PlayerRank = (int)leaderboardInfo[BrainCloudConsts.JSON_RANK];
            }

            if (leaderboardInfo.ContainsKey(BrainCloudConsts.JSON_PLAYER_PICTURE_URL))
            {
                PlayerPictureUrl = (string)leaderboardInfo[BrainCloudConsts.JSON_PLAYER_PICTURE_URL];
            }

            if (leaderboardInfo.ContainsKey(BrainCloudConsts.JSON_DATA) && leaderboardInfo[BrainCloudConsts.JSON_DATA] != null)
            {
                m_currentLeaderboardExtraData = (Dictionary<string, object>)leaderboardInfo[BrainCloudConsts.JSON_DATA];
            }
        }

        public void ReadFriendsPlayerData(object in_object)
        {
            Dictionary<string, object> friendsInfo = (Dictionary<string, object>)in_object;

            if (friendsInfo.ContainsKey(BrainCloudConsts.JSON_PLAYER_ID))
            {
                ProfileId = (string)friendsInfo[BrainCloudConsts.JSON_PLAYER_ID];
            }
            else if (friendsInfo.ContainsKey(BrainCloudConsts.JSON_PROFILE_ID))
            {
                ProfileId = (string)friendsInfo[BrainCloudConsts.JSON_PROFILE_ID];
            }

            Presence.ProfileId = ProfileId;

            if (friendsInfo.ContainsKey(BrainCloudConsts.JSON_NAME))
            {
                PlayerName = (string)friendsInfo[BrainCloudConsts.JSON_NAME];
            }
            else if (friendsInfo.ContainsKey(BrainCloudConsts.JSON_PLAYER_NAME))
            {
                PlayerName = (string)friendsInfo[BrainCloudConsts.JSON_PLAYER_NAME];
            }
            else if (friendsInfo.ContainsKey(BrainCloudConsts.JSON_PROFILE_NAME))
            {
                PlayerName = (string)friendsInfo[BrainCloudConsts.JSON_PROFILE_NAME];
            }

            if (friendsInfo.ContainsKey(BrainCloudConsts.JSON_PLAYER_PICTURE_URL))
            {
                PlayerPictureUrl = (string)friendsInfo[BrainCloudConsts.JSON_PLAYER_PICTURE_URL];
            }

            if (friendsInfo.ContainsKey(BrainCloudConsts.JSON_SUMMARY_FRIEND_DATA))
            {
                SummaryFriendData = (Dictionary<string, object>)friendsInfo[BrainCloudConsts.JSON_SUMMARY_FRIEND_DATA];
                if (SummaryFriendData != null && SummaryFriendData.ContainsKey(BrainCloudConsts.JSON_RANK))
                    PlayerRank = (int)SummaryFriendData[BrainCloudConsts.JSON_RANK];
            }

            if (friendsInfo.ContainsKey(BrainCloudConsts.JSON_ONLINE))
            {
                Presence.IsOnline = (bool)friendsInfo[BrainCloudConsts.JSON_ONLINE];
            }

            if (friendsInfo.ContainsKey(BrainCloudConsts.JSON_ACTIVITY))
            {
                Dictionary<string, object> activity = (Dictionary<string, object>)friendsInfo[BrainCloudConsts.JSON_ACTIVITY];
                if (activity.ContainsKey(BrainCloudConsts.JSON_LOCATION))
                {
                    Presence.Location = (string)activity[BrainCloudConsts.JSON_LOCATION];
                }
                if (activity.ContainsKey(BrainCloudConsts.JSON_STATUS))
                {
                    Presence.Status = (string)activity[BrainCloudConsts.JSON_STATUS];
                }
                if (activity.ContainsKey(BrainCloudConsts.JSON_LOBBY_ID))
                {
                    Presence.LobbyId = (string)activity[BrainCloudConsts.JSON_LOBBY_ID];
                }
                if (activity.ContainsKey(BrainCloudConsts.JSON_LOBBY_TYPE))
                {
                    Presence.LobbyType = (string)activity[BrainCloudConsts.JSON_LOBBY_TYPE];
                }
            }
        }

        public void ReadPresencePlayerData(object in_object)
        {
            Dictionary<string, object> friendsInfo = (Dictionary<string, object>)in_object;
            Dictionary<string, object> userData = (Dictionary<string, object>)friendsInfo[BrainCloudConsts.JSON_USER];

            if (userData.ContainsKey(BrainCloudConsts.JSON_ID))
            {
                ProfileId = (string)userData[BrainCloudConsts.JSON_ID];
                Presence.ProfileId = ProfileId;
            }

            if (userData.ContainsKey(BrainCloudConsts.JSON_NAME))
            {
                PlayerName = (string)userData[BrainCloudConsts.JSON_NAME];
            }

            if (userData.ContainsKey(BrainCloudConsts.JSON_PIC))
            {
                PlayerPictureUrl = (string)userData[BrainCloudConsts.JSON_PIC];
            }

            if (friendsInfo.ContainsKey(BrainCloudConsts.JSON_ONLINE))
            {
                Presence.IsOnline = (bool)friendsInfo[BrainCloudConsts.JSON_ONLINE];
            }

            if (friendsInfo.ContainsKey(BrainCloudConsts.JSON_SUMMARY_FRIEND_DATA))
            {
                SummaryFriendData = (Dictionary<string, object>)friendsInfo[BrainCloudConsts.JSON_SUMMARY_FRIEND_DATA];
                if (SummaryFriendData != null && SummaryFriendData.ContainsKey(BrainCloudConsts.JSON_RANK))
                    PlayerRank = (int)SummaryFriendData[BrainCloudConsts.JSON_RANK];
            }

            if (friendsInfo.ContainsKey(BrainCloudConsts.JSON_ACTIVITY))
            {
                Dictionary<string, object> activity = (Dictionary<string, object>)friendsInfo[BrainCloudConsts.JSON_ACTIVITY];
                if (activity.ContainsKey(BrainCloudConsts.JSON_LOCATION))
                {
                    Presence.Location = (string)activity[BrainCloudConsts.JSON_LOCATION];
                }
                if (activity.ContainsKey(BrainCloudConsts.JSON_STATUS))
                {
                    Presence.Status = (string)activity[BrainCloudConsts.JSON_STATUS];
                }
                if (activity.ContainsKey(BrainCloudConsts.JSON_LOBBY_ID))
                {
                    Presence.LobbyId = (string)activity[BrainCloudConsts.JSON_LOBBY_ID];
                }
                if (activity.ContainsKey(BrainCloudConsts.JSON_LOBBY_TYPE))
                {
                    Presence.LobbyType = (string)activity[BrainCloudConsts.JSON_LOBBY_TYPE];
                }
            }
        }

        public bool IsCurrentPlayer()
        {
            if (ProfileId == GPlayerMgr.Instance.PlayerData.ProfileId)
                return true;
            return false;
        }

        public Dictionary<string, object> GetCurrentLeaderboardExtraData()
        {
            return m_currentLeaderboardExtraData;
        }

        public bool CanBeInvited()
        {
            return Presence.Location != null && (Presence.Location.Contains(GPlayerMgr.LOCATION_MAIN_MENU) || Presence.Location.Contains(GPlayerMgr.LOCATION_LOBBY));
        }

        private Dictionary<string, object> m_currentLeaderboardExtraData = new Dictionary<string, object>();
        #endregion
    }
}