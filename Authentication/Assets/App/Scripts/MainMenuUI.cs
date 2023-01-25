using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    private const float HEADER_SPACER_HEIGHT = 50;
    private const string PROFILE_ID_TEXT = "Profile ID:\n{0}";
    private const string ANONYMOUS_ID_TEXT = "Anonymous ID:\n{0}";
    private const string APP_INFO_TEXT = "{0} ({1}) v{2}";

    private static readonly int UI_IS_ACTIVE = Animator.StringToHash("IsActive");

    [Header("Main")]
    [SerializeField] private BrainCloudManager BC = default;
    [SerializeField] private Animator MainMenuAnim = default;
    [SerializeField] private Animator BlockerAnim = default;
    [SerializeField] private Transform MenuContent = default;
    [SerializeField] private CanvasGroup HeaderCG = default;
    [SerializeField] private LayoutElement HeaderSpacer = default;

    [Header("Text")]
    [SerializeField] private TMP_Text HeaderLabel = default;
    [SerializeField] private TMP_Text AppInfoLabel = default;

    [Header("Buttons")]
    [SerializeField] private Button OpenMenuButton = default;
    [SerializeField] private Button CloseMenuButton = default;
    [SerializeField] private Button BlockerButton = default;
    [SerializeField] private Button LogoutButton = default;

    [Header("TEMP")]
    [SerializeField] private GameObject LoginContent = default;
    [SerializeField] private GameObject MainContent = default;

    public bool MainMenuActive
    {
        get => MainMenuAnim.GetBool(UI_IS_ACTIVE);
        set => SetMainMenuActiveState(value);
    }

    private void OnEnable()
    {
        OpenMenuButton.onClick.AddListener(OnOpenMenuButton);
        CloseMenuButton.onClick.AddListener(OnCloseMenuButton);
        BlockerButton.onClick.AddListener(OnCloseMenuButton);
        LogoutButton.onClick.AddListener(OnLogoutButton);

        // Enable all content buttons
    }

    private void Start()
    {
        DisableMainMenuUse();

        // Set App ID & Version properly
        AppInfoLabel.text = string.Format(APP_INFO_TEXT, BrainCloudManager.AppName, 00000, Application.version);
    }

    private void OnDisable()
    {
        OpenMenuButton.onClick.RemoveAllListeners();
        CloseMenuButton.onClick.RemoveAllListeners();
        BlockerButton.onClick.RemoveAllListeners();
        LogoutButton.onClick.RemoveAllListeners();

        // Disable all content buttons
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

    private void OnLogoutButton()
    {
        BC.HandlePlayerLogout(() =>
        {
            DisableMainMenuUse();

            LoginContent.SetActive(true);
            MainContent.SetActive(false);

            BC.ResetPlayerData();
        });
    }

    public void EnableMainMenuUse()
    {
        if (!string.IsNullOrEmpty(BC.ProfileID))
        {
            HeaderLabel.gameObject.SetActive(true);
            ShowProfileID();
            HeaderSpacer.preferredHeight = 0;
        }

        OpenMenuButton.gameObject.SetActive(true);
    }

    public void DisableMainMenuUse()
    {
        MainMenuActive = false;

        HeaderLabel.gameObject.SetActive(false);
        HeaderSpacer.preferredHeight = HEADER_SPACER_HEIGHT;
        OpenMenuButton.gameObject.SetActive(false);
    }

    public void ShowProfileID()
    {
        if (HeaderLabel.isActiveAndEnabled && !string.IsNullOrEmpty(BC.ProfileID))
        {
            HeaderLabel.text = string.Format(PROFILE_ID_TEXT, BC.ProfileID);
        }
    }

    public void ShowAnonymousID()
    {
        if (HeaderLabel.isActiveAndEnabled && !string.IsNullOrEmpty(BC.AnonymousID))
        {
            HeaderLabel.text = string.Format(ANONYMOUS_ID_TEXT, BC.AnonymousID);
        }
    }
}
