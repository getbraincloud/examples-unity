using Gameframework;
using UnityEngine;
using UnityEngine.UI;
using BrainCloud;
using BrainCloudUnity;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;

namespace BrainCloudUNETExample
{
    public class MainOptionsSubState : BaseSubState
    {
        public static string STATE_NAME = "mainOptionsSubState";

        public Text GlobalVolumeDisplayText = null;
        public Text MusicVolumeDisplayText = null;
        public Text SoundVolumeDisplayText = null;

        public Text NotificationsText = null;
        public Text FacebookButtonText = null;
        public Text VersionText = null;
        public GameObject EmailSection = null;
        public Toggle ShowToolsToggle = null;
        public InputField emailInputField = null;

        [SerializeField]
        private Button EditNameNavButton = null;
        [SerializeField]
        private Button DoneButton = null;
        [SerializeField]
        private Button LogOutButton = null;
        [SerializeField]
        private Button ConfirmButton = null;
        [SerializeField]
        private Button ViewButton = null;

        #region BaseState
        // Use this for initialization
        protected override void Start()
        {
            _stateInfo = new StateInfo(STATE_NAME, this);
            base.Start();

            VersionText.text = BrainCloudSettingsManual.Instance.GameVersion;

            /*
            GlobalVolumeDisplayText.transform.parent.parent.GetComponent<Slider>().value = GSoundMgr.Instance.GlobalVolume;
            MusicVolumeDisplayText.transform.parent.parent.GetComponent<Slider>().value = GSoundMgr.Instance.MusicVolume;
            SoundVolumeDisplayText.transform.parent.parent.GetComponent<Slider>().value = GSoundMgr.Instance.EffectVolume;
            */
            m_bPlayEffectOnTouchUp = false;
            updateHud();

#if UNITY_WEBGL || UNITY_STANDALONE
            emailInputField.onEndEdit.AddListener(delegate { OnEndEditHelper(); });
#endif

            GStateManager.Instance.EnableLoadingSpinner(false);
            SetupCustomNavigation();
        }

        protected override void OnResumeStateImpl(bool wasPaused)
        {
            updateHud();
        }

#if UNITY_WEBGL || UNITY_STANDALONE
        private void OnEndEditHelper()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                OnConfirmEmailAddress();
        }
#endif

        protected void Update()
        {
            if (m_bPlayEffectOnTouchUp && Input.GetMouseButtonUp(0))
            {
                playClickEffect();
                m_bPlayEffectOnTouchUp = false;
            }

            if (emailInputField.isFocused && (Input.GetKeyDown(KeyCode.Tab)))
            {
                OnConfirmEmailAddress();
            }
        }
        #endregion

        #region Public
        public void OnControlsButton()
        {
            GStateManager.Instance.PushSubState(ControlsSubState.STATE_NAME);
        }

        public void OnLogoutButton()
        {
            onLogoutConfirm();
        }

        public void OnMasterVolume(Slider in_slider)
        {
            m_bPlayEffectOnTouchUp = true;
            GSoundMgr.Instance.GlobalVolume = in_slider.value;
            updateHud();
        }

        public void OnEffectVolume(Slider in_slider)
        {
            m_bPlayEffectOnTouchUp = true;
            GSoundMgr.Instance.EffectVolume = in_slider.value;
            updateHud();
        }

        public void OnMusicVolume(Slider in_slider)
        {
            GSoundMgr.Instance.MusicVolume = in_slider.value;
            updateHud();
        }

        public void OnRequestPermissionForNotification()
        {
            GStateManager.Instance.PushSubState(EnableNotificationsSubState.STATE_NAME);
        }

        public void OnCloseDialog()
        {
            GSoundMgr.Instance.SavePlayerSettings();
            ExitSubState();
        }

        public void OnFacebookConnectButton()
        {
            if (GStateManager.Instance.CurrentStateId == MainMenuState.STATE_NAME)// && FB.IsLoggedIn)
                OnFacebookDisconnect();
            else
                OnLoginFacebook();
        }

        public static void OnLoginFacebook()
        {
            GPlayerMgr.Instance.LoginFacebook(OnMergeEmailPasswordSuccess, OnAttachIdentityFail);
        }

        public void OnFacebookDisconnect()
        {
            OnFacebookDisconnectPrompt();
        }

        public void onDetachIdentity(string in_response, object in_obj)
        {
            //GEventManager.TriggerEvent(GCasinoConfigManager.ON_FACEBOOK_STATUS_CHANGED);
            updateHud();
            HudHelper.DisplayMessageDialog("FACEBOOK", "SUCCESSFULLY DISCONNECTED FROM FACEBOOK.", "OK");
        }

