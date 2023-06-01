using BrainCloud;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Used for the app's navigation.
/// </summary>
public class MainMenuUI : ContentUIBehaviour
{
    private const string PROFILE_ID_TEXT = "<align=left>Profile ID:</align>\n{0}";
    private const string ANONYMOUS_ID_TEXT = "<align=left>Anonymous ID:</align>\n{0}";
    private const string APP_INFO_TEXT = "{0} ({1}) v{2}";

    private static readonly int UI_IS_ACTIVE = Animator.StringToHash("IsActive");

    [Header("Main")]
    [SerializeField] private Animator MainMenuAnim = default;
    [SerializeField] private GameObject HeaderSpacer = default;
    [SerializeField] private TMP_Text AppInfoLabel = default;

    [Header("Buttons")]
    [SerializeField] private Button BlockerButton = default;
    [SerializeField] private Button OpenMenuButton = default;
    [SerializeField] private Button CloseMenuButton = default;
    [SerializeField] private MenuItemUI ProfileIDButton = default;
    [SerializeField] private MenuItemUI AnonymousIDButton = default;
    [SerializeField] private MenuItemUI LogoutButton = default;

    [Header("Menu Items")]
    [SerializeField] private Transform MenuContent = default;
    [SerializeField] private MenuItemUI MenuItemTemplate = default;
    [SerializeField] private ServiceItem[] ServiceItemUIs = default;

    [Header("UI Control")]
    [SerializeField] private PopupUI Popup = default;
    [SerializeField] private LoginContentUI LoginContent = default;
    [SerializeField] private AppContentUI AppContent = default;
    [SerializeField] private LoggerContentUI Logger = default;

    public bool MainMenuActive
    {
        get => MainMenuAnim.GetBool(UI_IS_ACTIVE);
        set => SetMainMenuActiveState(value);
    }

    private List<MenuItemUI> menuItems = default;

    #region Unity Messages

    protected override void Awake()
    {
        ProfileIDButton.Label = string.Empty;
        AnonymousIDButton.Label = string.Empty;

        base.Awake();
    }

    private void OnEnable()
    {
        OpenMenuButton.onClick.AddListener(OnOpenMenuButton);
        CloseMenuButton.onClick.AddListener(OnCloseMenuButton);
        BlockerButton.onClick.AddListener(OnCloseMenuButton);

        ProfileIDButton.ButtonAction = OnProfileIDButton;
        AnonymousIDButton.ButtonAction = OnAnonymousIDButton;
        LogoutButton.ButtonAction = OnLogoutButton;

        ProfileIDButton.enabled = true;
        AnonymousIDButton.enabled = true;
        LogoutButton.enabled = true;

        if (!menuItems.IsNullOrEmpty())
        {
            foreach (MenuItemUI item in menuItems)
            {
                item.enabled = true;
            }
        }
    }

    protected override void Start()
    {
        CreateMenuItems();
        ChangeToLoginContent();

        AppInfoLabel.text = string.Format(APP_INFO_TEXT, BCManager.AppName, BCManager.Client.AppId, BCManager.Client.AppVersion);

        InitializeUI();

        base.Start();
    }

    private void OnDisable()
    {
        OpenMenuButton.onClick.RemoveAllListeners();
        CloseMenuButton.onClick.RemoveAllListeners();
        BlockerButton.onClick.RemoveAllListeners();

        ProfileIDButton.ButtonAction = null;
        AnonymousIDButton.ButtonAction = null;
        LogoutButton.ButtonAction = null;

        ProfileIDButton.enabled = false;
        AnonymousIDButton.enabled = false;
        LogoutButton.enabled = false;

        if (!menuItems.IsNullOrEmpty())
        {
            foreach (MenuItemUI item in menuItems)
            {
                item.enabled = false;
            }
        }
    }

    protected override void OnDestroy()
    {
        menuItems.Clear();
        menuItems = null;

        base.OnDestroy();
    }

    #endregion

    #region UI

    public void ChangeToAppContent()
    {
        LoginContent.IsInteractable = false;
        AppContent.IsInteractable = true;

        LoginContent.gameObject.SetActive(false);
        AppContent.gameObject.SetActive(true);
        OpenMenuButton.gameObject.SetActive(true);
    }

    public void ChangeToLoginContent()
    {
        MainMenuActive = false;

        ProfileIDButton.gameObject.SetActive(false);
        AnonymousIDButton.gameObject.SetActive(false);
        HeaderSpacer.SetActive(true);

        LoginContent.IsInteractable = true;
        AppContent.IsInteractable = false;

        LoginContent.gameObject.SetActive(true);
        AppContent.gameObject.SetActive(false);
        OpenMenuButton.gameObject.SetActive(false);
    }

    protected override void InitializeUI()
    {
        MainMenuActive = false;
    }

