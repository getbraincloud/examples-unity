using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.LitJson;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenIdentity : BCScreen {
	
    string m_twitterPin = "";

    string m_email = "";
    string m_emailPass = "";

    string m_username = "";
    string m_password = "";
    
    Twitter.RequestTokenResponse m_RequestTokenResponse;

    [SerializeField] InputField emailField;
    [SerializeField] InputField emailPassField;
    [SerializeField] InputField universalField;
    [SerializeField] InputField universalPassField;

    private void Awake()
    {
        if (HelpMessage == null)
        {
            HelpMessage =   "The identity screen allows a user to attach or merge either an Email or Universal ID to an existing user.\n\n" +
                            "Universal identities and emails attached to users can be viewed on the User Summary page under the User Monitoring tab."; 
        }

        if (HelpURL == null)
        {
            HelpURL = "https://getbraincloud.com/apidocs/apiref/?cloudcode#capi-identity";
        }
    }

    public void OnLoginTwitterClick()
    {
        GameObject.FindObjectOfType<BCFuncScreenHandler>().TwitterCoroutine(Twitter.API.GetRequestToken(BrainCloudInterface.CONSUMER_KEY, BrainCloudInterface.CONSUMER_SECRET,
                                                                  new Twitter.RequestTokenCallback(this.OnSuccess_GetTwitterPIN)));
    }

    public void OnAttachTwitterClick()
    {
        GameObject.FindObjectOfType<BCFuncScreenHandler>().TwitterCoroutine(Twitter.API.GetAccessToken(BrainCloudInterface.CONSUMER_KEY, BrainCloudInterface.CONSUMER_SECRET,
                                                                  m_RequestTokenResponse.Token, m_twitterPin, new Twitter.AccessTokenCallback(this.OnSuccess_AttachTwitter)));
    }

    public void OnMergeTwitterClick()
    {
        GameObject.FindObjectOfType<BCFuncScreenHandler>().TwitterCoroutine(Twitter.API.GetAccessToken(BrainCloudInterface.CONSUMER_KEY, BrainCloudInterface.CONSUMER_SECRET, 
                                                                  m_RequestTokenResponse.Token, m_twitterPin, new Twitter.AccessTokenCallback(this.OnSuccess_AuthenticateTwitter)));
    }

    public void OnTwitterPinEndEdit(string pin)
    {
        m_twitterPin = pin; 
    }

    public void OnAttachEmailClick()
    {
        m_email = emailField.text;
        m_emailPass = emailPassField.text;

        BrainCloudInterface.instance.AttachEmailIdentity(m_email, m_emailPass);
    }

    public void OnMergeEmailClick()
    {
        m_email = emailField.text;
        m_emailPass = emailPassField.text;

        BrainCloudInterface.instance.MergeEmailIdentity(m_email, m_emailPass);
    }

    public void OnAttachUniversalClick()
    {
        m_username = universalField.text;
        m_password = universalPassField.text;

        BrainCloudInterface.instance.AttachUniversalIdentity(m_username, m_password);
    }

    public void OnMergeUniversalClick()
    {
        m_username = universalField.text;
        m_password = universalPassField.text;

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
}
