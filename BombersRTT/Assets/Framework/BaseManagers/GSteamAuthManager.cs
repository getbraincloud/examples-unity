using BrainCloud;
using System;
using System.Collections.Generic;
#if STEAMWORKS_ENABLED
using Steamworks;
#endif

namespace Gameframework
{
    public class GSteamAuthManager : SingletonBehaviour<GSteamAuthManager>
    {
        public bool IsSteamInitialized
        {
            get { return m_bSteamInitialized; }
        }

        public override void OnApplicationQuit()
        {
#if STEAMWORKS_ENABLED
            SteamAPI.Shutdown();
#endif
            base.OnApplicationQuit();
        }

        public void SetupSteamManager()
        {
#if STEAMWORKS_ENABLED
            if (SteamManager.Initialized)
            {
                m_bSteamInitialized = true;

                m_transactionCallback = Callback<MicroTxnAuthorizationResponse_t>.Create(OnTransactionResponse);
                m_gameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
                m_getAuthSessionTicketResponse = Callback<GetAuthSessionTicketResponse_t>.Create(OnGetAuthSessionTicketResponse);
            }
#endif
        }

        public void MergeSteamAccount(SuccessCallback in_success = null, FailureCallback in_fail = null, object in_obj = null)
        {
#if STEAMWORKS_ENABLED
            if (m_steamIdStr != "" && m_authToken != "")
            {
                GCore.Wrapper.IdentityService.MergeSteamIdentity(m_steamIdStr, m_authToken, onHandleAuthResponse + in_success, onHandleAuthResponseFail + in_fail, in_obj);
            }
#endif
            {
                in_fail(505, 999999, "STEAM NOT INITIALIZED", in_obj);
            }
        }

        public void AttachSteamAccount(bool in_bAttach = false, SuccessCallback in_success = null, FailureCallback in_fail = null, object in_obj = null)
        {
#if STEAMWORKS_ENABLED
            if (IsSteamInitialized)
            {
                m_bAttachSteam = in_bAttach;
                m_steamAuthSuccess = in_success;
                m_steamFailure = in_fail;
                m_steamObj = in_obj;

                m_ticket = new byte[1024];
                SteamUser.GetAuthSessionTicket(m_ticket, 1024, out m_ticketSize);
            }
            else if (in_fail != null)
#endif
            {
                in_fail(505, 999999, "STEAM NOT INITIALIZED", in_obj);
            }
        }
#if STEAMWORKS_ENABLED
        private void OnGetAuthSessionTicketResponse(GetAuthSessionTicketResponse_t pCallback)
        {
            CSteamID steamId = SteamUser.GetSteamID();
            if (pCallback.m_hAuthTicket != HAuthTicket.Invalid && m_ticketSize != 0)
            {
                m_steamIdStr = steamId.ToString();
                m_authToken = BitConverter.ToString(m_ticket, 0, (int)m_ticketSize).Replace("-", string.Empty);

                if (!m_bAttachSteam)
                {
                    GCore.Wrapper.AuthenticateSteam(m_steamIdStr, m_authToken, false, onHandleAuthResponse + m_steamAuthSuccess, m_steamFailure, m_steamObj);
                }
                else
                {
                    GCore.Wrapper.IdentityService.AttachSteamIdentity(m_steamIdStr, m_authToken, onHandleAuthResponse + m_steamAuthSuccess, m_steamFailure, m_steamObj);
                }
            }
        }

        private void onHandleAuthResponse(string json, object obj)
        {
            resetAuthInfo();
        }
        private void onHandleAuthResponseFail(int status, int reasonCode, string json, object obj)
        {
            resetAuthInfo();
        }
        private void resetAuthInfo()
        {
            m_authToken = "";
            m_steamIdStr = "";
        }

        private void OnTransactionResponse(MicroTxnAuthorizationResponse_t pCallback)
        {
            GIAPManager.Instance.FinalizeSteamPurchase();
        }

        private void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
        {
            GStateManager.Instance.CurrentState.SetUIEnabled(pCallback.m_bActive == 0);
        }
#pragma warning disable 414
        protected Callback<MicroTxnAuthorizationResponse_t> m_transactionCallback;
        protected Callback<GameOverlayActivated_t> m_gameOverlayActivated;
        private Callback<GetAuthSessionTicketResponse_t> m_getAuthSessionTicketResponse;

#pragma warning restore 414
#endif

#pragma warning disable 414

        private SuccessCallback m_steamAuthSuccess = null;
        private FailureCallback m_steamFailure = null;
        private object m_steamObj = null;
        private bool m_bAttachSteam = false;
        private uint m_ticketSize;
        private byte[] m_ticket;
        private string m_authToken = "";
        private string m_steamIdStr = "";

        private bool m_bSteamInitialized = false;
#pragma warning restore 414
    }
}
