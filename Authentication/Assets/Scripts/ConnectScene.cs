﻿using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using BrainCloud.Common;

public class ConnectScene : MonoBehaviour
{
    public GameObject MainScene;
    public BCConfig BCConfig;
    
    public static BrainCloudWrapper _bc;
    
    Vector2 m_scrollPosition;
    string m_authStatus = "Welcome to brainCloud!";
    int m_selectedAuth = 0;
    enum eAuthTypes {
        EMAIL,
        UNIVERSAL,
        ANONYMOUS,
        GOOGLE,
        FACEBOOK,
        XBOXLIVE
    };

    string[] m_authTypes = {
        "Email",
        "Universal",
        "Anonymous",
        "Google (Android Devices)",
        "Facebook"
    };
    
    private bool signedIn;
    private int playerNumber;

    bool useAdvancedAuthentication = false;
    AuthenticationType selectedAdvancedAuthType;

    eAuthTypes currentAuthType;

    string inputProfileId;
    string inputPassword;
    string inputEntityName;
    string inputEntityAge;

    int statusTextInitSize;
    const int STATUS_SHRINK_SIZE = 8;

    const string GOOGLE_AUTH_INFO = "Only for Android";
    const string FACEBOOK_AUTH_INFO = "Only for WEBGL"; 

    //UI elements set in editor.
    [SerializeField] Text statusText;
    [SerializeField] Text profileIdText;
    [SerializeField] Text passwordText;
    [SerializeField] InputField profileIdField;
    [SerializeField] InputField passwordField;
    [SerializeField] InputField entityNameField;
    [SerializeField] InputField entityAgeField;
    [SerializeField] Text entityNameText;
    [SerializeField] Text entityAgeText;
    [SerializeField] Text advAuthInfoText;
    [SerializeField] Toggle advAuthToggle;
    [SerializeField] Text extraAuthInfoText; 
    [SerializeField] Button resetProfileIdButton;
    [SerializeField] Button resetAnonIdButton;
    [SerializeField] Text storedProfileIdText;
    [SerializeField] Text storedAnonymousIdText;
    [SerializeField] Text googleIdText;
    [SerializeField] Text serverAuthCodeText;
    [SerializeField] Button googleSignInButton; 

    void Start()
    {
        ChangeAuthType(eAuthTypes.EMAIL);

        statusTextInitSize = statusText.fontSize; 
    }

    private void OnEnable()
    {
        statusText.fontSize = statusTextInitSize;
        statusText.text = "Welcome to brainCloud";

        if (BrainCloudInterface.instance == null)
            return;
        
        storedProfileIdText.text = BrainCloudInterface.instance.GetStoredProfileID();
        storedAnonymousIdText.text = BrainCloudInterface.instance.GetStoredAnonymousID();
        
    }

    void ChangeAuthType(eAuthTypes authType)
    {
        switch(authType)
        {
            case eAuthTypes.EMAIL:
                selectedAdvancedAuthType = AuthenticationType.Email;
                SetScreen("Email", "Password", authType);
                advAuthToggle.interactable = true;
                break;
            case eAuthTypes.UNIVERSAL:
                selectedAdvancedAuthType = AuthenticationType.Universal;
                SetScreen("User ID", "Password", authType);
                advAuthToggle.interactable = true;
                break;
            case eAuthTypes.ANONYMOUS:
                selectedAdvancedAuthType = AuthenticationType.Anonymous;
                SetScreen("Profile ID", "Anonymous", authType);
                advAuthToggle.interactable = true;
                break;
            case eAuthTypes.GOOGLE:
                selectedAdvancedAuthType = AuthenticationType.Google;
                SetScreen("Profile ID", "Anonymous", authType);
                advAuthToggle.interactable = false;
                break;
            case eAuthTypes.FACEBOOK:
                selectedAdvancedAuthType = AuthenticationType.Facebook;
                SetScreen("Profile ID", "Anonymous", authType);
                advAuthToggle.interactable = false;
                break;
            case eAuthTypes.XBOXLIVE:
                SetScreen("Profile ID", "Anonymous", authType);
                advAuthToggle.interactable = false;
                break;
        }
    }