    private void CreateMenuItems()
    {
        menuItems = new List<MenuItemUI>();

        foreach(ServiceItem serviceItem in ServiceItemUIs)
        {
            MenuItemUI menuItem = Instantiate(MenuItemTemplate, MenuContent);
            menuItem.gameObject.SetActive(true);
            menuItem.gameObject.SetName("{0}MenuItem", serviceItem.Name);
            menuItem.Label = serviceItem.Name;
            menuItem.ButtonAction = () => OnMenuItemButton(serviceItem);

            menuItems.Add(menuItem);
        }

        MenuItemTemplate.gameObject.SetActive(false);
    }

    private void SetMainMenuActiveState(bool isActive)
    {
        if (MainMenuActive == isActive)
        {
            return;
        }

        if (!UserHandler.ProfileID.IsEmpty())
        {
            ProfileIDButton.Label = string.Format(PROFILE_ID_TEXT, UserHandler.ProfileID);
            ProfileIDButton.gameObject.SetActive(true);
        }

        if (!UserHandler.AnonymousID.IsEmpty())
        {
            AnonymousIDButton.Label = string.Format(ANONYMOUS_ID_TEXT, UserHandler.AnonymousID);
            AnonymousIDButton.gameObject.SetActive(true);
        }

        HeaderSpacer.SetActive(!(ProfileIDButton.gameObject.activeSelf || AnonymousIDButton.gameObject.activeSelf));

        OpenMenuButton.gameObject.SetActive(!isActive);
        MainMenuAnim.SetBool(UI_IS_ACTIVE, isActive);
    }

    private void OnOpenMenuButton()
    {
        MainMenuActive = true;
    }

    private void OnCloseMenuButton()
    {
        MainMenuActive = false;
    }

    private void OnProfileIDButton()
    {
        if (!UserHandler.ProfileID.IsEmpty())
        {
            GUIUtility.systemCopyBuffer = UserHandler.ProfileID;
            Debug.Log($"Copied Profile ID ({UserHandler.ProfileID}) to clipboard.");
        }
        else
        {
            Debug.LogWarning("Profile ID not found. Was this enabled by mistake?");
        }
    }

    private void OnAnonymousIDButton()
    {
        if (!UserHandler.AnonymousID.IsEmpty())
        {
            GUIUtility.systemCopyBuffer = UserHandler.AnonymousID;
            Debug.Log($"Copied Anonymous ID ({UserHandler.AnonymousID}) to clipboard.");
        }
        else
        {
            Debug.LogWarning("Anonymous ID not found. Was this enabled by mistake?");
        }
    }

    private void OnMenuItemButton(ServiceItem serviceItem)
    {
        MainMenuActive = false;

        AppContent.LoadServiceItemContent(serviceItem);

        Debug.Log($"Opening {serviceItem.Name} Service UI");
    }

    private void OnLogoutButton()
    {
        MainMenuActive = false;

        PopupInfoBody logoutBody = new PopupInfoBody("Would you like to <b>disconnect</b> your account upon logout?", PopupInfoBody.Type.Centered);
        PopupInfoBody warningBody = new PopupInfoBody("Warning!\nYou are logged in anonymously. You will lose access to your account if you <b>disconnect!</b>", PopupInfoBody.Type.Error);

        PopupInfoBody[] bodyTexts;
        if (UserHandler.AnonymousUser)
        {
            bodyTexts = new PopupInfoBody[] { logoutBody, warningBody };
        }
        else
        {
            bodyTexts = new PopupInfoBody[] { logoutBody };
        }

        PopupInfoButton[] popupButtons = new PopupInfoButton[]
        {
            new PopupInfoButton("Logout", PopupInfoButton.Color.Plain, () => OnLogoutConfirm(false)),
            new PopupInfoButton("Logout and Disconnect", PopupInfoButton.Color.Plain, () => OnLogoutConfirm(true)),
        };

        Popup.DisplayPopup(new PopupInfo("Disconnect Account?", bodyTexts, popupButtons));
    }

    private void OnLogoutConfirm(bool disconnectAccount)
    {
        IsInteractable = false;

        SuccessCallback onSuccess = OnSuccess("Logging Out...", () =>
        {
#if FACEBOOK_SDK
            if(Facebook.Unity.FB.IsLoggedIn)
            {
                Facebook.Unity.FB.LogOut();
            }
#endif
#if GOOGLE_OPENID_SDK
            Google.GoogleSignIn.DefaultInstance.SignOut();
#endif
            if (disconnectAccount)
            {
                UserHandler.ResetAuthenticationData();
#if GOOGLE_OPENID_SDK
                Google.GoogleSignIn.DefaultInstance.Disconnect();
#endif
            }

            LoginContent.ResetRememberUserPref();
            ChangeToLoginContent();

            AppContent.ResetUI();
            Logger.ResetUI();
        });

        FailureCallback onFailure = OnFailure("Logout Failed! Please try again in a few moments.", () =>
        {
            IsInteractable = true;
        });

        UserHandler.HandleUserLogout(onSuccess, onFailure);
    }

    #endregion
}
