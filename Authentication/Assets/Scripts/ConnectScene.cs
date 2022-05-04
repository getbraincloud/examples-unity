using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

#if UNITY_WSA
using Microsoft.Xbox.Services;
using Microsoft.Xbox.Services.Client;
#endif

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

#if UNITY_WEBGL || UNITY_STANDALONE_WIN
using Facebook.Unity;
#endif

class FacebookUser
{
    public string id;
    public string first_name;
    public string last_name;
    public string email;
}

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
    string m_universalUserId = "";
    string m_universalPwd = "";
    string m_emailId = "";
    string m_emailPwd = "";
    string m_googleId = "";
    string m_serverAuthCode = "";
    private FacebookUser _localFacebookUser = new FacebookUser();
    public static string CONSUMER_KEY = "";
    public static string CONSUMER_SECRET = "";
    
    private bool signedIn;
    private int playerNumber;

    //AnthonyTODO: Additions for UI Rework
    //Revealing in editor for debugging purposes
    [SerializeField] eAuthTypes currentAuthType;

    //Setting these in editor for simplicity
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

    //Revealing to editor for debugging purposes
    [SerializeField] string inputProfileId;
    [SerializeField] string inputPassword; 

#if UNITY_WSA
    private XboxLiveUser _xboxLiveUser;
#endif
    void Start()
    {
        _bc = BCConfig.GetBrainCloud();
        ChangeAuthType(eAuthTypes.EMAIL);
#if UNITY_ANDROID
        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
            .RequestIdToken()
            .RequestEmail()
            .RequestServerAuthCode(false)
            .Build();
                
        PlayGamesPlatform.InitializeInstance (config);
#endif
        
#if UNITY_WSA
        _xboxLiveUser = new XboxLiveUser();
        try
        {
            SignInManager.Instance.OnPlayerSignOut(playerNumber, OnPlayerSignOut);
            SignInManager.Instance.OnPlayerSignIn(playerNumber, OnPlayerSignIn);
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
#endif
        
        //m_AccessTokenResponse = new Twitter.AccessTokenResponse();

    }

    //AnthonyTODO: UI Related Methods

    public void OnAuthTypeChange(int val)
    {
        currentAuthType = (eAuthTypes)val;

        ChangeAuthType(currentAuthType);
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
            storedProfileIdText.text = _bc.GetStoredProfileId();
            storedAnonymousIdText.text = _bc.GetStoredAnonymousId();
        }
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
                AuthenticateEmail();
                break;
            case eAuthTypes.UNIVERSAL:
                AuthenticateUniversal();
                break;
            case eAuthTypes.ANONYMOUS:
                AuthenticateAnonymous();
                break;
            case eAuthTypes.GOOGLE:
                AuthenticateGoogle();
                break;
            case eAuthTypes.FACEBOOK:
                AuthenticateFacebook();
                break;
            case eAuthTypes.XBOXLIVE:
                break; 
        }
    }

    void AuthenticateEmail()
    {
        m_emailId = inputProfileId;
        m_emailPwd = inputPassword;

        _bc.ResetStoredProfileId();
        _bc.ResetStoredAnonymousId();
        _bc.AuthenticateEmailPassword(m_emailId, m_emailPwd, true, OnSuccess_Authenticate, OnError_Authenticate);
    }

    void AuthenticateUniversal()
    {
        m_universalUserId = inputProfileId;
        m_universalPwd = inputPassword; 

        _bc.ResetStoredProfileId();
        _bc.ResetStoredAnonymousId();
        _bc.AuthenticateUniversal(m_universalUserId, m_universalPwd, true, OnSuccess_Authenticate, OnError_Authenticate);
    }

    void AuthenticateAnonymous()
    {
        _bc.AuthenticateAnonymous(OnSuccess_Authenticate, OnError_Authenticate);
    }

    void AuthenticateGoogle()
    {
        //AnthonyTODO: Figure out how to get Google Authentication working. Requires building to android device.
        _bc.AuthenticateGoogle(m_googleId, m_serverAuthCode, true, OnSuccess_Authenticate, OnError_Authenticate);
    }

    void AuthenticateFacebook()
    {
        //AnthonyTODO: Waiting on Facebook fix?
    }

    public void OnResetProfileID()
    {
        _bc.ResetStoredProfileId();
        storedProfileIdText.text = _bc.GetStoredProfileId(); 
    }

    public void OnResetAnonymousID()
    {
        _bc.ResetStoredAnonymousId();
        storedAnonymousIdText.text = _bc.GetStoredAnonymousId();
    }

    public void OnGoogleSignIn()
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

    //THIS IS THE ENTIRE OLD SYSTEM FOR AUTHENTICATION. USE THIS AS REFERENCE. 
    #region OnGuiSystem
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
#endregion OnGuiSystem

    public void OnSuccess_Authenticate(string responseData, object cbObject)
    {
        m_authStatus = "Authenticate successful!";
        gameObject.SetActive(false);
        MainScene.SetActive(true);
    }
    
	public void OnError_Authenticate(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        m_authStatus = "Authenticate failed: " + statusMessage;
        Debug.LogError("OnError_Authenticate: " + statusMessage);
    }
    //***************Facebook Specific Functions***************************
    private void InitCallback()
    {
        m_authStatus = "Facebook Initialized!!";
    }

    private void HideUnity(bool isGameShown)
    {
        m_authStatus = $"Game show status : {isGameShown}";

    }