        public void updateHud()
        {
            /*
            FacebookButtonText.transform.parent.parent.gameObject.SetActive(false);
            if (GStateManager.Instance.CurrentStateId == MainMenuState.STATE_NAME)// && FB.IsLoggedIn)
                FacebookButtonText.text = "DISCONNECT";
            else
                FacebookButtonText.text = "CONNECT";
                */

            bool bIsPeerConnected = GPlayerMgr.Instance.PlayerData.IsPeerConnected;
            /*
            GlobalVolumeDisplayText.text = HudHelper.QuickRound(GSoundMgr.Instance.GlobalVolume * 100).ToString();
            MusicVolumeDisplayText.text = HudHelper.QuickRound(GSoundMgr.Instance.MusicVolume * 100).ToString();
            SoundVolumeDisplayText.text = HudHelper.QuickRound(GSoundMgr.Instance.EffectVolume * 100).ToString();
            */

            if (GSettingsMgr.PushAuthorized)
            {
                NotificationsText.text = "ENABLED";
            }
            else
            {
                NotificationsText.text = "DISABLED";
            }

            ShowToolsToggle.isOn = GPlayerMgr.Instance.PlayerData.IsTester;
            EmailSection.SetActive(GPlayerMgr.Instance.PlayerData.IsTester);
            emailInputField.text = GPlayerMgr.Instance.PlayerData.PlayerEmail;
        }

        public void InputFieldChanged()
        {
            Text text = emailInputField.GetComponentInChildren<Text>();
            text.color = LIGHT_TEXT;
        }

        public void FinishedEditing()
        {
            EditNameNavButton.gameObject.SetActive(true);
            EditNameNavButton.Select();
            emailInputField.interactable = false;
        }

        public void EditNameNav()
        {
            emailInputField.interactable = true;
            emailInputField.Select();
            EditNameNavButton.gameObject.SetActive(false);
        }

        public void OnShowDeveloperTools()
        {
            EmailSection.SetActive(ShowToolsToggle.isOn);
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[BrainCloudConsts.JSON_IS_TESTER] = ShowToolsToggle.isOn;
            GCore.Wrapper.ScriptService.RunScript("SetIsTester", JsonWriter.Serialize(data));
            GPlayerMgr.Instance.PlayerData.IsTester = ShowToolsToggle.isOn;

            SetupCustomNavigation();
        }

        public void OnConfirmEmailAddress()
        {
            GCore.Wrapper.PlayerStateService.UpdateContactEmail(emailInputField.text, onUpdateContactEmailSuccess);
        }
        #endregion

        #region Private
        private void onUpdateContactEmailSuccess(string jsonResponse, object cbObject)
        {
            GPlayerMgr.Instance.PlayerData.PlayerEmail = emailInputField.text;
        }

        private void SetupCustomNavigation()
        {
            Navigation toggleNav = ShowToolsToggle.navigation;
            Navigation logoutNav = LogOutButton.navigation;
            Navigation doneNav = DoneButton.navigation;
            if (ShowToolsToggle.isOn)
            {
                // Set Nav when Dev Tools are visible
                toggleNav.selectOnDown = EditNameNavButton;
                logoutNav.selectOnDown = ShowToolsToggle;
                doneNav.selectOnUp = EditNameNavButton;
                //doneNav.selectOnLeft = EditNameNavButton;
            }
            else
            {
                // Set Nav when Dev Tools are hidden
                toggleNav.selectOnDown = DoneButton;
                logoutNav.selectOnDown = ShowToolsToggle;
                doneNav.selectOnUp = ShowToolsToggle;
                //doneNav.selectOnLeft = ShowToolsToggle;
            }
            ShowToolsToggle.navigation = toggleNav;
            LogOutButton.navigation = logoutNav;
            DoneButton.navigation = doneNav;
        }

        private void OnFacebookDisconnectPrompt()
        {
            HudHelper.DisplayMessageDialogTwoButton("FACEBOOK",
                "ARE YOU SURE THAT YOU WANT TO DISCONNECT FROM FACEBOOK?\n",
                "CANCEL", null, "CONFIRM", onDisconnectFromFacebook,
                GenericMessageSubState.eButtonColors.GREEN, GenericMessageSubState.eButtonColors.RED,
                true, null);
        }

        private void onDisconnectFromFacebook()
        {
            GPlayerMgr.Instance.LogoutFacebook(true, onDetachIdentity);
        }

