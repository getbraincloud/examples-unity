using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Gameframework
{
    public class GStateManager : SingletonBehaviour<GStateManager>
    {
        public static string UNDEFINED_STATE = "";
        #region Singleton Instance

        //Temporary Delegate that will be passed to the states once they are registered then we will nullify the Manager's delegate. (See OnEnterNewState or substate)
        public delegate void PauseDelegate();
        public delegate void InitializeDelegate(BaseState in_state);
        public InitializeDelegate OnInitializeDelegate;
        public delegate void StateChangeDelegate(StateInfo in_stateInfo);
        public StateChangeDelegate OnStateChange;
        public StateChangeDelegate OnSubStateChange;
        #endregion

        #region MonoBehaviour overrides
        void Start()
        {
            // Create the loading screen game object.
            CreateLoadingScreen();
        }

        void Update()
        {
            if (!m_bLoading && m_loadingScreen != null)
            {
                // we aren't loading 
                // is there a new state to continue to ?
                string nextState = NextStateId;
                string currentState = CurrentStateId;

                if (!ReferenceEquals(nextState, UNDEFINED_STATE) &&
                    !ReferenceEquals(nextState, currentState) &&
                    !m_bLoading)
                {
                    EnterNewState();
                }

                if (!m_bLoadingSubState)
                {
                    // see if you can handle subbstates
                    bool shouldPause = m_substatesRequestedToPush.Count > 0 ? m_substatesRequestedToPush[0].bNextShouldPauseState : false;
                    bool shouldDisable = m_substatesRequestedToPush.Count > 0 ? m_substatesRequestedToPush[0].bNextShouldDisableUI : false;
                    string nextSubStateId = NextSubStateId;
                    StateInfo subStateToExit = GetSubStateToExit();

                    if (!ReferenceEquals(nextSubStateId, UNDEFINED_STATE) &&
                            (m_currentSubState == null ||                       // no current state 
                            (m_currentSubState != null &&                       // or the current state is not the one we want to go into
                            !ReferenceEquals(m_currentSubState.StateId, nextSubStateId))))
                    {
                        if (shouldPause)
                            PauseSubState();

                        EnterNewSubState(shouldDisable);
                    }
                    else if (subStateToExit != null)
                    {
                        // exit sub state
                        // and all others in the queue!
                        for (int subStateIndex = 0; subStateIndex < m_gameSubStatesToPop.Count; ++subStateIndex)
                        {
                            UnloadSubState(m_gameSubStatesToPop[subStateIndex]);
                        }
                        m_gameSubStatesToPop.Clear();
                        // and resume the next one in the queue
                        ResumeSubState();
                    }

                    if (m_bClearSubStates)
                        ProcessClearAllSubStates();
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_bLoading = true;
            if (m_loadingScreen) Destroy(m_loadingScreen.gameObject);
        }
        #endregion 

        #region Properties

        // State Getters
        public string PreviousStateId
        {
            get { return m_sPreviousStateId; }
        }

        public BaseState CurrentState
        {
            get
            {
                BaseState toReturn = null;
                if (m_currentState != null && m_currentState.State != null)
                {
                    toReturn = m_currentState.State;
                }

                return toReturn;
            }
        }

        public string CurrentStateId
        {
            get
            {
                string toReturn = UNDEFINED_STATE;
                if (m_currentState != null)
                {
                    toReturn = m_currentState.StateId;
                }
                return toReturn;
            }
        }

        public string NextStateId
        {
            get { return m_sNextStateId; }
        }

        // Sub State getters
        public string PreviousSubStateId
        {
            get { return m_sPreviousSubStateId; }
        }

        public string CurrentSubStateId
        {
            get
            {
                string toReturn = UNDEFINED_STATE;
                if (m_currentSubState != null)
                {
                    toReturn = m_currentSubState.StateId;
                }
                return toReturn;
            }
        }

        public BaseState CurrentSubState
        {
            get
            {
                BaseState toReturn = null;
                if (m_currentSubState != null && m_currentSubState.State != null)
                {
                    toReturn = m_currentSubState.State;
                }
                return toReturn;
            }
        }

        public string NextStateFlowId
        {
            get { return m_sNextStateFlowId; }
            set { m_sNextStateFlowId = value; }
        }

        public int NumSubStatesActive()
        {
            return m_gameSubStates.Count;
        }

        public BaseState PreviousSubState
        {
            get
            {
                BaseState toReturn = null;

                if (m_gameSubStates.Count > 1)
                {
                    // current sub state is always at the front . ie location 0
                    StateInfo previousOne = m_gameSubStates[1];// the previous one
                    if (previousOne.StateId != m_sNextSubStateId && previousOne != m_currentSubState)
                    {
                        toReturn = previousOne.State;
                    }
                }

                return toReturn;
            }
        }

        public string NextSubStateId
        {
            get
            {
                if (m_sNextSubStateId == UNDEFINED_STATE && m_substatesRequestedToPush.Count > 0)
                {
                    m_sNextSubStateId = m_substatesRequestedToPush[0].sName;
                    m_substatesRequestedToPush.RemoveAt(0);
                    return m_sNextSubStateId;
                }
                else
                {
                    return m_sNextSubStateId;
                }
            }
        }

        public bool IsLoadingState
        {
            get { return m_bLoading; }
        }

        public bool IsLoadingSubState
        {
            get { return m_bLoadingSubState; }
        }
        #endregion

        #region publics
        // use to change states nicely
        public bool ChangeState(string in_nextState, LoadingScreen.eEffect in_effect = LoadingScreen.eEffect.Black)
        {
            bool toChange = false;

            /*
#if UNITY_EDITOR || UNITY_EDITOR_OSX
            if (m_currentState == null)
            {
                m_currentState = CurrentState.StateInfo;
            }
#endif
*/
            // not changing to the same state
            if (m_currentState != null && ReferenceEquals(in_nextState, m_currentState.StateId))
            {
                return toChange;
            }

            // changing the next state in the update
            m_loadingEffect = in_effect;
            m_sNextStateId = in_nextState;
            toChange = true;

            return toChange;
        }

        // use to push sub states (ie scenes that will be added to main scenes)
        // ie, menus , hud scenes, dialogs
        public bool PushSubState(string in_nextSubState, bool bShouldPauseState = false, bool bShouldDisablePreviousStateUI = true)
        {
            bool bToReturn = false;

            // not changing to the same state
            if (m_gameSubStates.Count > 0 && in_nextSubState == m_gameSubStates[0].StateId)
            {
                return bToReturn;
            }

            m_substatesRequestedToPush.Add(new PushSubstateData(in_nextSubState, bShouldPauseState, bShouldDisablePreviousStateUI));
            bToReturn = true;

            // if we should pause the main state
            // and this is the first one
            if (bShouldPauseState && m_gameSubStates.Count == 0 && m_gameSubStatesToPop.Count == 0)
            {
                PauseState();
            }

            return bToReturn;
        }

        public bool PopSubState(StateInfo in_subState)
        {
            bool bToReturn = false;

            for (int i = 0; i < m_gameSubStates.Count; ++i)
            {
                if (m_gameSubStates[i] != null && in_subState.State != null &&
                    m_gameSubStates[i].State == in_subState.State)
                {
                    // the state we want to pop is in the current game state list
                    m_gameSubStatesToPop.Add(in_subState);
                    bToReturn = true;
                    break;
                }
            }

            return bToReturn;
        }

        // find a substate in the stack
        public BaseSubState FindSubState(string stateId)
        {
            StateInfo si;
            for (int i = 0; i < m_gameSubStates.Count; ++i)
            {
                si = m_gameSubStates[i];
                if (si.StateId == stateId)
                    return si.State as BaseSubState;
            }
            return null;
        }

        // pop substates until the actual substate shows up
        public BaseState PopAllSubStatesTo(string stateId)
        {
            StateInfo si;
            for (int i = 0; i < m_gameSubStates.Count; ++i)
            {
                si = m_gameSubStates[i];
                if (si.StateId == stateId)
                    return si.State;
                else
                    PopSubState(si.State.StateInfo);
            }
            return null;
        }

        // pop the current substate
        public bool PopCurrentSubState()
        {
            bool bToReturn = false;
            if (m_currentSubState != null && m_currentSubState.State != null)
            {
                PopSubState(m_currentSubState);
                bToReturn = true;
            }

            return bToReturn;
        }

        // called from the Start() of a State
        public void OnEnterNewState(StateInfo in_stateInfo)
        {
            // next state is now current state
            if (in_stateInfo != null && in_stateInfo.State != null &&
                in_stateInfo.StateId == m_sNextStateId)
            {
                GDebug.Log("Time Loading -- " + in_stateInfo.StateId + "  " +
                    m_sNextStateId + " --- " + (Time.realtimeSinceStartup - m_fOriginalRealTimeSinceStartup));

                m_bLoading = false;
                m_fOriginalRealTimeSinceStartup = Time.realtimeSinceStartup;

                // current state is now the previous
                m_sPreviousStateId = m_currentState != null ? m_currentState.StateId : UNDEFINED_STATE;

                m_currentState = in_stateInfo;

                m_sNextStateId = UNDEFINED_STATE;

                ResumeState();
            }

            if (OnStateChange != null)
                OnStateChange(m_currentState);
        }

        // called from Start() of a SubState
        public void OnEnterNewSubState(StateInfo in_stateInfo)
        {
            // next sub state is now current sub state
            if (in_stateInfo != null && in_stateInfo.State != null &&
                in_stateInfo.StateId == m_sNextSubStateId)
            {
                GDebug.Log("Time Loading SubState -- " + m_sNextSubStateId + " --- " +
                    (Time.realtimeSinceStartup - m_fOriginalRealTimeSinceStartupSubState));

                m_bLoadingSubState = false;
                m_fOriginalRealTimeSinceStartupSubState = Time.realtimeSinceStartup;

                // current sub state is now the previous
                if (m_currentSubState != null)
                    m_sPreviousSubStateId = m_currentSubState.StateId;
                else
                    m_sPreviousSubStateId = UNDEFINED_STATE;

                m_currentSubState = in_stateInfo;
                m_gameSubStates.Insert(0, in_stateInfo);

                m_sNextSubStateId = UNDEFINED_STATE;

                if (OnSubStateChange != null)
                    OnSubStateChange(m_currentSubState);
            }
        }

        // GLOBAL STATE PAUSE
        public void PauseState()
        {
            BaseState currentState = CurrentState;
            if (currentState != null && Time.timeScale != 0)
            {
                // pause the state, save the previous time delta
                // and set the timescale to 0
                currentState.OnPauseState();
                m_fSavedTimeDelta = Time.timeScale;
                Time.timeScale = 0;
            }
        }

        // GLOBAL STATE RESUME
        public void ResumeState()
        {
            StartCoroutine(ResumeStateActions());
        }

        private IEnumerator ResumeStateActions()
        {
            yield return YieldFactory.GetWaitForEndOfFrame();

            if (OnStateChange != null)
                OnStateChange(m_currentState);

            BaseState currentState = CurrentState;

            // resume the state and become paused
            if (currentState != null && Time.timeScale == 0.0f && m_fSavedTimeDelta != 0.0f)
            {
                // restore the timescale, reset the saved time delta
                // and resume the current state
                Time.timeScale = m_fSavedTimeDelta;
                m_fSavedTimeDelta = 0.0f;
                currentState.OnResumeState(true);
            }
            else if (currentState != null && PreviousSubStateId != UNDEFINED_STATE)
            {
                currentState.OnResumeState(false);
            }
        }

        public void RetrieveInitializedDelegates(BaseState in_state)
        {
            in_state.OnInitializeDelegate += OnInitializeDelegate;
        }

        public bool IsLoadingSpinnerActive()
        {
            bool bToReturn = false;
            if (m_loadingSpinner)
            {
                bToReturn = m_loadingSpinner.gameObject.activeInHierarchy;
            }
            return bToReturn;
        }

        public bool EnableLoadingSpinner(bool in_value)
        {
            bool bToReturn = false;
            // create it for the first time
            // TODO: make this into a setter, so that GStateManager stays generic
            if (in_value && m_loadingSpinner == null)
            {
                //m_loadingSpinner = GEntityFactory.Instance.CreateResourceAtPath("Prefabs/UI/LoadingSpinner", null).GetComponent<Transform>();
                DontDestroyOnLoad(m_loadingSpinner.gameObject);
            }

            if (m_loadingSpinner != null)
            {
                // show it
                if (in_value && !m_loadingSpinner.gameObject.activeSelf)
                {
                    m_loadingSpinner.gameObject.SetActive(in_value);
                }
                // disable it
                else if (!in_value && m_loadingSpinner.gameObject.activeSelf)
                {
                    m_loadingSpinner.gameObject.SetActive(in_value);
                }
            }
            return bToReturn;
        }

        // USE THIS VERY SELDOMLY, its used during the first splash scene
        // in order to force the current state, or remove the current state to force
        // a reload
        public bool ForceStateInfo(StateInfo info)
        {
            bool toReturn = false;
            if (m_currentState == null || info == null)
            {
                m_currentState = info;
                toReturn = true;
            }

            m_bLoading = false;

            return toReturn;
        }

        public void ForcedUpdatedLoadingAssetBundle()
        {
            if (m_loadingScreen)
            {
                m_loadingScreen.ForcedUpdatedLoadingAssetBundle();
            }
        }

        public void EnableLoadingScreen(bool in_value)
        {
            if (m_loadingScreen)
            {
                m_loadingScreen.EnableLoadingScreen(in_value);
            }
        }

        public void UpdateAssetBundleLoading(AsyncOperation operation)
        {
            if (m_loadingScreen)
            {
                m_loadingScreen.UpdateAssetBundleLoading(operation);
            }
        }

        public void RemoveSplashLoadingScreen()
        {
            if (m_loadingScreen)
            {
                m_loadingScreen.RemoveSplashLoadingScreen();
            }
        }
#endregion

#region Private Helpers
        private void CreateLoadingScreen()
        {
            if (m_loadingScreen == null)
            {
                //GameObject loadingScreen = GEntityFactory.Instance.CreateResourceAtPath("Prefabs/UI/LoadingScreen", transform);
                //m_loadingScreen = loadingScreen.GetComponent<LoadingScreen>();
            }
        }

        private void PauseSubState()
        {
            BaseState currentSubState = CurrentSubState;
            if (currentSubState != null)
            {
                m_sPreviousSubStateId = currentSubState.StateInfo.StateId;
                currentSubState.OnPauseState();
            }
        }

        private void ResumeSubState()
        {
            BaseState currentSubState = CurrentSubState;
            if (currentSubState != null)
            {
                currentSubState.OnResumeState(true);
            }
        }

        private void EnterNewState()
        {
            m_bLoading = true;
            m_fOriginalRealTimeSinceStartup = Time.realtimeSinceStartup;

            ClearAllSubStates();
            CreateLoadingScreen();
            m_loadingScreen.LoadLevel(m_sNextStateId, false, m_loadingEffect, null, null);
        }

        public void SetCurrentStateEnabled(bool in_isEnabled)
        {
            if (m_currentSubState != null && m_currentSubState.State != null)
                m_currentSubState.State.SetUIEnabled(in_isEnabled);
            if (m_currentState != null && m_currentState.State != null)
                m_currentState.State.SetUIEnabled(in_isEnabled);
        }

        private StateInfo GetSubStateToExit()
        {
            StateInfo toReturn = null;
            if (m_gameSubStatesToPop != null && m_gameSubStatesToPop.Count > 0 && m_gameSubStatesToPop[0] != null)
            {
                toReturn = ((StateInfo)m_gameSubStatesToPop[0]);    // the first element
                if (m_gameSubStatesToPop[0].StateId == "")
                {
                    m_gameSubStatesToPop.RemoveAt(0);
                    toReturn = null;
                }
            }
            return toReturn;
        }

        private bool UnloadSubState(StateInfo in_subStateToExit)
        {
            bool bToReturn = false;
            if (in_subStateToExit != null && in_subStateToExit.State != null)
            {
                in_subStateToExit.State.SetUIEnabled(false);

                // remove the current substate from the current game states
                // when we remove substates we remove them all
                if (m_gameSubStates.Count > 0) m_gameSubStates.Remove(in_subStateToExit);

                m_sPreviousSubStateId = in_subStateToExit.StateId;

                // .clean em up 
                if (m_currentSubState == in_subStateToExit) m_currentSubState.Cleanup();
                else in_subStateToExit.Cleanup();

                bToReturn = true;
            }

            // we exited one, do we have others to go to automatically? 
            if (m_gameSubStates.Count > 0)
            {
                StateInfo frontOfPack = m_gameSubStates[0];// the front
                if (frontOfPack.StateId != m_sNextSubStateId && frontOfPack != m_currentSubState)
                {
                    m_currentSubState = frontOfPack;
                }
            }
            else
            {
                ResumeState();
            }

            SetCurrentStateEnabled(true);

            return bToReturn;
        }

        private void EnterNewSubState(bool in_bShouldDisable)
        {
            if (in_bShouldDisable)
                SetCurrentStateEnabled( false );

            // loads it asynchronously
            m_bLoadingSubState = true;
            m_fOriginalRealTimeSinceStartupSubState = Time.realtimeSinceStartup;
            CreateLoadingScreen();
            m_loadingScreen.LoadLevel(m_sNextSubStateId, true, LoadingScreen.eEffect.Invalid, null, null);
        }

        private void ProcessClearAllSubStates()
        {
            // clean up the current substate
            m_currentSubState = null;

            // all other substates
            if (m_gameSubStates != null)
            {
                for (int i = 0; i < m_gameSubStates.Count; ++i)
                {
                    m_gameSubStates[i].Cleanup();
                }
                m_gameSubStates.Clear();
            }

            m_sPreviousSubStateId = UNDEFINED_STATE;
            m_sNextSubStateId = UNDEFINED_STATE;

            if (m_gameSubStatesToPop != null)
                m_gameSubStatesToPop.Clear();

            ResumeState();

            m_bClearSubStates = false;
        }

        public void ClearAllSubStates()
        {
            m_bClearSubStates = true;
        }

#endregion

#region Properties
        struct PushSubstateData
        {
            public PushSubstateData(string name, bool in_pause, bool in_disableUI)
            {
                sName = name;
                bNextShouldPauseState = in_pause;
                bNextShouldDisableUI = in_disableUI;
            }
            public string sName;
            public bool bNextShouldPauseState;
            public bool bNextShouldDisableUI;
        }
        private List<StateInfo> m_gameSubStates = new List<StateInfo>();
        private List<StateInfo> m_gameSubStatesToPop = new List<StateInfo>();
        private List<PushSubstateData> m_substatesRequestedToPush = new List<PushSubstateData>();   // read from the front

        private StateInfo m_currentSubState = null;
        private StateInfo m_currentState = null;

        private bool m_bLoading = false;
        private bool m_bLoadingSubState = false;
        private bool m_bClearSubStates = false;

        private string m_sPreviousStateId = UNDEFINED_STATE;
        private string m_sNextStateId = UNDEFINED_STATE;
        private string m_sPreviousSubStateId = UNDEFINED_STATE;
        private string m_sNextSubStateId = UNDEFINED_STATE;

        private string m_sNextStateFlowId = UNDEFINED_STATE;

        private float m_fSavedTimeDelta = 1f;
        private float m_fOriginalRealTimeSinceStartup = 0f;
        private float m_fOriginalRealTimeSinceStartupSubState = 0f;

        private LoadingScreen m_loadingScreen = null;
        private Transform m_loadingSpinner = null;
        private LoadingScreen.eEffect m_loadingEffect = LoadingScreen.eEffect.Invalid;
#endregion
    }
}