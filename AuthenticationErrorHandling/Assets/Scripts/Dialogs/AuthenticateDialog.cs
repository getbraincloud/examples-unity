using System;
using BrainCloud;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

public class AuthenticateDialog : Dialog
{
    public void OnDestroy()
    {
        Detach();
    }

    private ResponseState m_state = ResponseState.InProgress;
    private ExampleAccountType m_exampleAccountType = ExampleAccountType.Anonymous;

    public void OnGUI()
    {
        GUILayout.Window(0, SIZE.Dialog(), DoAuthWindow, "WrapperAuthenticateDialog: " + m_exampleAccountType);
    }

    public static void AuthenticateAsAnonymous()
    {
        GameObject dialogObject = new GameObject("Dialog");
        AuthenticateDialog dialog = dialogObject.AddComponent<AuthenticateDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Anonymous;

        App.Bc.Client
            .AuthenticationService.AuthenticateAnonymous(false, dialog.OnSuccess_Authenticate,
                dialog.OnError_AuthenticateAnonymous);
    }

    public static void AuthenticateAsUniversal_1(bool forceCreate = false)
    {
        GameObject dialogObject = new GameObject("Dialog");
        AuthenticateDialog dialog = dialogObject.AddComponent<AuthenticateDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Universal_1;


        App.Bc.Client
            .AuthenticationService.AuthenticateUniversal(UtilValues.getUniversal_1(), UtilValues.getPassword(),
                forceCreate, dialog.OnSuccess_Authenticate, dialog.OnError_Authenticate);
    }

    public static void AuthenticateAsUniversal_2(bool forceCreate = false)
    {
        GameObject dialogObject = new GameObject("Dialog");
        AuthenticateDialog dialog = dialogObject.AddComponent<AuthenticateDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Universal_2;

        App.Bc.Client
            .AuthenticationService.AuthenticateUniversal(UtilValues.getUniversal_2(), UtilValues.getPassword(),
                forceCreate, dialog.OnSuccess_Authenticate, dialog.OnError_Authenticate);
    }

    public static void AuthenticateAsEmail(bool forceCreate = false)
    {
        GameObject dialogObject = new GameObject("Dialog");
        AuthenticateDialog dialog = dialogObject.AddComponent<AuthenticateDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Email;

        App.Bc.Client.AuthenticationService
            .AuthenticateEmailPassword(UtilValues.getEmail(), UtilValues.getPassword(), forceCreate,
                dialog.OnSuccess_Authenticate, dialog.OnError_Authenticate);
    }

    public static void AuthenticateAsGooglePlay(bool forceCreate = false)
    {
#if UNITY_ANDROID
        GameObject dialogObject = new GameObject("Dialog");
        AuthenticateDialog dialog = dialogObject.AddComponent<AuthenticateDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.GooglePlay;

        GoogleIdentity.RefreshGoogleIdentity(identity =>
        {
            App.Bc.AuthenticateGoogle(identity.GoogleId, identity.GoogleToken, forceCreate,
                dialog.OnSuccess_Authenticate, dialog.OnError_Authenticate);
        });

#else
        ErrorDialog.DisplayErrorDialog("AuthenticateAsGooglePlay", "You can only use GooglePlay auth on Android Devices");
#endif
    }

    private void DoAuthWindow(int windowId)
    {
        GUILayout.BeginHorizontal();

        if (m_state == ResponseState.InProgress)
        {
            GUILayout.Label("In Progress");
        }
        else if (m_state == ResponseState.Success)
        {
            GUILayout.Label("Success");
        }
        else if (m_state == ResponseState.Error)
        {
            GUILayout.Label("Error");
        }

        if (m_state != ResponseState.InProgress)
        {
            CloseButton();
        }

        GUILayout.EndHorizontal();

        DisplayResponse();
    }

    public void OnSuccess_Authenticate(string responseData, object cbObject)
    {
        ErrorHandlingApp.getInstance().m_user.OnLoginResponse(responseData);

        m_state = ResponseState.Success;
        m_response = responseData;
        Debug.Log("OnSuccess_Authenticate: " + responseData);
    }

