using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrainCloud;
using BrainCloud.LitJson;
using BrainCloud.Common;

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

//BrainCloudInterface is designed to interface between screen UI and all Braincloud functionality accessible from BrainCloudWrapper.
//This class exists as a way to demonstrate how brainCloud methods are called and how to utilize their respective success and failure callbacks. 

public class BrainCloudInterface : MonoBehaviour
{
    public static BrainCloudInterface instance;

    BCConfig bCConfig;

    public static BrainCloudWrapper _bc;

    string m_authStatus = "Welcome to brainCloud!";

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

    //*************** Authenticate Methods ***************
    public void AuthenticateEmail()
    {
        m_emailId = DataManager.instance.EmailID;
        m_emailPwd = DataManager.instance.EmailPass; 

        _bc.ResetStoredProfileId();
        _bc.ResetStoredAnonymousId();
        _bc.AuthenticateEmailPassword(m_emailId, m_emailPwd, true, OnSuccess_Authenticate, OnError_Authenticate);
    }

    public void AuthenticateUniversal()
    {
        m_universalUserId = DataManager.instance.UniversalUserID;
        m_universalPwd = DataManager.instance.UniversalPass;

        _bc.ResetStoredProfileId();
        _bc.ResetStoredAnonymousId();
        _bc.AuthenticateUniversal(m_universalUserId, m_universalPwd, true, OnSuccess_Authenticate, OnError_Authenticate);
    }

    public void AuthenticateAnonymous()
    {
        _bc.AuthenticateAnonymous(OnSuccess_Authenticate, OnError_Authenticate);
    }

    public void AuthenticateAdvanced(AuthenticationType authType, BrainCloud.AuthenticationIds ids, Dictionary<string, object> extraJson)
    {
        SuccessCallback successCallback = (response, cbObject) => { ScreenManager.instance.ActivateMainScreen(); };

        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        _bc.AuthenticateAdvanced(authType, ids, true, extraJson, successCallback, failureCallback);
    }

    public void AuthenticateGoogle()
    {
        _bc.AuthenticateGoogle(m_googleId, m_serverAuthCode, true, OnSuccess_Authenticate, OnError_Authenticate);
    }

    public void AuthenticateFacebook()
    {
        
    }

    public void OnSuccess_Authenticate(string responseData, object cbObject)
    {
        m_authStatus = "Authenticate successful!";
        Debug.Log(m_authStatus);

        ScreenManager.instance.ActivateMainScreen();
    }

    public void OnError_Authenticate(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Debug.Log(string.Format("Failed | {0}  {1}  {2}", statusCode, reasonCode, statusMessage));
    }

    public string GetStoredProfileID()
    {
        return _bc.GetStoredProfileId();
    }

    public string ResetStoredProfileID()
    {
        _bc.ResetStoredProfileId();

        return GetStoredProfileID(); 
    }

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

    public string GetStoredAnonymousID()
    {
        return _bc.GetStoredAnonymousId();
    }

    public string ResetStoredAnonymousID()
    {
        _bc.ResetStoredAnonymousId();

        return GetStoredAnonymousID(); 
    }

    //*************** PlayerStateService Methods ***************
    public void Logout()
    {
        SuccessCallback successCallback = (response, cbObject) => {

            ScreenManager.instance.ActivateConnectScreen(); 

        };

        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        _bc.PlayerStateService.Logout(successCallback, failureCallback);
    }

    public void ReadUserState(object cb)
    { 
        _bc.PlayerStateService.ReadUserState(OnReadUserState_Success, OnReadUserState_Failure, cb);
    }

    public void OnReadUserState_Success(string response, object cbObject)
    {
        if (cbObject.GetType() == typeof(ScreenPlayerXp))
        {
            JsonData jObj = JsonMapper.ToObject(response);
            DataManager.instance.PlayerLevel = (int)jObj["data"]["experienceLevel"];
            DataManager.instance.PlayerXP = (int)jObj["data"]["experiencePoints"];

            GameEvents.instance.UpdateLevelAndXP();
        }

        if (cbObject.GetType() == typeof(ScreenPlayerStats))
        {
            JsonData jObj = JsonMapper.ToObject(response);
            JsonData jStats = jObj["data"]["statistics"];
            IDictionary dStats = jStats as IDictionary;

            if (dStats != null)
            {
                foreach (string key in dStats.Keys)
                {
                    JsonData value = (JsonData)dStats[key];
                    long valAsLong = value.IsInt ? (int)value : (long)value; //LitJson can't upcast an int to a long.

                    DataManager.instance.PlayerStats[key] = valAsLong;
                }

                GameEvents.instance.InstantiatePlayerStats();
            }
        }
    }

