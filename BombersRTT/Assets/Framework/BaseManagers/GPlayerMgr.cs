using System;
using BrainCloud.JsonFx.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrainCloud;
using System.Globalization;
#if FACEBOOK_ENABLED
using Facebook.Unity;
#endif

namespace Gameframework
{
    public struct BCCurrencyData
    {
        public string currencyType;
        public object currencyAmount;
    }

    public class GPlayerMgr : SingletonBehaviour<GPlayerMgr>
    {
        #region public
        public PlayerData PlayerData { get; private set; }
        public Dictionary<string, object> CurrencyMap { get; private set; }
        public Dictionary<string, object> PeerCurrencyMap { get; private set; }
        public Dictionary<string, object> ParentCurrencyMap { get; private set; }

        public ulong CurrentServerTime { get; private set; }
        public Dictionary<string, ulong> PrimaryGlobalLeaderboardRemainingTime { get; private set; }

        public int LoginCount { get; private set; }
        public ulong LastLogin { get; private set; }

        public string PlayerPictureUrl { get; private set; }

        // turn into BCStats
        public Dictionary<string, object> CachedStats { get; private set; }
        public long GetUserStatNamed(string in_statName)
        {
            return HudHelper.GetLongValue(CachedStats, in_statName);
        }

        // only sets this locally, not with the cloud, the cloud takes care of the cloud
        public void SetUserStatNamed(string in_statName, long in_value)
        {
            CachedStats[in_statName] = in_value;
        }

        // Use this for initialization
        private void Start()
        {
            PlayerData = new PlayerData();
            CurrencyMap = new Dictionary<string, object>();
            PeerCurrencyMap = new Dictionary<string, object>();
            ParentCurrencyMap = new Dictionary<string, object>();
            CurrentServerTime = Convert.ToUInt64(Time.time * 1000.0f);
            PrimaryGlobalLeaderboardRemainingTime = new Dictionary<string, ulong>();
            StartCoroutine(UpdateSimulatedTimeDeltas());
        }

        [System.Runtime.InteropServices.DllImport("KERNEL32.DLL")]
        private static extern int GetSystemDefaultLCID();

        public void ReadPlayerState(SuccessCallback in_callback)
        {
            GCore.Wrapper.Client.PlayerStateService.ReadUserState(OnAuthSuccess + in_callback);
        }

        public void OnAuthSuccess(string in_json, object obj)
        {
            PlayerData.ProfileId = GCore.Wrapper.Client.AuthenticationService.ProfileId;

            onReadStats(in_json, obj);
            OnVirtualCurrencies(in_json, obj);

            // read the auth success response
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_json);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];

            ReadCurrentServerTime(jsonData);

            if (jsonData.ContainsKey(BrainCloudConsts.JSON_PLAYER_PICTURE_URL))
            {
                PlayerData.PlayerPictureUrl = ((string)jsonData[BrainCloudConsts.JSON_PLAYER_PICTURE_URL]);
                PlayerPictureUrl = PlayerData.PlayerPictureUrl;
            }

            if (jsonData.ContainsKey(BrainCloudConsts.JSON_PLAYER_NAME))
            {
                PlayerData.PlayerName = ((string)jsonData[BrainCloudConsts.JSON_PLAYER_NAME]);
            }

            if (jsonData.ContainsKey(BrainCloudConsts.JSON_IS_TESTER))
            {
                PlayerData.IsTester = ((bool)jsonData[BrainCloudConsts.JSON_IS_TESTER]);
            }

            if (jsonData.ContainsKey(BrainCloudConsts.JSON_PLAYER_EMAIL))
            {
                PlayerData.PlayerEmail = ((string)jsonData[BrainCloudConsts.JSON_PLAYER_EMAIL]);
            }

            if (jsonData.ContainsKey(BrainCloudConsts.JSON_VC_PURCHASED))
            {
                PlayerData.PlayerVCPurchased = ((int)jsonData[BrainCloudConsts.JSON_VC_PURCHASED]);
            }

            PlayerData.IsNewUser = false;
            if (jsonData.ContainsKey(BrainCloudConsts.JSON_NEW_USER) && (string)jsonData[BrainCloudConsts.JSON_NEW_USER] == "true")
            {
                PlayerData.IsNewUser = true;
                // its a new user! 
                createDefaultEntities();
            }

            PlayerData.IsPeerConnected = false;
            if (jsonData.ContainsKey(BrainCloudConsts.JSON_PEER_PROFILE_ID) && jsonData[BrainCloudConsts.JSON_PEER_PROFILE_ID] != null)
            {
                PlayerData.IsPeerConnected = true;
            }

            PlayerData.IsParentConnected = false;
            if (jsonData.ContainsKey(BrainCloudConsts.JSON_PARENT_PROFILE_ID) && jsonData[BrainCloudConsts.JSON_PARENT_PROFILE_ID] != null)
            {
                PlayerData.IsParentConnected = true;
            }

            LoginCount = jsonData.ContainsKey(BrainCloudConsts.JSON_LOGIN_COUNT) ? (int)jsonData[BrainCloudConsts.JSON_LOGIN_COUNT] : 0;
            LastLogin = jsonData.ContainsKey("lastLogin") ? Convert.ToUInt64(jsonData["lastLogin"]) : 0;

            //request the global properties
            GCore.Wrapper.GlobalAppService.ReadProperties(GConfigManager.Instance.OnReadBrainCloudProperties);
            //GCore.Wrapper.S3HandlingService.GetFileList("", GConfigManager.Instance.OnReadGlobalFileList);

#if BUY_CURRENCY_ENABLED
            string platform = GIAPManager.IAP_PLATFORM_FACEBOOK;
#if UNITY_IOS
            platform = GIAPManager.IAP_PLATFORM_ITUNES;
#elif UNITY_ANDROID
            platform = GIAPManager.IAP_PLATFORM_GOOGLEPLAY;
#elif STEAMWORKS_ENABLED
            platform = GIAPManager.IAP_PLATFORM_STEAM;
#endif
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            var regionInfo = new RegionInfo(System.Threading.Thread.CurrentThread.CurrentUICulture.LCID);
            GCore.Wrapper.Client.AppStoreService.GetSalesInventory(platform, regionInfo.ISOCurrencySymbol, GIAPManager.Instance.OnReadInventory);
#else
            //var region = RegionLocale.UsersCountryLocale.Equals("") ? "EN-US" : RegionLocale.UsersCountryLocale;
            //RegionInfo regionInfo = new RegionInfo(region);
            GCore.Wrapper.Client.AppStoreService.GetSalesInventory(platform, "USD", GIAPManager.Instance.OnReadInventory);
#endif
#endif
            // populate level meta data
            GLevelManager.Instance.PopulateRewardsList();

