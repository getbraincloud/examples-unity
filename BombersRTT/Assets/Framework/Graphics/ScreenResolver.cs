/*
 * This will scale the screen to the appropriate resolution based on the monitor the app is running on.
 * App is Windowed by default and will use an appropriate resolution that doesn't match or is greater than the monitor's resolution.
 * When Fullscreened (Alt + Enter), the app will use one of the matching supported resolutions, or the closest one that isn't greater than the monitor's resolution.
 *
 * Supported resolutions: 7680x4320 (8K), 5120x2880 (5K), 3840x2160 (4K), 2560x1440 (QHD), 1920x1080 (FHD), 1280x720 (HD, Default)
 */

using System.Collections.Generic;
using UnityEngine;

namespace Gameframework
{
    public class ScreenResolver : BaseBehaviour
    {
#if UNITY_EDITOR || !UNITY_STANDALONE
        private void Awake()
        {
            Destroy(gameObject);
        }
#elif UNITY_STANDALONE // This should only run on Standalone devices
        #region Static Helpers
        private readonly struct Resolution
        {
            public readonly int Width;
            public readonly int Height;

            public Resolution(int width, int height) { Width = width; Height = height; }
        }

        private const int HD_720P   = 720;
        private const int FHD_1080P = 1080;
        private const int QHD_1440P = 1440;
        private const int UHD_2160P = 2160;
        private const int UHD_2880P = 2880;
        private const int UHD_4320P = 4320;

        private static readonly List<int> ResolutionList = new List<int>
        {
            HD_720P, FHD_1080P, QHD_1440P, UHD_2160P, UHD_2880P, UHD_4320P
        };

        private static readonly Dictionary<int, Resolution> ResolutionConfigs = new Dictionary<int, Resolution>
        {
            { HD_720P,   new Resolution(1280, 720) },  { FHD_1080P, new Resolution(1920, 1080) },
            { QHD_1440P, new Resolution(2560, 1440) }, { UHD_2160P, new Resolution(3840, 2160) },
            { UHD_2880P, new Resolution(5120, 2880) }, { UHD_4320P, new Resolution(7680, 4320) }
        };
        #endregion

        private bool isFullScreen = false;
        private Resolution windowedConfig;
        private Resolution fullscreenConfig;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            int windowed = HD_720P, fullscreen = HD_720P;
            int screenHeight = Screen.currentResolution.height;
            foreach (int resolution in ResolutionList)
            {
                int compare = resolution - screenHeight;
                if (compare >= 0)
                {
                    windowed = resolution < screenHeight ? resolution : windowed;
                    fullscreen = resolution <= screenHeight ? resolution : fullscreen;
                }
            }

            windowedConfig = ResolutionConfigs[windowed];
            fullscreenConfig = ResolutionConfigs[fullscreen];

            Screen.SetResolution(windowedConfig.Width, windowedConfig.Height, FullScreenMode.Windowed);

            Debug.Log($"Setting Windowed Resolution to: {windowedConfig.Width}x{windowedConfig.Height}");
        }

        private void Update()
        {
            if(isFullScreen && !Screen.fullScreen)
            {
                isFullScreen = false;

                Screen.SetResolution(windowedConfig.Width, windowedConfig.Height, FullScreenMode.Windowed);

                Debug.Log($"Setting Windowed Resolution to: {windowedConfig.Width}x{windowedConfig.Height}");
            }
            else if (!isFullScreen && Screen.fullScreen)
            {
                isFullScreen = true;

                Screen.SetResolution(fullscreenConfig.Width, fullscreenConfig.Height, FullScreenMode.FullScreenWindow);

                Debug.Log($"Setting Windowed Resolution to: {fullscreenConfig.Width}x{fullscreenConfig.Height}");
            }
        }
#endif
        }
}
