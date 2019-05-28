using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using System;
using BrainCloudUnity;
using System.Text;
using System.Linq;

namespace Gameframework
{
    public class GCore : SingletonBehaviour<GCore>
    {
        public static bool IsFreshLaunch = true;
        public static BrainCloudWrapper Wrapper
        {
            get { return Instance.m_wrapper; }
        }

        #region Publics
        public bool IsInitialized
        {
            get { return m_bInitialized; }
        }

#if UNITY_ANDROID
        public void Update()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                if (GStateManager.Instance.IsLoadingState ||
                    GStateManager.Instance.IsLoadingSubState || 
                    GStateManager.Instance.CurrentStateId == BrainCloudUNETExample.SplashState.STATE_NAME) return;

                if (GStateManager.Instance.CurrentSubStateId != GStateManager.UNDEFINED_STATE)
                {
                    GSoundMgr.PlaySound("commonClick");
                    GStateManager.Instance.PopCurrentSubState();
                }
                else
                {
                    onApplicationQuit();
                    //HudHelper.DisplayMessageDialogTwoButton("GLN SLOTS", "DO YOU WISH TO CLOSE GLN SLOTS?", "NO", null, "YES", onApplicationQuit, GenericMessageSubState.eButtonColors.GREEN, GenericMessageSubState.eButtonColors.RED);
                }
            }
        }

        private void onApplicationQuit()
        {
            Application.Quit();
        }
#endif

        public void EnsureMgrsAreSetup()
        {
            if (!IsInitialized && !m_bStarted)
            {
                m_bStarted = true;
                GameObject go = new GameObject();
                m_wrapper = go.AddComponent<BrainCloudWrapper>();
                m_wrapper.WrapperName = "mainWrapper";
                Dictionary<string, string> gameIdSecretKeyMap = new Dictionary<string, string>();
                gameIdSecretKeyMap[BrainCloudSettingsManual.Instance.GameId] = BrainCloudSettingsManual.Instance.SecretKey;
                m_wrapper.InitWithApps(BrainCloudSettingsManual.Instance.DispatcherURL, BrainCloudSettingsManual.Instance.GameId, gameIdSecretKeyMap, BrainCloudSettingsManual.Instance.GameVersion);
                go.transform.SetParent(transform);

                #if !DEBUG_LOG_ENABLED
                m_wrapper.Client.EnableLogging(true);
                #endif

                StartCoroutine(StartUpMgrs());
            }
        }

        public void ResetAfterLogout()
        {
            IsFreshLaunch = true;
            GPlayerMgr.Instance.ResetBrainCloudAuth();
            GStateManager.Instance.PopAllSubStatesTo(""); // clear all substates
        }
        #endregion

        #region Singleton
        void Start()
        {
            EnsureMgrsAreSetup();
#if !UNITY_EDITOR && UNITY_IOS
            UnityEngine.iOS.NotificationServices.CancelAllLocalNotifications();
#endif
        }

        public void OnApplicationPause(bool in_paused)
        {
            if (in_paused)
            {
                GSettingsMgr.Save();
            }
        }

        public void HandleBrainCloudFailError(int status, int reasonCode, string jsonError, object cbObject)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            switch (reasonCode)
            {
                case ReasonCodes.UNABLE_TO_VALIDATE_PLAYER:
                case ReasonCodes.PLAYER_SESSION_EXPIRED:
                case ReasonCodes.NO_SESSION:
                case ReasonCodes.PLAYER_SESSION_LOGGED_OUT:
                    {
                        HudHelper.DisplayMessageDialog("SESSION EXPIRED", "YOUR SESSION HAS EXPIRED. RE-AUTHENTICATING...", "OK", OnSessionExpiredDialogClose);
                    }
                    break;
            }
        }

        public void AddRetry(BCRetryData reqData, object cbObject)
        {
            m_retryDelegate.Add(reqData);
        }

        public void ProcessRetryQueue()
        {
            for (int i = m_retryDelegate.Count - 1; m_retryDelegate.Count > 0; --i)
            {
                m_retryDelegate[i].retryDelegate(m_retryDelegate[i]);
                m_retryDelegate.RemoveAt(i);
            }
        }

        public static bool ApplicationIsQuitting = false;
        public override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            ApplicationIsQuitting = true;
            GSettingsMgr.Save();
        }

        #endregion

        #region Private

        private void CreateLocalNotificationInDays(string aMessage, int aDaysUntilMessage)
        {
            CreateLocalNotification(aMessage, DateTime.Now.AddDays(aDaysUntilMessage));
        }

        private void CreateLocalNotification(string aMessage, DateTime aTime)
        {
#if !UNITY_EDITOR && UNITY_IOS
            UnityEngine.iOS.LocalNotification shortNotification = new UnityEngine.iOS.LocalNotification();
            shortNotification.fireDate = aTime;
            shortNotification.alertBody = aMessage;
            UnityEngine.iOS.NotificationServices.ScheduleLocalNotification(shortNotification);
#endif
        }
