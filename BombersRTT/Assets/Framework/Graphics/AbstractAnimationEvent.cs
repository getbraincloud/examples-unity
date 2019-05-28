using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Gameframework
{
    public class AbstractAnimationEvent : BaseBehaviour
    {
        public object AttachedObject = null;

        // Delegates.
        public delegate void AnimationEventDelegate(AbstractAnimationEvent in_object);

        #region Public
        // Public methods.
        public void AddListener(string in_name, AnimationEventDelegate in_onEvent)
        {
            if (!m_arEventListeners.ContainsKey(in_name))
                m_arEventListeners.Add(in_name, null);

            m_arEventListeners[in_name] += in_onEvent;
        }

        public void RemoveListener(string in_name, AnimationEventDelegate in_onEvent)
        {
            if (m_arEventListeners.ContainsKey(in_name))
                m_arEventListeners[in_name] -= in_onEvent;
        }
        #endregion

        #region private
        // Animation event methods.
        private void OnAnimationEvent(string in_name)
        {
            StartCoroutine("SafeOnAnimationEvent", in_name);
        }

        private IEnumerator SafeOnAnimationEvent(object in_name)
        {
            yield return YieldFactory.GetWaitForEndOfFrame();

            string name = (string)in_name;
            if (m_arEventListeners != null && m_arEventListeners.ContainsKey(name))
                m_arEventListeners[name](this);
            else
                GDebug.LogWarning("Animation event key not found : " + name, gameObject);

        }

        // Private members.
        private Dictionary<string, AnimationEventDelegate> m_arEventListeners = new Dictionary<string, AnimationEventDelegate>();
        #endregion
    }
}
