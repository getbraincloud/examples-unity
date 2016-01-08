using UnityEngine;

namespace BrainCloudPhotonExample
{
    /// <summary>
    /// Allows toggling full screen mode
    /// </summary>
    public class FullscreenBtn : MonoBehaviour
    {
        public void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
        }
    }
}