    void SetScreen(string profileIdInfo, string passwordInfo, eAuthTypes authType)
    {
        profileIdText.text = profileIdInfo + ":";
        passwordText.text = passwordInfo + ":";

        ActivateAdvancedAuthFields();

        //Deactivating non-constant elements first to make method simpler.
        profileIdField.gameObject.SetActive(false);
        passwordField.gameObject.SetActive(false);
        passwordField.contentType = InputField.ContentType.Password;
        resetProfileIdButton.gameObject.SetActive(false);
        resetAnonIdButton.gameObject.SetActive(false);
        storedProfileIdText.gameObject.SetActive(false);
        storedAnonymousIdText.gameObject.SetActive(false);
        googleIdText.gameObject.SetActive(false);
        serverAuthCodeText.gameObject.SetActive(false);
        googleSignInButton.gameObject.SetActive(false);
        extraAuthInfoText.text = "";

        switch (authType)
        {
            case eAuthTypes.EMAIL:
                profileIdField.gameObject.SetActive(true);
                passwordField.gameObject.SetActive(true);
                break;
            case eAuthTypes.UNIVERSAL:
                profileIdField.gameObject.SetActive(true);
                passwordField.gameObject.SetActive(true);
                break;
            case eAuthTypes.ANONYMOUS:
                resetProfileIdButton.gameObject.SetActive(true);
                resetAnonIdButton.gameObject.SetActive(true);
                storedProfileIdText.gameObject.SetActive(true);
                storedAnonymousIdText.gameObject.SetActive(true);
                storedProfileIdText.text = BrainCloudInterface.instance.GetStoredProfileID();
                storedAnonymousIdText.text = BrainCloudInterface.instance.GetStoredAnonymousID();
                break;
            case eAuthTypes.GOOGLE:
                resetProfileIdButton.gameObject.SetActive(true);
                resetAnonIdButton.gameObject.SetActive(true);
                storedProfileIdText.gameObject.SetActive(true);
                storedAnonymousIdText.gameObject.SetActive(true);
                storedProfileIdText.text = BrainCloudInterface.instance.GetStoredProfileID();
                storedAnonymousIdText.text = BrainCloudInterface.instance.GetStoredAnonymousID();
                googleIdText.gameObject.SetActive(true);
                serverAuthCodeText.gameObject.SetActive(true);
                googleSignInButton.gameObject.SetActive(true);
                advAuthToggle.isOn = false;
                extraAuthInfoText.text = GOOGLE_AUTH_INFO;
                break;
            case eAuthTypes.FACEBOOK:
                resetProfileIdButton.gameObject.SetActive(true);
                resetAnonIdButton.gameObject.SetActive(true);
                storedProfileIdText.gameObject.SetActive(true);
                storedAnonymousIdText.gameObject.SetActive(true);
                storedProfileIdText.text = BrainCloudInterface.instance.GetStoredProfileID();
                storedAnonymousIdText.text = BrainCloudInterface.instance.GetStoredAnonymousID();
                extraAuthInfoText.text = FACEBOOK_AUTH_INFO;
                break;
        }
    }

    //*************** UI Event Methods ***************
    public void OnAuthTypeChange(int val)
    {
        currentAuthType = (eAuthTypes)val;
        ChangeAuthType(currentAuthType);
    }

    public void OnProfileIdEndEdit(string input)
    {
        inputProfileId = input;
    }

    public void OnPasswordEndEdit(string input)
    {
        inputPassword = input; 
    }

    public void OnEntityNameEndEdit(string input)
    {
        inputEntityName = input; 
    }

    public void OnEntityAgeEndEdit(string input)
    {
        inputEntityAge = input; 
    }

    public void OnAdvancedAuthToggle(bool isToggled)
    {
        useAdvancedAuthentication = isToggled;
        ActivateAdvancedAuthFields();
    }

    void ActivateAdvancedAuthFields()
    {
        entityNameField.gameObject.SetActive(useAdvancedAuthentication);
        entityAgeField.gameObject.SetActive(useAdvancedAuthentication);
        entityNameText.gameObject.SetActive(useAdvancedAuthentication);
        entityAgeText.gameObject.SetActive(useAdvancedAuthentication);
        advAuthInfoText.gameObject.SetActive(useAdvancedAuthentication);
    }

    public void OnAuthenticate()
    {
        statusText.fontSize = STATUS_SHRINK_SIZE;
        statusText.text = "Attempting to Authenticate...";

        if(useAdvancedAuthentication)
        {
            BrainCloud.AuthenticationIds ids;
            Dictionary<string, object> extraJson = new Dictionary<string, object>();
            extraJson["name"] = inputEntityName;
            extraJson["age"] = inputEntityAge;

            if(selectedAdvancedAuthType == AuthenticationType.Anonymous)
            {
                ids.externalId = BrainCloudInterface.instance.GetStoredAnonymousID();
                ids.authenticationToken = "";
                ids.authenticationSubType = "";

                BrainCloudInterface.instance.AuthenticateAdvanced(selectedAdvancedAuthType, ids, extraJson);
            }
            else
            {
                ids.externalId = inputProfileId;
                ids.authenticationToken = inputPassword;
                ids.authenticationSubType = "";

                BrainCloudInterface.instance.AuthenticateAdvanced(selectedAdvancedAuthType, ids, extraJson);
            }

            return;
        }

        switch(currentAuthType)
        {
            case eAuthTypes.EMAIL:
                DataManager.instance.EmailID = inputProfileId;
                DataManager.instance.EmailPass = inputPassword;
                BrainCloudInterface.instance.AuthenticateEmail();
                break;
            case eAuthTypes.UNIVERSAL:
                DataManager.instance.UniversalUserID = inputProfileId;
                DataManager.instance.UniversalPass = inputPassword;
                BrainCloudInterface.instance.AuthenticateUniversal();
                break;
            case eAuthTypes.ANONYMOUS:
                BrainCloudInterface.instance.AuthenticateAnonymous();
                break;
            case eAuthTypes.GOOGLE:
                BrainCloudInterface.instance.AuthenticateGoogle();
                break;
            case eAuthTypes.FACEBOOK:
                BrainCloudInterface.instance.AuthenticateFacebook();
                break;
            case eAuthTypes.XBOXLIVE:
                break; 
        }
    }

    public void OnResetProfileID()
    {
        storedProfileIdText.text = BrainCloudInterface.instance.ResetStoredProfileID(); 
    }

    public void OnResetAnonymousID()
    {
        storedAnonymousIdText.text = BrainCloudInterface.instance.ResetStoredAnonymousID();
    }

    public void OnGoogleSignIn()
    {
        BrainCloudInterface.instance.GoogleSignIn();
    }

}