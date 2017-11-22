using System;
using UnityEngine;
using System.Collections;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using LitJson;
using UnityEngine.SceneManagement;

public class ConnectScene : MonoBehaviour
{
    Vector2 m_scrollPosition;
    string m_authStatus = "Welcome to brainCloud!";
    int m_selectedAuth = 0;
    enum eAuthTypes {
        EMAIL,
        UNIVERSAL,
        TWITTER,
        ANONYMOUS,
        GOOGLE
    };
    string[] m_authTypes = {
        "Email",
        "Universal",
        "Twitter",
        "Anonymous",
        "Google (Android Devices)"
    };
    string m_universalUserId = "";
    string m_universalPwd = "";
    string m_emailId = "";
    string m_emailPwd = "";
    string m_googleId = "";
    string m_serverAuthCode = "";

    public static string CONSUMER_KEY = "";
    public static string CONSUMER_SECRET = "";
    Twitter.RequestTokenResponse m_RequestTokenResponse;
    Twitter.AccessTokenResponse m_AccessTokenResponse;
    //string m_facebookUserId = "";
    //string m_facebookAuthToken = "";

    void Start()
    {
        BrainCloudWrapper.Initialize();
        m_AccessTokenResponse = new Twitter.AccessTokenResponse();
    }
    
    void Update()
    {
    }