    public void OnError_AuthenticateAnonymous(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        m_state = ResponseState.Error;
        m_response = reasonCode + ":" + statusMessage;
        Debug.LogError("OnError_Authenticate: " + statusMessage);

        if (ErrorHandling.SharedErrorHandling(statusCode, reasonCode, statusMessage, cbObject, gameObject))
        {
            return;
        }

        switch (reasonCode)
        {
            case ReasonCodes.MISSING_IDENTITY_ERROR:
            {
                // Anonymous id is invalid
                // Clear the profile id, generate a new Anonymous id, and re-authenticate
                App.Bc.Client.AuthenticationService.ClearSavedProfileID();
                App.Bc.Client.AuthenticationService.AnonymousId =
                    App.Bc.Client.AuthenticationService.GenerateAnonymousId();
                App.Bc.Client
                    .AuthenticationService.AuthenticateAnonymous(true, OnSuccess_Authenticate,
                        OnError_AuthenticateAnonymous);
                break;
            }
            case ReasonCodes.MISSING_PROFILE_ERROR:
            {
                // Anonymous id doesn't exist in database
                // Must set forceCreate to true
                App.Bc.Client
                    .AuthenticationService.AuthenticateAnonymous(true, OnSuccess_Authenticate,
                        OnError_AuthenticateAnonymous);
                break;
            }

            case ReasonCodes.SECURITY_ERROR:
            {
                // Credentials are invalid
                // Generate a new Anonymous id, and re-authenticate
                App.Bc.Client.AuthenticationService.AnonymousId =
                    App.Bc.Client.AuthenticationService.GenerateAnonymousId();
                App.Bc.Client
                    .AuthenticationService.AuthenticateAnonymous(true, OnSuccess_Authenticate,
                        OnError_AuthenticateAnonymous);
                break;
            }

            case ReasonCodes.MISSING_REQUIRED_PARAMETER:
            {
                // Anonymous id cannot be blank
                // Generate a new Anonymous id, and re-authenticate
                App.Bc.Client.AuthenticationService.AnonymousId =
                    App.Bc.Client.AuthenticationService.GenerateAnonymousId();
                App.Bc.Client
                    .AuthenticationService.AuthenticateAnonymous(true, OnSuccess_Authenticate,
                        OnError_AuthenticateAnonymous);
                break;
            }

            default:
            {
                // log the reasonCode to your own internal error checking
                ErrorHandling.UncaughtError(statusCode, reasonCode, statusMessage, cbObject, gameObject);

                break;
            }
        }
    }

    public void OnError_Authenticate(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        m_state = ResponseState.Error;
        m_response = reasonCode + ":" + statusMessage;
        Debug.LogError("OnError_Authenticate: " + statusMessage);

        if (ErrorHandling.SharedErrorHandling(statusCode, reasonCode, statusMessage, cbObject, gameObject))
        {
            return;
        }

        switch (reasonCode)
        {
            case ReasonCodes.MISSING_IDENTITY_ERROR:
            {
                // User's identity doesn't match one existing on brainCloud
                // Reset profile id and re-authenticate
                App.Bc.Client.AuthenticationService.ClearSavedProfileID();
                ReAuthenticate(true);

                // @see WrapperAuthenticateDialog for an example that uses
                // permission dialog before creating the new profile

                break;
            }
            case ReasonCodes.SWITCHING_PROFILES:
            {
                // User profile id doesn't match the identity they are attempting to authenticate
                // Reset profile id and re-authenticate
                App.Bc.Client.AuthenticationService.ClearSavedProfileID();
                ReAuthenticate();

                // @see WrapperAuthenticateDialog for an example that uses
                // permission dialog before swapping
                break;
            }
            case ReasonCodes.TOKEN_DOES_NOT_MATCH_USER:
            {
                // User is receiving  an error that they're username or password is wrong.
                // decide how this will be handled, such as prompting the user to re-enter 
                // there login details
                Destroy(gameObject);
                ErrorDialog.DisplayErrorDialog(
                    "Incorrect username or password. Please check your information and try again.", m_response);

                break;
            }
            case ReasonCodes.MISSING_PROFILE_ERROR:
            {
                // User is receiving an error that they're trying to authenticate an account that doesn't exist.
                // decide how this will be handled, such as creating the account by setting the forceCreate flag to true
                ReAuthenticate(true);

                break;
            }
            default:
            {
                // log the reasonCode to your own internal error checking
                ErrorHandling.UncaughtError(statusCode, reasonCode, statusMessage, cbObject, gameObject);

                break;
            }
        }
    }

    private void ReAuthenticate(bool forceCreate = false)
    {
        Destroy(gameObject);

        switch (m_exampleAccountType)
        {
            case ExampleAccountType.Universal_1:
            {
                AuthenticateAsUniversal_1(forceCreate);

                break;
            }

            case ExampleAccountType.Universal_2:
            {
                AuthenticateAsUniversal_2(forceCreate);

                break;
            }

            case ExampleAccountType.Email:
            {
                AuthenticateAsEmail(forceCreate);

                break;
            }

            case ExampleAccountType.GooglePlay:
            {
                AuthenticateAsGooglePlay(forceCreate);

                break;
            }
        }
    }
}