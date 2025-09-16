using BrainCloud;
using BrainCloud.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if APPLE_SDK
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Native;
using AppleAuth.Interfaces;
#endif

#if GAMECENTER_SDK
using Apple.GameKit;
#endif

#if FACEBOOK_SDK
using Facebook.Unity;
#endif

#if GOOGLE_SDK
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

#if GOOGLE_OPENID_SDK
using Google;
#endif

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
    /// Authenticate the user through Steam
    /// </summary>
    public static void AuthenticateSteam(string userid, string sessionTicket, bool forceCreate, SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.Wrapper.AuthenticateSteam(userid, sessionTicket, forceCreate, onSuccess, onFailure, cbObject);

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
    public static void HandleUserLogout(bool forgetUser, SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null) =>
        BCManager.Wrapper.Logout(forgetUser, onSuccess, onFailure, cbObject);

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

#if APPLE_SDK
    // For the sake of handling Apple authentication via this example app, we will keep AppleAuthManager here
    private static IAppleAuthManager appleAuthManager;

    // It would be best practice to include this loop in your own custom manager
    private static IEnumerator HandleAppleAuthManagerUpdate()
    {
        while (appleAuthManager != null)
        {
            appleAuthManager.Update();
            yield return null;
        }
    }

    /// <summary>
    /// Authenticate the user using their Apple account.
    /// </summary>
    /// Sign in with Apple Unity Plugin: https://github.com/lupidan/apple-signin-unity
    public static void AuthenticateApple(bool forceCreate = true, SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null)
    {
#if UNITY_STANDALONE_OSX || UNITY_IOS
        // For the sake of handling Apple authentication via this example app, we will create our AppleAuthManager here.
        // If you intend on including this script in your own app it is highly recommended that you create your own
        // custom manager to handle all the features supplied by AppleAuthManager.
        if (AppleAuthManager.IsCurrentPlatformSupported && appleAuthManager == null)
        {
            var deserializer = new PayloadDeserializer();
            appleAuthManager = new AppleAuthManager(deserializer);

            BCManager.Wrapper.StartCoroutine(HandleAppleAuthManagerUpdate());
        }
        else
        {
            onFailure(0, 0, ErrorResponse.CreateGeneric("<b>AuthenticateApple</b> is not available on this platform."), cbObject);
            return;
        }

        // These are only an example of a nonce and state; please provide your own method of generating these
        long ticks = DateTime.UtcNow.Ticks;
        string nonce = ticks.ToString();
        string state = (~ticks).ToString();

        // You can request the user's email and full name from here
        // Note: You will only receive the user's email and name the first time the user logs in.
        // Future logins will only return null unless the user revokes access to this app.
        // If you require those to be stored in your app, ensure that you store them when the user first logs in.
        AppleAuthLoginArgs loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeFullName,
                                                              nonce, state);

        appleAuthManager.LoginWithAppleId(loginArgs,
            credential =>
            {
                if (credential is IAppleIDCredential appleIdCredential && state == appleIdCredential.State)
                {
                    string identityToken = Encoding.UTF8.GetString(appleIdCredential.IdentityToken, 0,
                                                                   appleIdCredential.IdentityToken.Length);

                    BCManager.Wrapper.AuthenticateApple(appleIdCredential.User, identityToken,
                                                        forceCreate, onSuccess, onFailure, cbObject);
                }
                else
                {
                    onFailure(0, 0, ErrorResponse.CreateGeneric("An error has occured. Please try again."), cbObject);
                }
            },
            error =>
            {
                string errorMessage = error != null ? $"{error.LocalizedDescription}" : "An error has occured. Please try again.";
                onFailure(0, 0, ErrorResponse.CreateGeneric(errorMessage), cbObject);
            });
#else
        Debug.LogError("AuthenticateApple is not available on this platform. Check your scripting defines. Returning with error...");
        onFailure(0, 0, ErrorResponse.CreateGeneric("<b>AuthenticateApple</b> is not available on this platform."), cbObject);
#endif
    }
#endif

#if GAMECENTER_SDK
    /// <summary>
    /// Authenticate the user using their Game Center account.
    /// </summary>
    /// Apple Unity Plug-Ins (Apple.Core & Apple.GameKit Required): https://github.com/apple/unityplugins
    public static void AuthenticateGameCenter(bool forceCreate = true, SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null)
    {
#if UNITY_STANDALONE_OSX || UNITY_IOS
        IEnumerator WaitForGKLocalPlayerAuthenticated()
        {
            DateTime stop = DateTime.UtcNow.AddSeconds(60.0f);
            while (DateTime.UtcNow < stop &&
                   (GKLocalPlayer.Local == null || !GKLocalPlayer.Local.IsAuthenticated || GKLocalPlayer.Local.GamePlayerId.IsEmpty()))
            {
                yield return null;
            }

            if (GKLocalPlayer.Local != null && GKLocalPlayer.Local.IsAuthenticated && !GKLocalPlayer.Local.GamePlayerId.IsEmpty())
            {
                Debug.Log($"GamePlayerId: {GKLocalPlayer.Local.GamePlayerId}");
                BCManager.Wrapper.AuthenticateGameCenter(GKLocalPlayer.Local.GamePlayerId,
                                                         forceCreate, onSuccess, onFailure, cbObject);
            }
            else
            {
                onFailure(0, 0, ErrorResponse.CreateGeneric("An error has occured. Please try again."), cbObject);
            }
        }

        if (GKLocalPlayer.Local == null || !GKLocalPlayer.Local.IsAuthenticated)
        {
            BCManager.Wrapper.StartCoroutine(WaitForGKLocalPlayerAuthenticated());
            GKLocalPlayer.Authenticate();
        }
        else if (!GKLocalPlayer.Local.GamePlayerId.IsEmpty())
        {
            BCManager.Wrapper.AuthenticateGameCenter(GKLocalPlayer.Local.GamePlayerId,
                                                     forceCreate, onSuccess, onFailure, cbObject);
        }
        else
        {
            onFailure(0, 0, ErrorResponse.CreateGeneric("An error has occured. Please try again."), cbObject);
        }
#else
        Debug.LogError("AuthenticateGameCenter is not available on this platform. Check your scripting defines. Returning with error...");
        onFailure(0, 0, ErrorResponse.CreateGeneric("<b>AuthenticateGameCenter</b> is not available on this platform."), cbObject);
#endif
    }
