using BrainCloud;
using BrainCloud.Common;
using System;
using System.Collections.Generic;

/// TODO: More authentication methods are coming!
/// <summary>
/// <para>
/// Interacts with <see cref="BCManager"/> to handle User Authentication.
/// </para>
///
/// <para>
/// This script can be copied into your Unity or C# project alongside <see cref="BCManager"/>
/// to be used for all the various authentication methods for your brainCloud app.
/// </para>
/// 
/// <seealso cref="BrainCloudWrapper"/><br></br>
/// <seealso cref="BrainCloudClient"/><br></br>
/// <seealso cref="BCManager"/>
/// </summary>
public static class UserHandler
{
    public static string ProfileID => BCManager.Wrapper.GetStoredProfileId();

    public static string AnonymousID => BCManager.Wrapper.GetStoredAnonymousId();

    public static AuthenticationType AuthenticationType => AuthenticationType.FromString(BCManager.Wrapper.GetStoredAuthenticationType());

    #region Authentication Methods

    public static void AuthenticateEmail(string email, string password, Action onSuccess = null, Action onFailure = null)
    {
        BCManager.Wrapper.AuthenticateEmailPassword(email, password, true, BCManager.CreateSuccessCallback("Email Authentication Successful", onSuccess),
                                                                           BCManager.CreateFailureCallback("Email Authentication Failed", onFailure));
    }

    public static void AuthenticateUniversal(string username, string password, Action onSuccess = null, Action onFailure = null)
    {
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

    public static void HandleUserReconnect(Action onSuccess = null, Action onFailure = null)
    {
        BCManager.Wrapper.Reconnect(BCManager.CreateSuccessCallback("Reconnect Success", onSuccess),
                                    BCManager.CreateFailureCallback("Reconnect Failed", onFailure));
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
    }

    #endregion
}
