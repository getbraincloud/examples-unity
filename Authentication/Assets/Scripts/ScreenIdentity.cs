using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using LitJson;
using UnityEngine.SceneManagement;

public class ScreenIdentity : BCScreen {
	
    string m_twitterPin = "";

    string m_email = "";
    string m_emailPass = "";

    string m_username = "";
    string m_password = "";
    
    Twitter.RequestTokenResponse m_RequestTokenResponse;
    
    public ScreenIdentity(BrainCloudWrapper bc) : base(bc) { }
    
    public override void Activate()
    {
    }

	public override void OnScreenGUI()
	{
		GUILayout.BeginVertical();

		GUILayout.BeginHorizontal();
        if (GUILayout.Button("Login with Twitter"))
        {
            GameObject.FindObjectOfType<MainScene>().TwitterCoroutine(Twitter.API.GetRequestToken(ConnectScene.CONSUMER_KEY, ConnectScene.CONSUMER_SECRET,
                                                              new Twitter.RequestTokenCallback(this.OnSuccess_GetTwitterPIN)));
            //StartCoroutine(Twitter.API.GetRequestToken(CONSUMER_KEY, CONSUMER_SECRET,
           //                                                    new Twitter.RequestTokenCallback(this.OnSuccess_GetPIN)));
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Twitter PIN:");
        m_twitterPin = GUILayout.TextField(m_twitterPin, GUILayout.MinWidth(100));
        GUILayout.BeginVertical();
        if (GUILayout.Button("Attach New Twitter"))
        {
            m_mainScene.AddLog("Attaching new twitter account...");
            GameObject.FindObjectOfType<MainScene>().TwitterCoroutine(Twitter.API.GetAccessToken(ConnectScene.CONSUMER_KEY, ConnectScene.CONSUMER_SECRET, m_RequestTokenResponse.Token, m_twitterPin,
                               new Twitter.AccessTokenCallback(this.OnSuccess_AttachTwitter)));
        }
        if (GUILayout.Button("Merge Existing Twitter"))
        {
            m_mainScene.AddLog("Merging existing twitter account...");
            GameObject.FindObjectOfType<MainScene>().TwitterCoroutine(Twitter.API.GetAccessToken(ConnectScene.CONSUMER_KEY, ConnectScene.CONSUMER_SECRET, m_RequestTokenResponse.Token, m_twitterPin,
                               new Twitter.AccessTokenCallback(this.OnSuccess_AuthenticateTwitter)));
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Email:");
        m_email = GUILayout.TextField(m_email, GUILayout.MinWidth(100));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Password:");
        m_emailPass = GUILayout.PasswordField(m_emailPass, '*', GUILayout.MinWidth(100));
        GUILayout.BeginVertical();
        if (GUILayout.Button("Attach New Email"))
        {
            m_mainScene.AddLog("Attaching new email account...");
            _bc.IdentityService.AttachEmailIdentity(m_email, m_emailPass, Attach_Success, Attach_Fail);
        }
        if (GUILayout.Button("Merge Existing Email"))
        {
            m_mainScene.AddLog("Merging existing email account...");
            _bc.IdentityService.MergeEmailIdentity(m_email, m_emailPass, Attach_Success, Attach_Fail);
        }
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
        GUILayout.Space(20);
        GUILayout.BeginHorizontal();

        GUILayout.Label("Username:");
        m_username = GUILayout.TextField(m_username, GUILayout.MinWidth(100));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Password:");
        m_password = GUILayout.PasswordField(m_password, '*', GUILayout.MinWidth(100));
        GUILayout.BeginVertical();
        if (GUILayout.Button("Attach New Universal"))
        {
            m_mainScene.AddLog("Attaching new universal account...");
            _bc.IdentityService.AttachUniversalIdentity(m_username, m_password, Attach_Success, Attach_Fail);
        }
        if (GUILayout.Button("Merge Existing Universal"))
        {
            m_mainScene.AddLog("Merging existing universal account...");
            _bc.IdentityService.MergeUniversalIdentity(m_username, m_password, Attach_Success, Attach_Fail);
        }
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
        GUILayout.Space(20);
        GUILayout.BeginHorizontal();

		if (GUILayout.Button("Logout"))
		{
			_bc.PlayerStateService.Logout(Logout_Success, Failure_Callback, null);
		}
		GUILayout.EndHorizontal();
		
		GUILayout.EndVertical();
    }

    private void Attach_Success(string json, object o)
    {
        m_mainScene.AddLog("SUCCESS");
        m_mainScene.AddLogJson(json);
        m_mainScene.AddLog("");
    }

    private void Attach_Fail(int a, int b, string json, object o)
    {
        m_mainScene.AddLog("FAILURE");
        m_mainScene.AddLog("Status:" + a);
        m_mainScene.AddLog("Reason Code: " + b);
        if (json.Length > 150)
        {
            m_mainScene.AddLog(json.Substring(0, 150) + "...");
            Debug.LogError(json);
        }
        else
        {
            m_mainScene.AddLog(json);
        }
        m_mainScene.AddLog("");
    }

    public void OnSuccess_GetTwitterPIN(bool success, Twitter.RequestTokenResponse response)
    {
        if (success)
        {
            m_RequestTokenResponse = response;
            Twitter.API.OpenAuthorizationPage(response.Token);
        }
        else
        {
            Debug.Log("OnRequestTokenCallback - failed.");
        }
    }

    void OnSuccess_AttachTwitter(bool success, Twitter.AccessTokenResponse response)
    {
        if (success)
        {
            _bc.IdentityService.AttachTwitterIdentity(response.UserId, response.Token, response.TokenSecret, Attach_Success, Attach_Fail);
            //_bc.IdentityService.AttachTwitterIdentity(response.UserId, response.Token, response.TokenSecret, Attach_Success, Attach_Fail);
        }
        else
        {
            Debug.Log("OnAccessTokenCallback - failed.");
        }
    }

    void OnSuccess_AuthenticateTwitter(bool success, Twitter.AccessTokenResponse response)
    {
        if (success)
        {
            _bc.IdentityService.MergeTwitterIdentity(response.UserId, response.Token, response.TokenSecret, Attach_Success, Attach_Fail);
            //_bc.IdentityService.AttachTwitterIdentity(response.UserId, response.Token, response.TokenSecret, Attach_Success, Attach_Fail);
        }
        else
        {
            Debug.Log("OnAccessTokenCallback - failed.");
        }
    }

	private void Logout_Success(string json, object o)
	{
	    SceneManager.LoadScene("Connect");
	}
}