    void OnGUI()
    {
        float tw = Screen.width / 14;
        float th = Screen.height / 7;
        Rect dialog = new Rect(tw, th, Screen.width - tw*2, Screen.height - th*2);
        float offsetX = 30;
        float offsetY = 30;
        Rect innerDialog = new Rect(dialog.x + offsetX, dialog.y + offsetY, dialog.width - (offsetX * 2), dialog.height - (offsetY * 2));

        GUI.Box(dialog, "Unity Example");
        GUILayout.BeginArea(innerDialog);

        GUILayout.Box("Choose Authentication Type");

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        m_selectedAuth = GUILayout.SelectionGrid(m_selectedAuth, m_authTypes, m_authTypes.Length);
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
                BrainCloudWrapper.GetInstance().ResetStoredProfileId();
                BrainCloudWrapper.GetInstance().ResetStoredAnonymousId();
                BrainCloudWrapper.GetInstance().AuthenticateEmailPassword(
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
                BrainCloudWrapper.GetInstance().ResetStoredProfileId();
                BrainCloudWrapper.GetInstance().ResetStoredAnonymousId();
                BrainCloudWrapper.GetInstance().AuthenticateUniversal(
                    m_universalUserId, m_universalPwd, true, OnSuccess_Authenticate, OnError_Authenticate);
            }
        }
        else if (m_selectedAuth == (int) eAuthTypes.TWITTER)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            if (GUILayout.Button("Get PIN"))
            {
                StartCoroutine(Twitter.API.GetRequestToken(CONSUMER_KEY, CONSUMER_SECRET,
                                                               new Twitter.RequestTokenCallback(this.OnSuccess_GetPIN)));
            }
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("PIN:");
            m_emailId = GUILayout.TextField(m_emailId, GUILayout.MinWidth(200));
            //m_emailPwd = GUILayout.PasswordField(m_emailPwd, '*', GUILayout.MinWidth(200));
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Enter PIN", GUILayout.ExpandWidth(false)))
            {
                m_authStatus = "Attempting to authenticate";

                // clear out any previous profile or anonymous ids
                BrainCloudWrapper.GetInstance().ResetStoredProfileId();
                BrainCloudWrapper.GetInstance().ResetStoredAnonymousId();
                StartCoroutine(Twitter.API.GetAccessToken(CONSUMER_KEY, CONSUMER_SECRET, m_RequestTokenResponse.Token, m_emailId,
                               new Twitter.AccessTokenCallback(this.OnSuccess_AuthenticateTwitter)));
                //BrainCloudWrapper.GetBC().AuthenticationService.AuthenticateTwitter()

                //BrainCloudWrapper.GetInstance().AuthenticateEmailPassword(
                   // m_emailId, m_emailPwd, true, OnSuccess_Authenticate, OnError_Authenticate);
            }
        }
        else if (m_selectedAuth == (int) eAuthTypes.ANONYMOUS)
        {
            GUILayout.Label("Profile Id: " + BrainCloudWrapper.GetInstance().GetStoredProfileId());
            GUILayout.Label("Anonymous Id: " + BrainCloudWrapper.GetInstance().GetStoredAnonymousId());
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Authenticate", GUILayout.ExpandWidth(false)))
            {
                m_authStatus = "Attempting to authenticate";
                BrainCloudWrapper.GetInstance().AuthenticateAnonymous(OnSuccess_Authenticate, OnError_Authenticate);
            }
            if (GUILayout.Button("Reset Profile Id", GUILayout.ExpandWidth(false)))
            {
                BrainCloudWrapper.GetInstance().ResetStoredProfileId();
            }
            if (GUILayout.Button("Reset Anonymous Id", GUILayout.ExpandWidth(false)))
            {
                BrainCloudWrapper.GetInstance().ResetStoredAnonymousId();
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
                m_authStatus += "\n\nInfo: If the authentication popup appears but nothing occurs after, it probably means the app isn't fully set up. Please follow the instruction here:\nhttp://getbraincloud.com/apidocs/portal-usage/authentication-google/ \n\n";
                
                PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
                    .RequestIdToken()
                    .RequestServerAuthCode(false)
                    .Build();
                
                PlayGamesPlatform.InitializeInstance (config);
                PlayGamesPlatform.Activate().Authenticate((bool success) => {

                    if (success)
                    {
                        m_googleId = PlayGamesPlatform.Instance.GetUserId();
                        m_serverAuthCode = PlayGamesPlatform.Instance.GetServerAuthCode();

                        m_authStatus += "\nGoogle Auth Success. Now click \"Authenticate\"\n\n";
                    }
                    else
                    {
                        m_authStatus += "\nGoogle Auth Failed. See documentation: https://github.com/playgameservices/play-games-plugin-for-unity\n\n";
                    }
                });
            }


            GUILayout.Label("Profile Id: " + BrainCloudWrapper.GetInstance().GetStoredProfileId());
            GUILayout.Label("Anonymous Id: " + BrainCloudWrapper.GetInstance().GetStoredAnonymousId());
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Authenticate", GUILayout.ExpandWidth(false)))
            {
                m_authStatus = "Attempting to authenticate";
                BrainCloudWrapper.GetInstance().AuthenticateGoogle(m_googleId, m_serverAuthCode, true, OnSuccess_Authenticate, OnError_Authenticate);
            }
            if (GUILayout.Button("Reset Profile Id", GUILayout.ExpandWidth(false)))
            {
                BrainCloudWrapper.GetInstance().ResetStoredProfileId();
            }
            if (GUILayout.Button("Reset Anonymous Id", GUILayout.ExpandWidth(false)))
            {
                BrainCloudWrapper.GetInstance().ResetStoredAnonymousId();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
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

        //GUILayout.FlexibleSpace();
        //GUILayout.Box("Stored Information");

        GUILayout.EndArea();
    }
    
    IEnumerator GetGoogleAccountDetailsWithDelay()
    {
        yield return new WaitForSeconds(2);
        
      //  m_googleId = PlayGamesPlatform.Instance.GetIdToken();
      //  m_serverAuthCode = PlayGamesPlatform.Instance.GetServerAuthCode();
    }
    
    public void OnSuccess_GetPIN(bool success, Twitter.RequestTokenResponse response)
    {
        if (success)
        {
            string log = "OnRequestTokenCallback - succeeded";
            log += "\n    Token : " + response.Token;
            log += "\n    TokenSecret : " + response.TokenSecret;
            print(log);

            m_RequestTokenResponse = response;

            Twitter.API.OpenAuthorizationPage(response.Token);
        }
        else
        {
            print("OnRequestTokenCallback - failed.");
        }
    }


    void OnSuccess_AuthenticateTwitter(bool success, Twitter.AccessTokenResponse response)
    {
        if (success)
        {
            string log = "OnAccessTokenCallback - succeeded";
            log += "\n    UserId : " + response.UserId;
            log += "\n    ScreenName : " + response.ScreenName;
            log += "\n    Token : " + response.Token;
            log += "\n    TokenSecret : " + response.TokenSecret;
            print(log);

            m_AccessTokenResponse = response;

            BrainCloudWrapper.GetBC().AuthenticationService.AuthenticateTwitter(response.UserId, response.Token, response.TokenSecret, true, OnSuccess_Authenticate, OnError_Authenticate);
        }
        else
        {
            print("OnAccessTokenCallback - failed.");
        }
    }
    public void OnSuccess_Authenticate(string responseData, object cbObject)
    {
        m_authStatus = "Authenticate successful!";
        SceneManager.LoadScene("Main");
    }
    
	public void OnError_Authenticate(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        m_authStatus = "Authenticate failed: " + statusMessage;
        Debug.LogError("OnError_Authenticate: " + statusMessage);
    }
}
