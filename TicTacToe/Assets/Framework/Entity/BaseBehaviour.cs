using UnityEngine;

namespace Gameframework
{
    public class BaseBehaviour : MonoBehaviour
    {
        static public T FindInParents<T>(GameObject go) where T : Component
        {
            if (go == null) return null;
            var comp = go.GetComponent<T>();

            if (comp != null)
                return comp;

            Transform t = go.transform.parent;
            while (t != null && comp == null)
            {
                comp = t.gameObject.GetComponent<T>();
                t = t.parent;
            }
            return comp;
        }

        #region public 

        #endregion

        #region Protected Mono
        protected virtual void OnDestroy()
        {
            CancelInvoke();
            StopAllCoroutines();
        }
        #endregion
    }
}

