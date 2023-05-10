using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// To help render canvases properly on devices with a notch and/or rounded corners.
/// </summary>
/// https://forum.unity.com/threads/canvashelper-resizes-a-recttransform-to-iphone-xs-safe-area.521107
[RequireComponent(typeof(Canvas))]
public class CanvasHelper : MonoBehaviour
{
    private static readonly List<CanvasHelper> helpers = new List<CanvasHelper>();

    public static UnityEvent OnResolutionOrOrientationChanged = new UnityEvent();

    private static bool screenChangeVarsInitialized = false;
    private static ScreenOrientation lastOrientation = ScreenOrientation.LandscapeLeft;
    private static Vector2 lastResolution = Vector2.zero;
    private static Rect lastSafeArea = Rect.zero;

    [SerializeField] private bool resizeHorizontal = true;
    [SerializeField] private bool resizeVertical = true;

    private Canvas canvas;
    private RectTransform safeAreaTransform;

    private void Awake()
    {
        if (!helpers.Contains(this))
        {
            helpers.Add(this);
        }

        canvas = GetComponent<Canvas>();

        safeAreaTransform = transform.Find("SafeArea") as RectTransform;

        if (!screenChangeVarsInitialized)
        {
            lastOrientation = Screen.orientation;
            lastResolution.x = Screen.width;
            lastResolution.y = Screen.height;
            lastSafeArea = Screen.safeArea;

            screenChangeVarsInitialized = true;
        }

        ApplySafeArea();
    }

    private void Update()
    {
        if (helpers[0] != this)
        {
            return;
        }

        if (Application.isMobilePlatform &&
            Screen.orientation != lastOrientation)
        {
            OrientationChanged();
        }

        if (Screen.safeArea != lastSafeArea)
        {
            SafeAreaChanged();
        }

        if (Screen.width != lastResolution.x ||
            Screen.height != lastResolution.y)
        {
            ResolutionChanged();
        }
    }

    private void ApplySafeArea()
    {
        if (safeAreaTransform == null)
        {
            return;
        }

        var safeArea = Screen.safeArea;

        var anchorMin = safeArea.position;
        var anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= canvas.pixelRect.width;
        anchorMin.y /= canvas.pixelRect.height;
        anchorMax.x /= canvas.pixelRect.width;
        anchorMax.y /= canvas.pixelRect.height;

        safeAreaTransform.anchorMin = new Vector2(resizeHorizontal ? anchorMin.x : safeAreaTransform.anchorMin.x,
                                                    resizeVertical ? anchorMin.y : safeAreaTransform.anchorMin.y);
        safeAreaTransform.anchorMax = new Vector2(resizeHorizontal ? anchorMax.x : safeAreaTransform.anchorMax.x,
                                                    resizeVertical ? anchorMax.y : safeAreaTransform.anchorMax.y);
    }

    private void OnDestroy()
    {
        if (helpers != null && helpers.Contains(this))
        {
            helpers.Remove(this);
        }
    }

    private static void OrientationChanged()
    {
        lastOrientation = Screen.orientation;
        lastResolution.x = Screen.width;
        lastResolution.y = Screen.height;

        OnResolutionOrOrientationChanged.Invoke();
    }

    private static void ResolutionChanged()
    {
        lastResolution.x = Screen.width;
        lastResolution.y = Screen.height;

        OnResolutionOrOrientationChanged.Invoke();
    }

    private static void SafeAreaChanged()
    {
        lastSafeArea = Screen.safeArea;

        for (int i = 0; i < helpers.Count; i++)
        {
            helpers[i].ApplySafeArea();
        }
    }
}
