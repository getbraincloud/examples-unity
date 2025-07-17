using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Gameframework
{
    public class BaseState : ContentGameObjectReplacer
    {
        #region Delegates
        public GStateManager.PauseDelegate OnUITogglePause;
        public GStateManager.PauseDelegate OnUIToggleResume;
        public GStateManager.InitializeDelegate OnInitializeDelegate;
        #endregion

        public Selectable FirstSelected = null;

        #region Properties
        public BaseStateController Controller { get; set; }
        public BaseStateView View { get; set; }

        public StateInfo StateInfo
        {
            get { return _stateInfo; }
        }
        public bool IsPaused
        {
            get { return _bPaused; }
        }
        #endregion

        #region MonoBehavior 
        protected override void Start()
        {
            base.Start();
            // this supports stuff from the editor right away
            StartCoroutine(spinUntilManagersAreSetup());
            m_lastSelectedGameObject = FirstSelected;
            //Invoke("ActivateUpdateKeyboardMouseControls", 0.1f);
        }
        protected void OnEnable()
        {
            if (m_keyboardCheckRoutine == null && m_checkKeyboardRoutineOnEnable)
            {
                m_keyboardCheckRoutine = StartCoroutine(UpdateKeyboardMouseControls());
            }
        }
        protected virtual void OnEnter()
        {
            GStateManager stateMgr = GStateManager.Instance;
            stateMgr.OnEnterNewState(_stateInfo);
            stateMgr.RetrieveInitializedDelegates(this);
            if (OnInitializeDelegate != null)
                OnInitializeDelegate(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnInitializeDelegate = null;
            Destroy(this.gameObject);
            ActivateUpdateKeyboardMouseControlsCoroutine(false);
        }
        #endregion

        #region Public Methods
        public void SetUIEnabled(bool in_isEnabled)
        {
            /*
            if (m_eventSystem)
                m_eventSystem.enabled = in_isEnabled;
            */

            if (in_isEnabled)
            {
                for (int i = 0; i < m_toEnable.Count; ++i)
                {
                    if (m_toEnable[i] != null)
                        m_toEnable[i].enabled = true;
                }
                m_toEnable.Clear();
            }
            else
            {
                //List<Selectable> selectables = transform.FindDeepChildren<Selectable>().FindAll(x => x.interactable && x.enabled);
                // for (int i = 0; i < selectables.Count; ++i)
                // {
                //     m_toEnable.Add(selectables[i]);
                //     selectables[i].enabled = false;
                // }
            }
        }

        private List<Selectable> m_toEnable = new List<Selectable>();

        public void OnPauseState()
        {
            _bPaused = true;
            enabled = false;

            if (OnUITogglePause != null)
                OnUITogglePause();

            SetUIEnabled(false);

            OnPauseStateImpl();
            ActivateUpdateKeyboardMouseControlsCoroutine(false);
        }

        public void OnResumeState(bool wasPause)
        {
            if (wasPause)
            {
                _bPaused = false;
                enabled = true;

                if (OnUIToggleResume != null)
                    OnUIToggleResume();
            }

            if (FirstSelected != null)
                FirstSelected.Select();

            SetUIEnabled(true);

            OnResumeStateImpl(wasPause);
            ActivateUpdateKeyboardMouseControlsCoroutine(true);
        }

        public void UpdateLastSelected()
        {
            if (m_lastSelectedGameObject == null || EventSystem.current.currentSelectedGameObject != m_lastSelectedGameObject.gameObject)
            {
                if (EventSystem.current.currentSelectedGameObject != null)
                    m_lastSelectedGameObject = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
            }
        }

        protected virtual void OnPauseStateImpl() { }
        protected virtual void OnResumeStateImpl(bool wasPaused) { }
        #endregion

        #region Protected Properties
        protected StateInfo _stateInfo = null;

        protected bool _bPaused = false;
        #endregion

        #region Private
        private IEnumerator spinUntilManagersAreSetup()
        {
            while (!GCore.Instance.IsInitialized)
            {
                yield return YieldFactory.GetWaitForEndOfFrame();
            }

            continueOnEnter();
        }

        private void continueOnEnter()
        {
            _bPaused = false;

            if (FirstSelected != null)
                FirstSelected.Select();

            SwapViewAndSwapObjects();

            // now that we've swapped, ensure we have the correct references to the controller and view!
            GameObject activeView = GetActiveView();
            if (activeView != null)
            {
                //Controller = activeView.transform.FindDeepChild<BaseStateController>();
                //View = activeView.transform.FindDeepChild<BaseStateView>();
            }

            OnEnter();
        }

        private IEnumerator UpdateKeyboardMouseControls()
        {
            while (true)
            {
                UpdateLastSelected();
                if (Input.GetMouseButtonDown(0))
                {
                    if (m_lastSelectedGameObject != null)
                        m_lastSelectedGameObject.Select();
                    yield return YieldFactory.GetWaitForEndOfFrame();
                }
                yield return null;
            }
        }
        private void ActivateUpdateKeyboardMouseControls()
        {
            ActivateUpdateKeyboardMouseControlsCoroutine(true);
        }
        private void ActivateUpdateKeyboardMouseControlsCoroutine(bool active)
        {
            if (m_keyboardCheckRoutine != null)
                StopCoroutine(m_keyboardCheckRoutine);

            if (active && gameObject.activeInHierarchy)
            {
                m_keyboardCheckRoutine = StartCoroutine(UpdateKeyboardMouseControls());
            }
            m_checkKeyboardRoutineOnEnable = active;
        }

        private Selectable m_lastSelectedGameObject;
        private Coroutine m_keyboardCheckRoutine = null;
        private bool m_checkKeyboardRoutineOnEnable = false;
        #endregion

    }

    #region State Info Helper Class
    public class StateInfo
    {
        // using string as an ID in this case because using a value 
        // may change after editor build changes
        public StateInfo(string in_id, BaseState in_state)
        {
            m_state = in_state;
            m_stateId = in_id;
        }

        // public getters 
        public string StateId
        {
            get { return m_stateId; }
        }

        public BaseState State
        {
            get { return m_state; }
        }

        public AsyncOperation Cleanup()
        {
            m_state.gameObject.SetActive(false);
            if (m_state != null) GameObject.Destroy(m_state.gameObject);
            m_state = null;

            string originalStateId = m_stateId;

            m_stateId = GStateManager.UNDEFINED_STATE;

            // if its a substate, there may be multiple created, and unity's scene management
            // even if you pass in the "scene" does not correctly remove the scene mentioned
            // this may result in an "empty" scene being left behind between state loads, but this is better then
            // having a substate removed when not wanting it removed
            if (!string.IsNullOrEmpty(originalStateId) && originalStateId != GStateManager.UNDEFINED_STATE && GStateManager.Instance.FindSubState(originalStateId) == null &&
                (GStateManager.Instance.CurrentState != null || GStateManager.Instance.NumSubStatesActive() > 0))
                return SceneManager.UnloadSceneAsync(originalStateId);
            else
            {
                return null;
            }
        }

        private string m_stateId = GStateManager.UNDEFINED_STATE;
        private BaseState m_state = null;
    }
    #endregion
}
