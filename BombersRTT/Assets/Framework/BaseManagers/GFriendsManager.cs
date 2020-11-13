using BrainCloud;
using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;

namespace Gameframework
{
    public class GFriendsManager : SingletonBehaviour<GFriendsManager>
    {
        public const string ON_FRIENDS_LIST_UPDATED = "OnFriendsListUpdated";

        #region Public Variables
        public List<PlayerData> Friends { get; private set; }
        public List<PlayerData> SearchResults { get; private set; }
        public string OriginalUserName { get; private set; }

        public Dictionary<string, object> RecentlyViewed { get; private set; }
        #endregion

        #region Public
        public override void StartUp()
        {
            base.StartUp();
            Friends = new List<PlayerData>();
            SearchResults = new List<PlayerData>();
        }

        public void GetRecentlyViewedEntity(SuccessCallback in_success = null, FailureCallback in_failure = null, object cbObj = null)
        {
            GCore.Wrapper.EntityService.GetSingleton("recentlyViewed", OnReadRecentlyViewedEntitySuccess + in_success, in_failure, cbObj);
        }

        public void SetRecentlyViewedData(List<LobbyMemberInfo> in_lobbyInfo)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            List<RecentlyViewed> newMembers = new List<RecentlyViewed>();
            RecentlyViewed item = new RecentlyViewed();
            foreach (LobbyMemberInfo member in in_lobbyInfo)
            {
                if (member.ProfileId != "" && member.ProfileId != GPlayerMgr.Instance.PlayerData.ProfileId)
                {
                    item = new RecentlyViewed(member);
                    newMembers.Add(item);
                }
            }
            data["previousMembers"] = newMembers;

            GCore.Wrapper.EntityService.UpdateSingleton("recentlyViewed", JsonWriter.Serialize(data), ACL.None().ToJsonString(), -1);
        }

        public void OnReadRecentlyViewedEntitySuccess(string in_data, object in_obj)
        {
            try
            {
                Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_data);
                Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];

