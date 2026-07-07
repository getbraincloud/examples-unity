using System;
using System.Collections;
using Gameframework;
using UnityEngine;

public class StateManager : SingletonBehaviour<StateManager>
{
    [SerializeField] private PopUpUI _genericPopUpUI;

    [SerializeField] private AddCurrencyAnimation _addCurrencyAnimation;

    private Canvas _canvas;

    public override void Awake()
    {
        base.Awake();

        //Initialize managers
        if (BrainCloudManager.Instance != null)
        {
            BrainCloudManager.Instance.StartUp();
        }

        if (BrainCloudManager.Instance.CanReconnectUser())
        {
            BrainCloudManager.Instance.ReconnectUser();
            StartCoroutine(WaitToReconnect());
        }
        else
        {
            SceneLoader.LoadLevel(BitBuddiesConsts.LOGIN_SCENE_NAME);
            SceneLoader.ShowLoadingScreen();
        }
    }

    public void PlayCurrencyAnimationLocal(RectTransform in_startPosition, RectTransform target, CurrencyTypes in_currencyType, RectTransform canvasRect, Vector2 offset)
    {
        var endPosition = GetCanvasLocalPos(canvasRect, target) + offset;
        var startPosition = GetCanvasLocalPos(canvasRect, in_startPosition);

        var animation = Instantiate(_addCurrencyAnimation, in_startPosition.parent);
        animation.PlayLocal(startPosition, endPosition, in_currencyType);
    }

    public void PlayCurrencyAnimationWorld(RectTransform in_startPosition, RectTransform target, CurrencyTypes in_currencyType, RectTransform canvasRect, Transform parent)
    {
        Vector2 startPosition = in_startPosition.position;
        Vector2 endPosition = (Vector2)target.position;

        var animation = Instantiate(_addCurrencyAnimation, parent);
        animation.PlayWorld(startPosition, endPosition, in_currencyType);
    }

    private Vector2 GetOverlayCanvasPos(RectTransform canvasRect, RectTransform target)
    {
        // Walk up to find position relative to the root canvas
        Vector2 localPos = target.anchoredPosition;
        Transform current = target.parent;

        while (current != null && current != canvasRect)
        {
            localPos += (Vector2)current.localPosition;
            current = current.parent;
        }

        return localPos;
    }

    private Vector2 GetCanvasLocalPos(RectTransform canvasRect, RectTransform target)
    {
        if (_canvas == null)
        {
            _canvas = canvasRect.GetComponent<Canvas>();
            Debug.Log($"Canvas scale factor: {_canvas.scaleFactor}");
        }
        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(cam, target.position),
            cam,
            out Vector2 localPoint
        );
        return localPoint;
    }

    //The idea here is to use InitializeUI to re-assign the UI elements to the updated variables. 
    public void RefreshScreen()
    {
        GameManager.Instance.UpdateSelectedAppChildrenInfo();
        var screens = FindObjectsByType<ContentUIBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (ContentUIBehaviour screen in screens)
        {
            screen.RefreshScreen();
        }
    }

    private IEnumerator WaitToReconnect()
    {
        yield return new WaitUntil(() => !BrainCloudManager.Instance.IsProcessingRequest);
        SceneLoader.LoadLevel(BitBuddiesConsts.PARENT_SCENE_NAME);
        SceneLoader.ShowLoadingScreen();
        yield return null;
    }

    public void GoToParent()
    {
        SceneLoader.LoadLevel(BitBuddiesConsts.PARENT_SCENE_NAME);
        SceneLoader.ShowLoadingScreen();
    }

    public void GoToLogin()
    {
        SceneLoader.LoadLevel(BitBuddiesConsts.LOGIN_SCENE_NAME);
        SceneLoader.ShowLoadingScreen();
    }

    public void GoToBuddysRoom()
    {
        if (BrainCloudManager.Instance.IsProcessingRequest)
        {
            SceneLoader.LoadLevelWithCondition
            (
                BitBuddiesConsts.GAME_SCENE_NAME,
                waitCondition: () => !BrainCloudManager.Instance.IsProcessingRequest
            );
        }
        else
        {
            SceneLoader.LoadLevel(BitBuddiesConsts.GAME_SCENE_NAME);
            SceneLoader.ShowLoadingScreen();
        }

        SceneLoader.ShowLoadingScreen();
    }
}
