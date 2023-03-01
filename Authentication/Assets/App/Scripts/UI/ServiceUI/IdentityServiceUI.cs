using BrainCloud;
using System.Net.Mail;
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
public class IdentityServiceUI : ContentUIBehaviour
{
    private const int MINIMUM_USERNAME_LENGTH = 4;
    private const int MINIMUM_PASSWORD_LENGTH = 6;

    [Header("Main")]
    [SerializeField] private TMP_InputField EmailLoginField = default;
    [SerializeField] private TMP_InputField EmailPasswordField = default;
    [SerializeField] private Button EmailAttachButton = default;
    [SerializeField] private Button EmailMergeButton = default;
    [SerializeField] private TMP_InputField UniversalLoginField = default;
    [SerializeField] private TMP_InputField UniversalPasswordField = default;
    [SerializeField] private Button UniversalAttachButton = default;
    [SerializeField] private Button UniversalMergeButton = default;

    private BrainCloudIdentity identityService = default;

    #region Unity Messages

    protected override void Awake()
    {
        EmailLoginField.text = string.Empty;
        EmailPasswordField.text = string.Empty;
        UniversalLoginField.text = string.Empty;
        UniversalPasswordField.text = string.Empty;

        base.Awake();
    }

    private void OnEnable()
    {
        EmailLoginField.onEndEdit.AddListener((email) => CheckEmailVerification(email));
        EmailPasswordField.onEndEdit.AddListener((password) => CheckPasswordVerification(EmailPasswordField, password));
        EmailAttachButton.onClick.AddListener(OnEmailAttachButton);
        EmailMergeButton.onClick.AddListener(OnEmailMergeButton);
        UniversalLoginField.onEndEdit.AddListener((username) => CheckUsernameVerification(username));
        UniversalPasswordField.onEndEdit.AddListener((password) => CheckPasswordVerification(UniversalPasswordField, password));
        UniversalAttachButton.onClick.AddListener(OnUniversalAttachButton);
        UniversalMergeButton.onClick.AddListener(OnUniversalMergeButton);
    }

    protected override void Start()
    {
        identityService = BCManager.IdentityService;

        base.Start();
    }

    private void OnDisable()
    {
        EmailLoginField.onEndEdit.RemoveAllListeners();
        EmailPasswordField.onEndEdit.RemoveAllListeners();
        EmailAttachButton.onClick.RemoveAllListeners();
        EmailMergeButton.onClick.RemoveAllListeners();
        UniversalLoginField.onEndEdit.RemoveAllListeners();
        UniversalPasswordField.onEndEdit.RemoveAllListeners();
        UniversalAttachButton.onClick.RemoveAllListeners();
        UniversalMergeButton.onClick.RemoveAllListeners();
    }

    protected override void OnDestroy()
    {
        identityService = null;

        base.OnDestroy();
    }

    #endregion

    #region UI

    protected override void InitializeUI()
    {
        EmailLoginField.text = string.Empty;
        EmailLoginField.DisplayNormal();
        EmailPasswordField.text = string.Empty;
        EmailPasswordField.DisplayNormal();
        UniversalLoginField.text = string.Empty;
        UniversalLoginField.DisplayNormal();
        UniversalPasswordField.text = string.Empty;
        UniversalPasswordField.DisplayNormal();
    }

    private bool CheckEmailVerification(string value)
    {
        EmailLoginField.text = value.Trim();
        if (!EmailLoginField.text.IsEmpty())
        {
            try
            {
                // Use MailAddress to validate the email address
                // NOTE: This is NOT a guaranteed way to validate an email address and this will NOT ping the email to
                // validate that it is a real email address. brainCloud DOES NOT validate this upon registration as well.
                // You should implement your own method of verificaiton.
                MailAddress validate = new MailAddress(EmailLoginField.text);

                string user = validate.User;
                string host = validate.Host;
                if (user.IsEmpty() || host.IsEmpty() ||
                    !host.Contains('.') || host.StartsWith('.') || host.EndsWith('.'))
                {
                    EmailLoginField.DisplayError();
                    Debug.LogError("Please use a valid email address.");
                    return false;
                }
            }
            catch
            {
                EmailLoginField.DisplayError();
                Debug.LogError("Please use a valid email address.");
                return false;
            }

            return true;
        }

        return false;
    }

