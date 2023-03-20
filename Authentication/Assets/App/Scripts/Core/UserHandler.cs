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
    /// <summary>
    /// Whether the user logged in has no identities attached to them.
    /// </summary>
    public static bool AnonymousUser { get; set; }

    /// <summary>
    /// The Profile ID of the user. This is how they are identified on brainCloud.
    /// </summary>
    public static string ProfileID => BCManager.Wrapper.GetStoredProfileId();

    /// <summary>
    /// The Anonymous ID attached to the device. This identifies a user based on the device and allows
    /// the user to be anonymous or to log in without having to store the user's email and password.
    /// </summary>
    public static string AnonymousID => BCManager.Wrapper.GetStoredAnonymousId();

    /// <summary>
    /// How the user logged into this session.
    /// </summary>
    public static AuthenticationType AuthenticationType => AuthenticationType.FromString(BCManager.Wrapper.GetStoredAuthenticationType());

    #region Authentication Methods

    /// <summary>
    /// Authenticate the user using their email and password.
    /// </summary>
    public static void AuthenticateEmail(string email, string password, bool forceCreate = true, SuccessCallback onSuccess = null,
                                         FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.Wrapper.AuthenticateEmailPassword(email, password, forceCreate, onSuccess, onFailure, cbObject);

    /// <summary>
    /// Authenticate the user using a username they set and a password.
    /// </summary>
    public static void AuthenticateUniversal(string username, string password, bool forceCreate = true, SuccessCallback onSuccess = null,
                                             FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.Wrapper.AuthenticateUniversal(username, password, forceCreate, onSuccess, onFailure, cbObject);

    /// <summary>
    /// Authenticate the user anonymously.
    /// </summary>
    public static void AuthenticateAnonymous(SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.Wrapper.AuthenticateAnonymous(onSuccess, onFailure, cbObject);

    /// <summary>
    /// Authenticate the user in a way that can be customize. Allows you to send user inputted information with <paramref name="extraJSON"/>.
    /// </summary>
    public static void AuthenticateAdvanced(AuthenticationType authType, AuthenticationIds ids, Dictionary<string, object> extraJSON,
                                            bool forceCreate = true, SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.Wrapper.AuthenticateAdvanced(authType, ids, forceCreate, extraJSON, onSuccess, onFailure, cbObject);

    /// <summary>
    /// Allows the user to reconnect using only their <see cref="AnonymousID"/>.
    /// </summary>
    public static void HandleUserReconnect(SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.Wrapper.Reconnect(onSuccess, onFailure, cbObject);

    /// <summary>
    /// Allows the user to log out of the app during a session.
    /// </summary>
    public static void HandleUserLogout(SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.PlayerStateService.Logout(onSuccess, onFailure, cbObject);

    /// <summary>
    /// Reset the authentication data stored on the user's device.
    /// </summary>
    public static void ResetAuthenticationData()
    {
        BCManager.Wrapper.ResetStoredProfileId();
        BCManager.Wrapper.ResetStoredAnonymousId();
        BCManager.Wrapper.ResetStoredAuthenticationType();
    }

    #endregion
}
