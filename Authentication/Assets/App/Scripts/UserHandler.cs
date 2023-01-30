using BrainCloud;
using BrainCloud.Common;
using BrainCloud.Entity;
using System;
using System.Collections.Generic;

public static class UserHandler
{
    public static string ProfileID => BCManager.Wrapper.GetStoredProfileId();

    public static string AnonymousID => BCManager.Wrapper.GetStoredAnonymousId();

    public static AuthenticationType AuthenticationType => AuthenticationType.FromString(BCManager.Wrapper.GetStoredAuthenticationType());

    #region Authentication Methods

    public static void AuthenticateEmail(string email, string password, Action onSuccess = null, Action onFailure = null)
    {
        BCManager.Wrapper.ResetStoredProfileId();
        BCManager.Wrapper.ResetStoredAnonymousId();
        BCManager.Wrapper.AuthenticateEmailPassword(email, password, true, BCManager.CreateSuccessCallback("Email Authentication Successful", onSuccess),
                                                                           BCManager.CreateFailureCallback("Email Authentication Failed", onFailure));
    }

    public static void AuthenticateUniversal(string username, string password, Action onSuccess = null, Action onFailure = null)
    {
        BCManager.Wrapper.ResetStoredProfileId();
        BCManager.Wrapper.ResetStoredAnonymousId();
        BCManager.Wrapper.AuthenticateUniversal(username, password, true, BCManager.CreateSuccessCallback("Universal Authentication Successful", onSuccess),
                                                                          BCManager.CreateFailureCallback("Universal Authentication Failed", onFailure));
    }

    public static void AuthenticateAnonymous(Action onSuccess = null, Action onFailure = null)
    {
        BCManager.Wrapper.AuthenticateAnonymous(BCManager.CreateSuccessCallback("Anonymous Authentication Successful", onSuccess),
                                                BCManager.CreateFailureCallback("Anonymous Authentication Failed", onFailure));
    }

    public static void AuthenticateAdvanced(AuthenticationType authType, AuthenticationIds ids, Dictionary<string, object> extraJson,
                                            Action onSuccess = null, Action onFailure = null)
    {
        BCManager.Wrapper.AuthenticateAdvanced(authType, ids, true, extraJson,BCManager.CreateSuccessCallback("Authentication Successful", onSuccess),
                                                                              BCManager.CreateFailureCallback("Authentication Failed", onFailure));
    }

    public static void HandleUserLogout(Action onSuccess = null, Action onFailure = null)
    {
        BCManager.PlayerStateService.Logout(BCManager.CreateSuccessCallback("Logout Success", onSuccess),
                                            BCManager.CreateFailureCallback("Logout Failed", onFailure));
    }

    public static void ResetAuthenticationData()
    {
        BCManager.Wrapper.ResetStoredProfileId();
        BCManager.Wrapper.ResetStoredAnonymousId();
        BCManager.Wrapper.ResetStoredAuthenticationType();

        PlayerPrefsHandler.SavePlayerPref(PlayerPrefKey.RememberUser, false);
    }

    #endregion
}
