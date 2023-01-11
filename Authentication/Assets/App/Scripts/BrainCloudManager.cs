using BrainCloud.Common;
using BrainCloud;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BrainCloudManager : MonoBehaviour
{
    public static string AppName => Application.productName;

    public BrainCloudWrapper Wrapper { get; private set; }
    public string ProfileID => Wrapper.GetStoredProfileId();
    public string AnonymousID => Wrapper.GetStoredAnonymousId();
    public AuthenticationType AuthenticationType => AuthenticationType.FromString(Wrapper.GetStoredAuthenticationType());

    public static SuccessCallback CreateSuccessCallback(string logMessage = null, Action onSuccess = null)
    {
        logMessage = string.IsNullOrEmpty(logMessage) ? "Success" : logMessage;
        return (jsonResponse, cbObject) =>
        {
            Debug.Log($"{logMessage}\nJSON Response:\n{jsonResponse}");

            onSuccess?.Invoke();
        };
    }

    public static FailureCallback CreateFailureCallback(string errorMessage = null, Action onFailure = null)
    {
        errorMessage = string.IsNullOrEmpty(errorMessage) ? "Failure" : errorMessage;
        return (status, reasonCode, jsonError, cbObject) =>
        {
            Debug.LogError($"{errorMessage}\nStatus: {status}\nReason: {reasonCode}\nJSON Response:\n{jsonError}");

            onFailure?.Invoke();
        };
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        Wrapper = gameObject.AddComponent<BrainCloudWrapper>();
        Wrapper.WrapperName = AppName;
        Wrapper.Init(); // Init data is taken from the brainCloud Unity Plugin

        PlayerPrefsHandler.InitPlayerPrefs();
    }

    public void AuthenticateEmail(string email, string password, Action onSuccess = null, Action onFailure = null)
    {
        Wrapper.ResetStoredProfileId();
        Wrapper.ResetStoredAnonymousId();
        Wrapper.AuthenticateEmailPassword(email, password, true, CreateSuccessCallback("Email Authentication Successful", onSuccess),
                                                                 CreateFailureCallback("Email Authentication Failed", onFailure));
    }

    public void AuthenticateUniversal(string username, string password, Action onSuccess = null, Action onFailure = null)
    {
        Wrapper.ResetStoredProfileId();
        Wrapper.ResetStoredAnonymousId();
        Wrapper.AuthenticateUniversal(username, password, true, CreateSuccessCallback("Universal Authentication Successful", onSuccess),
                                                                CreateFailureCallback("Universal Authentication Failed", onFailure));
    }

    public void AuthenticateAnonymous(Action onSuccess = null, Action onFailure = null)
    {
        Wrapper.AuthenticateAnonymous(CreateSuccessCallback("Anonymous Authentication Successful", onSuccess),
                                      CreateFailureCallback("Anonymous Authentication Failed", onFailure));
    }

    public void AuthenticateAdvanced(AuthenticationType authType, AuthenticationIds ids, Dictionary<string, object> extraJson,
                                     Action onSuccess = null, Action onFailure = null)
    {
        Wrapper.AuthenticateAdvanced(authType, ids, true, extraJson,
                                     CreateSuccessCallback("Authentication Successful", onSuccess),
                                     CreateFailureCallback("Authentication Failed", onFailure));
    }
}
