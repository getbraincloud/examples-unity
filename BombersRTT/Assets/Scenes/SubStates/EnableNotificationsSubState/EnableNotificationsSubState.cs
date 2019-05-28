using Gameframework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BrainCloudUNETExample
{
    public class EnableNotificationsSubState : BaseSubState
    {
        public static string STATE_NAME = "enableNotificationsSubState";

        public TextMeshProUGUI NotificationsTitleText = null;
        public GameObject EnableNotificationsParentObject;
        public GameObject DisableNotificationsParentObject;

        #region BaseState
        // Use this for initialization
        protected override void Start()
        {
            _stateInfo = new StateInfo(STATE_NAME, this);
            base.Start();

            NotificationsTitleText.text = GSettingsMgr.PushAuthorized ? "DISABLE NOTIFICATIONS?" : "ENABLE NOTIFICATIONS?";
            EnableNotificationsParentObject.SetActive(!GSettingsMgr.PushAuthorized);
            DisableNotificationsParentObject.SetActive(GSettingsMgr.PushAuthorized);
        }
        #endregion

        #region Public
        public void OnEnableNotifications()
        {
#if !UNITY_EDITOR
#if UNITY_IOS || UNITY_ANDROID
            GPlayerMgr.Instance.RegisterForNotifications();
            GSettingsMgr.PushAuthorized = true;
            GStateManager.Instance.PopSubState(_stateInfo);
#endif
#else
            GSettingsMgr.PushAuthorized = true;
            GStateManager.Instance.PopSubState(_stateInfo);
#endif
        }

        public void OnDisableNotifications()
        {
            GSettingsMgr.PushAuthorized = false;
            GStateManager.Instance.PopSubState(_stateInfo);
        }
        #endregion
    }
}