            //Retrieve the list of Identities associated with this account
            GCore.Wrapper.Client.IdentityService.GetIdentities(OnGetIdentitiesSuccess);

            // Retrieve the BC Events
            GCore.Wrapper.Client.EventService.GetEvents(onGetBCEventsSuccess, onGetBCEventsFailed, null);
            GetXpData();
            ////////////////
            ////////////////

            // Register for Push Notifications
            if (GSettingsMgr.PushAuthorized)
                RegisterPushNotificationDeviceToken();

            GEventManager.TriggerEvent(GEventManager.ON_PLAYER_DATA_UPDATED);
        }

        public bool onAuthFail(int status, int reasonCode, string jsonError, object cbObject)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            switch (reasonCode)
            {
                case ReasonCodes.GAME_VERSION_NOT_SUPPORTED:
                    {
                        // Get App Upgrade URL from response
                        Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(jsonError);
                        if (jsonMessage.ContainsKey(BrainCloudConsts.JSON_UPGRADE_APP_ID))
                        {
                            m_AppUpgradeURL = (string)jsonMessage[BrainCloudConsts.JSON_UPGRADE_APP_ID];
                        }
                        displayAppUpgradeMessage();
                        return true;
                    }
            }
            return false;
        }

        private void displayAppUpgradeMessage()
        {
            HudHelper.DisplayMessageDialog("WE'VE IMPROVED", "WE'VE MADE CHANGES TO THE GAME THAT WILL IMPROVE YOUR EXPERIENCE!", "UPGRADE NOW", displayBlockingUpdateApp);
        }

        private void displayBlockingUpdateApp()
        {
            HudHelper.OpenURL(m_AppUpgradeURL);
            Invoke("displayAppUpgradeMessage", 0.25f);
        }

        public void GetXpData()
        {
            GCore.Wrapper.ScriptService.RunScript("GetXpData", null, OnGetXPDataSuccess);
        }

        public void UpdatePlayerName(string in_name, SuccessCallback in_success = null, FailureCallback in_failure = null)
        {
            GCore.Wrapper.Client.PlayerStateService.UpdateUserName(in_name, in_success, in_failure);
            PlayerData.PlayerName = (in_name);
            GEventManager.TriggerEvent(GEventManager.ON_PLAYER_DATA_UPDATED);
        }

        public void OnResponseResetPacketTimeout(string jsonResponse, object cbObject)
        {
            GCore.Wrapper.Client.SetPacketTimeoutsToDefault();
        }

        public void OnResponseResetPacketTimeoutFailure(int status, int reasonCode, string jsonError, object cbObject)
        {
            GCore.Wrapper.Client.SetPacketTimeoutsToDefault();
        }

        public void OnFailedReadMatchMakingResults(int status, int reasonCode, string jsonError, object cbObject)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            // TODO: Add a proper error message 
            HudHelper.DisplayMessageDialog("NO PLAYERS FOUND", "PLEASE TRY AGAIN AT ANOTHER TIME", "OK");
        }

        public void ResetBrainCloudAuth()
        {
            GCore.Wrapper.ResetStoredProfileId();
            GCore.Wrapper.ResetStoredAnonymousId();
            GCore.Wrapper.ResetStoredAuthenticationType();

            GLevelManager.Instance.ResetCachedLevel();
            CachedStats.Clear();

            // lets also make a new player data so nothing is left around
            PlayerData = new PlayerData();
            CurrencyMap = new Dictionary<string, object>();
            PeerCurrencyMap = new Dictionary<string, object>();
            ParentCurrencyMap = new Dictionary<string, object>();
        }

        public void ReadCurrentServerTime(Dictionary<string, object> in_dict)
        {
            if (in_dict.ContainsKey(BrainCloudConsts.JSON_SERVER_TIME))
            {
                CurrentServerTime = Convert.ToUInt64(in_dict[BrainCloudConsts.JSON_SERVER_TIME]);
            }
        }

        public void DeleteBCEvent(BaseBrainCloudEventData in_baseBCEventData, SuccessCallback in_success = null, FailureCallback in_failure = null)
        {
            if (in_baseBCEventData != null && in_baseBCEventData.evId != null)
            {
                GCore.Wrapper.Client.EventService.DeleteIncomingEvent(
                in_baseBCEventData.evId,
                onDeleteBCEventSuccess + in_success,
                onDeleteBCEventFailed + in_failure,
                null);
            }
        }

        public bool ProcessBCEvents()
        {
            bool bNewItemProcessed = false;
            // Do we have any events to process?
            if (m_baseBrainCloudEventDataList.Count > 0)
            {
                BaseBrainCloudEventData baseBrainCloudEventData = GetNextAvailableBrainCloudEvent();
                if (baseBrainCloudEventData != null)
                {
                    if (baseBrainCloudEventData.eventType.Contains(BrainCloudConsts.JSON_EVENT_SYSTEM_TOURNAMENT_COMPLETE))
                    {
                        // SYSTEM_TOURNAMENT_COMPLETE Event received
                        // claim the reward
                        TournamentClaimReward(baseBrainCloudEventData, null, null, baseBrainCloudEventData);
                        bNewItemProcessed = true;
                    }
                    // Process other event types here
                }
            }
            return bNewItemProcessed;
        }

        public void TournamentClaimReward(BaseBrainCloudEventData in_brainCloudEventData, SuccessCallback in_success = null, FailureCallback in_failure = null, object cbObject = null)
        {
            GCore.Wrapper.Client.TournamentService.ClaimTournamentReward(in_brainCloudEventData.leaderboardId,
                in_brainCloudEventData.versionId,
                onClaimRewardSuccess + in_success,
                onClaimRewardFailed + in_failure,
                cbObject);
        }

        public void TournamentGetTournamentStatus(BaseBrainCloudEventData in_brainCloudEventData, SuccessCallback in_success = null, FailureCallback in_failure = null)
        {
            GCore.Wrapper.Client.TournamentService.GetTournamentStatus(in_brainCloudEventData.leaderboardId,
                 in_brainCloudEventData.versionId,
                in_success,
                in_failure,
                null);
        }

        public void RegisterForNotifications()
        {
#if UNITY_IOS //|| UNITY_TVOS
            // register for push notifications
            UnityEngine.iOS.NotificationServices.RegisterForNotifications(
                                                    UnityEngine.iOS.NotificationType.Alert |
                                                    UnityEngine.iOS.NotificationType.Badge |
                                                    UnityEngine.iOS.NotificationType.Sound);
#elif UNITY_ANDROID
           // Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
#endif
        }
