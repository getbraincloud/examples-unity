using Gameframework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if FACEBOOK_ENABLED
using Facebook.Unity;
#endif

namespace BrainCloudUNETExample
{
    public class FriendCell : ImageDownloader
    {
        public static string SYSTEM_MESSAGE = "SYSTEM_MESSAGE";
        public const string DEFAULT_NAME = "DEFAULT NAME";

        #region public Properties
        public TextMeshProUGUI UserName;
        public TextMeshProUGUI Status;
        public GameObject AddButton;
        public GameObject RemoveButton;
        public Image PresenceIcon = null;
        public Sprite OnlineSprite = null;
        public Sprite OfflineSprite = null;
        public string ProfileId { get; private set; }

        [SerializeField]
        private PlayerRankIcon PlayerRankIcon = null;
        #endregion

        #region public
        public bool IsYou { get { return GCore.Wrapper.Client.ProfileId == ProfileId; } }

        public virtual void Init(PlayerData in_pData, bool in_addButton, bool in_removeButton)
        {
            m_data = in_pData;
            UpdateUI(m_data);

            bool IsYou = m_data.ProfileId == GPlayerMgr.Instance.PlayerData.ProfileId;
            // Only display the Add or Remove button for other players than yourself.
            AddButton.SetActive(!IsYou && in_addButton);
            RemoveButton.SetActive(!IsYou && in_removeButton);
            if (GStateManager.Instance.CurrentSubStateId == LobbySubState.STATE_NAME)
                gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(223, 34);
        }

        public virtual void UpdateUI(PlayerData in_pData)
        {
            m_data = in_pData;

            ProfileId = m_data.ProfileId;
            PlayerRankIcon.UpdateIcon(m_data.PlayerRank);
            m_originalUserName = m_data.PlayerName;
            UserName.text = IsYou ? "You" : m_data.PlayerName == "" ? DEFAULT_NAME : m_data.PlayerName;
            Status.text = m_data.Presence.IsOnline ? (m_data.Presence.Location.Length == 0 ? "Updating..." : m_data.Presence.Location) : "Offline";
            PresenceIcon.sprite = m_data.Presence.IsOnline ? OnlineSprite : OfflineSprite;
            updateLobbyInviteDisplay();
        }

        public void OnInviteToLobby()
        {
            if (m_inviteToLobbyObject != null)
            {
                if (m_data.Presence.Location != null && m_data.Presence.Location.Contains(GPlayerMgr.LOCATION_LOBBY))
                {
                    string[] list = { m_data.Presence.LobbyType };
                    GCore.Wrapper.LobbyService.GetRegionsForLobbies(list, getRegionsJoinLobby, null, "");

                }
                else
                {
                    GFriendsManager.Instance.OnInviteToLobby(ProfileId, m_originalUserName);
                }
            }
        }
        #endregion

        #region private
        private void getRegionsJoinLobby(string in_str, object obj)
        {
            GCore.Wrapper.LobbyService.PingRegions(onPingRegionJoinLobby, null, obj);
        }

        private void onPingRegionJoinLobby(string in_str, object obj)
        {
            Dictionary<string, object> playerExtra = new Dictionary<string, object>();
            playerExtra.Add("cxId", GCore.Wrapper.Client.RTTConnectionID);
            playerExtra.Add(GBomberRTTConfigManager.JSON_GOLD_WINGS, GPlayerMgr.Instance.GetCurrencyBalance(GBomberRTTConfigManager.CURRENCY_GOLD_WINGS) > 0 ? true : false);

            GCore.Wrapper.LobbyService.JoinLobby(m_data.Presence.LobbyId, true, playerExtra, "");

            GStateManager.Instance.PushSubState(JoiningGameSubState.STATE_NAME);
            GCore.Wrapper.RTTService.RegisterRTTLobbyCallback(BombersNetworkManager.Instance.LobbyCallback);
        }

        private void updateLobbyInviteDisplay()
        {
            Transform tempTrans = transform.FindDeepChild("InviteButton");
            if (tempTrans != null) m_inviteToLobbyObject = tempTrans.gameObject;
            if (m_inviteToLobbyObject != null) m_inviteToLobbyObject.SetActive(!IsYou && m_originalUserName != SYSTEM_MESSAGE && m_data.Presence.IsOnline && m_data.CanBeInvited());
        }

        private string m_originalUserName = "";
        private GameObject m_inviteToLobbyObject;
        private PlayerData m_data;
        #endregion
    }
}
