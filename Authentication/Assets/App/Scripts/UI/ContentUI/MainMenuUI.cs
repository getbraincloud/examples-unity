using BrainCloud;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Used for the app's navigation.
/// </summary>
public class MainMenuUI : ContentUIBehaviour
{
    private const float HEADER_SPACER_HEIGHT = 50;
    private const string PROFILE_ID_TEXT = "Profile ID:\n{0}";
    private const string ANONYMOUS_ID_TEXT = "Anonymous ID:\n{0}";
    private const string APP_INFO_TEXT = "{0} ({1}) v{2}";

    private static readonly int UI_IS_ACTIVE = Animator.StringToHash("IsActive");

    [Header("Main")]
    [SerializeField] private Animator MainMenuAnim = default;
    [SerializeField] private Button BlockerButton = default;
    [SerializeField] private LayoutElement HeaderSpacer = default;

    [Header("Text")]
    [SerializeField] private TMP_Text HeaderLabel = default;
    [SerializeField] private TMP_Text AppInfoLabel = default;

    [Header("Buttons")]
    [SerializeField] private Button OpenMenuButton = default;
    [SerializeField] private Button CloseMenuButton = default;
    [SerializeField] private MenuItemUI LogoutButton = default;

    [Header("Menu Items")]
    [SerializeField] private Transform MenuContent = default;
    [SerializeField] private MenuItemUI MenuItemTemplate = default;
    [SerializeField] private ServiceItem[] ServiceItemUIs = default;

    [Header("UI Control")]
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
        HeaderLabel.text = string.Empty;
        AppInfoLabel.text = string.Empty;

        base.Awake();
    }

    private void OnEnable()
    {
        OpenMenuButton.onClick.AddListener(OnOpenMenuButton);
        CloseMenuButton.onClick.AddListener(OnCloseMenuButton);
        BlockerButton.onClick.AddListener(OnCloseMenuButton);

        if (!menuItems.IsNullOrEmpty())
        {
            foreach (MenuItemUI item in menuItems)
            {
                item.enabled = true;
            }
        }

        LogoutButton.ButtonAction = OnLogoutButton;
        LogoutButton.enabled = true;
    }

    protected override void Start()
    {
        CreateMenuItems();
        ChangeToLoginContent();

        AppInfoLabel.text = string.Format(APP_INFO_TEXT, BCManager.AppName, BCManager.Client.AppId, BCManager.Client.AppVersion);

        base.Start();

        InitializeUI();
    }

    private void OnDisable()
    {
        OpenMenuButton.onClick.RemoveAllListeners();
        CloseMenuButton.onClick.RemoveAllListeners();
        BlockerButton.onClick.RemoveAllListeners();

        if (!menuItems.IsNullOrEmpty())
        {
            foreach (MenuItemUI item in menuItems)
            {
                item.enabled = false;
            }
        }

        LogoutButton.ButtonAction = null;
        LogoutButton.enabled = false;
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
        if (!UserHandler.ProfileID.IsEmpty())
        {
            HeaderLabel.gameObject.SetActive(true);
            ShowProfileID();
            HeaderSpacer.preferredHeight = 0;
        }

        LoginContent.IsInteractable = false;
        AppContent.IsInteractable = true;

        LoginContent.gameObject.SetActive(false);
        AppContent.gameObject.SetActive(true);
        OpenMenuButton.gameObject.SetActive(true);
    }

    public void ChangeToLoginContent()
    {
        MainMenuActive = false;

        HeaderLabel.gameObject.SetActive(false);
        HeaderSpacer.preferredHeight = HEADER_SPACER_HEIGHT;

        LoginContent.IsInteractable = true;
        AppContent.IsInteractable = false;

        LoginContent.gameObject.SetActive(true);
        AppContent.gameObject.SetActive(false);
        OpenMenuButton.gameObject.SetActive(false);
    }

    public void ShowProfileID()
    {
        if (HeaderLabel.gameObject.activeSelf && !UserHandler.ProfileID.IsEmpty())
        {
            HeaderLabel.text = string.Format(PROFILE_ID_TEXT, UserHandler.ProfileID);
        }
    }

    public void ShowAnonymousID()
    {
        if (HeaderLabel.gameObject.activeSelf && !UserHandler.AnonymousID.IsEmpty())
        {
            HeaderLabel.text = string.Format(ANONYMOUS_ID_TEXT, UserHandler.AnonymousID);
        }
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
            menuItem.gameObject.SetName(serviceItem.Name, "{0}MenuItem");
            menuItem.Label = serviceItem.Name;
            menuItem.ButtonAction = () => OnMenuItemButton(serviceItem);

            menuItems.Add(menuItem);
        }

        MenuItemTemplate.gameObject.SetActive(false);
    }

    private void SetMainMenuActiveState(bool isActive)
    {
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

    private void OnMenuItemButton(ServiceItem serviceItem)
    {
        MainMenuActive = false;

        AppContent.LoadServiceItemContent(serviceItem);

        Debug.Log($"Opening {serviceItem.Name} Service UI");
    }

    private void OnLogoutButton()
    {
        IsInteractable = false;

        SuccessCallback onSuccess = OnSuccess("Logging Out...", () =>
        {
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
