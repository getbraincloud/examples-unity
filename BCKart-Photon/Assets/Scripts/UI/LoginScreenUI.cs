using UnityEngine;
using UnityEngine.UI;
using System;

public class LoginScreenUI : MonoBehaviour
{
    public InputField usernameInput;
    public InputField pwdInput;
    public Button confirmButton;

    public UIScreen profileSetupUI;

    public void AssertProfileLoggedAuthenticated()
    {
        GetMyScreen();
        BrainCloudWrapper bcWrapper = BCManager.Wrapper;

        // if BC is not authenticated, 
        // and they have info to auto-login, do it.
        string storedProfile = bcWrapper.GetStoredProfileId();
        string storedAnonymous = bcWrapper.GetStoredAnonymousId();
        bool isAuthenticated = bcWrapper.Client.Authenticated;
        if (!isAuthenticated)
        {
            UIScreen.Focus(myScreen);
            if (storedProfile != null && storedProfile != "" &&
                storedAnonymous != null && storedAnonymous != "")
            {
                bcWrapper.AuthenticateAnonymous(
                    OnAuthSuccess,
                    OnAuthError);
            }
        }  
    }

    private void Start()
    {
        GetMyScreen();
        usernameInput.onValueChanged.AddListener(x =>
        {
            // disallows empty usernames to be input
            confirmButton.interactable = !string.IsNullOrEmpty(x);
        });
        pwdInput.onValueChanged.AddListener(x =>
        {
            // disallows empty pwd to be input
            confirmButton.interactable = !string.IsNullOrEmpty(x);
        });
    }

    public void OnLogin()
    {
        authenticateWithBrainCloud(false);
    }

    private void authenticateWithBrainCloud(bool forceCreateProfile)
    {
        BCManager.Wrapper.AuthenticateUniversal(
         usernameInput.text,
         pwdInput.text,
         forceCreateProfile,
         OnAuthSuccess,
         OnAuthError);
    }

    public void OnAuthSuccess(string jsonResponse, object cbObject)
    {
        Debug.Log("LoginScreenUI OnAuthSuccess: " + jsonResponse);

        BrainCloudAuthResponse authResponse = JsonUtility.FromJson<BrainCloudAuthResponse>(jsonResponse);

        if (authResponse?.data != null)
        {
            ClientInfo.LoginData = authResponse.data;
            ClientInfo.Username = ClientInfo.LoginData.playerName;
        }
        else
        {
            Debug.LogError("Failed to parse BrainCloudLoginData from response!");
        }

        // we also need to get the username from the identities
        BCManager.Wrapper.IdentityService.GetIdentities(OnGetIdentitiesSuccess, OnGetIdentitiesError);
    }

    public void OnAuthError(int status, int reasonCode, string jsonError, object cbObject)
    {
        Debug.LogError($"LoginScreenUI OnAuthError: {status}, {reasonCode}, {jsonError}");

        // handle display errors to user
        switch (reasonCode)
        {
            // Lets try a force create on this automagically
            case BrainCloud.ReasonCodes.MISSING_PROFILE_ERROR:
                {
                    authenticateWithBrainCloud(true);
                }
                break;
        }
    }
    
    public void OnGetIdentitiesSuccess(string jsonResponse, object cbObject)
    {
        Debug.Log("LoginScreenUI OnGetIdentitiesSuccess: " + jsonResponse);

        // Parse JSON into our strongly typed structure
        var response = JsonUtility.FromJson<IdentityResponse>(jsonResponse);

        // get the universal id which is assumed to be logged in
        var universalId = response?.data?.identities?.Universal;

        if (!string.IsNullOrEmpty(universalId))
        {
            ClientInfo.LoginData.universalId = universalId;
        }

        // Ok! We've finished the chain of data after logging in!
        myScreen.Back();

        // if their name is not setup, force them to set it up
        if (string.IsNullOrEmpty(ClientInfo.LoginData.playerName))
        {
            UIScreen.Focus(profileSetupUI);
        }
    }

    public void OnGetIdentitiesError(int status, int reasonCode, string jsonError, object cbObject)
    {
        Debug.LogError($"LoginScreenUI OnGetIdentitiesError: {status}, {reasonCode}, {jsonError}");
    }

    private UIScreen GetMyScreen()
    {
        if (!myScreen)
            myScreen = GetComponent<UIScreen>();

        return myScreen;
    }
    
    private UIScreen myScreen;
}

[Serializable]
public class Identities
{
    public string Universal;
}

[Serializable]
public class Data
{
    public Identities identities;
}

[Serializable]
public class IdentityResponse
{
    public Data data;
    public int status;
}

[Serializable]
public class BrainCloudAuthResponse
{
    public BrainCloudLoginData data;
    public int status;
}

[Serializable]
public class BrainCloudLoginData
{
    public string id;
    public string profileId;
    public string countryCode;
    public string languageCode;
    public string playerName; // this is mapped to the display field (Nickname)
    public bool emailVerified;
    public bool isTester;
    public string sessionId;
    public string newUser;
    public string emailAddress;
    public string universalId; // this is different to the player name in that this gets mapped to the universal identity
}