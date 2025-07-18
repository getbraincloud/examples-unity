using UnityEngine;
namespace Gameframework
{
    // this is persistent and doesn't get destroyed
    public class SingletonBehaviour<T> : MonoBehaviour
        where T : Component
    {
        public static T Instance
        {
            get { return GetInstance(); }
        }

        public static T singleton
        {
            get { return GetInstance(); }
        }

        virtual public void StartUp()
        {
        }

        #region BaseBehaviour
        public virtual void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
            if (m_instance == null)
            {
                m_instance = this as T;
                this.name = this.GetType().Name;
            }
            else
            {
                Destroy(this.gameObject);
            }
        }

        public virtual void OnApplicationQuit()
        {
            // release reference on exit
            m_instance = null;
        }
        #endregion

        #region PRIVATE
        private static T GetInstance()
        {
            if (m_instance == null)
            {
                m_instance = FindObjectOfType<T>();
                if (m_instance == null)
                {
                    GameObject obj = new GameObject();
                    //obj.hideFlags = HideFlags.HideAndDontSave;
                    m_instance = obj.AddComponent<T>();
                }
            }
            return m_instance;
        }

        private static T m_instance;
        #endregion
    }
}
