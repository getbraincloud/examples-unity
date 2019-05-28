﻿using Gameframework;
using BrainCloud;
using BrainCloud.Common;
using UnityEngine;
using UnityEngine.UI;
namespace BrainCloudUNETExample
{
    public class ConnectingSubState : BaseSubState
    {
        public static string STATE_NAME = "connectingSubState";

        public InputField UsernameBox = null;
        public InputField PasswordBox = null;

        public Sprite IconEmail = null;
        public Sprite IconPilot = null;

        public GameObject ButtonGroup = null;
        public GameObject ButtonGroupWithCancel = null;
        public Text InstructionText = null;
        public Text LoginButtonText = null;

        [SerializeField]
        private GameObject Panel = null;

        private static string ms_instructionText = "";
        private static string ms_buttonText = "";
        private static bool ms_canCancel = false;

        public static void PushConnectingSubState(string in_instructionText, string in_buttonText, bool in_canCancel = true)
        {
            GStateManager stateMgr = GStateManager.Instance;
            stateMgr.OnInitializeDelegate += onPushConnectingSubStateLoaded;
            ms_instructionText = in_instructionText;
            ms_buttonText = in_buttonText;
            ms_canCancel = in_canCancel;
            stateMgr.PushSubState(STATE_NAME);
        }

        private static void onPushConnectingSubStateLoaded(BaseState in_state)
        {
            GStateManager stateMgr = GStateManager.Instance;
            if (in_state as ConnectingSubState)
            {
                stateMgr.OnInitializeDelegate -= onPushConnectingSubStateLoaded;
                ((ConnectingSubState)in_state).Init(ms_instructionText, ms_buttonText, ms_canCancel);
            }
        }

        #region BaseState
        protected override void Start()
        {
            _stateInfo = new StateInfo(STATE_NAME, this);
            base.Start();

            if (GCore.IsFreshLaunch)
            {
                GStateManager.Instance.EnableLoadingSpinner(true);
                authenticateBraincloud();
            }
            else
            {
                GStateManager.Instance.EnableLoadingSpinner(false);
                Panel.SetActive(true);
            }
            updateViewDisplay();
        }
        override public void ExitSubState()
        {
            MainMenuState menu = GStateManager.Instance.CurrentState as MainMenuState;
            if (menu != null && !GPlayerMgr.Instance.IsUniversalIdAttached())
            {
                // UniversalID wasn't attached so restore the user's name to its previous value.
                menu.RestoreName();
            }
            base.ExitSubState();
        }
        #endregion

        #region Public
        public void OnLoginButton()
        {
            if (ValidateUserName())
            {
                if (m_defaultAuthType == AuthenticationType.Universal)
                {
                    GCore.Wrapper.IdentityService.AttachUniversalIdentity(UsernameBox.text, PasswordBox.text, onAttachSuccess, onAuthFail);
                    m_lastAuthType = AuthenticationType.Universal;
                }
                else if (m_defaultAuthType == AuthenticationType.Email)
                {
                    GCore.Wrapper.IdentityService.AttachEmailIdentity(UsernameBox.text, PasswordBox.text, onAttachSuccess, onAuthFail);
                    m_lastAuthType = AuthenticationType.Email;
                }
                // Add support for other Authentication types here
            }
        }

        public void OnCancelButton()
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            ExitSubState();
        }

        public void setAuthenticationType(AuthenticationType in_authType)
        {
            m_defaultAuthType = in_authType;
            updateViewDisplay();
        }
        #endregion

        #region Private
        private void Init(string in_instructionText, string in_buttonText, bool in_canCancel)
        {
            InstructionText.text = in_instructionText;
            LoginButtonText.text = in_buttonText;
            ButtonGroup.SetActive(!in_canCancel);
            ButtonGroupWithCancel.SetActive(in_canCancel);
        }