#if UNITY_ANDROID
        //public Firebase.FirebaseApp FireBase { get; private set; }
#endif
        private IEnumerator StartUpMgrs()
        {
            if (!m_bInitialized)
            {
                Physics.queriesHitTriggers = true;
                // Start up StateMgr
                yield return YieldFactory.GetWaitForEndOfFrame();
                GDebug.Log("StartupMgrs --- " + GStateManager.Instance.CurrentStateId);

                yield return YieldFactory.GetWaitForEndOfFrame();
                GEventManager.Instance.StartUp();

                yield return YieldFactory.GetWaitForEndOfFrame();
                GConfigManager.Instance.StartUp();

                yield return YieldFactory.GetWaitForEndOfFrame();
                GPlayerMgr.Instance.StartUp();

                // Register our Global BC Error handler
                yield return YieldFactory.GetWaitForEndOfFrame();

                m_wrapper.Client.EnableNetworkErrorMessageCaching(true);
                m_wrapper.Client.RegisterNetworkErrorCallback(onNetworkError);
                m_wrapper.Client.RegisterGlobalErrorCallback(HandleBrainCloudFailError);

                yield return YieldFactory.GetWaitForEndOfFrame();
                GSoundMgr.Instance.StartUp();

                yield return YieldFactory.GetWaitForEndOfFrame();
                GLevelManager.Instance.StartUp();

                yield return YieldFactory.GetWaitForEndOfFrame();
                GFriendsManager.Instance.StartUp();

#if UNITY_ANDROID
                /*
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Create and hold a reference to your FirebaseApp, i.e.
                FireBase = Firebase.FirebaseApp.DefaultInstance;
                // Set a flag here indicating that Firebase is ready to use by your
                // application.
            }
            else
            {
                GDebug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });
        */
#elif STEAMWORKS_ENABLED
                GSteamAuthManager.Instance.StartUp();
                GSteamAuthManager.Instance.SetupSteamManager();
                yield return YieldFactory.GetWaitForSeconds(0.15f);
#endif

            }

            yield return YieldFactory.GetWaitForEndOfFrame();
            m_bInitialized = true;
        }

        private void OnSessionExpiredDialogClose()
        {
            GStateManager.Instance.PushSubState(BrainCloudUNETExample.ConnectingSubState.STATE_NAME);
        }
        private void onNetworkError()
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            HudHelper.DisplayMessageDialog("ERROR", "COULD NOT CONNECT. PLEASE CHECK YOUR INTERNET CONNECTION AND TRY AGAIN.", "OKAY", retryConnection);
        }

        private void retryConnection()
        {
            GStateManager.Instance.EnableLoadingSpinner(true);
            m_wrapper.Client.RetryCachedMessages();
        }

        private bool m_bInitialized = false;
        private bool m_bStarted = false;

        private List<BCRetryData> m_retryDelegate = new List<BCRetryData>();
        private BrainCloudWrapper m_wrapper = null;
        #endregion
    }

    public struct BCRetryData
    {
        public BCRetryData(RetryDelegate in_delegate, object in_retryData, SuccessCallback in_success = null, FailureCallback in_failure = null)
        {
            retryDelegate = in_delegate;
            retryData = in_retryData;
            success = in_success;
            failure = in_failure;
        }
        public RetryDelegate retryDelegate;
        public object retryData;
        public SuccessCallback success;
        public FailureCallback failure;
    }
    public delegate void RetryDelegate(object cbObject);
}
