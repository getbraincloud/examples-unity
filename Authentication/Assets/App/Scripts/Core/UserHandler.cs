using BrainCloud;
using BrainCloud.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

#if FACEBOOK_SDK
using Facebook.Unity;
#endif

#if GOOGLE_PLAY_GAMES_SDK
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

using Google;

/// TODO: More authentication methods are coming!
/// <summary>
/// Interacts with <see cref="BCManager"/> to handle User Authentication.
///
/// <para>
/// This script can be copied into your Unity or C# project alongside <see cref="BCManager"/>
/// to be used for all the various authentication methods for your brainCloud app.
/// </para>
/// 
/// <br><seealso cref="BrainCloudWrapper"/></br>
/// <br><seealso cref="BrainCloudClient"/></br>
/// <br><seealso cref="BCManager"/></br>
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
    /// Authenticate the user anonymously. This keeps the ProfileID tied to the device.
    /// If the user disconnects their ProfileID from the app, then the user can no longer log into the same account.
    /// </summary>
    public static void AuthenticateAnonymous(SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.Wrapper.AuthenticateAnonymous(onSuccess, onFailure, cbObject);

    /// <summary>
    /// Authenticate the user in a way that can be customized. Allows you to send user inputted information with <paramref name="extraJSON"/>.
    /// <b>AdvancedAuthPostHook</b> will be called on brainCloud to make use of the <paramref name="extraJSON"/>.
    /// <br>Note: This example only accepts <see cref="UserData"/> for the JSON. This will create a user entity for the user.</br>
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
    /// <br>Note: If the user is logged in anonymously then they will no longer be able to log back into to the same account!</br>
    /// </summary>
    public static void ResetAuthenticationData()
    {
        BCManager.Wrapper.ResetStoredProfileId();
        BCManager.Wrapper.ResetStoredAnonymousId();
        BCManager.Wrapper.ResetStoredAuthenticationType();
    }

    #endregion

#region External Authentication Methods

#if FACEBOOK_SDK
    /// <summary>
    /// Authenticate the user using their Facebook account.
    /// </summary>
    /// Facebook SDK for Unity: https://developers.facebook.com/docs/unity
    public static void AuthenticateFacebook(bool forceCreate = true, SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null)
    {
        // Adjust the permissions as it relates to your app
        // Please see Facebook's perfmissions reference:
        // https://developers.facebook.com/docs/permissions/reference
        var perms = new List<string>() { "public_profile" };

        void onFBResult(ILoginResult result)
        {
            if (FB.IsLoggedIn && result.AccessToken != null)
            {
                BCManager.Wrapper.AuthenticateFacebook(result.AccessToken.UserId, result.AccessToken.TokenString,
                                                       forceCreate, onSuccess, onFailure, cbObject);
            }
            else if (result.Cancelled)
            {
                onFailure(0, 0, new ErrorResponse(0, 0, "Log in was cancelled.").Serialize(), cbObject);
            }
            else // Error
            {
                string errorMessage = result.Error.IsNullOrEmpty() ? "An error has occured. Please try again." : result.Error;
                onFailure(0, 0, new ErrorResponse(0, 0, errorMessage).Serialize(), cbObject);
            }
        }

#if UNITY_STANDALONE || UNITY_WEBGL || UNITY_ANDROID
        FB.LogInWithReadPermissions(perms, onFBResult); /// <seealso cref="FB.LogInWithPublishPermissions(IEnumerable{string}, FacebookDelegate{ILoginResult})"/>
#elif UNITY_IOS
        FB.Mobile.LoginWithTrackingPreference(LoginTracking.ENABLED, perms, null, onFBResult);
#else
        Debug.LogError("AuthenticateFacebook is not available on this platform. Check your scripting defines. Returning with error...");
        onFailure(0, 0, new ErrorResponse(0, 0, "<b>AuthenticateFacebook</b> is not available on this platform.").Serialize(), cbObject);
#endif
    }

    /// <summary>
    /// Authenticate the user using their Facebook account with limited permissions.
    /// Facebook's Graph API will not be available for these users.
    /// <br>Only available on iOS.</br>
    /// </summary>
    /// Read more: https://getbraincloud.com/apidocs/apple-ios-14-5-privacy-facebook-limited-login-mode/
    /// Facebook Limited Login: https://developers.facebook.com/docs/facebook-login/limited-login
    public static void AuthenticateFacebookLimited(bool forceCreate = true, SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null)
    {
#if UNITY_IOS
        // Adjust the permissions as it relates to your app
        // These are the permissions available in Limited mode:
        // https://developers.facebook.com/docs/facebook-login/limited-login/permissions
        var perms = new List<string>() { "public_profile" };

        string nonce = System.DateTime.UtcNow.Ticks.ToString(); // This is only an example of a nonce; please provide your own method of generating a nonce

        FB.Mobile.LoginWithTrackingPreference(LoginTracking.LIMITED, perms, nonce, (result) =>
        {
            if (FB.IsLoggedIn && result.AuthenticationToken != null &&
                result.AuthenticationToken.Nonce == nonce)
            {
                BCManager.Wrapper.AuthenticateFacebookLimited(FB.Mobile.CurrentProfile().UserID,
                                                              result.AuthenticationToken.TokenString,
                                                              forceCreate, onSuccess, onFailure, cbObject);
            }
            else if (result.Cancelled)
            {
                onFailure(0, 0, new ErrorResponse(0, 0, "Log in was cancelled.").Serialize(), cbObject);
            }
            else // Error
            {
                string errorMessage = result.Error.IsNullOrEmpty() ? "An error has occured. Please try again." : result.Error;
                onFailure(0, 0, new ErrorResponse(0, 0, errorMessage).Serialize(), cbObject);
            }
        });
#else
        Debug.LogError("AuthenticateFacebookLimited is only available on iOS. Returning with error...");
        onFailure(0, 0, new ErrorResponse(0, 0, "<b>AuthenticateFacebookLimited</b> is only available on iOS. Please use <b>AuthenticateFacebook</b> instead.").Serialize(), cbObject);
#endif
    }
#endif

#if GOOGLE_PLAY_GAMES_SDK
    /// <summary>
    /// Authenticate the user using their Google account via Google Play Games.
    /// </summary>
    /// Google Play Games plugin for Unity: https://developer.android.com/games/pgs/unity/overview
    public static void AuthenticateGoogle(bool forceCreate = true, SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null)
    {
#if UNITY_ANDROID
        PlayGamesPlatform.Instance.ManuallyAuthenticate((status) =>
        {
            if (status == SignInStatus.Success)
            {
                PlayGamesPlatform.Instance.RequestServerSideAccess(false, (response) =>
                {
                    BCManager.Wrapper.AuthenticateGoogle(PlayGamesPlatform.Instance.GetUserId(), response,
                                                         forceCreate, onSuccess, onFailure, cbObject);
                });
            }
            else if (status == SignInStatus.Canceled)
            {
                onFailure(0, 0, new ErrorResponse(0, 0, "Log in was cancelled.").Serialize(), cbObject);
            }
            else // Error
            {
                onFailure(0, 0, new ErrorResponse(0, 0, "An error has occured. Please try again.").Serialize(), cbObject);
            }
        });
#else
        Debug.LogError("AuthenticateGoogle is not available on this platform. Check your scripting defines. Returning with error...");
        onFailure(0, 0, new ErrorResponse(0, 0, "<b>AuthenticateGoogle</b> is not available on this platform.").Serialize(), cbObject);
#endif
    }
#endif

    /// <summary>
    /// Authenticate the user using their Google account via Google Sign-In.
    /// </summary>
    /// Google Sign-In Unity Plugin: https://developers.google.com/identity/sign-in/
    public static void AuthenticateGoogleOpenId(bool forceCreate = true, SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null)
    {
#if UNITY_ANDROID || UNITY_IOS
        // Adjust the configuration as it relates to your app
        // This will need to be set-up in your app somewhere before sign-in
        // These values are all required to be set to these for authentication
        if (GoogleSignIn.Configuration == null)
        {
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                WebClientId = "YOUR_WEB_CLIENT_ID_HERE",
                RequestEmail = true,
                RequestIdToken = true,
                UseGameSignIn = false
            };

            GoogleSignIn.DefaultInstance.EnableDebugLogging(true);
        }

        try
        {
            GoogleSignIn.DefaultInstance.SignIn().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    using IEnumerator<Exception> enumerator = task.Exception.InnerExceptions.GetEnumerator();
                    {
                        if (enumerator.MoveNext())
                        {
                            GoogleSignIn.SignInException error = (GoogleSignIn.SignInException)enumerator.Current;
                            onFailure(0, 0, new ErrorResponse(0, 0, $"{error.Status}\n{error.Message}").Serialize(), cbObject);
                        }
                        else
                        {
                            onFailure(0, 0, new ErrorResponse(0, 0, $"An error has occured. Please try again.\nError: {task.Exception}").Serialize(), cbObject);
                        }
                    }
                }
                else if (task.IsCanceled)
                {
                    onFailure(0, 0, new ErrorResponse(0, 0, "Log in was cancelled.").Serialize(), cbObject);
                }
                else
                {
                    BCManager.Wrapper.AuthenticateGoogleOpenId(task.Result.Email, task.Result.IdToken,
                                                               forceCreate, onSuccess, onFailure, cbObject);
                }
            });
        }
        catch
        {
            onFailure(0, 0, new ErrorResponse(0, 0, $"An error has occured. Please try again.").Serialize(), cbObject);
        }
#else
        Debug.LogError("AuthenticateGoogleOpenID is not available on this platform. Check your scripting defines. Returning with error...");
        onFailure(0, 0, new ErrorResponse(0, 0, "<b>AuthenticateGoogleOpenID</b> is not available on this platform.").Serialize(), cbObject);
#endif
    }

    #endregion
}
