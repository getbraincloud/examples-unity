using BrainCloud;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IdentityServiceUI : MonoBehaviour, IServiceUI
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

    public bool IsInteractable
    {
        get { return UICanvasGroup.interactable; }
        set { UICanvasGroup.interactable = value; }
    }

    private BrainCloudIdentity identityService = default;

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
}
