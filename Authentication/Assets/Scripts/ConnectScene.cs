using System;
using UnityEngine;
using System.Collections;
using LitJson;

public class ConnectScene : MonoBehaviour
{
    Vector2 m_scrollPosition;
    string m_authStatus = "Welcome to brainCloud!";
    int m_selectedAuth = 0;
    enum eAuthTypes {
        EMAIL,
        UNIVERSAL,
        TWITTER,
        ANONYMOUS
    };
    string[] m_authTypes = {
        "Email",
        "Universal",
        "Twitter",
        "Anonymous"
    };
    string m_universalUserId = "";
    string m_universalPwd = "";
    string m_emailId = "";
    string m_emailPwd = "";

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
        float tw = Screen.width / 5;
        float th = Screen.height / 7;
        Rect dialog = new Rect(tw, th, tw*3, th*5);
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
        Application.LoadLevel("Main");
    }
    
	public void OnError_Authenticate(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        m_authStatus = "Authenticate failed: " + statusMessage;
        Debug.LogError("OnError_Authenticate: " + statusMessage);
    }
}