#if UNITY_ANDROID 
        /*
        private string m_firebaseToken = "";
        public void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
        {
            m_firebaseToken = token.Token;
        }
        */
#endif

        public void RegisterPushNotificationDeviceToken(SuccessCallback in_success = null, FailureCallback in_failure = null)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (UnityEngine.iOS.NotificationServices.deviceToken != null)
            {
                GCore.Wrapper.PushNotificationService.RegisterPushNotificationDeviceToken(UnityEngine.iOS.NotificationServices.deviceToken,
                                                                                                    in_success,
                                                                                                    in_failure,
                                                                                                    null);
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            /*
            if (m_firebaseToken != "")
            {
                GCore.Wrapper.PushNotificationService.RegisterPushNotificationDeviceToken(BrainCloud.Common.Platform.GooglePlayAndroid,
                                                                                                    m_firebaseToken,
                                                                                                    in_success,
                                                                                                    in_failure,
                                                                                                    null);
            }
            */
#endif
        }

        public void OnVirtualCurrencies(string in_json, object in_cbObject)
        {
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_json);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
            Dictionary<string, object> jsonCurrency = null; // we'll skip if this is null
            Dictionary<string, object> jsonPeerCurrency = null;// we'll skip if this is null
            Dictionary<string, object> jsonParentCurrency = null;// we'll skip if this is null

            try
            {
                // default currency call
                jsonCurrency = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_CURRENCY_MAP];
                if (jsonData.ContainsKey(BrainCloudConsts.JSON_PEER_CURRENCY))
                    jsonPeerCurrency = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_PEER_CURRENCY];
            }
            catch (Exception)
            {
                // from cashing in the itunes receipt
                if (jsonData.ContainsKey(BrainCloudConsts.JSON_PLAYER_CURRENCY))
                {
                    jsonData = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_PLAYER_CURRENCY];
                    jsonCurrency = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_CURRENCY_MAP];
                    if (jsonData.ContainsKey(BrainCloudConsts.JSON_PEER_CURRENCY))
                        jsonPeerCurrency = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_PEER_CURRENCY];
                    if (jsonData.ContainsKey(BrainCloudConsts.JSON_PARENT_CURRENCY))
                        jsonParentCurrency = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_PARENT_CURRENCY];

                }
                // on auth call/ read player state ?
                // or maybe purchasing as wel1
                else if (jsonData != null && !jsonData.ContainsKey(BrainCloudConsts.JSON_CURRENCY))
                {
                    if (jsonData.ContainsKey(BrainCloudConsts.JSON_RESPONSE))
                    {
                        try
                        {
                            jsonData = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_RESPONSE];
                        }
                        catch (Exception) { };

                        // a cloud script return type
                        if (jsonData.ContainsKey(BrainCloudConsts.JSON_CURRENCY))
                        {
                            jsonData = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_CURRENCY];
                        }

                        if (!jsonData.ContainsKey(BrainCloudConsts.JSON_DATA))
                        {
                            // TODO: handle bad currency update!, since the cloud script was successful,
                            // but the awarding was not correct
                            return;
                        }

                        jsonData = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_DATA];
                        jsonCurrency = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_CURRENCY_MAP];
                        if (jsonData.ContainsKey(BrainCloudConsts.JSON_PEER_CURRENCY))
                            jsonPeerCurrency = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_PEER_CURRENCY];
                        if (jsonData.ContainsKey(BrainCloudConsts.JSON_PARENT_CURRENCY))
                            jsonParentCurrency = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_PARENT_CURRENCY];

                        // some strangeness
                        if (jsonCurrency.ContainsKey("loot"))
                        {
                            PeerCurrencyMap = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_CURRENCY_MAP];
                            jsonCurrency = null;
                        }
                    }
                }
                // back from a cloud code call ??
                else
                {
                    jsonCurrency = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_CURRENCY];
                    if (jsonData.ContainsKey(BrainCloudConsts.JSON_PEER_CURRENCY))
                        jsonPeerCurrency = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_PEER_CURRENCY];
                    if (jsonData.ContainsKey(BrainCloudConsts.JSON_PARENT_CURRENCY))
                        jsonParentCurrency = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_PARENT_CURRENCY];
                }
            }

            if (jsonCurrency != null)
            {
                CurrencyMap = jsonCurrency;
            }

            if (jsonPeerCurrency != null)
            {
                PeerCurrencyMap = (Dictionary<string, object>)jsonPeerCurrency[PEER_NAME];
            }

            if (jsonParentCurrency != null)
            {
                ParentCurrencyMap = (Dictionary<string, object>)jsonParentCurrency[PARENT_NAME];
            }

            GEventManager.TriggerEvent(GEventManager.ON_PLAYER_DATA_UPDATED);
        }

        public bool IsEmailAttached()
        {
            return (GCore.Wrapper.Client.Authenticated && HudHelper.IsEmailFormat(PlayerData.PlayerEmail));
        }

        public bool IsUniversalIdAttached()
        {
            return (GCore.Wrapper.Client.Authenticated && PlayerData.UniversalId != null && PlayerData.UniversalId != "");
        }

        public bool IsSteamIdAttached()
        {
            return (GCore.Wrapper.Client.Authenticated && PlayerData.SteamId != null && PlayerData.SteamId != "");
        }

        public void ConnectToEmailIdentity(string in_email, string in_password,
                                            SuccessCallback in_mergeSuccess = null, FailureCallback in_mergeFailure = null,
                                            SuccessCallback in_switchSuccess = null, FailureCallback in_switchFailure = null)
        {
            GStateManager.Instance.EnableLoadingSpinner(true);
            if (PlayerData.PlayerVCPurchased > 0)
            {
                // Merging profile
                GCore.Wrapper.Client.IdentityService.MergeEmailIdentity(in_email, in_password,
                    OnAuthSuccess + (SuccessCallback)OnHideLoadingSpinner + in_mergeSuccess, OnHideLoadingSpinner + in_mergeFailure);
            }
            else
            {
                // No purchase were done, Switching profile
                GCore.Wrapper.SmartSwitchAuthenticateEmail(in_email, in_password, true,
                    OnAuthSuccess + (SuccessCallback)OnHideLoadingSpinner + in_switchSuccess, OnHideLoadingSpinner + in_switchFailure);
            }
        }

        public void ConnectToFacebookIdentity(string in_userId, string in_token,
                                            SuccessCallback in_mergeSuccess = null, FailureCallback in_mergeFailure = null,
                                            SuccessCallback in_switchSuccess = null, FailureCallback in_switchFailure = null)
        {
#if FACEBOOK_ENABLED
            GStateManager.Instance.EnableLoadingSpinner(true);
            if (PlayerData.PlayerVCPurchased > 0)
            {
                AccessToken aToken = AccessToken.CurrentAccessToken;
                GCore.Wrapper.IdentityService.AttachFacebookIdentity(aToken.UserId, aToken.TokenString, OnAuthSuccess + (SuccessCallback)OnHideLoadingSpinner + in_mergeSuccess, OnHideLoadingSpinner + in_mergeFailure);
            }
            else
            {
                ResetBrainCloudAuth();
                // No purchase were done, Switching profile
                GCore.Wrapper.SmartSwitchAuthenticateFacebook(in_userId, in_token, true,
                    OnAuthSuccess + (SuccessCallback)OnHideLoadingSpinner + in_switchSuccess, OnHideLoadingSpinner + in_switchFailure);
            }
#endif
        }

        // bool = success
        public bool HandleFinalizePurchaseSuccess(string jsonResponse, object obj)
        {
            bool toReturn = false;
#if STEAMWORKS_ENABLED
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(jsonResponse);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];

            int resultCode = (int)jsonData[BrainCloudConsts.JSON_RESULT_CODE];

            // FAILURE
            if (resultCode != 0)
            {
                //Dictionary<string, object> transactionSummary = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_TRANSACTION_SUMMARY];
                // TODO, MORE ERROR HANDLING
                string errorMessage = (string)jsonData[BrainCloudConsts.JSON_ERROR_MESSAGE];
                HudHelper.DisplayMessageDialog("PURCHASE FAILED", errorMessage, "OK");
            }
            else
            {
                toReturn = true;
            }
