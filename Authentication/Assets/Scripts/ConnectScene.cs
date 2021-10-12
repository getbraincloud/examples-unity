using UnityEngine;
using System.Collections.Generic;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

#if UNITY_WEBGL
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
        FACEBOOK
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
    
    void Start()
    {
        _bc = BCConfig.GetBrainCloud();
#if UNITY_ANDROID
        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
            .RequestIdToken()
            .RequestEmail()
            .RequestServerAuthCode(false)
            .Build();
                
        PlayGamesPlatform.InitializeInstance (config);
#endif
        //m_AccessTokenResponse = new Twitter.AccessTokenResponse();

    }
   
    void OnGUI()
    {
        float tw = Screen.width / 14.0f;
        float th = Screen.height / 7.0f;
        Rect dialog = new Rect(tw, th, Screen.width - tw*2, Screen.height - th*2);
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
        
        if (m_selectedAuth == (int) eAuthTypes.EMAIL)
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
        else if (m_selectedAuth == (int) eAuthTypes.UNIVERSAL)
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
        else if (m_selectedAuth == (int) eAuthTypes.ANONYMOUS)
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
        else if (m_selectedAuth == (int) eAuthTypes.GOOGLE)
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
                    FB.Init(InitCallback,HideUnity);
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
                FB.LogInWithReadPermissions(perms,AuthCallback);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
#else
            m_authStatus = "Facebook login is only available on WebGL & Windows Standalone";
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
}