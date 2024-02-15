using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserContentUI : ContentUIBehaviour
{
    [Header("User Settings")]
    [SerializeField] private TMP_InputField UsernameField = default;
    [SerializeField] private Button PictureButton = default;
    [SerializeField] private Image ProfilePictureImage = default;
    [SerializeField] private Sprite DefaultProfilePicture = default;
    [SerializeField] private Button ClearButton = default;

    [Header("Controls")]
    [SerializeField] private Button ResetPasswordButton = default;
    [SerializeField] private Button SaveButton = default;
    [SerializeField] private Button BackButton = default;

    [Header("Navigation")]
    [SerializeField] private MainContentUI MainContent = default;

    #region Unity Messages

    protected override void Awake()
    {
        UsernameField.text = string.Empty;
        ProfilePictureImage.sprite = null;

        base.Awake();
    }

    private void OnEnable()
    {
        PictureButton.onClick.AddListener(OnPictureButton);
        ClearButton.onClick.AddListener(OnClearButton);
        ResetPasswordButton.onClick.AddListener(OnResetPasswordButton);
        SaveButton.onClick.AddListener(OnSaveButton);
        BackButton.onClick.AddListener(OnBackButton);
    }

    protected override void Start()
    {
        InitializeUI();

        base.Start();

        IsInteractable = false;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        PictureButton.onClick.RemoveAllListeners();
        ClearButton.onClick.RemoveAllListeners();
        ResetPasswordButton.onClick.RemoveAllListeners();
        SaveButton.onClick.RemoveAllListeners();
        BackButton.onClick.RemoveAllListeners();
    }

    protected override void OnDestroy()
    {
        //

        base.OnDestroy();
    }

    #endregion

    #region UI

    protected override void InitializeUI()
    {
        UsernameField.text = "John Smith"; // Need to get Username
        ProfilePictureImage.sprite = DefaultProfilePicture; // Need to get proper Profile Picture
    }

    private void OnPictureButton()
    {
        // Need to be able to upload new profile picture
    }
    private void OnClearButton()
    {
        // Need to clear profile picture
    }

    private void OnResetPasswordButton()
    {
        // Need to reset password
    }

    private void OnSaveButton()
    {
        // Need to save settings
    }

    private void OnBackButton()
    {
        MainContent.IsInteractable = true;
        MainContent.gameObject.SetActive(true);

        IsInteractable = false;
        gameObject.SetActive(false);
    }

    #endregion

    #region brainCloud

    private void OnServiceFunction()
    {
        //
    }

    #endregion
}