        private void Update()
        {
            if (UsernameBox.isFocused && (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Return)))
            {
                UsernameBox.DeactivateInputField();
                PasswordBox.ActivateInputField();
                PasswordBox.Select();
            }
            else if (PasswordBox.isFocused && (Input.GetKeyDown(KeyCode.Tab)))
            {
                PasswordBox.DeactivateInputField();
                UsernameBox.ActivateInputField();
                UsernameBox.Select();
            }
        }

        // call this when you want to close down the state
        private void onCompleteConnectingSubState()
        {
            GCore.Wrapper.SetStoredProfileId(GCore.Wrapper.Client.ProfileId);
            GCore.Wrapper.SetStoredAnonymousId(GCore.Wrapper.Client.AuthenticationService.AnonymousId);

            GStateManager.Instance.EnableLoadingSpinner(false);
            GStateManager.Instance.PopSubState(_stateInfo);

            if (GCore.IsFreshLaunch)
            {
                GCore.IsFreshLaunch = false;
            }

            GStateManager.Instance.ChangeState(MainMenuState.STATE_NAME);
        }

        private void authenticateBraincloud()
        {
            GCore.Wrapper.AuthenticateAnonymous(onAuthSuccess, onAuthFail);
        }

        private void onAuthSuccess(string jsonResponse, object cbObject)
        {
            // pass off to the PlayerMgr
            GPlayerMgr.Instance.OnAuthSuccess(jsonResponse, cbObject);
            onConnectComplete();
        }

        private void onAttachSuccess(string jsonResponse, object cbObject)
        {
            GPlayerMgr.Instance.PlayerData.PlayerName = UsernameBox.text;
            if (m_lastAuthType == AuthenticationType.Universal)
            {
                GPlayerMgr.Instance.PlayerData.UniversalId = UsernameBox.text;
                GCore.Wrapper.Client.PlayerStateService.UpdateUserName(UsernameBox.text);
            }
            GCore.Instance.ProcessRetryQueue();
            onCompleteConnectingSubState();
        }

        private void displayPlayerInMatchDisplay()
        {
            GStateManager.Instance.EnableLoadingSpinner(true);
            Invoke("authenticateBraincloud", 15.0f);
        }

        private void displayPlayerInMatchMessage()
        {
            HudHelper.DisplayMessageDialog("HOLD ON", "YOUR TEAM IS CURRENTLY COMPETING AGAINST ANOTHER AT THE MOMENT.  RETRYING.", "OK", displayPlayerInMatchDisplay);
        }

        private void displayTokenMisMatchMessage()
        {
            if (m_lastAuthType == AuthenticationType.Universal)
                HudHelper.DisplayMessageDialog("AUTHENTICATION ERROR", "THE USERNAME AND PASSWORD COMBINATION DO NOT MATCH.  PLEASE TRY AGAIN.", "OK");
            else if (m_lastAuthType == AuthenticationType.Email)
                HudHelper.DisplayMessageDialog("AUTHENTICATION ERROR", "THE EMAIL AND PASSWORD COMBINATION DO NOT MATCH.  PLEASE TRY AGAIN.", "OK");

            // Add support for other Authentication types here
        }

        private void displayDuplicateIdentityTypeMessage()
        {
            HudHelper.DisplayMessageDialog("WARNING", "THIS NAME IS ALREADY TAKEN, PLEASE TRY ANOTHER ONE.", "OK");
        }

        private void onAuthFail(int status, int reasonCode, string jsonError, object cbObject)
        {
            // pass off to the PlayerMgr
            if (GPlayerMgr.Instance.onAuthFail(status, reasonCode, jsonError, cbObject))
                return;

            switch (reasonCode)
            {
                case ReasonCodes.TOKEN_DOES_NOT_MATCH_USER:
                    {
                        displayTokenMisMatchMessage();
                    }
                    break;
                case ReasonCodes.PLAYER_IN_MATCH:
                    {
                        displayPlayerInMatchMessage();
                    }
                    break;
                case ReasonCodes.UNABLE_TO_VALIDATE_PLAYER:
                case ReasonCodes.PLAYER_SESSION_EXPIRED:
                case ReasonCodes.NO_SESSION:
                case ReasonCodes.PLAYER_SESSION_LOGGED_OUT:
                    {
                        authenticateBraincloud();
                    }
                    break;
                case ReasonCodes.SWITCHING_PROFILES:
                case ReasonCodes.MISSING_IDENTITY_ERROR:
                    {
                        // lets clear the info. and reauth
                        GCore.Wrapper.ResetStoredProfileId();
                        GCore.Wrapper.ResetStoredAnonymousId();

                        authenticateBraincloud();
                    }
                    break;
                case ReasonCodes.MERGE_PROFILES:
                    OnMergeIdentity();
                    break;
                case ReasonCodes.DUPLICATE_IDENTITY_TYPE:
                case ReasonCodes.NEW_CREDENTIAL_IN_USE:
                    displayDuplicateIdentityTypeMessage();
                    break;
                default:
                    break;
            }
        }

        private void onAttachSteamAccount(string in_response, object obj)
        {
            GStateManager.Instance.EnableLoadingSpinner(true);
            // lets try a full re-auth afterwards
            authenticateBraincloud();
        }

        private void onConnectComplete()
        {
            // check the attached identities
#if !STEAMWORKS_ENABLED
            if (GPlayerMgr.Instance.IsEmailAttached())
            {
                GCore.Instance.ProcessRetryQueue();
                onCompleteConnectingSubState();
            }
            else
#endif
            {
                GStateManager.Instance.EnableLoadingSpinner(true);
                GEventManager.StartListening(GEventManager.ON_IDENTITIES_UPDATED, onIdentitiesUpdated);
            }
        }

        private void onIdentitiesUpdated()
        {
            GEventManager.StopListening(GEventManager.ON_IDENTITIES_UPDATED, onIdentitiesUpdated);
#if STEAMWORKS_ENABLED
            if (!GPlayerMgr.Instance.IsSteamIdAttached())
            {
                GSteamAuthManager.Instance.AttachSteamAccount(true, onAttachSteamAccount, onAuthFail);
                m_lastAuthType = AuthenticationType.Steam;
            }
            else 
#endif
            if (GPlayerMgr.Instance.IsUniversalIdAttached())
            {
                // universal IS ATTACHED
                GCore.Instance.ProcessRetryQueue();
                onCompleteConnectingSubState();
            }
            else
            {
                GStateManager.Instance.EnableLoadingSpinner(false);
                Panel.SetActive(true);
            }
        }

        private void OnMergeIdentity()
        {
            GStateManager.Instance.EnableLoadingSpinner(true);

            if (m_lastAuthType == AuthenticationType.Universal)
                GCore.Wrapper.IdentityService.MergeUniversalIdentity(UsernameBox.text, PasswordBox.text, onMergeIdentitySuccess, onAuthFail);
            else if (m_lastAuthType == AuthenticationType.Email)
                GCore.Wrapper.IdentityService.MergeEmailIdentity(UsernameBox.text, PasswordBox.text, onMergeIdentitySuccess, onAuthFail);
            else if (m_lastAuthType == AuthenticationType.Steam)
                GSteamAuthManager.Instance.MergeSteamAccount(onMergeIdentitySuccess, onAuthFail);

            // Add support for other Authentication types here
        }

        private void onMergeIdentitySuccess(string jsonResponse, object cbObject)
        {
            GPlayerMgr.Instance.ReadPlayerState(onReadPlayerStateAfterMergeIdentitySuccess);
        }

        private void onReadPlayerStateAfterMergeIdentitySuccess(string jsonResponse, object cbObject)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            GCore.Instance.ProcessRetryQueue();
            onCompleteConnectingSubState();
        }

        private bool ValidateUserName()
        {
            UsernameBox.text = UsernameBox.text.Trim();
            if (UsernameBox.text.Length < MIN_CHARACTERS)
            {
                HudHelper.DisplayMessageDialog("DISALLOWED NAME", "THE NAME MUST BE AT LEAST " + MIN_CHARACTERS + " CHARACTERS LONG.", "OK");
                UsernameBox.text = GPlayerMgr.Instance.PlayerData.PlayerName;
                return false;
            }
            return true;
        }

        private void updateViewDisplay()
        {
            UsernameBox.characterLimit = MAX_CHARACTERS;
            PasswordBox.characterLimit = MAX_CHARACTERS;

            if (m_defaultAuthType == AuthenticationType.Universal)
            {
                UsernameBox.transform.Find("Placeholder").GetComponent<Text>().text = "Enter Username";
                UsernameBox.transform.Find("Icon").GetComponent<Image>().sprite = IconPilot;
                UsernameBox.contentType = InputField.ContentType.Alphanumeric;
            }
            else if (m_defaultAuthType == AuthenticationType.Email)
            {
                UsernameBox.transform.Find("Placeholder").GetComponent<Text>().text = "Enter Email";
                UsernameBox.transform.Find("Icon").GetComponent<Image>().sprite = IconEmail;
                UsernameBox.contentType = InputField.ContentType.EmailAddress;
            }
            // Add support for other Authentication types here
        }
        #endregion
        private int MIN_CHARACTERS = 3;
        private int MAX_CHARACTERS = 25;
        private AuthenticationType m_lastAuthType = AuthenticationType.Anonymous;
        private AuthenticationType m_defaultAuthType = AuthenticationType.Universal;
    }
}
