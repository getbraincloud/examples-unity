using BrainCloud;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>
/// Example of how user idenity can be handled via brainCloud's Identity service.
/// </para>
/// 
/// <seealso cref="BrainCloudIdentity"/>
/// </summary>
/// API Link: https://getbraincloud.com/apidocs/apiref/?csharp#capi-identity
public class IdentityServiceUI : MonoBehaviour, IContentUI
{
    [Header("Main")]
    [SerializeField] private CanvasGroup UICanvasGroup = default;
    [SerializeField] private TMP_InputField EmailLoginField = default;
    [SerializeField] private TMP_InputField EmailPasswordField = default;
    [SerializeField] private Button EmailAttachButton = default;
    [SerializeField] private Button EmailMergeButton = default;
    [SerializeField] private TMP_InputField UniversalLoginField = default;
    [SerializeField] private TMP_InputField UniversalPasswordField = default;
    [SerializeField] private Button UniversalAttachButton = default;
    [SerializeField] private Button UniversalMergeButton = default;

    private BrainCloudIdentity identityService = default;

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
        EmailLoginField.text = string.Empty;
        EmailPasswordField.text = string.Empty;
        UniversalLoginField.text = string.Empty;
        UniversalPasswordField.text = string.Empty;
    }

    private void OnEnable()
    {
        EmailAttachButton.onClick.AddListener(OnEmailAttachButton);
        EmailMergeButton.onClick.AddListener(OnEmailMergeButton);
        UniversalAttachButton.onClick.AddListener(OnUniversalAttachButton);
        UniversalMergeButton.onClick.AddListener(OnUniversalMergeButton);
    }

    private void Start()
    {
        identityService = BCManager.IdentityService;

        IsInteractable = true;
    }

    private void OnDisable()
    {
        EmailAttachButton.onClick.RemoveAllListeners();
        EmailMergeButton.onClick.RemoveAllListeners();
        UniversalAttachButton.onClick.RemoveAllListeners();
        UniversalMergeButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        identityService = null;
    }

    #endregion

    #region UI

    private void OnEmailAttachButton()
    {
        identityService.AttachEmailIdentity(EmailLoginField.text, EmailPasswordField.text);
    }

    private void OnEmailMergeButton()
    {
        identityService.MergeEmailIdentity(EmailLoginField.text, EmailPasswordField.text);
    }

    private void OnUniversalAttachButton()
    {
        identityService.AttachUniversalIdentity(UniversalLoginField.text, UniversalLoginField.text);
    }

    private void OnUniversalMergeButton()
    {
        identityService.MergeUniversalIdentity(UniversalLoginField.text, UniversalLoginField.text);
    }

    #endregion

    #region brainCloud

    private void OnSuccess(string response, object _)
    {
        // TODO: Better testing and handling
    }

    #endregion
}
