using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.LitJson;
using UnityEngine.SceneManagement;

public class ScreenIdentity : BCScreen {
	
    string m_twitterPin = "";

    string m_email = "";
    string m_emailPass = "";

    string m_username = "";
    string m_password = "";
    
    Twitter.RequestTokenResponse m_RequestTokenResponse;
    
    public ScreenIdentity(BrainCloudWrapper bc) : base(bc) { }

    public override void Activate(BrainCloudWrapper bc)
    {
        _bc = bc;
    }

    public void OnLoginTwitterClick()
    {
        GameObject.FindObjectOfType<MainScene>().TwitterCoroutine(Twitter.API.GetRequestToken(BrainCloudInterface.CONSUMER_KEY, BrainCloudInterface.CONSUMER_SECRET,
                                                                  new Twitter.RequestTokenCallback(this.OnSuccess_GetTwitterPIN)));
    }

    public void OnAttachTwitterClick()
    {
        GameObject.FindObjectOfType<MainScene>().TwitterCoroutine(Twitter.API.GetAccessToken(BrainCloudInterface.CONSUMER_KEY, BrainCloudInterface.CONSUMER_SECRET,
                                                                  m_RequestTokenResponse.Token, m_twitterPin, new Twitter.AccessTokenCallback(this.OnSuccess_AttachTwitter)));
    }

    public void OnMergeTwitterClick()
    {
        GameObject.FindObjectOfType<MainScene>().TwitterCoroutine(Twitter.API.GetAccessToken(BrainCloudInterface.CONSUMER_KEY, BrainCloudInterface.CONSUMER_SECRET, 
                                                                  m_RequestTokenResponse.Token, m_twitterPin, new Twitter.AccessTokenCallback(this.OnSuccess_AuthenticateTwitter)));
    }

    public void OnTwitterPinEndEdit(string pin)
    {
        m_twitterPin = pin; 
    }

    public void OnEmailEndEdit(string email)
    {
        m_email = email; 
    }

    public void OnEmailPassEndEdit(string pass)
    {
        m_emailPass = pass; 
    }

    public void OnAttachEmailClick()
    {
        BrainCloudInterface.instance.AttachEmailIdentity(m_email, m_emailPass);
    }

    public void OnMergeEmailClick()
    {
        BrainCloudInterface.instance.MergeEmailIdentity(m_email, m_password);
    }

    public void OnUsernameEndEdit(string username)
    {
        m_username = username; 
    }

    public void OnPasswordEndEdit(string pass)
    {
        m_password = pass;
    }

    public void OnAttachUniversalClick()
    {
        BrainCloudInterface.instance.AttachUniversalIdentity(m_username, m_password);
    }

    public void OnMergeUniversalClick()
    {
        BrainCloudInterface.instance.MergeUniversalIdentity(m_username, m_password); 
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
            BrainCloudInterface.instance.AttachTwitterIdentity(response.UserId, response.Token, response.TokenSecret);
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
            BrainCloudInterface.instance.MergeTwitterIdentity(response.UserId, response.Token, response.TokenSecret);
        }
        else
        {
            Debug.Log("OnAccessTokenCallback - failed.");
        }
    }

    protected override void OnDisable()
    {
        
    }
}