                // parse the recentlyViewed Entity
                RecentlyViewed = jsonData;
            }
            catch (System.Exception)
            {
            }
        }

        public void GetPresenceOfFriends(SuccessCallback in_success = null, FailureCallback in_failure = null)
        {
            GCore.Wrapper.Client.PresenceService.GetPresenceOfFriends(m_platform, m_includeOffline, OnGetPresenceOfFriendsSuccess + in_success, OnGetPresenceOfFriendsFailed + in_failure);
        }

        public void FindUserByUniversalId(string in_searchText, int in_maxResults, SuccessCallback in_success = null, FailureCallback in_failure = null)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[BrainCloudConsts.JSON_SEARCH_TEXT] = in_searchText;
            data[BrainCloudConsts.JSON_MAX_RESULTS] = in_maxResults;
            // reliance on FIND FRIENDS script 
            GCore.Wrapper.ScriptService.RunScript("FindFriends", JsonWriter.Serialize(data), OnFindUserByUniversalIdSuccess + in_success, OnFindUserByUniversalIdFailure + in_failure, data);
        }

        public bool IsProfileIdInFriendsList(string in_profileID)
        {
            if (Friends != null)
            {
                for (int i = 0; i < Friends.Count; ++i)
                {
                    if (Friends[i].ProfileId.Equals(in_profileID))
                        return true;
                }
            }
            return false;
        }

        public void ParsePresenceCallback(string in_message, ref PresenceData in_presenceData)
        {
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_message);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
            Dictionary<string, object> fromData = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_FROM_KEY];

            in_presenceData.Reset();
            if (fromData.ContainsKey(BrainCloudConsts.JSON_ID))
            {
                in_presenceData.ProfileId = (string)fromData[BrainCloudConsts.JSON_ID];
            }
            if (jsonData.ContainsKey(BrainCloudConsts.JSON_ONLINE))
            {
                in_presenceData.IsOnline = (bool)jsonData[BrainCloudConsts.JSON_ONLINE];
            }
            Dictionary<string, object> activity;
            if (jsonData.ContainsKey(BrainCloudConsts.JSON_ACTIVITY))
            {
                activity = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_ACTIVITY];

                if (activity.ContainsKey(BrainCloudConsts.JSON_LOCATION))
                {
                    in_presenceData.Location = (string)activity[BrainCloudConsts.JSON_LOCATION];
                }
                if (activity.ContainsKey(BrainCloudConsts.JSON_STATUS))
                {
                    in_presenceData.Status = (string)activity[BrainCloudConsts.JSON_STATUS];
                }
                if (activity.ContainsKey(BrainCloudConsts.JSON_LOBBY_ID))
                {
                    in_presenceData.LobbyId = (string)activity[BrainCloudConsts.JSON_LOBBY_ID];
                }
                if (activity.ContainsKey(BrainCloudConsts.JSON_LOBBY_TYPE))
                {
                    in_presenceData.LobbyType = (string)activity[BrainCloudConsts.JSON_LOBBY_TYPE];
                }
            }
        }

        public void OnInviteToLobby(string in_ProfileId, string in_originalUserName)
        {
            // send event to other person
            Dictionary<string, object> jsonData = new Dictionary<string, object>();
            jsonData[BrainCloudConsts.JSON_LAST_CONNECTION_ID] = GCore.Wrapper.Client.RTTConnectionID;
            jsonData[BrainCloudConsts.JSON_PROFILE_ID] = GCore.Wrapper.Client.ProfileId;
            jsonData[BrainCloudConsts.JSON_USER_NAME] = GPlayerMgr.Instance.PlayerData.PlayerName;
            OriginalUserName = in_originalUserName;

            GCore.Wrapper.Client.EventService.SendEvent(in_ProfileId, "OFFER_JOIN_LOBBY",
                JsonWriter.Serialize(jsonData));

            GEventManager.TriggerEvent(GEventManager.ON_INVITED_FRIEND);
        }
        #endregion

        #region Private
        private void OnGetPresenceOfFriendsSuccess(string in_stringData, object in_obj)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            GDebug.Log(string.Format("OnGetPresenceOfFriends Success | {0}", in_stringData));

            Friends.Clear();

            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_stringData);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
            if (jsonData.ContainsKey(BrainCloudConsts.JSON_PRESENCE))
            {
                PlayerData pData = null;
                object[] friends = ((object[])jsonData[BrainCloudConsts.JSON_PRESENCE]);
                if (friends.Length > 0)
                {
                    for (int it = 0; it < friends.Length; ++it)
                    {
                        pData = new PlayerData();
                        pData.ReadPresencePlayerData(friends[it]);
                        Friends.Add(pData);
                    }
                }
            }

            GEventManager.TriggerEvent(ON_FRIENDS_LIST_UPDATED);
        }

        private void OnGetPresenceOfFriendsFailed(int statusCode, int reasonCode, string in_stringData, object in_obj)
        {
            GDebug.Log(string.Format("OnGetPresenceOfFriends Failed | {0}  {1}  {2}", statusCode, reasonCode, in_stringData));
        }

        private void OnFindUserByUniversalIdSuccess(string in_jsonString, object in_obj)
        {
            GDebug.Log(string.Format("OnFindUserByUniversalIdSuccess Success | {0}", in_jsonString));
            GStateManager.Instance.EnableLoadingSpinner(false);

            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_jsonString);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
            Dictionary<string, object> jsonResponse = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_RESPONSE];

            if ((int)jsonResponse["status"] == 400 || (int)jsonResponse["status"] == 500)
            {
                OnFindUserByUniversalIdFailure((int)jsonResponse["status"], (int)jsonResponse["reason_code"], (string)jsonResponse["status_message"], null);
                return;
            }
            Dictionary<string, object> reelData = (Dictionary<string, object>)jsonResponse[BrainCloudConsts.JSON_DATA];

            if (reelData.ContainsKey(BrainCloudConsts.JSON_MATCHES))
            {
                SearchResults.Clear();

                object[] friends = ((object[])reelData[BrainCloudConsts.JSON_MATCHES]);
                if (friends.Length > 0)
                {
                    for (int it = 0; it < friends.Length; ++it)
                    {
                        PlayerData pData = new PlayerData();
                        pData.ReadFriendsPlayerData(friends[it]);
                        if (!IsProfileIdInFriendsList(pData.ProfileId))
                            SearchResults.Add(pData);
                    }
                }
            }
        }

        private void OnFindUserByUniversalIdFailure(int statusCode, int reasonCode, string in_stringData, object in_obj)
        {
            GDebug.Log(string.Format("OnFindUserByUniversalId Failed | {0}  {1}  {2}", statusCode, reasonCode, in_stringData));
            GStateManager.Instance.EnableLoadingSpinner(false);

            switch (reasonCode)
            {
                case ReasonCodes.DATABASE_ERROR:
                    HudHelper.DisplayMessageDialog("ERROR", "THE SEARCH OPERATION TIMED OUT, PLEASE TRY AGAIN.", "OK");
                    break;
                case ReasonCodes.MINIMUM_SEARCH_INPUT:
                    HudHelper.DisplayMessageDialog("ERROR", "INVALID SEARCH CRITERIA. PLEASE ENTER A MINIMUM OF 3 CHARACTERS.", "OK");
                    break;
            }
        }

        private string m_platform = "";   // denotes All
        private bool m_includeOffline = true;
        #endregion
    }

    public class PresenceData
    {
        public string ProfileId;
        public bool IsOnline;
        public string Location;
        public string Status;
        public string LobbyId;
        public string LobbyType;

        public PresenceData()
        {
            Reset();
        }

        public void Reset()
        {
            ProfileId = "";
            IsOnline = false;
            Location = "";
            Status = "";
            LobbyId = "";
            LobbyType = "";
        }
    }

    public class RecentlyViewed
    {
        public RecentlyViewed() { }
        public RecentlyViewed(Dictionary<string, object> in_dict)
        {
            Name = in_dict.ContainsKey("Name") ? (string)in_dict["Name"] : "";
            ProfileId = in_dict.ContainsKey("ProfileId") ? (string)in_dict["ProfileId"] : "";

        }
        public RecentlyViewed(LobbyMemberInfo in_info)
        {
            Name = in_info.Name;
            ProfileId = in_info.ProfileId;
        }
        public string ProfileId;
        public string Name;
    }

    public class LobbyMemberInfo
    {
        public LobbyMemberInfo(Dictionary<string, object> in_dict)
        {
            Name = (string)in_dict["name"];
            PictureURL = (string)in_dict["pic"];
            Team = (string)in_dict["team"];

            try
            {
                ExtraData = (Dictionary<string, object>)in_dict["extra"];
            }
            catch (Exception)
            {
                ExtraData = new Dictionary<string, object>();
            }

            if (in_dict.ContainsKey("cxId")) CXId = (string)in_dict["cxId"];
            else if (ExtraData.ContainsKey("cxId")) CXId = (string)ExtraData["cxId"];

            Rating = GConfigManager.ReadIntSafely(in_dict, "rating");
            IsReady = GConfigManager.ReadBoolSafely(in_dict, "isReady");

            if (in_dict.ContainsKey("profileId")) ProfileId = (string)in_dict["profileId"];
            else if (ExtraData.ContainsKey("profileId")) ProfileId = (string)ExtraData["profileId"];

            if (in_dict.ContainsKey("netId")) NetId = Convert.ToInt16(in_dict["netId"]);
        }

        public string ProfileId;
        public string Name;
        public string PictureURL;
        public string Team;

        public string CXId;

        public int Rating;
        public short NetId;

        public bool IsReady;
        public bool LobbyReadyUp = false;
        // delete
        public BrainCloudUNETExample.Game.BombersPlayerController PlayerController = null;

        public Dictionary<string, object> ExtraData;
    }
}