#endif

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
                onFailure(0, 0, ErrorResponse.CreateGeneric("Log in was cancelled."), cbObject);
            }
            else // Error
            {
                string errorMessage = result.Error ?? "An error has occured. Please try again.";
                onFailure(0, 0, ErrorResponse.CreateGeneric(errorMessage), cbObject);
            }
        }

#if UNITY_STANDALONE || UNITY_WEBGL || UNITY_ANDROID
        FB.LogInWithReadPermissions(perms, onFBResult); /// <seealso cref="FB.LogInWithPublishPermissions(IEnumerable{string}, FacebookDelegate{ILoginResult})"/>
#elif UNITY_IOS
        FB.Mobile.LoginWithTrackingPreference(LoginTracking.ENABLED, perms, null, onFBResult);
#else
        Debug.LogError("AuthenticateFacebook is not available on this platform. Check your scripting defines. Returning with error...");
        onFailure(0, 0, ErrorResponse.CreateGeneric("<b>AuthenticateFacebook</b> is not available on this platform."), cbObject);
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

        string nonce = DateTime.UtcNow.Ticks.ToString(); // This is only an example of a nonce; please provide your own method of generating a nonce

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
                onFailure(0, 0, ErrorResponse.CreateGeneric("Log in was cancelled."), cbObject);
            }
            else // Error
            {
                string errorMessage = result.Error ?? "An error has occured. Please try again.";
                onFailure(0, 0, ErrorResponse.CreateGeneric(errorMessage), cbObject);
            }
        });
#else
        Debug.LogError("AuthenticateFacebookLimited is only available on iOS. Returning with error...");
        onFailure(0, 0, ErrorResponse.CreateGeneric("<b>AuthenticateFacebookLimited</b> is only available on iOS. Please use <b>AuthenticateFacebook</b> instead."), cbObject);
#endif
    }
#endif

#if GOOGLE_SDK
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
                onFailure(0, 0, ErrorResponse.CreateGeneric("Log in was cancelled."), cbObject);
            }
            else // Error
            {
                onFailure(0, 0, ErrorResponse.CreateGeneric("An error has occured. Please try again."), cbObject);
            }
        });
#else
        Debug.LogError("AuthenticateGoogle is not available on this platform. Check your scripting defines. Returning with error...");
        onFailure(0, 0, ErrorResponse.CreateGeneric("<b>AuthenticateGoogle</b> is not available on this platform."), cbObject);
#endif
    }
#endif

#if GOOGLE_OPENID_SDK
    /// <summary>
    /// Authenticate the user using their Google account via Google Sign-In.
    /// </summary>
    /// Google Sign-In Unity Plugin: https://developers.google.com/identity/sign-in/
    public static void AuthenticateGoogleOpenId(bool forceCreate = true, SuccessCallback onSuccess = null, FailureCallback onFailure = null, object cbObject = null)
    {
#if DEVELOPMENT_BUILD && UNITY_IOS
        Debug.LogWarning("Development Builds for iOS causes issues with the GoogleSignIn SDK.\n"+
                         "You may need to disable Development Builds for AuthenticateGoogleOpenId to work.");
#endif
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
#if UNITY_ANDROID
            GoogleSignIn.DefaultInstance.EnableDebugLogging(true);
#endif
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
                            onFailure(0, 0, ErrorResponse.CreateGeneric($"{error.Status}\n{error.Message}"), cbObject);
                        }
                        else
                        {
                            onFailure(0, 0, ErrorResponse.CreateGeneric($"An error has occured. Please try again.\nError: {task.Exception}"), cbObject);
                        }
                    }
                }
                else if (task.IsCanceled)
                {
                    onFailure(0, 0, ErrorResponse.CreateGeneric("Log in was cancelled."), cbObject);
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
            onFailure(0, 0, ErrorResponse.CreateGeneric($"An error has occured. Please try again."), cbObject);
        }
#else
        Debug.LogError("AuthenticateGoogleOpenID is not available on this platform. Check your scripting defines. Returning with error...");
        onFailure(0, 0, ErrorResponse.CreateGeneric("<b>AuthenticateGoogleOpenID</b> is not available on this platform."), cbObject);
#endif
    }
#endif

    #endregion
}
