using System.Collections.Generic;
using UnityEngine.UI;
using Gameframework;

namespace BrainCloudUNETExample
{
    public class ConfirmJoinLobbySubState : BaseSubState
    {
        public static string STATE_NAME = "confirmJoinLobby";

        #region BaseState
        protected override void Start()
        {
            _stateInfo = new StateInfo(STATE_NAME, this);
            base.Start();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
        #endregion

        #region Public 
        public void LateInit(string in_profileId, string in_userName)
        {
            m_offeredByProfileId = in_profileId;
            Text label = transform.FindDeepChild("Label").gameObject.GetComponent<Text>();
            label.text = "<color=yellow>" + in_userName + "</color> is requesting to join a lobby with them.\nDo you accept ?";
        }

        public void ConfirmJoinGameWithOther()
        {
            // send event to confirm
            Dictionary<string, object> jsonData = new Dictionary<string, object>();
            jsonData[BrainCloudConsts.JSON_LAST_CONNECTION_ID] = GCore.Wrapper.Client.RTTConnectionID;
            jsonData[BrainCloudConsts.JSON_PROFILE_ID] = GCore.Wrapper.Client.ProfileId;
            jsonData[BrainCloudConsts.JSON_USER_NAME] = GPlayerMgr.Instance.PlayerData.PlayerName;

            // send event to other person
            GCore.Wrapper.Client.EventService.SendEvent(m_offeredByProfileId, "CONFIRM_JOIN_LOBBY",
                BrainCloud.JsonFx.Json.JsonWriter.Serialize(jsonData));

            BombersNetworkManager.WaitOnLobbyJoin();
            ExitSubState();
        }

        public void RefusedJoinGameWithOther()
        {
            // send event to confirm
            Dictionary<string, object> jsonData = new Dictionary<string, object>();
            jsonData[BrainCloudConsts.JSON_LAST_CONNECTION_ID] = GCore.Wrapper.Client.RTTConnectionID;
            jsonData[BrainCloudConsts.JSON_PROFILE_ID] = GCore.Wrapper.Client.ProfileId;
            jsonData[BrainCloudConsts.JSON_USER_NAME] = GPlayerMgr.Instance.PlayerData.PlayerName;

            // send event to other person
            GCore.Wrapper.Client.EventService.SendEvent(m_offeredByProfileId, "REFUSED_JOIN_LOBBY",
                BrainCloud.JsonFx.Json.JsonWriter.Serialize(jsonData));

            ExitSubState();
        }
        #endregion

        #region Private
        private string m_offeredByProfileId = "";
        #endregion
    }
}
