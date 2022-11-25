using System.Collections.Generic;
using UnityEngine.Events;

namespace Gameframework
{
    public class GEventManager : SingletonBehaviour<GEventManager>
    {
        #region EventNames
        public const string ON_PROCESS_BC_EVENTS = "OnProcessBCEvents";
        public const string ON_REQUEST_PERMISSIONS = "OnRequestPermissions";
        public const string ON_PLAYER_DATA_UPDATED = "OnPlayerDataUpdated";
        public const string ON_PLAYER_LEVEL_UP = "OnPlayerLevelUP";
        public const string ON_IAP_PRODUCTS_UPDATED = "OnIAPProductsUpdated";
        public const string ON_IDENTITIES_UPDATED = "OnIdentitiesUpdated";
        public const string ON_INVITED_FRIEND = "OnInvitedFriend";
        public const string ON_REFUSED_INVITE_FRIEND = "OnRefusedInviteFriend";
        public const string ON_RTT_ENABLED = "OnRTTEnabled";
        #endregion

        private Dictionary<string, UnityEvent> eventDictionary;

        public override void Awake()
        {
            base.Awake();
            Init();
        }

        void Init()
        {
            if (eventDictionary == null)
            {
                eventDictionary = new Dictionary<string, UnityEvent>();
            }
        }

        public static void StartListening(string eventName, UnityAction listener)
        {
            UnityEvent thisEvent = null;
            if (Instance.eventDictionary.TryGetValue(eventName, out thisEvent))
            {
                thisEvent.AddListener(listener);
            }
            else
            {
                thisEvent = new UnityEvent();
                thisEvent.AddListener(listener);
                Instance.eventDictionary.Add(eventName, thisEvent);
            }
        }

        public static void StopListening(string eventName, UnityAction listener)
        {
            UnityEvent thisEvent = null;
            if (Instance.eventDictionary.TryGetValue(eventName, out thisEvent))
            {
                thisEvent.RemoveListener(listener);
            }
        }

        public static void TriggerEvent(string eventName)
        {
            UnityEvent thisEvent = null;
            if (Instance.eventDictionary.TryGetValue(eventName, out thisEvent))
            {
                thisEvent?.Invoke();
            }
        }
    }
}