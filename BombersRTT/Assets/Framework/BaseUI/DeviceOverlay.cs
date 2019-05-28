using UnityEngine;
namespace Gameframework
{
    public class DeviceOverlay : BaseBehaviour
    {
        #region public properties
        public GameObject IphoneXFrame = null;
        public GameObject SafeArea = null;
        #endregion

        private void Start()
        {
#if UNITY_EDITOR
            DontDestroyOnLoad(gameObject);
#else
            Destroy(gameObject);
#endif

        }
#if UNITY_EDITOR
        void Update()
        {
            if (Input.GetKeyUp("x"))
            {
                IphoneXFrame.SetActive(!IphoneXFrame.activeInHierarchy);
            }

            if (Input.GetKeyUp("s"))
            {
                SafeArea.SetActive(!SafeArea.activeInHierarchy);
            }

            if (Input.GetKeyUp("r"))
            {
                SafeArea.transform.SetParent(null);                     // remove safe area
                IphoneXFrame.transform.Rotate(0, 180, 0);               // rotate it 
                SafeArea.transform.SetParent(IphoneXFrame.transform);   // put the safe area back on
            }
        }
#endif
    }
}