    public void OnReadUserState_Failure(int status, int code, string error, object cbObject)
    {
        Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error));
    }

    //*************** PlayerStatisticsService Methods ***************
    public void IncrementExperiencePoints(int incrementAmount)
    {
        SuccessCallback successCallback = (response, cbObject) =>
        {
            JsonData jObj = JsonMapper.ToObject(response);
            DataManager.instance.PlayerLevel = (int)jObj["data"]["experienceLevel"];
            DataManager.instance.PlayerXP = (int)jObj["data"]["experiencePoints"];

            GameEvents.instance.UpdateLevelAndXP();
        };

        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        _bc.PlayerStatisticsService.IncrementExperiencePoints(incrementAmount, successCallback, failureCallback);
    }

    public void IncrementUserStats(string userStat)
    {
        SuccessCallback successCallback = (response, cbObject) => {

            JsonData jObj = JsonMapper.ToObject(response);
            JsonData jStats = jObj["data"]["statistics"];
            IDictionary dStats = jStats as IDictionary;

            if (dStats == null)
                return;

            if (!dStats.Contains(userStat))
                return;

            if (!DataManager.instance.PlayerStats.ContainsKey(userStat))
                return;

            JsonData value = (JsonData)dStats[userStat];
            long valueAsLong = value.IsInt ? (int)value : (long)value;

            DataManager.instance.PlayerStats[userStat] = valueAsLong;

            GameEvents.instance.IncrementUserStat(userStat);
        };

        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        string jsonData = "{ \"" + userStat + "\" : 1 }";

        _bc.PlayerStatisticsService.IncrementUserStats(jsonData, successCallback, failureCallback);
    }

    //*************** GlobalStatisticsService Methods ***************
    public void ReadAllGlobalStats()
    {
        SuccessCallback successCallback = (response, cbObject) => {

            JsonData jObj = JsonMapper.ToObject(response);
            JsonData jStats = jObj["data"]["statistics"];
            IDictionary dStats = jStats as IDictionary;
            if (dStats != null)
            {
                foreach (string key in dStats.Keys)
                {
                    JsonData value = (JsonData)dStats[key];
                    DataManager.instance.GlobalStats[key] = value.IsInt ? (int)value : (long)value;
                }

                GameEvents.instance.InstantiateGlobalStats(); 
            }
        };

        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        _bc.GlobalStatisticsService.ReadAllGlobalStats(successCallback, failureCallback);
    }

    public void IncrementGlobalStats(string globalStatName)
    {
        SuccessCallback successCallback = (response, cbObject) => {

            DataManager dataManager = DataManager.instance; 

            JsonData jObj = JsonMapper.ToObject(response);
            JsonData jStats = jObj["data"]["statistics"];
            IDictionary dStats = jStats as IDictionary;
            
            if(dStats == null)
                return;
            
            if(!dStats.Contains(globalStatName))
                return;

            if (!dataManager.GlobalStats.ContainsKey(globalStatName))
                return;

            JsonData value = (JsonData)dStats[globalStatName];

            long valueAsLong = value.IsInt ? (int)value : (long)value;
            dataManager.GlobalStats[globalStatName] = valueAsLong;

            GameEvents.instance.IncrementGlobalStat(globalStatName);
        };

        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        string jsonData = "{ \"" + globalStatName + "\" : 1 }";

        _bc.GlobalStatisticsService.IncrementGlobalStats(jsonData, successCallback, failureCallback);
    }

    //*************** VirtualCurrencyService Methods ***************
    public void GetVirtualCurrency(string currency)
    {
        SuccessCallback successCallback = (response, cbObject) =>
        {
            JsonData jObj = JsonMapper.ToObject(response);
            JsonData jCurMap = jObj["data"]["currencyMap"];
            IDictionary dCurMap = jCurMap as IDictionary;

            foreach (string key in dCurMap.Keys)
            {
                DataManager.Currency c = null;
                if (DataManager.instance.Currencies.ContainsKey(key))
                {
                    c = DataManager.instance.Currencies[key];
                }
                else
                {
                    c = new DataManager.Currency();
                    DataManager.instance.Currencies[key] = c;
                }
                c.currencyType = key;
                c.purchased = (int)jCurMap[key]["purchased"];
                c.balance = (int)jCurMap[key]["balance"];
                c.consumed = (int)jCurMap[key]["consumed"];
                c.awarded = (int)jCurMap[key]["awarded"];
            }

            GameEvents.instance.GetVirtualCurrency();
        };

        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        _bc.VirtualCurrencyService.GetCurrency(currency, successCallback, failureCallback);
    }

    //*************** ScriptService Methods ***************
    public void RunCloudCodeScript(string scriptname, string scriptdata)
    {
        SuccessCallback successCallback = (response, cbObject) => { Debug.Log(string.Format("Success | {0}", response)); };
        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        _bc.ScriptService.RunScript(scriptname, scriptdata, successCallback, failureCallback);
    }

    //*************** IdentityService Methods ***************
    public void AttachEmailIdentity(string email, string password)
    {
        SuccessCallback successCallback = (response, cbObject) => { Debug.Log("Succesfully attached email."); };
        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        _bc.IdentityService.AttachEmailIdentity(email, password, successCallback, failureCallback);
    }

    public void MergeEmailIdentity(string email, string password)
    {
        SuccessCallback successCallback = (response, cbObject) => { Debug.Log("Succesfully merged email."); };
        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        _bc.IdentityService.MergeEmailIdentity(email, password, successCallback, failureCallback);
    }

    public void AttachUniversalIdentity(string username, string password)
    {
        SuccessCallback successCallback = (response, cbObject) => { Debug.Log("Succesfully attached universal identity."); };
        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        _bc.IdentityService.AttachUniversalIdentity(username, password, successCallback, failureCallback);
    }

    public void MergeUniversalIdentity(string username, string password)
    {
        SuccessCallback successCallback = (response, cbObject) => { Debug.Log("Succesfully merged universal identity."); };
        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        _bc.IdentityService.MergeUniversalIdentity(username, password, successCallback, failureCallback);
    }

    public void AttachTwitterIdentity(string userId, string token, string tokenSecret)
    {
        SuccessCallback successCallback = (response, cbObject) => { Debug.Log("Succesfully attached twitter identity."); };
        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        _bc.IdentityService.AttachTwitterIdentity(userId, token, tokenSecret, successCallback, failureCallback);
    }

    public void MergeTwitterIdentity(string userId, string token, string tokenSecret)
    {
        SuccessCallback successCallback = (response, cbObject) => { Debug.Log("Succesfully merged twitter identity."); };
        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        _bc.IdentityService.MergeTwitterIdentity(userId, token, tokenSecret, successCallback, failureCallback);
    }

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

    //AnthonyTODO: This will exist here temporarily until I Figure out where it should go.
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
