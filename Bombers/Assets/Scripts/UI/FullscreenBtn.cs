using UnityEngine;
using UnityEngine.UI;

namespace BrainCloudPhotonExample
{
    /// <summary>
    /// Allows toggling full screen mode
    /// </summary>
    public class FullscreenBtn : MonoBehaviour
    {
        private void Awake()
        {
            Button btn = GetComponent<Button>();
            GameObject btnSoundObj = GameObject.Find("ButtonSound");

            if (btn && btnSoundObj)
            {
                btn.onClick.AddListener(btnSoundObj.GetComponent<AudioSource>().Play);
            }
        }

        public void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
        }
    }
}