    private bool CheckUsernameVerification(string value)
    {
        UniversalLoginField.text = value.Trim();
        if (!UniversalLoginField.text.IsEmpty())
        {
            if (UniversalLoginField.text.Length < MINIMUM_USERNAME_LENGTH)
            {
                UniversalLoginField.DisplayError();
                Debug.LogError($"Please use a username with at least {MINIMUM_USERNAME_LENGTH} characters.");
                return false;
            }

            return true;
        }

        return false;
    }

    private bool CheckPasswordVerification(TMP_InputField pwField, string value)
    {
        pwField.text = value.Trim();
        if (!pwField.text.IsEmpty())
        {
            if (pwField.text.Length < MINIMUM_PASSWORD_LENGTH)
            {
                pwField.DisplayError();
                Debug.LogError($"Please use a password with at least {MINIMUM_PASSWORD_LENGTH} characters.");
                return false;
            }

            return true;
        }

        return false;
    }

    private void OnEmailAttachButton()
    {
        if (CheckEmailVerification(EmailLoginField.text) &&
            CheckPasswordVerification(EmailPasswordField, EmailPasswordField.text))
        {
            IsInteractable = false;
            identityService.AttachEmailIdentity(EmailLoginField.text, EmailPasswordField.text,
                                                OnSuccess("AttachEmailIdentity Success", () => IsInteractable = true),
                                                OnFailure("AttachEmailIdentity Failed", () => IsInteractable = true));
        }
        else
        {
            EmailLoginField.DisplayError();
            EmailPasswordField.DisplayError();
            Debug.LogError("Please fill out both the email and password fields properly.");
        }
    }

    private void OnEmailMergeButton()
    {
        if (CheckEmailVerification(EmailLoginField.text) &&
            CheckPasswordVerification(EmailPasswordField, EmailPasswordField.text))
        {
            IsInteractable = false;
            identityService.MergeEmailIdentity(EmailLoginField.text, EmailPasswordField.text,
                                               OnSuccess("MergeEmailIdentity Success", () => IsInteractable = true),
                                               OnFailure("MergeEmailIdentity Failed", () => IsInteractable = true));
        }
        else
        {
            EmailLoginField.DisplayError();
            EmailPasswordField.DisplayError();
            Debug.LogError("Please fill out both the email and password fields properly.");
        }
    }

    private void OnUniversalAttachButton()
    {
        if (CheckUsernameVerification(UniversalLoginField.text) &&
            CheckPasswordVerification(UniversalPasswordField, UniversalPasswordField.text))
        {
            IsInteractable = false;
            identityService.AttachUniversalIdentity(UniversalLoginField.text, UniversalPasswordField.text,
                                                    OnSuccess("MergeUniversalIdentity Success", () => IsInteractable = true),
                                                    OnFailure("MergeUniversalIdentity Failed", () => IsInteractable = true));
        }
        else
        {
            UniversalLoginField.DisplayError();
            UniversalPasswordField.DisplayError();
            Debug.LogError("Please fill out both the username and password fields properly.");
        }
    }

    private void OnUniversalMergeButton()
    {
        if (CheckUsernameVerification(UniversalLoginField.text) &&
            CheckPasswordVerification(UniversalPasswordField, UniversalPasswordField.text))
        {
            IsInteractable = false;
            identityService.MergeUniversalIdentity(UniversalLoginField.text, UniversalLoginField.text,
                                                   OnSuccess("MergeUniversalIdentity Success", () => IsInteractable = true),
                                                   OnFailure("MergeUniversalIdentity Failed", () => IsInteractable = true));
        }
        else
        {
            UniversalLoginField.DisplayError();
            UniversalPasswordField.DisplayError();
            Debug.LogError("Please fill out both the username and password fields properly.");
        }
    }

    #endregion
}