#if UNITY_WEBGL || UNITY_STANDALONE_WIN
    private void AuthCallback(ILoginResult result)
    {
        if (FB.IsLoggedIn)
        {
            m_authStatus = "Facebook Login Successful";
            var token = AccessToken.CurrentAccessToken;
            GetInfo();
            //Authenticate user to braincloud
            _bc.AuthenticateFacebook(token.UserId, token.TokenString, true, OnSuccess_FacebookAuthenticate, OnError_Authenticate);
            foreach (string perm in token.Permissions)
            {
                Debug.Log("IsLoggedIn: perm: "+perm);
            }
        }
        else
        {
            m_authStatus = "User cancelled login";
            Debug.Log("User cancelled login");
        }
    }
    private void OnSuccess_FacebookAuthenticate(string responseData, object cbObject)
    {
        m_authStatus += "\n Braincloud Authenticated! \n";
        m_authStatus += responseData;
    }
    
    private void GetInfo()
    {
        //Use the graph explorer to find what you need for the query parameter in here --> https://developers.facebook.com/tools/explorer/
        FB.API("/me?fields=id,first_name,last_name,email", HttpMethod.GET, result =>
        {
            if(result.Error != null)
            {
                Debug.Log("Result error");
            }
            _localFacebookUser = BrainCloud.JsonFx.Json.JsonReader.Deserialize<FacebookUser>(result.RawResult);
        });
    }
#endif
    
#if UNITY_WSA
    private void OnPlayerSignIn(XboxLiveUser xboxLiveUserParam, XboxLiveAuthStatus authStatus, string errorMessage)
    {
        if(authStatus == XboxLiveAuthStatus.Succeeded)
        {
            _xboxLiveUser = xboxLiveUserParam; //store the xboxLiveUser SignedIn
            signedIn = true;
        }
        else
        {
            if (XboxLiveServicesSettings.Instance.DebugLogsOn)
            {
                Debug.LogError(errorMessage); //Log the error message in case of unsuccessful call.
            }
        }
    }

    private void OnPlayerSignOut(XboxLiveUser xboxLiveUserParam, XboxLiveAuthStatus authStatus, string errorMessage)
    {
        if (authStatus == XboxLiveAuthStatus.Succeeded)
        {
            _xboxLiveUser = null;
            signedIn = false;
        }
        else
        {
            if (XboxLiveServicesSettings.Instance.DebugLogsOn)
            {
                Debug.LogError(errorMessage);
            }
        }
    }
#endif

    private void OnDestroy()
    {
#if UNITY_WSA
        if (SignInManager.Instance == null) return;
        
        SignInManager.Instance.RemoveCallbackFromPlayer(playerNumber, OnPlayerSignOut);
        SignInManager.Instance.RemoveCallbackFromPlayer(playerNumber, OnPlayerSignIn);
#endif
    }
}