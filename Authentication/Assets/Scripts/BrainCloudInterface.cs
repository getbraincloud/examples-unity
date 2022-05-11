using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

public class BrainCloudInterface : MonoBehaviour
{
    public static BrainCloudInterface instance;

    /*public*/ BCConfig bCConfig;

    public static BrainCloudWrapper _bc;

    string m_authStatus = "Welcome to brainCloud!";

    //AnthonyTODO: Not sure if we'll need this
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

#if UNITY_WSA
    private XboxLiveUser _xboxLiveUser;
#endif

    void Start()
    {
        instance = this;

        bCConfig = FindObjectOfType<BCConfig>(); 

        if(bCConfig != null)
        {
            _bc = bCConfig.GetBrainCloud();
        }

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

        DontDestroyOnLoad(this);
        //m_AccessTokenResponse = new Twitter.AccessTokenResponse();
    }

    public void AuthenticateEmail()
    {
        m_emailId = DataManager.instance.GetEmailID();
        m_emailPwd = DataManager.instance.GetEmailPassword(); 

        _bc.ResetStoredProfileId();
        _bc.ResetStoredAnonymousId();
        _bc.AuthenticateEmailPassword(m_emailId, m_emailPwd, true, OnSuccess_Authenticate, OnError_Authenticate);
    }

    public void AuthenticateUniversal()
    {
        m_universalUserId = DataManager.instance.GetUniversalUserID();
        m_universalPwd = DataManager.instance.GetUniversalUserPassword();

        _bc.ResetStoredProfileId();
        _bc.ResetStoredAnonymousId();
        _bc.AuthenticateUniversal(m_universalUserId, m_universalPwd, true, OnSuccess_Authenticate, OnError_Authenticate);
    }

    public void AuthenticateAnonymous()
    {
        _bc.AuthenticateAnonymous(OnSuccess_Authenticate, OnError_Authenticate);
    }

    public void AuthenticateGoogle()
    {
        //AnthonyTODO: Figure out how to get Google Authentication working. Requires building to android device.
        _bc.AuthenticateGoogle(m_googleId, m_serverAuthCode, true, OnSuccess_Authenticate, OnError_Authenticate);
    }

    public void AuthenticateFacebook()
    {
        //AnthonyTODO: Waiting on Facebook fix?
    }
    public string GetStoredProfileID()
    {
        return _bc.GetStoredProfileId();
    }

    //Find out what the difference between this and the stored profile id is.
    public string GetAuthenticatedProfileID()
    {
        return _bc.Client.AuthenticationService.ProfileId; 
    }

    public string GetAppID()
    {
        return _bc.Client.AppId;
    }

    public string GetAppVersion()
    {
        return _bc.Client.AppVersion; 
    }

    public string ResetProfileID()
    {
        _bc.ResetStoredProfileId();

        return GetStoredProfileID(); 
    }

    public string GetStoredAnonymousID()
    {
        return _bc.GetStoredAnonymousId();
    }

    public string ResetAnonymousID()
    {
        _bc.ResetStoredAnonymousId();

        return GetStoredAnonymousID(); 
    }

    public void OnSuccess_Authenticate(string responseData, object cbObject)
    {
        m_authStatus = "Authenticate successful!";
        Debug.Log(m_authStatus);

        ScreenManager.instance.ActivateMainScreen();
    }

    public void OnError_Authenticate(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        m_authStatus = "Authenticate failed: " + statusMessage;
        Debug.LogError("OnError_Authenticate: " + statusMessage);
    }




    //AnthonyTODO: this will exist here until I decide where to put it.
    //*******************Google Sign In Stuff*********************
    public void GoogleSignIn()
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

    //AnthonyTODO: This will exist here temporarily until I can decide where it should go.
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
                Debug.Log("IsLoggedIn: perm: " + perm);
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
            if (result.Error != null)
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
