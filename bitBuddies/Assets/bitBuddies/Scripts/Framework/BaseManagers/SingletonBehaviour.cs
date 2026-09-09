using UnityEngine;

namespace Gameframework
{
    // This is persistent and doesn't get destroyed
    public class SingletonBehaviour<T> : MonoBehaviour where T : Component
    {
        public static T Instance
        {
            get { return GetInstance(); }
        }

        virtual public void StartUp() { }

        #region BaseBehaviour

        public virtual void Awake()
        {
            if (m_instance == null)
            {
                m_instance = this as T;
                name = GetType().Name;
                DontDestroyOnLoad(gameObject);
            }
            else if (m_instance != this as T)
            {
                Destroy(this);
            }
        }

        public virtual void OnDestroy()
        {
            if (m_instance == this as T)
            {
                m_instance = null;
            }
        }

        #endregion

        #region GetInstance()

        private static T GetInstance()
        {
            if (m_instance == null)
            {
                m_instance = FindFirstObjectByType<T>();
                if (m_instance == null)
                {
                    GameObject obj = new GameObject();
                    //obj.hideFlags = HideFlags.HideAndDontSave;
                    m_instance = obj.AddComponent<T>();
                }
            }

            return m_instance;
        }

        protected static T m_instance;

        #endregion
    }
}