#endif
            return toReturn;
        }

        public void HandleFailedPurchaseEvent(int status, int reasonCode, string jsonError, object cbObject)
        {
            if (GStateManager.Instance.FindSubState(GenericMessageSubState.STATE_NAME) != null ||
                GStateManager.Instance.NextSubStateId == GenericMessageSubState.STATE_NAME)
            {
                return;
            }

            // user cancelled event
            string jsonErrorLower = jsonError.ToLower();
            if (jsonErrorLower.Contains("usercancelled"))
            {
                HudHelper.DisplayMessageDialog("PURCHASE FAILED", "PURCHASE TRANSACTION CANCELLED!", "OK");
            }
            // make these two MUTE
            else if (jsonErrorLower.Contains("unknown") || jsonErrorLower.Contains(":'7'"))
            {
                // DO NOTHING!!
            }
            // otherwise report it blindly
            else
            {
                HudHelper.DisplayMessageDialog("PURCHASE FAILED", jsonError, "OK");
            }
        }

        public void OnReadXPDataSuccess(Dictionary<string, object> in_obj)
        {
            PlayerData.PlayerXPData.OriginalLevel = PlayerData.PlayerXPData.CurrentLevel;
            PlayerData.PlayerXPData.Init(in_obj);
            pushLevelUpState(PlayerData.PlayerXPData.OriginalLevel);
        }

        public bool ReadPrimaryLeaderboardRemainingTime(Dictionary<string, object> in_dict)
        {
            string leaderboardId = (string)in_dict[BrainCloudConsts.JSON_LEADERBOARD_ID];

            if (in_dict.ContainsKey(BrainCloudConsts.JSON_TIME_BEFORE_RESET))
            {
                PrimaryGlobalLeaderboardRemainingTime[leaderboardId] = (ulong)HudHelper.GetLongValue(in_dict[BrainCloudConsts.JSON_TIME_BEFORE_RESET]);
                return true;
            }
            return false;
        }

        private void OnReadGlobalLeaderboard(string in_json, object in_cbObject)
        {
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_json);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];

            // Update our server time with the latest value from the server.
            ReadCurrentServerTime(jsonData);

            ReadPrimaryLeaderboardRemainingTime(jsonData);
        }

        public void GetGlobalLeaderboardPage(string in_leaderboardName, int in_start, int in_end, bool in_playerView, SuccessCallback in_success = null, FailureCallback in_failure = null)
        {
            if (in_start < 0) in_start = 0;
            if (in_playerView)
            {
                GCore.Wrapper.SocialLeaderboardService.GetGlobalLeaderboardView(in_leaderboardName,
                    BrainCloudSocialLeaderboard.SortOrder.HIGH_TO_LOW,
                    in_start, in_end,
                    OnReadGlobalLeaderboard + in_success,
                    in_failure,
                    null);
            }
            else
            {
                GCore.Wrapper.SocialLeaderboardService.GetGlobalLeaderboardPage(in_leaderboardName,
                    BrainCloudSocialLeaderboard.SortOrder.HIGH_TO_LOW,
                    in_start, in_end,
                    OnReadGlobalLeaderboard + in_success,
                    in_failure,
                    null);
            }
        }

        public void PostScoreToLeaderboard(string in_leaderboardName, int in_score, SuccessCallback in_success = null, FailureCallback in_failure = null)
        {
            GCore.Wrapper.SocialLeaderboardService.PostScoreToLeaderboard(in_leaderboardName,
                in_score,
                "",
                in_success,
                in_failure,
                null);
        }

        public void LoginFacebook(SuccessCallback in_success = null, FailureCallback in_failure = null)
        {
#if FACEBOOK_ENABLED
            m_loginFacebookSuccessDelegate = in_success;
            m_loginFacebookFailureDelegate = in_failure;
            GStateManager.Instance.EnableLoadingSpinner(true);
            var perms = new List<string>() { "public_profile", "email", "user_friends" };
            FB.LogInWithReadPermissions(perms, AuthCallback);
#endif
        }

        public void LogoutFacebook(bool in_revertToAnon, SuccessCallback in_success = null, FailureCallback in_failure = null)
        {
            // and disconnect the identity
#if FACEBOOK_ENABLED
            AccessToken aToken = AccessToken.CurrentAccessToken;
            GCore.Wrapper.IdentityService.DetachFacebookIdentity(aToken.UserId, in_revertToAnon, in_success, in_failure);
            FB.LogOut();
#endif
        }

        public void AwardPlayerCurrency(string in_type, ulong in_value,
            SuccessCallback in_success = null,
            FailureCallback in_failure = null)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[BrainCloudConsts.JSON_CURRENCY_TYPE] = in_type;
            data[BrainCloudConsts.JSON_CURRENCY_AMOUNT] = in_value;
            GCore.Wrapper.ScriptService.RunScript("AwardCurrency", JsonWriter.Serialize(data), OnVirtualCurrencies + in_success, handleRetryCurrencyFail + in_failure, data);
        }

        public void ConsumePlayerCurrency(string in_type, ulong in_value,
            SuccessCallback in_success = null,
            FailureCallback in_failure = null)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[BrainCloudConsts.JSON_CURRENCY_TYPE] = in_type;
            data[BrainCloudConsts.JSON_CURRENCY_AMOUNT] = in_value;
            GCore.Wrapper.ScriptService.RunScript("ConsumeCurrency", JsonWriter.Serialize(data), OnVirtualCurrencies + in_success, handleRetryConsumeFail + in_failure, data);
        }

        public void ConvertCurrency(string in_awardType, ulong in_awardValue, string in_consumeType, ulong in_consumeValue,
            SuccessCallback in_success = null,
            FailureCallback in_failure = null)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[BrainCloudConsts.JSON_AWARD_CURRENCY_TYPE] = in_awardType;
            data[BrainCloudConsts.JSON_AWARD_CURRENCY_AMOUNT] = in_awardValue;
            data[BrainCloudConsts.JSON_CONSUME_CURRENCY_TYPE] = in_consumeType;
            data[BrainCloudConsts.JSON_CONSUME_CURRENCY_AMOUNT] = in_consumeValue;
            GCore.Wrapper.ScriptService.RunScript("ConvertCurrency", JsonWriter.Serialize(data), OnVirtualCurrencies + in_success, handleRetryCurrencyFail + in_failure, data);
        }

        public void UpdatePlayerSummaryData()
        {
            Dictionary<string, object> data = getFriendSummaryString();
            if (data.Count > 0)
            {
                GCore.Wrapper.PlayerStateService.UpdateSummaryFriendData(JsonWriter.Serialize(data), onSummaryDataUpdated);
            }
        }

        public long GetCurrencyBalance(string in_type)
        {
            long toReturn = 0;

            Dictionary<string, object> currencyMap = GPlayerMgr.Instance.CurrencyMap;
            if (currencyMap.ContainsKey(in_type))
            {
                Dictionary<string, object> currencyType = (Dictionary<string, object>)currencyMap[in_type];
                toReturn = (long)HudHelper.GetLongValue(currencyType, BrainCloudConsts.VIRTUAL_CURRENCY_BALANCE);
            }

            if (PlayerData.IsPeerConnected)
            {
                currencyMap = GPlayerMgr.Instance.PeerCurrencyMap;
                if (currencyMap.ContainsKey(in_type))
                {
                    Dictionary<string, object> currencyType = (Dictionary<string, object>)currencyMap[in_type];
                    toReturn = (long)HudHelper.GetLongValue(currencyType, BrainCloudConsts.VIRTUAL_CURRENCY_BALANCE);
                }
            }

            if (PlayerData.IsParentConnected)
            {
                currencyMap = GPlayerMgr.Instance.ParentCurrencyMap;
                if (currencyMap.ContainsKey(in_type))
                {
                    Dictionary<string, object> currencyType = (Dictionary<string, object>)currencyMap[in_type];
                    toReturn = (long)HudHelper.GetLongValue(currencyType, BrainCloudConsts.VIRTUAL_CURRENCY_BALANCE);
                }
            }
            return toReturn;
        }

        public void ValidateString(string in_string, SuccessCallback success = null, FailureCallback failure = null)
        {
            if (in_string.Length < MIN_CHARACTERS_GAME_NAME)
            {
                HudHelper.DisplayMessageDialog("DISALLOWED NAME", "THE NAME MUST BE AT LEAST " + MIN_CHARACTERS_GAME_NAME + " CHARACTERS LONG.", "OK");
                return;
            }
            m_validateStringSuccess = success;
            m_validateStringFailure = failure;
            GStateManager.Instance.EnableLoadingSpinner(true);
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[BrainCloudConsts.JSON_IN_STRING] = in_string;
            GCore.Wrapper.ScriptService.RunScript("WebPurifyString", JsonWriter.Serialize(data), OnWebPurifyStringSuccess, OnWebPurifyStringError, data);
        }
        #endregion

        #region private
