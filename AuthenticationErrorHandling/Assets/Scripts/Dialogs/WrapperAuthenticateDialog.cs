using BrainCloud;
using UnityEngine;

public class WrapperAuthenticateDialog : Dialog
{
    public void OnDestroy()
    {
        Detach();
    }

    ResponseState m_state = ResponseState.InProgress;
    ExampleAccountType m_exampleAccountType = ExampleAccountType.Anonymous;


    public void OnGUI()
    {
        GUILayout.Window(0, SIZE.Dialog(), DoAuthWindow, "WrapperAuthenticateDialog: " + m_exampleAccountType);
    }

    public static void AuthenticateAsAnonymous()
    {
        GameObject dialogObject = new GameObject("Dialog");
        WrapperAuthenticateDialog dialog = dialogObject.AddComponent<WrapperAuthenticateDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Anonymous;

        BrainCloudWrapper.GetInstance()
            .AuthenticateAnonymous(dialog.OnSuccess_Authenticate, dialog.OnError_AuthenticateAnonymous);
    }


    public static void AuthenticateAsUniversal_1(bool forceCreate = false)
    {
        GameObject dialogObject = new GameObject("Dialog");
        WrapperAuthenticateDialog dialog = dialogObject.AddComponent<WrapperAuthenticateDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Universal_1;


        BrainCloudWrapper.GetInstance()
            .AuthenticateUniversal(UtilValues.getUniversal_1(), UtilValues.getPassword(), forceCreate,
                dialog.OnSuccess_Authenticate, dialog.OnError_Authenticate);
    }

    public static void AuthenticateAsUniversal_2(bool forceCreate = false)
    {
        GameObject dialogObject = new GameObject("Dialog");
        WrapperAuthenticateDialog dialog = dialogObject.AddComponent<WrapperAuthenticateDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Universal_2;

        BrainCloudWrapper.GetInstance()
            .AuthenticateUniversal(UtilValues.getUniversal_2(), UtilValues.getPassword(), forceCreate,
                dialog.OnSuccess_Authenticate, dialog.OnError_Authenticate);
    }

    public static void AuthenticateAsEmail(bool forceCreate = false)
    {
        GameObject dialogObject = new GameObject("Dialog");
        WrapperAuthenticateDialog dialog = dialogObject.AddComponent<WrapperAuthenticateDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Email;

        BrainCloudWrapper.GetInstance()
            .AuthenticateEmailPassword(UtilValues.getEmail(), UtilValues.getPassword(), forceCreate,
                dialog.OnSuccess_Authenticate, dialog.OnError_Authenticate);
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

        BrainCloudWrapper.GetInstance()
            .SetStoredAuthenticationType(UtilExampleAccountType.getTypeName(m_exampleAccountType));
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
                // User's identity doesn't match one existing on brainCloud
                // Reset the profile id, and re-authenticate
                BrainCloudWrapper.GetInstance().ResetStoredProfileId();
                Destroy(gameObject);
                AuthenticateAsAnonymous();

                break;
            }

            case ReasonCodes.SWITCHING_PROFILES:
            {
                // User profile id doesn't match the identity they are attempting to authenticate
                // Reset the profile id, and re-authenticate
                BrainCloudWrapper.GetInstance().ResetStoredProfileId();
                Destroy(gameObject);
                AuthenticateAsAnonymous();

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
                // decide how this will be handled, such as with a account creation prompt, informing
                // them that they'll be creating a new profile
                Destroy(gameObject);
                WrapperCreateAccountDialog.CreateDialog(m_exampleAccountType);

                break;
            }
            case ReasonCodes.SWITCHING_PROFILES:
            {
                // User profile id doesn't match the identity they are attempting to authenticate
                // decide how this will be handled, such as with a switch account prompt, informing
                // them that they'll be switching profiles
                Destroy(gameObject);
                WrapperSwitchAccountDialog.CreateDialog(m_exampleAccountType);

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
            case ExampleAccountType.Anonymous:
            {
                AuthenticateAsAnonymous();

                break;
            }

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
        }
    }
}