using Gameframework;
using PrimeTween;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StateManager : SingletonBehaviour<StateManager>
{
    [SerializeField] private GameObject CurrencyHolder;
    [SerializeField] private AnimationCurve CurrencyEasing;

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

    public void PlayCurrencyAnimation(CurrencyTypes currencyType, Vector3 startPosition, Vector3 endPosition)
    {
        var currencySprite = Instantiate(CurrencyHolder, null, false).GetComponentInChildren<Image>();
        currencySprite.sprite = AssetLoader.GetCurrencySprite(currencyType);
        currencySprite.transform.position = startPosition;

        Tween.Position(currencySprite.transform, startPosition, endPosition, 1.0f, Easing.Curve(CurrencyEasing), 1, CycleMode.Restart, 0.0f, 0.1f, true)
             .OnComplete(target: this, target => Destroy(currencySprite.transform.parent.gameObject));

        Tween.Scale(currencySprite.transform, 0.1f, 0.25f, Easing.Standard(Ease.OutQuad), 1, CycleMode.Restart, 0.75f, 0.0f, true);
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
            screen.ResetUI();
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
