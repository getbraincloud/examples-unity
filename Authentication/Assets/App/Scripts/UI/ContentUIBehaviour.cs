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

    /// <summary>
    /// Whether or not this ContentUIBehaviour and its child UI elements is interactable with Unity's EventSystem.
    /// </summary>
    public bool IsInteractable
    {
        get { return ContentUICG.interactable; }
        set { ContentUICG.interactable = value; }
    }

    /// <summary>
    /// The transparency of this ContentUIBehaviour and its child UI elements.
    /// </summary>
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

    /// <summary>
    /// Reset the ContentUIBehaviour to its initial state.
    /// </summary>
    public void ResetUI()
    {
        if (isInitialized)
        {
            InitializeUI();
        }
    }

    /// <summary>
    /// Override this to be able to set up the various UI elements to their initial states.
    /// </summary>
    protected abstract void InitializeUI();

    #endregion

    #region brainCloud Callback Helpers

    /// <summary>
    /// A quick method for ContentUIBehaviours to create a <see cref="SuccessCallback"/> with <see cref="BCManager.HandleSuccess(string, Action)"/>.
    /// </summary>
    protected SuccessCallback OnSuccess(string logMessage, Action onSuccess) =>
        BCManager.HandleSuccess(logMessage, onSuccess);

    /// <summary>
    /// A quick method for ContentUIBehaviours to create a <see cref="SuccessCallback"/> with <see cref="BCManager.HandleSuccess(string, Action{string})"/>.
    /// </summary>
    protected SuccessCallback OnSuccess(string logMessage, Action<string> onSuccessS) =>
        BCManager.HandleSuccess(logMessage, onSuccessS);

    /// <summary>
    /// A quick method for ContentUIBehaviours to create a <see cref="SuccessCallback"/> with <see cref="BCManager.HandleSuccess(string, Action{string, object})"/>.
    /// </summary>
    protected SuccessCallback OnSuccess(string logMessage, Action<string, object> onSuccessSO) =>
        BCManager.HandleSuccess(logMessage, onSuccessSO);

    /// <summary>
    /// A quick method for ContentUIBehaviours to create a <see cref="FailureCallback"/> with <see cref="BCManager.HandleFailure(string, Action)"/>.
    /// </summary>
    protected FailureCallback OnFailure(string errorMessage, Action onFailure) =>
        BCManager.HandleFailure(errorMessage, onFailure);

    /// <summary>
    /// A quick method for ContentUIBehaviours to create a <see cref="FailureCallback"/> with <see cref="BCManager.HandleFailure(string, Action{string})"/>.
    /// </summary>
    protected FailureCallback OnFailure(string errorMessage, Action<string> onFailureS) =>
        BCManager.HandleFailure(errorMessage, onFailureS);

    /// <summary>
    /// A quick method for ContentUIBehaviours to create a <see cref="FailureCallback"/> with <see cref="BCManager.HandleSuccess(string, Action{string, object})"/>.
    /// </summary>
    protected FailureCallback OnFailure(string errorMessage, Action<string, object> onFailureSO) =>
        BCManager.HandleFailure(errorMessage, onFailureSO);

    #endregion
}
