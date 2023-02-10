using UnityEngine;

/// <summary>
/// Interface for some basic uniform functions for various UI screens and content.
/// </summary>
public interface IContentUI
{
    public bool IsInteractable { get; set; }

    public float Opacity { get; set; }

    public GameObject GameObject { get; }

    public Transform Transform { get; }

/* Template to copy for classes that make use of this interface
    [Header("Main")]
    [SerializeField] private CanvasGroup UICanvasGroup = default;

    private BrainCloudService bcService = default;
 
    #region IContentUI

    public bool IsInteractable
    {
        get { return UICanvasGroup.interactable; }
        set { UICanvasGroup.interactable = value; }
    }

    public float Opacity
    {
        get { return UICanvasGroup.alpha; }
        set { UICanvasGroup.alpha = value < 0.0f ? 0.0f : value > 1.0f ? 1.0f : value; }
    }

    public GameObject GameObject => gameObject;

    public Transform Transform => transform;

    #endregion

    #region Unity Messages

    private void Awake()
    {

    }

    private void OnEnable()
    {

    }

    private void Start()
    {
        bcService = BCManager.Service;
    }

    private void OnDisable()
    {

    }

    private void OnDestroy()
    {
        bcService = null;
    }

    #endregion

    #region UI

    private void OnInteractable()
    {

    }

    #endregion

    #region brainCloud

    private void OnServiceFunction()
    {

    }

    #endregion
*/
}
