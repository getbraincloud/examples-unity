using BrainCloud;
using BrainCloud.JsonFx.Json;
using BrainCloud.Common;
using System.Collections.Generic;
using UnityEngine;

using Facebook.Unity;

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
    public static void AuthenticateEmail(string email, string password, bool forceCreate = true,
                                         SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.Wrapper.AuthenticateEmailPassword(email, password, forceCreate, onSuccess, onFailure, cbObject);

    /// <summary>
    /// Authenticate the user using a username they set and a password.
    /// </summary>
    public static void AuthenticateUniversal(string username, string password, bool forceCreate = true,
                                             SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null) =>
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

    #region External Authentication Methods

    /// <summary>
    /// 
    /// </summary>
    public static void AuthenticateFacebook(bool forceCreate = true, SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null)
    {
        // Adjust the permissions as you see fit
        // Please see Facebook's perfmissions reference:
        // https://developers.facebook.com/docs/permissions/reference
        var perms = new List<string>() { "public_profile" };

#if UNITY_STANDALONE_WIN || UNITY_WEBGL || UNITY_ANDROID
        FB.LogInWithReadPermissions(perms, (result) => /// <seealso cref="FB.LogInWithPublishPermissions(IEnumerable{string}, FacebookDelegate{ILoginResult})"/>
        {
            if (FB.IsLoggedIn)
            {
                Debug.Log($"Is AccessToken not null? {result.AccessToken != null}");
                Debug.Log($"Is AuthenticationToken not null? {result.AuthenticationToken != null}");

                if (result.AccessToken != null)
                {
                    BCManager.Wrapper.AuthenticateFacebook(result.AccessToken.UserId, result.AccessToken.TokenString, forceCreate, onSuccess, onFailure, cbObject);
                    return;
                }
            }
            else if (result.Cancelled)
            {
                onFailure(0, 0, JsonWriter.Serialize(new ErrorResponse(0, 0, "Log in was cancelled.")), cbObject);
                return;
            }

            // Error
            onFailure(0, 0, JsonWriter.Serialize(new ErrorResponse(0, 0, result.Error)), cbObject);
        });
#elif UNITY_IOS
        string nonce = System.DateTime.Now.Ticks.ToString(); // Please provide your own nonce, this is simply used as an example

        FB.Mobile.LoginWithTrackingPreference(LoginTracking.ENABLED, perms, nonce, (result) =>
        {
            if (FB.IsLoggedIn)
            {
                Debug.Log($"Is AccessToken not null? {result.AccessToken != null}");
                Debug.Log($"Is AuthenticationToken not null? {result.AuthenticationToken != null} If not, what is the nonce? Original: {nonce}, FB: {result.AuthenticationToken?.Nonce}");

                if (result.AccessToken != null)
                {
                    BCManager.Wrapper.AuthenticateFacebook(result.AccessToken.UserId, result.AccessToken.TokenString, forceCreate, onSuccess, onFailure, cbObject);
                    return;
                }
                else if (result.AuthenticationToken != null && result.AuthenticationToken.Nonce == nonce)
                {
                    BCManager.Wrapper.AuthenticateFacebook(FB.Mobile.CurrentProfile().UserID, result.AuthenticationToken.TokenString, forceCreate, onSuccess, onFailure, cbObject);
                    return;
                }
            }
            else if (result.Cancelled)
            {
                onFailure(0, 0, JsonWriter.Serialize(new ErrorResponse(0, 0, "Log in was cancelled.")), cbObject);
                return;
            }

            // Error
            onFailure(0, 0, JsonWriter.Serialize(new ErrorResponse(0, 0, result.Error)), cbObject);
        });
#else
        Debug.LogError("AuthenticateFacebook is not available on this platform. Check your scripting defines. Returning with error...");
        onFailure(0, 0, JsonWriter.Serialize(new ErrorResponse(0, 0, "<b>AuthenticateFacebook</b> is not available on this platform.")), cbObject);
#endif
    }

    /// <summary>
    /// Read more: https://getbraincloud.com/apidocs/apple-ios-14-5-privacy-facebook-limited-login-mode/
    /// </summary>
    public static void AuthenticateFacebookLimited(bool forceCreate = true, SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null)
    {
#if UNITY_IOS
        // Adjust the permissions as you see fit
        // These are the permissions available in Limited mode:
        // https://developers.facebook.com/docs/facebook-login/limited-login/permissions
        var perms = new List<string>() { "public_profile" };

        string nonce = System.DateTime.Now.Ticks.ToString(); // Please provide your own nonce, this is simply used as an example

        FB.Mobile.LoginWithTrackingPreference(LoginTracking.LIMITED, perms, nonce, (result) =>
        {
            if (FB.IsLoggedIn)
            {
                Debug.Log($"Is AccessToken not null? {result.AccessToken != null}");
                Debug.Log($"Is AuthenticationToken not null? {result.AuthenticationToken != null} If not, what is the nonce? Original: {nonce}, FB: {result.AuthenticationToken?.Nonce}");

                if (result.AccessToken != null)
                {
                    BCManager.Wrapper.AuthenticateFacebookLimited(result.AccessToken.UserId, result.AccessToken.TokenString, forceCreate, onSuccess, onFailure, cbObject);
                    return;
                }
                else if (result.AuthenticationToken != null && result.AuthenticationToken.Nonce == nonce)
                {
                    BCManager.Wrapper.AuthenticateFacebookLimited(FB.Mobile.CurrentProfile().UserID, result.AuthenticationToken.TokenString, forceCreate, onSuccess, onFailure, cbObject);
                    return;
                }
            }
            else if (result.Cancelled)
            {
                onFailure(0, 0, JsonWriter.Serialize(new ErrorResponse(0, 0, "Log in was cancelled.")), cbObject);
                return;
            }

            // Error
            onFailure(0, 0, JsonWriter.Serialize(new ErrorResponse(0, 0, result.Error)), cbObject);
        });
#else
        Debug.LogError("AuthenticateFacebookLimited is only available on iOS. Returning with error...");
        onFailure(0, 0, JsonWriter.Serialize(new ErrorResponse(0, 0, "<b>AuthenticateFacebookLimited</b> is only available on iOS. Please use <b>AuthenticateFacebook</b> instead.")), cbObject);
#endif
    }

    #endregion
}
