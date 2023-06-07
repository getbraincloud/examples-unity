using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SignInWithApple;

public class BCInterface : MonoBehaviour
{
	Text status;
	string statusText;
    string email;
	string userId;
	string idToken;

	void Start()
    {
		BCConfig._bc.SetAlwaysAllowProfileSwitch(true);
		BCConfig._bc.Client.EnableLogging(true);

		status = GameObject.Find("Status").GetComponent<Text>();

        statusText = "READY";

		gameObject.AddComponent<SignInWithApple>();
	}

    void Update()
    {
		status.text = statusText;
    }

    public void OnAppleSignIn()
	{
        statusText = "signing in with apple";
		var siwa = gameObject.GetComponent<SignInWithApple>();
		siwa.Login(OnLogin);
	}

	public void GetCredentialStateApple()
	{
		// User id that was obtained from the user signed into your app for the first time.
		var siwa = gameObject.GetComponent<SignInWithApple>();
		siwa.GetCredentialState("<userid>", OnCredentialState);
	}

	public void OnAuthBraincloudApple()
	{
        statusText = "Signing in with braincloud";
		BCConfig._bc.AuthenticateApple(userId, idToken, true, OnSuccess_Authenticate, OnError_Authenticate);
	}

	public void OnSuccess_Authenticate(string responseData, object cbObject)
	{
		statusText = "Logged into braincloud!\n" + responseData;
	}

	public void OnError_Authenticate(int statusCode, int reasonCode, string statusMessage, object cbObject)
	{
		statusText = "Failed to Login to braincloud...\n" + statusMessage + "\n" + reasonCode;
	}

	private void OnCredentialState(SignInWithApple.CallbackArgs args)
	{
		Debug.Log("User credential state is: " + args.credentialState);
        statusText = "Sign in with Apple login has completed.";
    }

	private void OnLogin(SignInWithApple.CallbackArgs args)
	{
		Debug.Log("Sign in with Apple login has completed.");
        statusText = "Sign in with Apple login has completed.\n USERID" + args.userInfo.userId + "/n IDTOKEN" + args.userInfo.idToken;
        userId = args.userInfo.userId;
        idToken = args.userInfo.idToken;
    }
}
