using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System.Threading.Tasks;
using Google.Impl;
using Google;

public class BCInterface : MonoBehaviour
{
    Text status;
    string statusText;
    string googleId;
    string serverAuthCode;
    GoogleSignInConfiguration configuration;

    string webClientId = "1022852368407-7qlbvmsn30bu9nbk63bjaqucnra3otb3.apps.googleusercontent.com";


    // Start is called before the first frame update
    void Start()
    {
        //allow the people who sign in to change profiles. 
        BCConfig._bc.SetAlwaysAllowProfileSwitch(true);
        BCConfig._bc.Client.EnableLogging(true);

        status = GameObject.Find("Status").GetComponent<Text>();

        configuration = configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestEmail = true,
            RequestIdToken = true,
            RequestAuthCode = true
        };
    }

    // Update is called once per frame
    void Update()
    {
        status.text = statusText;
    }

    public void OnGoogleSignOut()
    {
        //Sign out
        PlayGamesPlatform.Instance.SignOut();
        GoogleSignIn.DefaultInstance.SignOut();
        statusText = "Setup and Signed out of Google Play\nNow, authenticate Google!";
    }

    public void OnGoogleSignIn()
    {
        //set the google configuration to the configuration object you set up
        GoogleSignIn.Configuration = configuration;

        //Can define this if you're using the game signin
        GoogleSignIn.Configuration.UseGameSignIn = true;

        // With the configuration set, its now time to start trying to sign in. Pass in a callback to wait for success. 
        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnGoogleResult);
    }

    //use a callback with a task to easily get results from the callback in order 	to get the values you need. 
    public void OnGoogleResult(Task<GoogleSignInUser> task)
    {
        if (task.IsFaulted)
        {
            statusText = "Failed to log in to google...";
        }
        else
        {
            serverAuthCode = task.Result.AuthCode;
            statusText = "Logged into google! \n ServerAuthCode: " + serverAuthCode; 
        }
    }

    public void OnGooglePlayGamesSignIn()
    {
        statusText = "Requesting Tokens";

        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
        .RequestIdToken()
        .RequestServerAuthCode(false)
        .Build();

        statusText += "\ncompleted requesting, now initializing\n" + "\nRequesting Auth Code: " + config.IsRequestingAuthCode;

        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.Activate();

        Social.localUser.Authenticate((bool success) => {
            if (success)
            {
                googleId = PlayGamesPlatform.Instance.GetUserId();
                statusText = "Logged into play games! \n GoogleId: " + googleId;
            }
            else
            {
                statusText = "Failed to log into play games...";
            }
        });
    }

    public void OnAuthBraincloud()
    {
        BCConfig._bc.AuthenticateGoogle(googleId, serverAuthCode, true, OnSuccess_Authenticate, OnError_Authenticate);
    }

    public void OnSuccess_Authenticate(string responseData, object cbObject)
    {
        statusText = "Logged into braincloud! \n" + responseData;
    }

    public void OnError_Authenticate(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        statusText = "Failed to Login to braincloud...";
    }
}
