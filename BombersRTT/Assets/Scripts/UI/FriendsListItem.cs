using Gameframework;

namespace BrainCloudUNETExample
{
    public class FriendsListItem : FriendCell
    {
        #region Public
        public PlayerData ItemData { get; private set; }

        public void Init(RecentlyViewed in_pData, bool in_add, bool in_remove)
        {
            PlayerData playerData = new PlayerData();
            playerData.ProfileId = in_pData.ProfileId;
            playerData.PlayerName = in_pData.Name;

            ItemData = playerData;
            base.Init(ItemData, in_add, in_remove);
        }

        public override void Init(PlayerData in_pData, bool in_add, bool in_remove)
        {
            ItemData = in_pData;
            base.Init(ItemData, in_add, in_remove);
        }

        public override void UpdateUI(PlayerData in_pData)
        {
            ItemData = in_pData;
            base.UpdateUI(ItemData);
        }

        public void UpdateOnlineStatus(PresenceData in_presenceData)
        {
            ItemData.Presence = in_presenceData;
            UpdateUI(ItemData);
        }

        public void RefreshOnlineVisibility()
        {
            gameObject.SetActive(ItemData.Presence.IsOnline);
        }

        public void OnRemoveFriendButton()
        {
            GStateManager.Instance.EnableLoadingSpinner(true);

            string[] profileIds = { ItemData.ProfileId };
            GCore.Wrapper.Client.FriendService.RemoveFriends(profileIds, OnRemoveFriendsSuccess, OnRemoveFriendsError);
        }

        public void OnAddFriendButton()
        {
            GStateManager.Instance.EnableLoadingSpinner(true);

            string[] profileIds = { ItemData.ProfileId };
            GCore.Wrapper.Client.FriendService.AddFriends(profileIds, OnAddFriendsSuccess, OnAddFriendsError);
        }
        #endregion

        #region Private
        private void OnRemoveFriendsSuccess(string in_stringData, object in_obj)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            GDebug.Log(string.Format("Success | {0}", in_stringData));

            GFriendsManager.Instance.GetPresenceOfFriends();
        }

        private void OnRemoveFriendsError(int statusCode, int reasonCode, string in_stringData, object in_obj)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            GDebug.Log(string.Format("RemoveFriends Failed | {0}  {1}  {2}", statusCode, reasonCode, in_stringData));
        }

        private void OnAddFriendsSuccess(string in_stringData, object in_obj)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            GDebug.Log(string.Format("Success | {0}", in_stringData));

            GFriendsManager.Instance.GetPresenceOfFriends();
        }

        private void OnAddFriendsError(int statusCode, int reasonCode, string in_stringData, object in_obj)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            GDebug.Log(string.Format("AddFriends Failed | {0}  {1}  {2}", statusCode, reasonCode, in_stringData));
        }
        #endregion
    }
}