        private static void ConnectFBAccount()
        {
            /*AccessToken aToken = AccessToken.CurrentAccessToken
            GPlayerMgr.Instance.ConnectToFacebookIdentity(aToken.UserId, aToken.TokenString,
                OnMergeEmailPasswordSuccess, OnMergeEmailPasswordFail, OnSmartSwitchSuccess, OnSmartSwitchFail);
                */
        }

        private static void OnAttachIdentityFail(int status, int reasonCode, string jsonError, object cbObject)
        {
            ConnectFBAccount();
        }

        private static void OnMergeEmailPasswordFail(int status, int reasonCode, string jsonError, object cbObject)
        {
            GDebug.Log("merge failed " + reasonCode + " " + jsonError);
        }

        private static void OnSmartSwitchFail(int status, int reasonCode, string jsonError, object cbObject)
        {
            OnMergeEmailPasswordFail(status, reasonCode, jsonError, cbObject);
            //Handle Fail
            GDebug.Log("SmartSwitchToOtherAccount failed " + reasonCode + " " + jsonError);
        }

        private static void OnMergeEmailPasswordSuccess(string jsonResponse, object cbObject)
        {
        }

        private static void OnSmartSwitchSuccess(string jsonResponse, object cbObject)
        {
        }

        private static void onReadSuccess(string jsonResponse, object cbObject)
        {
            //GEventManager.TriggerEvent(GCasinoConfigManager.ON_FACEBOOK_STATUS_CHANGED);
            HudHelper.DisplayMessageDialog("ACCOUNT CONNECTED SUCCESSFULLY!", "YOUR ACCOUNT HAS BEEN SUCCESSFULLY CONNECTED!", "OK");
        }

        private static void onSwitchReadSuccess(string jsonResponse, object cbObject)
        {
            HudHelper.DisplayMessageDialog("LOGIN SUCCESS!", "YOU SUCCESSFULLY LOGGED INTO YOUR ACCOUNT. HAVE FUN!", "OK");
        }

        private static void OnAttachFacebookIdentityFail(int status, int reasonCode, string jsonError, object cbObject)
        {
            //Handle Fail
            GDebug.Log("attach failed " + reasonCode + " " + jsonError);
            switch (reasonCode)
            {
                case ReasonCodes.DUPLICATE_IDENTITY_TYPE:
                    // Account already attached
                    break;
                case ReasonCodes.MERGE_PROFILES:
                    ConnectFBAccount();
                    break;
            }
        }

        private void ConfirmLogout()
        {
            HudHelper.DisplayMessageDialogTwoButtonWithInfoBox("LOGOUT", "PLEASE CONFIRM THAT YOU WANT TO LOGOUT.",
                "ALL PROGRESS AND CURRENCIES ON THIS DEVICE WILL BE RESET TO LEVEL 1. YOUR PROGRESS AND CURRENCIES REMAIN WITH YOUR ACCOUNT (BOMBER ID/PASSWORD, OR ANOTHER YOU HAVE ATTACHED).",
                "CANCEL", null, "LOGOUT", onLogout,
                GenericMessageSubState.eButtonColors.GREEN, GenericMessageSubState.eButtonColors.RED);
        }

        private void onLogoutConfirm()
        {
            Invoke("ConfirmLogout", 0.1f);
        }

        private void onLogout()
        {
            GCore.Wrapper.PlayerStateService.Logout(onPlayerLoggedOut);
        }

        private void onPlayerLoggedOut(string in_str, object obj)
        {
            HudHelper.DisplayMessageDialog("LOGGED OUT", "YOU HAVE BEEN LOGGED OUT.",
                "OK", onLoggedOut);
        }

        private void onLoggedOut()
        {
            GCore.Instance.ResetAfterLogout();
            GStateManager.Instance.ChangeState(SplashState.STATE_NAME);
        }

        private void playClickEffect()
        {
            GSoundMgr.PlaySound("commonClick");
        }

        private void DetachPeerProfile()
        {
            //GCore.Wrapper.Client.IdentityService.DetachPeer(GCasinoConfigManager.GAME_LOOT_PEER_CODE, OnDetachPeerProfileSuccess);
        }

        private void OnDetachPeerProfileSuccess(string in_jsonString, object in_obj)
        {
            GPlayerMgr.Instance.PlayerData.IsPeerConnected = false;
            updateHud();
        }

        private void OnDetachPeerProfileFailure(string in_jsonString, object in_obj)
        {
            HudHelper.DisplayMessageDialog("ERROR", "AN ERROR HAS OCURRED, PLEASE TRY AGAIN.", "OK");
        }

        bool m_bPlayEffectOnTouchUp = false;
        private Color LIGHT_TEXT = new Color(224, 193, 154, 255);
        #endregion
    }
}
