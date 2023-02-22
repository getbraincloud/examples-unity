using BrainCloud;
using BrainCloud.Common;
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

    public static void AuthenticateEmail(string email, string password, bool forceCreate = true, SuccessCallback onSuccess = null,
                                         FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.Wrapper.AuthenticateEmailPassword(email, password, forceCreate, onSuccess, onFailure, cbObject);

    public static void AuthenticateUniversal(string username, string password, bool forceCreate = true, SuccessCallback onSuccess = null,
                                             FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.Wrapper.AuthenticateUniversal(username, password, forceCreate, onSuccess, onFailure, cbObject);

    public static void AuthenticateAnonymous(SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.Wrapper.AuthenticateAnonymous(onSuccess, onFailure, cbObject);

    public static void AuthenticateAdvanced(AuthenticationType authType, AuthenticationIds ids, Dictionary<string, object> extraJson,
                                            bool forceCreate = true, SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.Wrapper.AuthenticateAdvanced(authType, ids, forceCreate, extraJson, onSuccess, onFailure, cbObject);

    public static void HandleUserReconnect(SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.Wrapper.Reconnect(onSuccess, onFailure, cbObject);

    public static void HandleUserLogout(SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.PlayerStateService.Logout(onSuccess, onFailure, cbObject);

    public static void ResetAuthenticationData()
    {
        BCManager.Wrapper.ResetStoredProfileId();
        BCManager.Wrapper.ResetStoredAnonymousId();
        BCManager.Wrapper.ResetStoredAuthenticationType();
    }

    #endregion
}
