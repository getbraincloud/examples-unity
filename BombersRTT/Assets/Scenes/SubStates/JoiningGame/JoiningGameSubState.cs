using Gameframework;
using UnityEngine;
using UnityEngine.UI;

namespace BrainCloudUNETExample
{
    public class JoiningGameSubState : BaseSubState
    {
        public static string STATE_NAME = "joiningGame";

#pragma warning disable 649
        [SerializeField]
        private Text m_mainLabel;
        [SerializeField]
        private Text m_timerLabel;
        [SerializeField]
        private GameObject m_closeButton;
#pragma warning restore 649
        #region BaseState
        protected override void Start()
        {
            _stateInfo = new StateInfo(STATE_NAME, this);
            m_maxTimeAllowedInState = GConfigManager.GetFloatValue("JoiningGameTimeOutTime");
            InvokeRepeating("updateTimer", 1.0f, 1.0f);

            BaseSubState findGameState = GStateManager.Instance.FindSubState(CreateGameSubState.STATE_NAME);
            if (findGameState != null) findGameState.ExitSubState();

            if (GStateManager.Instance.CurrentStateId == MainMenuState.STATE_NAME &&
                (GStateManager.Instance.CurrentSubStateId == ConfirmJoinLobbySubState.STATE_NAME ||
                 GStateManager.Instance.CurrentSubStateId == LobbySubState.STATE_NAME))
                m_closeButton.SetActive(false);

            base.Start();
        }
        #endregion

        #region Public 
        public override void ExitSubState()
        {
            BombersNetworkManager.Instance.CancelFindRequest();
            base.ExitSubState();
        }
        #endregion

        #region Private
        private int m_totalTimeInState;
        private float m_maxTimeAllowedInState;
        private string[] m_labelDisplay = { "Warming the Engines...", "Opening the Hangars...", "Priming the Weapons...", "Joining game..." };
        private void updateTimer()
        {
            ++m_totalTimeInState;
            m_timerLabel.text = HudHelper.ToMinuteSecondString(m_totalTimeInState);
            // update the text display every so often
            if (m_totalTimeInState % 10 == 0)
            {
                int randomNum = Random.Range(0, m_labelDisplay.Length);
                m_mainLabel.text = m_labelDisplay[randomNum];
            }

            if (m_totalTimeInState % m_maxTimeAllowedInState == 0)
            {
                HudHelper.DisplayMessageDialog("ERROR", "LETS TRY THAT AGAIN?", "YES!", onErrorOk);
            }
        }

        private void onErrorOk()
        {
            BombersNetworkManager.Instance.ConnectRTT();
            GStateManager.Instance.ClearAllSubStates();
        }
        #endregion
    }
}
