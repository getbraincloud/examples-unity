using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileModal : MonoBehaviour
{
    
    [SerializeField]
    private Animator _mainAnim;

    [SerializeField]
    private TMP_InputField _usernameInputField;
    [SerializeField]
    private Button _closeButton;
    [SerializeField]
    private Button _updateProfileButton;
    [SerializeField]
    private Button _restorePurchasesButton;
    [SerializeField]
    private Button _logoutButton;
    [SerializeField]
    private Image _profileImage;
    [SerializeField]
    private TextMeshProUGUI _projectVersionText, _bcVersionText, _serverVersionText;

    private bool _isLoggedOut = false;
    private Animator _modalAnim;

    private void Awake()
    {
        _modalAnim = GetComponent<Animator>();
    }
    public void OnFadeOutComplete()
    {
        if (_isLoggedOut)
        {
            _mainAnim.SetBool("FadeOut", true);
        }
    }

    private void OnEnable()
    {
        _closeButton.onClick.AddListener(OnCloseButtonClicked);
        _updateProfileButton.onClick.AddListener(OnUpdateProfileButtonClicked);
        _restorePurchasesButton.onClick.AddListener(OnRestorePurchasesButtonClicked);
        _logoutButton.onClick.AddListener(OnLogoutButtonClicked);

        GetVersions();

        _usernameInputField.text = AppManager.Instance.userData.PlayerName;
    }
    private void OnDisable()
    {
        _closeButton.onClick.RemoveAllListeners();
        _updateProfileButton.onClick.RemoveAllListeners();
        _restorePurchasesButton.onClick.RemoveAllListeners();
        _logoutButton.onClick.RemoveAllListeners();
    }

    private void GetVersions()
    {
        _bcVersionText.text = BCManager.Instance.BCWrapper.Client.BrainCloudClientVersion;
        _projectVersionText.text = Application.version;
        //get server version
        BCManager.Instance.BCWrapper.Client.GetAuthenticationService().getServerVersion(
            (string responseJson, object cbObject) =>
            {
                var root = JsonReader.Deserialize<Dictionary<string, object>>(responseJson)["data"] as Dictionary<string, object>;
                string serverVersion = root["serverVersion"] as string;
                _serverVersionText.text = serverVersion;
            }
        );
    }

    public void UpdateProfileImage(Sprite image)
    {
        _profileImage.sprite = image;
    }

    private void OnLogoutButtonClicked()
    {
        //Since user is choosing to log out their session, forget user to force re-authentication
        BCManager.Instance.BCWrapper.Logout(true, (string jsonResponse, object cbObject) =>
        {
            //logout success
            _isLoggedOut = true;
            //Set remember me to false to reset login modal state
            PlayerPrefs.SetInt(Globals.PP_REMEMBER_ME, 0);

            _modalAnim.SetBool("ShowModal", false);
        });

    }

    private void OnRestorePurchasesButtonClicked()
    {
        throw new NotImplementedException();
    }

    private void OnUpdateProfileButtonClicked()
    {
        AppManager.Instance.UpdatePlayerNameOnServer(_usernameInputField.text, () =>
        {
            //on success

        });
    }

    private void OnCloseButtonClicked()
    {
        _modalAnim.SetBool("ShowModal", false);
    }
}