#if FACEBOOK_ENABLED
        private SuccessCallback m_loginFacebookSuccessDelegate = null;
        private FailureCallback m_loginFacebookFailureDelegate = null;
        private void AuthCallback(ILoginResult result)
        {
            if (FB.IsLoggedIn)
            {
                AccessToken aToken = AccessToken.CurrentAccessToken;
                GCore.Wrapper.IdentityService.AttachFacebookIdentity(aToken.UserId, aToken.TokenString, m_loginFacebookSuccessDelegate + OnHideLoadingSpinner, m_loginFacebookFailureDelegate + OnHideLoadingSpinner);
            }
            else if (result.Error == null)
            {
                GStateManager.Instance.EnableLoadingSpinner(false);
                HudHelper.DisplayMessageDialog("USER CANCELLED LOGIN", "USER CANCELLED LOGIN", "OK");
            }
            else
            {
                GStateManager.Instance.EnableLoadingSpinner(false);
                HudHelper.DisplayMessageDialog("THERE WAS AN ERROR", result.Error, "OK");
            }
        }
#endif
        private void OnGetXPDataSuccess(string jsonResponse, object cbObject)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);

            PlayerData.PlayerXPData.OriginalLevel = PlayerData.PlayerXPData.CurrentLevel;
            PlayerData.PlayerXPData.Init(jsonResponse);
            pushLevelUpState(PlayerData.PlayerXPData.OriginalLevel);
        }

        private void pushLevelUpState(int originalLevel)
        {
            GEventManager.TriggerEvent(GEventManager.ON_PLAYER_DATA_UPDATED);

            if (originalLevel != 0 && originalLevel < PlayerData.PlayerXPData.CurrentLevel)
            {
                // TODO push level up state, 
                GDebug.Log("LEVELUP :: " + PlayerData.PlayerXPData.CurrentLevel);
                GEventManager.TriggerEvent(GEventManager.ON_PLAYER_LEVEL_UP);
            }
        }

        private IEnumerator UpdateSimulatedTimeDeltas()
        {
            float lastTime = Time.realtimeSinceStartup;
            float delta = 0.0f;
            while (true)
            {
                yield return YieldFactory.GetWaitForSeconds(UPDATE_SIMULATED_TIME);
                delta = (Time.realtimeSinceStartup - lastTime) * 1000.0f;

                // update the current server time
                CurrentServerTime += Convert.ToUInt64(delta);

                // Update the RemainingTime countdown
                List<string> keys = new List<string>(PrimaryGlobalLeaderboardRemainingTime.Keys);
                foreach (string key in keys)
                {
                    PrimaryGlobalLeaderboardRemainingTime[key] -= (ulong)(delta);
                    if ((long)PrimaryGlobalLeaderboardRemainingTime[key] < 0)
                        PrimaryGlobalLeaderboardRemainingTime[key] = 0;
                }
                lastTime = Time.realtimeSinceStartup;
            }
        }

        private void AwardPlayerCurrencyWithObject(object in_obj)
        {
            BCRetryData retryData = (BCRetryData)in_obj;
            AwardPlayerCurrency((BCCurrencyData)retryData.retryData, retryData.success, retryData.failure);
        }

        private void AwardPlayerCurrency(BCCurrencyData reqData, SuccessCallback in_success = null, FailureCallback in_failure = null)
        {
            AwardPlayerCurrency(reqData.currencyType, (ulong)reqData.currencyAmount, in_success, in_failure);
        }

        private void ConsumePlayerCurrencyWithObject(object in_obj)
        {
            BCRetryData retryData = (BCRetryData)in_obj;
            ConsumePlayerCurrency((BCCurrencyData)retryData.retryData, retryData.success, retryData.failure);
        }

        private void ConsumePlayerCurrency(BCCurrencyData reqData, SuccessCallback in_success = null, FailureCallback in_failure = null)
        {
            ConsumePlayerCurrency(reqData.currencyType, (ulong)reqData.currencyAmount, in_success, in_failure);
        }

        public void IncrementStatNamedWithValueLimit(string in_name, int in_value, int in_maxValue, SuccessCallback in_success = null, FailureCallback in_failure = null, object in_cbObject = null)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[in_name] = "INC_TO_LIMIT#" + in_value + "#" + in_maxValue;
            GCore.Wrapper.Client.PlayerStatisticsService.IncrementUserStats(data, in_success, in_failure, in_cbObject);
        }

        public void DecrementStatNamedWithValueLimit(string in_name, int in_value, int in_minValue, SuccessCallback in_success = null, FailureCallback in_failure = null, object in_cbObject = null)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[in_name] = "DEC_TO_LIMIT#" + in_value + "#" + in_minValue;
            GCore.Wrapper.Client.PlayerStatisticsService.IncrementUserStats(data, in_success, in_failure, in_cbObject);
        }

        /// <summary>
        /// Creates essential entities
        /// </summary>
        private void createDefaultEntities()
        {
        }

        private void onTriggerEventsOnReadStats(string in_json, object in_cbObject)
        {
            GEventManager.TriggerEvent(GEventManager.ON_PLAYER_DATA_UPDATED);
        }

        private void onReadStats(string in_json, object in_cbObject)
        {
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_json);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
            if (jsonData.ContainsKey(BrainCloudConsts.JSON_STATISTICS))
                CachedStats = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_STATISTICS];
        }

        private void onMatchMakingEnabledSuccess(string jsonResponse, object cbObject)
        {
            PlayerData.MatchMakingEnabled = true;
        }

        private void onSummaryDataUpdated(string jsonResponse, object cbObject)
        {
            GDebug.Log("onSummaryDataUpdated " + jsonResponse);
        }

        private Dictionary<string, object> getFriendSummaryString()
        {
            Dictionary<string, object> toReturn = new Dictionary<string, object>();

            // create the friend summary data
            if (PlayerData != null)
            {
                toReturn[BrainCloudConsts.JSON_RANK] = PlayerData.PlayerRank;
            }
            return toReturn;
        }

        private void OnGetIdentitiesSuccess(string in_json, object cbObject)
        {
            // read the GetIdentities success response
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_json);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];

            if (jsonData.ContainsKey(BrainCloudConsts.JSON_IDENTITIES))
            {
                Dictionary<string, object> jsonIdentities = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_IDENTITIES];

                if (jsonIdentities.ContainsKey(BrainCloudConsts.JSON_IDENTITY_EMAIL))
                {
                    PlayerData.PlayerEmail = ((string)jsonIdentities[BrainCloudConsts.JSON_IDENTITY_EMAIL]);
                }

                PlayerData.FacebookUserId = "";
                PlayerData.UniversalId = "";

                if (jsonIdentities.ContainsKey(BrainCloudConsts.JSON_IDENTITY_FACEBOOK))
                {
                    PlayerData.FacebookUserId = ((string)jsonIdentities[BrainCloudConsts.JSON_IDENTITY_FACEBOOK]);
                }

                if (jsonIdentities.ContainsKey(BrainCloudConsts.JSON_IDENTITY_UNIVERSAL))
                {
                    PlayerData.UniversalId = ((string)jsonIdentities[BrainCloudConsts.JSON_IDENTITY_UNIVERSAL]);
                }

                if (jsonIdentities.ContainsKey(BrainCloudConsts.JSON_IDENTITY_STEAM))
                {
                    PlayerData.SteamId = ((string)jsonIdentities[BrainCloudConsts.JSON_IDENTITY_STEAM]);
                }

                // This code is blocked for UnityEditor only to avoid having to enter a FB UserToken everytime we run the app.
                // Be aware that the negative impact of this is that the FB Login state won't reflect the real status of the player.
