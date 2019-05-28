using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Gameframework
{
    public class GenericAnimator : BaseBehaviour
    {
        #region Public Properties
        public string HoverAnimationTriggerKey = "";
        public string PressAnimationTriggerKey = "";
        public string SelectAnimationTriggerKey = "";

        public bool HasAnimator { get { return m_myAnimator != null; } }
        #endregion 

        #region Public 
        public void EnableAnimator(bool in_enable)
        {
            if (m_myAnimator != null)
            {
                m_myAnimator.enabled = in_enable;
            }
        }

        public void TriggerAnimation(string in_name)
        {
            if (enabled && m_myAnimator != null && in_name != "") m_myAnimator.SetTrigger(in_name);
        }

        public void PlayAnimation(string in_name, bool in_value)
        {
            if (enabled && m_myAnimator != null && in_name != "") m_myAnimator.SetBool(in_name, in_value);
        }

        public bool IsAnimationPlaying(string in_name)
        {
            if (enabled && m_myAnimator != null && in_name != "") return m_myAnimator.GetBool(in_name);
            else return false;
        }

        public float GetAnimationClipLength(string in_name)
        {
            float time = m_cachedAnimationClipLength.ContainsKey(in_name) ? m_cachedAnimationClipLength[in_name] : 0.0f;

            RuntimeAnimatorController ac = m_myAnimator.runtimeAnimatorController;
            // this ONLY runs IF time was not found
            for (int i = 0; i < ac.animationClips.Length && time == 0.0f; i++)                     
            {
                if (ac.animationClips[i].name == in_name || 
                    ac.animationClips[i].name == ac.name + "_" + in_name)
                {
                    time = ac.animationClips[i].length;
                }
            }

            // always write it
            m_cachedAnimationClipLength[in_name] = time;
            return time;
        }
        private Dictionary<string, float> m_cachedAnimationClipLength = new Dictionary<string, float>();
        #endregion

        #region protected
        // must have a collider set as a trigger to be set off
        protected virtual void OnMouseEnter()
        {
            if (!HasFlag(eAnimationState.Dragging))
            {
                OnSelected(false);
                OnHovered(true);
            }
        }

        protected virtual void OnMouseExit()
        {
            if (!HasFlag(eAnimationState.Dragging))
            {
                OnHovered(false);
                OnPressed(false);
            }
        }

        protected virtual void OnMouseDown()
        {
            OnSelected(false);
            OnPressed(true);
        }

        protected virtual void OnMouseUp()
        {
            // we were dragging ensure that we reset it
            if (HasFlag(eAnimationState.Dragging))
            {
                OnDrag(false);
                OnHovered(false);
                OnPressed(false);
            }
        }

        protected virtual void OnMouseUpAsButton()
        {
            OnSelected(true);
            OnPressed(false);
        }

        protected virtual void OnMouseDrag()
        {
            if (HasFlag(eAnimationState.Pressed) && !HasFlag(eAnimationState.Dragging))
            {
                OnDrag(true);
            }
        }

        // only triggered when going onto rigid bodies
        protected virtual void OnTriggerEnter(Collider in_collision)
        {
        }
        protected virtual void OnTriggerExit(Collider in_collision)
        {
        }
        protected virtual void OnHovered(bool in_isOver)
        {
            SetAnimationStateFlag(eAnimationState.Hovered, in_isOver);
            PlayAnimation(HoverAnimationTriggerKey, in_isOver);
        }

        protected virtual void OnPressed(bool in_isOver)
        {
            SetAnimationStateFlag(eAnimationState.Pressed, in_isOver);
            PlayAnimation(PressAnimationTriggerKey, in_isOver);
        }

        protected virtual void OnSelected(bool in_isOver)
        {
            SetAnimationStateFlag(eAnimationState.Selected, in_isOver);
            PlayAnimation(SelectAnimationTriggerKey, in_isOver);
            if (in_isOver)
            {
                OnSelected();
                StartCoroutine(DelayedSelectedToggle());
            }
        }

        protected virtual void OnDrag(bool in_isOver)
        {
            SetAnimationStateFlag(eAnimationState.Dragging, in_isOver);
        }

        protected virtual void OnSelected()
        {
        }
        protected Animator m_myAnimator = null;
        #endregion 

        #region MonoBehaviour
        // Use this for initialization
        protected virtual void Start()
        {
            m_myAnimator = this.GetComponent<Animator>();
            if (m_myAnimator == null) m_myAnimator = this.GetComponentInChildren<Animator>();
        }

        protected override void OnDestroy()
        {
            m_myAnimator = null;
            base.OnDestroy();
        }

        // Works with "None" as well
        protected bool HasFlag(eAnimationState in_flag)
        {
            return (m_currentState & in_flag) == in_flag;
        }
        #endregion

        #region private
        private IEnumerator DelayedSelectedToggle()
        {
            float originalTime = UnityEngine.Time.realtimeSinceStartup;
            while (UnityEngine.Time.realtimeSinceStartup - originalTime < 0.35f)
                yield return YieldFactory.GetWaitForEndOfFrame(); ;

            OnSelected(false);
        }

        private void SetAnimationStateFlag(eAnimationState in_flag, bool in_isOver)
        {
            if (in_isOver)
            {
                SetAnimationFlag(in_flag);
            }
            else
            {
                UnsetAnimationFlag(in_flag);
            }
        }
        private void SetAnimationFlag(eAnimationState in_flag)
        {
            m_currentState = m_currentState | in_flag;
        }

        private void UnsetAnimationFlag(eAnimationState in_flag)
        {
            m_currentState = m_currentState & (~in_flag);
        }

        private void ToggleAnimationFlag(eAnimationState in_flag)
        {
            m_currentState = m_currentState ^ in_flag;
        }

        private eAnimationState m_currentState = eAnimationState.Invalid;
        #endregion
    }

    public enum eAnimationState
    {
        Invalid = 0,

        Hovered = 0x1,
        Pressed = 0x2,
        Selected = 0x4,
        Dragging = 0x8,

        eAnimationStateMax
    }
}