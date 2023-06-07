using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
//Need to use these for google
using Google.Impl;
using Google;

public class BrainCloudInterface : MonoBehaviour
{
    //these are simply references to the unity specific canvas system
    Text status;
    string statusText;
    string email;
    string authCode;
    string idToken;

    GoogleSignInConfiguration configuration;
    //the webClientId of our googleOpenId test app. To test your own app, enter in your apps own webClientId
    string webClientId = "780423637529-hcg9gdbo4egbbh7h6k9pkukd8a9j9f8u.apps.googleusercontent.com";

    // Use this for initialization
    void Start()
    {
        //allow the people who sign in to change profiles. 
        BCConfig._bc.SetAlwaysAllowProfileSwitch(true);
        BCConfig._bc.Client.EnableLogging(true);

        //unity's ugly way to look for gameobjects
        status = GameObject.Find("Status").GetComponent<Text>();

        configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestEmail = true,
            RequestIdToken = true,
            //auth code is not needed for OpenId authentication
            RequestAuthCode = true
        };     
    }

    void Update()
    {
        status.text = statusText;
    }

    public void OnGoogleSignIn()
    {
        //set the google configuration to the configuration object you set up
        GoogleSignIn.Configuration = configuration;
        
        //Can define this
        GoogleSignIn.Configuration.UseGameSignIn = false;

        //Can also define these tags here like this.
        //GoogleSignIn.Configuration.RequestEmail = true;
        //GoogleSignIn.Configuration.RequestIdToken = true;
        //GoogleSignIn.Configuration.RequestAuthCode = true;

        statusText = "Calling Sign in";
        // With the configuration set, its now time to start trying to sign in. Pass in a callback to wait for success. 
        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnGoogleAuthSignIn);
    }

    public void OnAuthBraincloudOpenId()
    {
        BCConfig._bc.AuthenticateGoogleOpenId(email, idToken, true, OnSuccess_Authenticate, OnError_Authenticate);
    }

    public void OnSuccess_Authenticate(string responseData, object cbObject)
    {
        statusText = "Logged into braincloud!\n" + responseData;
    }

    public void OnError_Authenticate(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        statusText = "Failed to Login to braincloud...\n" + statusMessage + "\n" + reasonCode;        
    }

    //use a callback with a task to easily get results from the callback in order to get the values you need. 
    public void OnGoogleAuthSignIn(Task<GoogleSignInUser> task)
    {
        if (task.IsFaulted)
        {
            using (IEnumerator<System.Exception> enumerator =
                    task.Exception.InnerExceptions.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    GoogleSignIn.SignInException error =
                            (GoogleSignIn.SignInException)enumerator.Current;
                    statusText = "Got Error: " + error.Status + " " + error.Message;
                }
                else
                {
                    statusText = "Got Unexpected Exception?!?" + task.Exception;
                }
            }
        }
        else if (task.IsCanceled)
        {
            statusText = "Canceled";
        }
        else
        {
            authCode = task.Result.AuthCode;
            idToken = task.Result.IdToken;
            email = task.Result.Email;
            statusText = "Welcome: " + task.Result.DisplayName + "!\n" + idToken + " = idToken\n" + email + " = email";
        }
    }

    public void OnGoogleSignOut()
    {
        GoogleSignIn.DefaultInstance.SignOut();
        statusText = "Signed out of Google";
    }

}
