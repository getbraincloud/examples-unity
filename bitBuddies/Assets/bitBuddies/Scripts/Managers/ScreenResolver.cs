using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScreenResolver : MonoBehaviour
{
#pragma warning disable CS0414
    [SerializeField] private bool LaunchAppInFullScreen = false;
#pragma warning restore CS0414

#if UNITY_EDITOR || !UNITY_STANDALONE
    private void Awake()
    {
        Destroy(this);
    }

#elif UNITY_STANDALONE // This should only run on Standalone devices
    private readonly struct ScreenConfigs
    {
        public readonly int Width;
        public readonly int Height;

        public ScreenConfigs(int width, int height) { Width = width; Height = height; }
    }

    #region Static Helpers
    private const int STANDARD  = 576;
    private const int HD_720P   = 720;
    private const int FHD_1080P = 1080;
    private const int QHD_1440P = 1440;
    private const int UHD_2160P = 2160;
    private const int UHD_2880P = 2880;
    private const int UHD_4320P = 4320;
    private const float ASPECT  = 16.0f / 9.0f;

    private static readonly List<int> ResolutionList = new List<int>
    {
        STANDARD, HD_720P, FHD_1080P, QHD_1440P, UHD_2160P, UHD_2880P, UHD_4320P
    };

    private static readonly Dictionary<int, ScreenConfigs> ResolutionConfigs = new Dictionary<int, ScreenConfigs>
    {
        { STANDARD,  new ScreenConfigs(1024, 576) },
        { HD_720P,   new ScreenConfigs(1280, 720) },  { FHD_1080P, new ScreenConfigs(1920, 1080) },
        { QHD_1440P, new ScreenConfigs(2560, 1440) }, { UHD_2160P, new ScreenConfigs(3840, 2160) },
        { UHD_2880P, new ScreenConfigs(5120, 2880) }, { UHD_4320P, new ScreenConfigs(7680, 4320) }
    };
    #endregion

    private bool isFullScreen = false;
    private int currentPixels = 0;
    private ScreenConfigs fullscreenConfig;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(ResolveScreenSize());
    }

    private IEnumerator ResolveScreenSize()
    {
        yield return null;

        Resolution resolutions = Screen.resolutions[0];
        foreach (Resolution supported in Screen.resolutions)
        {
            resolutions = resolutions.height < supported.height ? supported : resolutions;
        }

        Debug.Log($"Max Resolution: {resolutions.width}x{resolutions.height}");

        int windowed = ResolutionList[0]; int fullscreen = ResolutionList[0];
        int screenHeight = resolutions.height;
        foreach (int resolution in ResolutionList)
        {
            int compare = screenHeight - resolution;

            if (compare >= 0)
            {
                windowed = resolution < screenHeight ? resolution : windowed;
                fullscreen = resolution <= screenHeight ? resolution : fullscreen;
            }
        }

        fullscreenConfig = ResolutionConfigs[fullscreen];

        if (LaunchAppInFullScreen)
        {
            isFullScreen = true;

#if !UNITY_STANDALONE_OSX
            Screen.SetResolution(fullscreenConfig.Width, fullscreenConfig.Height, FullScreenMode.FullScreenWindow);
#else
            Screen.SetResolution(fullscreenConfig.Width, fullscreenConfig.Height, FullScreenMode.MaximizedWindow);
#endif
        }
    }

    private void Update()
    {
        // Handle Fullscreen Switching
        if (isFullScreen && !Screen.fullScreen)
        {
            isFullScreen = false;
        }
        else if (!isFullScreen && Screen.fullScreen)
        {
            isFullScreen = true;

#if !UNITY_STANDALONE_OSX
            Screen.SetResolution(fullscreenConfig.Width, fullscreenConfig.Height, FullScreenMode.FullScreenWindow);
#else
            Screen.SetResolution(fullscreenConfig.Width, fullscreenConfig.Height, FullScreenMode.MaximizedWindow);
#endif
            currentPixels = fullscreenConfig.Width * fullscreenConfig.Height;
            return;
        }

        // Handle Window Resize
        if (!isFullScreen && currentPixels != (Screen.width * Screen.height))
        {
            Screen.SetResolution(Mathf.RoundToInt(Screen.height * ASPECT), Screen.height, FullScreenMode.Windowed);
        }

        currentPixels = Screen.width * Screen.height;
    }
#endif
}
