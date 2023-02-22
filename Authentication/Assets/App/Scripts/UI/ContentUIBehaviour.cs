using BrainCloud;
using System;
using UnityEngine;

/// <summary>
/// Base class for the various UI screens to inherit from.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class ContentUIBehaviour : MonoBehaviour
{
    [SerializeField] private CanvasGroup ContentUICG = default;

    [NonSerialized] public new GameObject gameObject = default; // Cache these for faster access
    [NonSerialized] public new Transform transform = default;

    public bool IsInteractable
    {
        get { return ContentUICG.interactable; }
        set { ContentUICG.interactable = value; }
    }

    public float Opacity
    {
        get { return ContentUICG.alpha; }
        set { ContentUICG.alpha = value < 0.0f ? 0.0f : value > 1.0f ? 1.0f : value; }
    }

    private bool isInitialized = false;

    #region Unity Messages

    protected virtual void Awake()
    {
        gameObject = base.gameObject;
        transform = base.transform;
    }

    protected virtual void Start()
    {
        isInitialized = true;
    }

    protected virtual void OnDestroy()
    {
        gameObject = null;
        transform = null;
        isInitialized = false;
    }

    #endregion

    #region UI

    public void ResetUI()
    {
        if (isInitialized)
        {
            InitializeUI();
        }
    }

    protected abstract void InitializeUI();

    #endregion

    #region brainCloud

    protected SuccessCallback OnSuccess(string response, Action onSuccess) =>
        BCManager.HandleSuccess(response, onSuccess);

    protected SuccessCallback OnSuccess(string response, Action<string> onSuccess) =>
        BCManager.HandleSuccess(response, onSuccess);

    protected FailureCallback OnFailure(string errorMessage, Action onFailure) =>
        BCManager.HandleFailure(errorMessage, onFailure);

    protected FailureCallback OnFailure(string errorMessage, Action<string> onFailure) =>
        BCManager.HandleFailure(errorMessage, onFailure);

    #endregion
}
