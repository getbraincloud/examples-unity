using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;
using UnityEngine;

public class ExternalAuthPanel : ContentUIBehaviour
{
    [Header("Main")]
    [SerializeField] private Transform ButtonContent = default;
    [SerializeField] private ButtonContent ButtonTemplate = default;
    [SerializeField] private ExternalAuthItem[] AuthItems = default;

    [Header("UI Control")]
    [SerializeField] private MainMenuUI MainMenu = default;
    [SerializeField] private PopupUI Popup = default;
    [SerializeField] private LoginContentUI LoginContent = default;
    [SerializeField] private MainLoginPanelUI MainLoginPanel = default;

    private AuthenticationType selectedAuthenticationType = AuthenticationType.Unknown;
    private List<ButtonContent> authButtons = default;

    #region Unity Messages

    private void OnEnable()
    {
        if (!authButtons.IsNullOrEmpty())
        {
            foreach (ButtonContent button in authButtons)
            {
                button.enabled = true;
            }
        }
    }

    protected override void Start()
    {
        authButtons = new List<ButtonContent>();

        foreach (ExternalAuthItem authItem in AuthItems)
        {
            ButtonContent button = Instantiate(ButtonTemplate, ButtonContent);
            button.gameObject.SetActive(true);
            button.gameObject.SetName(authItem.Name, "{0}MenuItem");
            button.Label = authItem.Name;
            button.LeftIcon = authItem.Icon;
            button.LabelColor = authItem.LabelColor;
            button.LeftIconColor = authItem.IconColor;
            button.BackgroundColor = authItem.BackgroundColor;
            button.Button.onClick.AddListener(() => OnExternalAuthentication(authItem.AuthenticationType));

            authButtons.Add(button);
        }

        InitializeUI();

        base.Start();
    }

    private void OnDisable()
    {
        if (!authButtons.IsNullOrEmpty())
        {
            foreach (ButtonContent button in authButtons)
            {
                button.enabled = false;
            }
        }
    }

    protected override void OnDestroy()
    {
        authButtons.Clear();
        authButtons = null;

        base.OnDestroy();
    }

    #endregion

    #region UI & Authentication

    protected override void InitializeUI()
    {
        FacebookManager.Initialize(HandleFacebook);
    }

    private void HandleFacebook(bool isGameShown)
    {
        LoginContent.IsInteractable = isGameShown;
    }

    private void OnExternalAuthentication(AuthenticationType type)
    {
        LoginContent.IsInteractable = false;
        selectedAuthenticationType = type;

        if (type == AuthenticationType.Facebook || type == AuthenticationType.FacebookLimited)
        {
            UserHandler.AuthenticateFacebook(true,
                                             OnSuccess("Authentication Success", OnAuthenticationSuccess),
                                             OnFailure("Authentication Failed", OnAuthenticationFailure));
        }
        else
        {
            Debug.LogError($"Unknown Authentication Type: {type}");
        }
    }

    private void OnAuthenticationSuccess()
    {
        BCManager.IdentityService.GetIdentities(OnSuccess("Get Identities Success", OnGetIdentitiesSuccess),
                                                OnFailure("Get Identities Failed", OnAuthenticationFailure));
    }

    private void OnGetIdentitiesSuccess(string response)
    {
        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;

        UserHandler.AnonymousUser = (data["identities"] as Dictionary<string, object>).Count <= 0;

        BCManager.Wrapper.SetStoredAuthenticationType(selectedAuthenticationType.ToString());
        MainLoginPanel.SetRememberMePref(true);

        MainMenu.ChangeToAppContent();
    }

    private void OnAuthenticationFailure(ErrorResponse response)
    {
        Popup.DisplayPopup(new PopupInfo("Could not Authenticate",
                                         new PopupInfoBody[] { new PopupInfoBody(response.Message, PopupInfoBody.Type.Centered),
                                                               new PopupInfoBody("Please try again.", PopupInfoBody.Type.Centered) },
                                         null, true, "Close"));

        LoginContent.IsInteractable = true;
    }

    #endregion
}
