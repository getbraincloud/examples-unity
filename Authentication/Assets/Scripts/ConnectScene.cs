using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


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

    eAuthTypes currentAuthType;

    string inputProfileId;
    string inputPassword; 

    //UI elements set in editor.
    [SerializeField] Text statusText;
    [SerializeField] Text profileIdText;
    [SerializeField] Text passwordText;
    [SerializeField] InputField profileIdField;
    [SerializeField] InputField passwordField;
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
    }

    void ChangeAuthType(eAuthTypes authType)
    {
        switch(authType)
        {
            case eAuthTypes.EMAIL:
                SetScreen("Email", "Password", true);
                break;
            case eAuthTypes.UNIVERSAL:
                SetScreen("User ID", "Password", true);
                break;
            case eAuthTypes.ANONYMOUS:
                SetScreen("Profile ID", "Anonymous", false);
                break;
            case eAuthTypes.GOOGLE:
                SetScreen("Profile ID", "Anonymous", false, true);
                break;
            case eAuthTypes.FACEBOOK:
                SetScreen("Profile ID", "Anonymous", false);
                break;
            case eAuthTypes.XBOXLIVE:
                SetScreen("Profile ID", "Anonymous", false);
                break;
        }
    }

    void SetScreen(string profileIdInfo, string passwordInfo, bool bHasInput, bool bIsGooogleLogin = false)
    {
        profileIdText.text = profileIdInfo + ":";
        passwordText.text = passwordInfo + ":"; 

        if(bIsGooogleLogin)
        {
            bHasInput = false;
            googleIdText.gameObject.SetActive(true);
            serverAuthCodeText.gameObject.SetActive(true);
            googleSignInButton.gameObject.SetActive(true);
        }
        else
        {
            googleIdText.gameObject.SetActive(false);
            serverAuthCodeText.gameObject.SetActive(false);
            googleSignInButton.gameObject.SetActive(false);
        }

        if(bHasInput)
        {
            profileIdField.gameObject.SetActive(true);
            passwordField.gameObject.SetActive(true);
            passwordField.contentType = InputField.ContentType.Password;
            resetProfileIdButton.gameObject.SetActive(false);
            resetAnonIdButton.gameObject.SetActive(false);
            storedProfileIdText.gameObject.SetActive(false);
            storedAnonymousIdText.gameObject.SetActive(false);
            googleIdText.gameObject.SetActive(false);
            serverAuthCodeText.gameObject.SetActive(false);
            googleSignInButton.gameObject.SetActive(false); 
        }
        else
        {
            profileIdField.gameObject.SetActive(false);
            passwordField.gameObject.SetActive(false);
            resetProfileIdButton.gameObject.SetActive(true);
            resetAnonIdButton.gameObject.SetActive(true);
            storedProfileIdText.gameObject.SetActive(true);
            storedAnonymousIdText.gameObject.SetActive(true);
            storedProfileIdText.text = BrainCloudInterface.instance.GetStoredProfileID();
            storedAnonymousIdText.text = BrainCloudInterface.instance.GetStoredAnonymousID();
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

    public void OnAuthenticate()
    {
        statusText.fontSize = 14;
        statusText.text = "Attempting to Authenticate...";

        switch(currentAuthType)
        {
            case eAuthTypes.EMAIL:
                DataManager.instance.SetEmailandPass(inputProfileId, inputPassword); 
                BrainCloudInterface.instance.AuthenticateEmail();
                break;
            case eAuthTypes.UNIVERSAL:
                DataManager.instance.SetUniversalIDandPass(inputProfileId, inputPassword);
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
        storedProfileIdText.text = BrainCloudInterface.instance.ResetProfileID(); 
    }

    public void OnResetAnonymousID()
    {
        storedAnonymousIdText.text = BrainCloudInterface.instance.ResetAnonymousID();
    }

    public void OnGoogleSignIn()
    {
        BrainCloudInterface.instance.GoogleSignIn();
    }

 
    #region Stuff To Remove
/*
    void OnGUI()
    {
        float tw = Screen.width / 14.0f;
        float th = Screen.height / 7.0f;
        Rect dialog = new Rect(tw, th, Screen.width - tw * 2, Screen.height - th * 2);
        float offsetX = 30;
        float offsetY = 30;
        Rect innerDialog = new Rect(dialog.x + offsetX, dialog.y + offsetY, dialog.width - (offsetX * 2), dialog.height - (offsetY * 2));

        GUI.Box(dialog, "Unity Example");
        GUILayout.BeginArea(innerDialog);

        GUILayout.Box("Choose Authentication Type");

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        m_selectedAuth = GUILayout.SelectionGrid(m_selectedAuth, m_authTypes, 4);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        if (m_selectedAuth == (int)eAuthTypes.EMAIL)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Email:");
            GUILayout.Label("Password:");
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            m_emailId = GUILayout.TextField(m_emailId, GUILayout.MinWidth(200));
            m_emailPwd = GUILayout.PasswordField(m_emailPwd, '*', GUILayout.MinWidth(200));
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Authenticate", GUILayout.ExpandWidth(false)))
            {
                m_authStatus = "Attempting to authenticate";

                // clear out any previous profile or anonymous ids
                _bc.ResetStoredProfileId();
                _bc.ResetStoredAnonymousId();
                _bc.AuthenticateEmailPassword(
                    m_emailId, m_emailPwd, true, OnSuccess_Authenticate, OnError_Authenticate);
            }
        }
        else if (m_selectedAuth == (int)eAuthTypes.UNIVERSAL)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("User Id:");
            GUILayout.Label("Password:");
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            m_universalUserId = GUILayout.TextField(m_universalUserId, GUILayout.MinWidth(200));
            m_universalPwd = GUILayout.PasswordField(m_universalPwd, '*', GUILayout.MinWidth(200));
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Authenticate", GUILayout.ExpandWidth(false)))
            {
                m_authStatus = "Attempting to authenticate";

                // clear out any previous profile or anonymous ids
                _bc.ResetStoredProfileId();
                _bc.ResetStoredAnonymousId();
                _bc.AuthenticateUniversal(
                    m_universalUserId, m_universalPwd, true, OnSuccess_Authenticate, OnError_Authenticate);
            }
        }
        else if (m_selectedAuth == (int)eAuthTypes.ANONYMOUS)
        {
            GUILayout.Label("Profile Id: " + _bc.GetStoredProfileId());
            GUILayout.Label("Anonymous Id: " + _bc.GetStoredAnonymousId());
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Authenticate", GUILayout.ExpandWidth(false)))
            {
                m_authStatus = "Attempting to authenticate";
                _bc.AuthenticateAnonymous(OnSuccess_Authenticate, OnError_Authenticate);
            }
            if (GUILayout.Button("Reset Profile Id", GUILayout.ExpandWidth(false)))
            {
                _bc.ResetStoredProfileId();
            }
            if (GUILayout.Button("Reset Anonymous Id", GUILayout.ExpandWidth(false)))
            {
                _bc.ResetStoredAnonymousId();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        else if (m_selectedAuth == (int)eAuthTypes.GOOGLE)
        {
            GUILayout.Label("Google Id: " + m_googleId);
            GUILayout.Label("Server Auth Code: " + m_serverAuthCode);


            if (GUILayout.Button("Google Signin", GUILayout.ExpandWidth(false)))
            {
#if UNITY_ANDROID
                m_authStatus += "\n\nInfo: If the authentication popup appears but nothing occurs after, it probably means the app isn't fully set up. Please follow the instruction here:\nhttp://getbraincloud.com/apidocs/portal-usage/authentication-google/ \n\n";
                
                PlayGamesPlatform.Activate().Authenticate((bool success) => {

                    if (success)
                    {
                        m_googleId = PlayGamesPlatform.Instance.GetUserId();
                        m_serverAuthCode = PlayGamesPlatform.Instance.GetServerAuthCode();

                        m_authStatus += "\nGoogle Auth Success. Now click \"Authenticate\"\n\n";
                    }
                    else
                    {
                        m_authStatus += "\nGoogle Auth Failed. See documentation: https://github.com/playgameservices/play-games-plugin-for-unity\n";
                        m_authStatus += "Note: this can only be tested on an Android Device\n\n";
                    }
                });
#else
                m_authStatus += "\n\nGoogle Login will only work on an Android Device. Please build to a test device\n\n";
#endif
            }

            GUILayout.Label("Profile Id: " + _bc.GetStoredProfileId());
            GUILayout.Label("Anonymous Id: " + _bc.GetStoredAnonymousId());
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Authenticate", GUILayout.ExpandWidth(false)))
            {
                m_authStatus = "Attempting to authenticate";
                _bc.AuthenticateGoogle(m_googleId, m_serverAuthCode, true, OnSuccess_Authenticate, OnError_Authenticate);
            }
            if (GUILayout.Button("Reset Profile Id", GUILayout.ExpandWidth(false)))
            {
                _bc.ResetStoredProfileId();
            }
            if (GUILayout.Button("Reset Anonymous Id", GUILayout.ExpandWidth(false)))
            {
                _bc.ResetStoredAnonymousId();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        else if (m_selectedAuth == (int)eAuthTypes.FACEBOOK)
        {
#if UNITY_WEBGL || UNITY_STANDALONE_WIN
            GUILayout.Label("Token Id: " + AccessToken.CurrentAccessToken.TokenString);
            GUILayout.Label("User Id: " + _localFacebookUser.id);
            GUILayout.Label("First Name: " + _localFacebookUser.first_name);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Initialize", GUILayout.ExpandWidth(false)))
            {
                if (!FB.IsInitialized)
                {
                    FB.Init(InitCallback, HideUnity);
                }
                else
                {
                    FB.ActivateApp();
                }
            }

            if (GUILayout.Button("Authenticate", GUILayout.ExpandWidth(false)))
            {
                m_authStatus = "Attempting to authenticate";
                var perms = new List<string>()
                {
                    "public_profile",
                    "email"
                };
                FB.LogInWithReadPermissions(perms, AuthCallback);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
#else
            m_authStatus = "Facebook login is only available on WebGL & Windows Standalone";
#endif
        }
        else if (m_selectedAuth == (int)eAuthTypes.XBOXLIVE)
        {
#if UNITY_WSA
            if (_xboxLiveUser.IsSignedIn)
            {
                GUILayout.Label("Player Number: " + playerNumber);
                GUILayout.Label("Gamer Tag: " + _xboxLiveUser.Gamertag);
                GUILayout.Label("User ID: " + _xboxLiveUser.XboxUserId);
                GUILayout.BeginHorizontal();
            }
            
            if(GUILayout.Button("Sign In",GUILayout.ExpandWidth(false)))
            {
                StartCoroutine(SignInManager.Instance.SignInPlayer(playerNumber));
            }
#else
            m_authStatus = "Xbox Live login is only available on Universal Windows Platform";
#endif
        }

        m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.TextArea(m_authStatus, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Clear Log", GUILayout.Height(25), GUILayout.Width(100)))
        {
            m_authStatus = "";
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }
*/    
#endregion
}