#if !UNITY_EDITOR && !UNITY_WEBGL && !UNITY_STANDALONE && !UNITY_PS4
                /*
                // if they have a FB user ID attached try logging in
                if (PlayerData.FacebookUserId != "" && !FB.IsLoggedIn)
                {
                    LoginFacebook();
                }
                */
#endif
                GEventManager.TriggerEvent(GEventManager.ON_IDENTITIES_UPDATED);
            }
        }

        private void onClaimRewardSuccess(string in_jsonString, object cb_object)
        {
            // Claim Reward succeeded so we can now Delete this BC event.
            DeleteBCEvent((BaseBrainCloudEventData)cb_object);

            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_jsonString);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];

            if (jsonData.ContainsKey(BrainCloudConsts.JSON_CLAIM_REWARD_REWARD_DETAILS))
            {
                Dictionary<string, object> rewardDetails = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_CLAIM_REWARD_REWARD_DETAILS];
                if (rewardDetails.Count > 0)
                {
                    if (rewardDetails.ContainsKey(BrainCloudConsts.JSON_CLAIM_REWARD_TOURNAMENTS))
                    {
                        object[] tournaments = ((object[])rewardDetails[BrainCloudConsts.JSON_CLAIM_REWARD_TOURNAMENTS]);
                        if (tournaments.Length > 0)
                        {
                            /*
                            Dictionary<string, object> tourneyDict = (Dictionary<string, object>)tournaments[0];
                            BrainCloudUNETExample.GiftRewardData rewardData = new BrainCloudUNETExample.GiftRewardData();
                            rewardData.InitFromTournamentInfo(tourneyDict);

                            // TODO: make this not associate with the active client!
                            // Display the ExhibitionResults dialog
                            GStateManager.InitializeDelegate init = null;
                            init = (BaseState state) =>
                            {
                                BrainCloudUNETExample.TournamentRewardSubState exhibitionResultsSubState = state as BrainCloudUNETExample.TournamentRewardSubState;
                                if (exhibitionResultsSubState)
                                {
                                    GStateManager.Instance.OnInitializeDelegate -= init;
                                    exhibitionResultsSubState.LateInit(rewardData);

                                    // Update the player's currencies and refresh the hud
                                    OnVirtualCurrencies(in_jsonString, null);
                                }
                            };

                            GStateManager.Instance.OnInitializeDelegate += init;
                            GStateManager.Instance.PushSubState(BrainCloudUNETExample.TournamentRewardSubState.STATE_NAME);
                            */
                        }
                    }
                }
            }
        }

        private void onClaimRewardFailed(int statusCode, int reasonCode, string in_stringData, object in_obj)
        {
            //TODO: Handle errors
            switch (reasonCode)
            {
                case ReasonCodes.NO_LEADERBOARD_FOUND:
                case ReasonCodes.PLAYER_NOT_ENROLLED_IN_TOURNAMENT:
                case ReasonCodes.TOURNAMENT_REWARDS_ALREADY_CLAIMED:
                    DeleteBCEvent((BaseBrainCloudEventData)in_obj);
                    break;
            }
        }

        private void onGetBCEventsSuccess(string in_jsonString, object cb_object)
        {
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_jsonString);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];

            if (jsonData.ContainsKey(BrainCloudConsts.JSON_EVENT_INCOMING_EVENTS))
            {
                object[] incoming_events = ((object[])jsonData[BrainCloudConsts.JSON_EVENT_INCOMING_EVENTS]);
                if (incoming_events.Length > 0)
                {
                    BaseBrainCloudEventData baseBrainCloudEventData = null;
                    for (int i = 0; i < incoming_events.Length; ++i)
                    {
                        Dictionary<string, object> incomingEvent = (Dictionary<string, object>)incoming_events[i];
                        if (incomingEvent.ContainsKey(BrainCloudConsts.JSON_EVENT_EVENT_DATA))
                        {
                            baseBrainCloudEventData = new BaseBrainCloudEventData();
                            Dictionary<string, object> eventData = (Dictionary<string, object>)incomingEvent[BrainCloudConsts.JSON_EVENT_EVENT_DATA];

                            if (eventData.Count > 0)
                            {
                                if (eventData.ContainsKey(BrainCloudConsts.JSON_EVENT_LEADERBOARD_ID))
                                {
                                    baseBrainCloudEventData.leaderboardId = ((string)eventData[BrainCloudConsts.JSON_EVENT_LEADERBOARD_ID]);
                                    baseBrainCloudEventData.versionId = ((int)eventData[BrainCloudConsts.JSON_EVENT_VERSION_ID]);
                                }

                                baseBrainCloudEventData.createdAt = ((long)incomingEvent[BrainCloudConsts.JSON_EVENT_CREATED_AT]);
                                baseBrainCloudEventData.fromPlayerId = ((string)incomingEvent[BrainCloudConsts.JSON_EVENT_FROM_PLAYER_ID]);
                                baseBrainCloudEventData.toPlayerId = ((string)incomingEvent[BrainCloudConsts.JSON_EVENT_TO_PLAYER_ID]);
                                baseBrainCloudEventData.eventType = ((string)incomingEvent[BrainCloudConsts.JSON_EVENT_EVENT_TYPE]);
                                baseBrainCloudEventData.evId = ((string)incomingEvent[BrainCloudConsts.JSON_EVENT_EV_ID]);
                            }

                            m_baseBrainCloudEventDataList.Add(baseBrainCloudEventData);
                        }
                    }
                    GEventManager.TriggerEvent(GEventManager.ON_PROCESS_BC_EVENTS);
                }
            }
        }

        private void onGetBCEventsFailed(int statusCode, int reasonCode, string in_stringData, object in_obj)
        {
            //TODO: Handle errors
        }

        private void onDeleteBCEventSuccess(string in_jsonString, object cb_object)
        {
            //TODO: Handle success
        }

        private void onDeleteBCEventFailed(int statusCode, int reasonCode, string in_stringData, object in_obj)
        {
            //TODO: Handle errors
        }

        private BaseBrainCloudEventData GetNextAvailableBrainCloudEvent()
        {
            BaseBrainCloudEventData toReturn = null;
            if (m_baseBrainCloudEventDataList.Count > 0)
            {
                toReturn = m_baseBrainCloudEventDataList[0];
                m_baseBrainCloudEventDataList.RemoveAt(0);
            }
            return toReturn;
        }

        private void handleRetryCurrencyFail(int status, int reasonCode, string jsonError, object cbObject)
        {
            BCRetryData retryData = new BCRetryData(AwardPlayerCurrencyWithObject, cbObject);
            GCore.Instance.AddRetry(retryData, cbObject);
        }

        private void handleRetryConsumeFail(int status, int reasonCode, string jsonError, object cbObject)
        {
            BCRetryData retryData = new BCRetryData(ConsumePlayerCurrencyWithObject, cbObject);
            GCore.Instance.AddRetry(retryData, cbObject);
        }

        public void OnHideLoadingSpinner(string in_json, object in_obj)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
        }

        public void OnHideLoadingSpinner(int status, int reasonCode, string jsonError, object cbObject)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
        }

        public void UpdateActivity(string in_location, string in_status, string in_lobbyId, bool in_forceUpdate = false)
        {
            if (in_forceUpdate || !m_currentLocation.Equals(in_location) || !m_currentStatus.Equals(in_status))
            {
                m_currentLocation = in_location;
                m_currentStatus = in_status;

                Dictionary<string, object> activity = new Dictionary<string, object>();
                activity.Add(BrainCloudConsts.JSON_LOCATION, in_location);
                activity.Add(BrainCloudConsts.JSON_STATUS, in_status);
                activity.Add(BrainCloudConsts.JSON_LOBBY_ID, in_lobbyId);

                GCore.Wrapper.Client.PresenceService.UpdateActivity(JsonWriter.Serialize(activity));
            }
        }

        private void OnWebPurifyStringSuccess(string in_stringData, object in_obj)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            GDebug.Log(string.Format("Success | {0}", in_stringData));

            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_stringData);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
            Dictionary<string, object> jsonResponse = (Dictionary<string, object>)jsonData[BrainCloudConsts.JSON_RESPONSE];

            if ((int)jsonResponse["status"] == 200)
            {
                if (jsonResponse.ContainsKey("reason_code") && (int)jsonResponse["reason_code"] == ReasonCodes.NAME_CONTAINS_PROFANITY)
                {
                    OnWebPurifyStringError((int)jsonResponse["status"], (int)jsonResponse["reason_code"], "Name contains profanity.", null);
                }
                else
                {
                    // Name is valid, call the success callback.
                    string validString = "";
                    if (jsonResponse.ContainsKey("validString"))
                        validString = (string)jsonResponse["validString"];
                    if (m_validateStringSuccess != null)
                    {
                        m_validateStringSuccess(validString, null);
                        m_validateStringSuccess = null;
                    }
                }
            }
            else if ((int)jsonResponse["status"] == 400 || (int)jsonResponse["status"] == 500)
            {
                OnWebPurifyStringError((int)jsonResponse["status"], (int)jsonResponse["reason_code"], (string)jsonResponse["status_message"], null);
            }
        }

        private void OnWebPurifyStringError(int statusCode, int reasonCode, string in_stringData, object in_obj)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            GDebug.Log(string.Format("Failed | {0}  {1}  {2}", statusCode, reasonCode, in_stringData));

            switch (reasonCode)
            {
                case ReasonCodes.NAME_CONTAINS_PROFANITY:
                    HudHelper.DisplayMessageDialog("DISALLOWED NAME", "THIS NAME IS CONSIDERED INAPPROPRIATE.\n PLEASE ENTER ANOTHER ONE.", "OK");
                    break;
                case ReasonCodes.WEBPURIFY_NOT_CONFIGURED:
                    HudHelper.DisplayMessageDialog("WEBPURIFY ERROR", "WEBPURIFY NOT CONFIGURED, PLEASE TRY AGAIN.", "OK");
                    break;
                case ReasonCodes.WEBPURIFY_EXCEPTION:
                    HudHelper.DisplayMessageDialog("WEBPURIFY ERROR", "WEBPURIFY EXCEPTION, PLEASE TRY AGAIN.", "OK");
                    break;
                case ReasonCodes.WEBPURIFY_FAILURE:
                    HudHelper.DisplayMessageDialog("WEBPURIFY ERROR", "WEBPURIFY FAILURE, PLEASE TRY AGAIN.", "OK");
                    break;
                case ReasonCodes.WEBPURIFY_NOT_ENABLED:
                    HudHelper.DisplayMessageDialog("WEBPURIFY ERROR", "WEBPURIFY IS NOT ENABLED", "OK");
                    break;
            }
            if (m_validateStringFailure != null)
            {
                m_validateStringFailure(0, 0, "", null);
                m_validateStringFailure = null;
            }
        }

        public const string LOCATION_MAIN_MENU = "In Main Menu";
        public const string LOCATION_LOBBY = "In Lobby";
        public const string LOCATION_GAME = "Playing";

        public const string STATUS_IDLE = "Idle";
        public const string STATUS_PLAYING = "Playing";

        public const string PEER_NAME = "gameloot";
        public const string PARENT_NAME = "master";

        private string m_AppUpgradeURL = "";
        private string m_currentLocation = "";
        private string m_currentStatus = "";
        private List<BaseBrainCloudEventData> m_baseBrainCloudEventDataList = new List<BaseBrainCloudEventData>();
        private const float UPDATE_SIMULATED_TIME = 0.5f;

        private SuccessCallback m_validateStringSuccess = null;
        private FailureCallback m_validateStringFailure = null;

        public static int MIN_CHARACTERS_GAME_NAME = 3;
        public static int MAX_CHARACTERS_GAME_NAME = 25;
        #endregion
    }
}
