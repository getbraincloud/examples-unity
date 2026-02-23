using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using BrainCloud.Plugin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ExampleApp;
using JsonReader = BrainCloud.JsonFx.Json.JsonReader;

public class LoginModal : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button LoginButton;
    [SerializeField] private Toggle RememberMeToggle;
    [SerializeField] private TMP_InputField userNameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private GameObject spinner;
    [SerializeField] private Animator bgAnim;

    private Animator _anim;
    private CanvasGroup _cg;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        _anim = GetComponent<Animator>();
    }

    private int InputSelected;
    #region Unity Messages

    private void OnEnable()
    {
        LoginButton.onClick.AddListener(OnLoginButton);
        RememberMeToggle.onValueChanged.AddListener(OnRememberMeToggled);

        //get value of remember me toggle that was previously saved
        if (PlayerPrefs.HasKey(Globals.PP_REMEMBER_ME))
        {
            int rememberMe = PlayerPrefs.GetInt(Globals.PP_REMEMBER_ME);
            RememberMeToggle.isOn = rememberMe == 1;
        }
    }

    private void OnDisable()
    {
        LoginButton.onClick.RemoveAllListeners();
        RememberMeToggle.onValueChanged.RemoveAllListeners();
    }
    #endregion
    private void OnRememberMeToggled(bool isOn)
    {
        PlayerPrefs.SetInt(Globals.PP_REMEMBER_ME, isOn ? 1 : 0);
    }
    private void OnLoginButton()
    {
        _cg.interactable = false;
        _anim.SetBool("loading", true);
        spinner.SetActive(true);
        BCManager.Instance.BCWrapper.AuthenticateUniversal(userNameInput.text, passwordInput.text, true, OnAuthenticationSuccess,
                                 OnAuthenticationFailure,
                                 this);
    }

    private void OnAuthenticationSuccess(string responseJson, object cbObject)
    {
        BCManager.Instance.BCWrapper.SetStoredAuthenticationType(AuthenticationType.Universal.ToString());

        AppManager.Instance.ProcessUserData(responseJson, () =>
        {
            _anim.SetBool("fadeOut", true);
        });

        Debug.Log($"User Anonymous ID: {BCManager.Instance.BCWrapper.GetStoredAnonymousId()}");

        Debug.Log("Authentication success! You are now logged into your app on brainCloud.");
    }

    private void OnAuthenticationFailure(int status, int reason, string jsonError, object cbObject)
    {
        BCManager.Instance.BCWrapper.ResetStoredAuthenticationType();

        AppManager.Instance.OnBrainCloudError(status, reason, jsonError, cbObject);

        Debug.LogError($"Authentication failed! Please try again.");

        _cg.interactable = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && Input.GetKey(KeyCode.LeftShift))
        {
            InputSelected--;
            if (InputSelected < 0) InputSelected = 0;
            SelectInputField();
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            InputSelected++;
            if (InputSelected > 1) InputSelected = 0;
            SelectInputField();
        }

        void SelectInputField()
        {
            switch (InputSelected)
            {
                case 0: userNameInput.Select();
                    break;
                case 1: passwordInput.Select();
                    break;
                case 2: RememberMeToggle.Select();
                    break;
            }
        }
    }

    public void OnFadeOutComplete()
    {
        AppManager.Instance.SwitchScenes("Main");
    }

    public void UsernameSelected() => InputSelected = 0;
    public void PasswordSelected() => InputSelected = 1;
    public void RememberMeSelected() => InputSelected = 2;
}
