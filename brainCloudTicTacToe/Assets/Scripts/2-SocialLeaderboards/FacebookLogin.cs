using UnityEngine;
using System.Collections;
using BrainCloud;
using System;
using LitJson;

public class FacebookLogin : MonoBehaviour {
	public Texture brainCloudLogo;
	public Spinner spinner;
	public string startScene;

	static public string PlayerId;
	static public string PlayerPicUrl;
	static public string PlayerName;

	private bool m_isConnecting = false;

	// Use this for initialization
	void Start () 
	{
        BrainCloudWrapper.Initialize();
	}
	
	// Update is called once per frame
	void Update () {
	}

	// every monobehavior has this
	void OnApplicationQuit() 
	{
	}

	void OnGUI () 
	{
		GUILayout.Window(0, new Rect(Screen.width / 2 - 125, Screen.height / 2 - 100, 250, 200), OnWindow, "brainCloud Login");
	}
	
	void OnWindow(int windowId) 
	{
		GUILayout.FlexibleSpace ();
		GUILayout.BeginHorizontal ();
		GUILayout.FlexibleSpace ();
		GUILayout.BeginVertical ();

		GUILayout.Box(brainCloudLogo);
		GUILayout.Space (30);

		GUI.enabled = !m_isConnecting;
		if (GUILayout.Button ("Connect with Facebook", GUILayout.MinHeight (50), GUILayout.MinWidth (100))) 
		{
			m_isConnecting = true;
			spinner.gameObject.SetActive(true);
			FB.Init(OnFacebookInit);
		}
		GUI.enabled = true;

		GUILayout.EndVertical ();
		GUILayout.FlexibleSpace ();
		GUILayout.EndHorizontal ();
		GUILayout.FlexibleSpace ();
	}

	void OnFacebookInit()
	{
		// Login to facebook. Using friends permission (This is required for social leaderboards)
		FB.Login("friends", OnFacebookLogin);
	}

	void OnFacebookLogin(FBResult result)
	{
		if (FB.IsLoggedIn) 
		{
			// It worked
			Debug.Log("Facebook auth success");

			// Now we login using Braincloud
			BrainCloudWrapper.GetBC().AuthenticationService.AuthenticateFacebook(
				FB.UserId, 
				FB.AccessToken, 
				true, 
				OnBraincloudAuthenticateFacebookSuccess, 
				OnBraincloudAuthenticateFacebookFailed);

			// Meanwhile, we will fetch info about our player, to get the profile pic and name
			FB.API("/me?fields=name,picture", Facebook.HttpMethod.GET, OnFacebookMe);
		}
		else
		{
			// It failed
			Debug.LogError("Facebook auth failed");
			m_isConnecting = false;
			spinner.gameObject.SetActive(false); // Hide spinner
		}
	}

	void OnFacebookMe(FBResult result)
	{
		JsonData data = JsonMapper.ToObject(result.Text);

		PlayerName = (string)data["name"];
		PlayerPicUrl = (string)data["picture"]["data"]["url"];
	}

	void OnBraincloudAuthenticateFacebookSuccess(string responseData, object cbObject)
	{
		Debug.Log("AuthenticateFacebook success");

		JsonData data = JsonMapper.ToObject(responseData)["data"];
		PlayerId = data["profileId"].ToString();
		Application.LoadLevel(startScene); // Load our game
	}

	void OnBraincloudAuthenticateFacebookFailed(int a, int b, string responseData, object cbObject)
	{
		m_isConnecting = false;
		spinner.gameObject.SetActive(false); // Hide spinner
		Debug.LogError("AuthenticateFacebook failed: " + responseData);
	}
}
