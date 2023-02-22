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
        BCManager.Wrapper.AuthenticateEmailPassword(email, password, true, BCManager.HandleSuccess("Email Authentication Successful", onSuccess),
                                                                           BCManager.HandleFailure("Email Authentication Failed", onFailure));
    }

    public static void AuthenticateUniversal(string username, string password, Action onSuccess = null, Action onFailure = null)
    {
        BCManager.Wrapper.AuthenticateUniversal(username, password, true, BCManager.HandleSuccess("Universal Authentication Successful", onSuccess),
                                                                          BCManager.HandleFailure("Universal Authentication Failed", onFailure));
    }

    public static void AuthenticateAnonymous(Action onSuccess = null, Action onFailure = null)
    {
        BCManager.Wrapper.AuthenticateAnonymous(BCManager.HandleSuccess("Anonymous Authentication Successful", onSuccess),
                                                BCManager.HandleFailure("Anonymous Authentication Failed", onFailure));
    }

    public static void AuthenticateAdvanced(AuthenticationType authType, AuthenticationIds ids, Dictionary<string, object> extraJson,
                                            Action onSuccess = null, Action onFailure = null)
    {
        BCManager.Wrapper.AuthenticateAdvanced(authType, ids, true, extraJson,BCManager.HandleSuccess("Authentication Successful", onSuccess),
                                                                              BCManager.HandleFailure("Authentication Failed", onFailure));
    }

    public static void HandleUserReconnect(Action onSuccess = null, Action onFailure = null)
    {
        BCManager.Wrapper.Reconnect(BCManager.HandleSuccess("Reconnect Success", onSuccess),
                                    BCManager.HandleFailure("Reconnect Failed", onFailure));
    }

    public static void HandleUserLogout(Action onSuccess = null, Action onFailure = null)
    {
        BCManager.PlayerStateService.Logout(BCManager.HandleSuccess("Logout Success", onSuccess),
                                            BCManager.HandleFailure("Logout Failed", onFailure));
    }

    public static void ResetAuthenticationData()
    {
        BCManager.Wrapper.ResetStoredProfileId();
        BCManager.Wrapper.ResetStoredAnonymousId();
        BCManager.Wrapper.ResetStoredAuthenticationType();
    }

    #endregion
}
