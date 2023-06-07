using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Threading;
using Google;

public class BCInterface : MonoBehaviour
{
    Text status;
    string statusText;
    string authCode;
    string idToken;
    string email;

    GoogleSignInConfiguration configuration;
    string webClientId = "1015055125254-p6j1sngbe2ui5mgneaakrkulo6cfemd4.apps.googleusercontent.com";
    //1015055125254-51e7vqcpp0j3r29l6dnbpngmrvb9ruc9.apps.googleusercontent.com
    //com.googleusercontent.apps.1015055125254-51e7vqcpp0j3r29l6dnbpngmrvb9ruc9

    // Start is called before the first frame update
    void Start()
    {
        BCConfig._bc.SetAlwaysAllowProfileSwitch(true);
        BCConfig._bc.Client.EnableLogging(true);

        status = GameObject.Find("Status").GetComponent<Text>();

        configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestEmail = true,
            RequestIdToken = true
        };
        statusText = "Setup!\n" + webClientId;
    }

    // Update is called once per frame
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

    public void OnGoogleSignOut()
    {
        GoogleSignIn.DefaultInstance.SignOut();
        statusText = "Signed out of Google";
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
            statusText = "Welcome: " + task.Result.DisplayName + "!\n" + idToken + " = idToken\n" + email + " = email\n" + authCode + " = authCode";
        }
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
}
