using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Used for the app's navigation.
/// </summary>
public class MainMenuUI : MonoBehaviour, IContentUI
{
    private const float HEADER_SPACER_HEIGHT = 50;
    private const string PROFILE_ID_TEXT = "Profile ID:\n{0}";
    private const string ANONYMOUS_ID_TEXT = "Anonymous ID:\n{0}";
    private const string APP_INFO_TEXT = "{0} ({1}) v{2}";

    private static readonly int UI_IS_ACTIVE = Animator.StringToHash("IsActive");

    [Header("Main")]
    [SerializeField] private CanvasGroup UICanvasGroup = default;
    [SerializeField] private Animator MainMenuAnim = default;
    [SerializeField] private Animator BlockerAnim = default;
    [SerializeField] private CanvasGroup HeaderCG = default;
    [SerializeField] private LayoutElement HeaderSpacer = default;

    [Header("Text")]
    [SerializeField] private TMP_Text HeaderLabel = default;
    [SerializeField] private TMP_Text AppInfoLabel = default;

    [Header("Buttons")]
    [SerializeField] private Button OpenMenuButton = default;
    [SerializeField] private Button CloseMenuButton = default;
    [SerializeField] private Button BlockerButton = default;
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

    private void Start()
    {
        CreateMenuItems();
        ChangeToLoginContent();

        AppInfoLabel.text = string.Format(APP_INFO_TEXT, BCManager.AppName, BCManager.Client.AppId, BCManager.Client.AppVersion);
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

    private void OnDestroy()
    {
        menuItems.Clear();
        menuItems = null;
    }

    #endregion

    #region UI

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
        HeaderCG.interactable = !isActive;
        OpenMenuButton.gameObject.SetActive(!isActive);
        MainMenuAnim.SetBool(UI_IS_ACTIVE, isActive);
        BlockerAnim.SetBool(UI_IS_ACTIVE, isActive);
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
        UserHandler.HandleUserLogout(() =>
        {
            ChangeToLoginContent();

            UserHandler.ResetAuthenticationData();

            AppContent.ShowDefaultContent();
            Logger.ResetLogger();
        },
        () =>
        {
            Logger.LogError("#APP - Logout Failed! Please try again in a few moments.");
        });

    }

    public void ChangeToAppContent()
    {
        if (!string.IsNullOrEmpty(UserHandler.ProfileID))
        {
            HeaderLabel.gameObject.SetActive(true);
            ShowProfileID();
            HeaderSpacer.preferredHeight = 0;
        }

        LoginContent.IsInteractable = false;
        AppContent.IsInteractable = true;

        LoginContent.gameObject.SetActive(false);
        AppContent.GameObject.SetActive(true);
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
        AppContent.GameObject.SetActive(false);
        OpenMenuButton.gameObject.SetActive(false);
    }

    public void ShowProfileID()
    {
        if (HeaderLabel.isActiveAndEnabled && !string.IsNullOrEmpty(UserHandler.ProfileID))
        {
            HeaderLabel.text = string.Format(PROFILE_ID_TEXT, UserHandler.ProfileID);
        }
    }

    public void ShowAnonymousID()
    {
        if (HeaderLabel.isActiveAndEnabled && !string.IsNullOrEmpty(UserHandler.AnonymousID))
        {
            HeaderLabel.text = string.Format(ANONYMOUS_ID_TEXT, UserHandler.AnonymousID);
        }
    }

    #endregion
}