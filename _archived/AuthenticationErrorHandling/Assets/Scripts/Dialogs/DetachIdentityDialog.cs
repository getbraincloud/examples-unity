using BrainCloud;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

public class DetachIdentityDialog : Dialog
{
    public void OnDestroy()
    {
        Detach();
    }

    ResponseState m_state = ResponseState.InProgress;
    ExampleAccountType m_exampleAccountType = ExampleAccountType.Anonymous;

    public void OnGUI()
    {
        GUILayout.Window(0, SIZE.Dialog(), DoAuthWindow, "DetachIdentityDialog: " + m_exampleAccountType);
    }

    public static void DetachIdentityUniversal_1(bool contiuneAsAnonymous = false)
    {
        GameObject dialogObject = new GameObject("Dialog");
        DetachIdentityDialog dialog = dialogObject.AddComponent<DetachIdentityDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Universal_1;

        App.Bc.Client
            .IdentityService.DetachUniversalIdentity(UtilValues.getUniversal_1(), contiuneAsAnonymous,
                dialog.OnSuccess_DetachIdentity, dialog.OnError_DetachIdentity);
    }

    public static void DetachIdentityUniversal_2(bool contiuneAsAnonymous = false)
    {
        GameObject dialogObject = new GameObject("Dialog");
        DetachIdentityDialog dialog = dialogObject.AddComponent<DetachIdentityDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Universal_2;

        App.Bc.Client
            .IdentityService.DetachUniversalIdentity(UtilValues.getUniversal_2(), contiuneAsAnonymous,
                dialog.OnSuccess_DetachIdentity, dialog.OnError_DetachIdentity);
    }

    public static void DetachIdentityEmail(bool contiuneAsAnonymous = false)
    {
        GameObject dialogObject = new GameObject("Dialog");
        DetachIdentityDialog dialog = dialogObject.AddComponent<DetachIdentityDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Email;

        App.Bc.Client
            .IdentityService.DetachEmailIdentity(UtilValues.getEmail(), contiuneAsAnonymous,
                dialog.OnSuccess_DetachIdentity, dialog.OnError_DetachIdentity);
    }
    
    public static void DetachIdentityGooglePlay(bool contiuneAsAnonymous = false)
    {
#if UNITY_ANDROID
        GameObject dialogObject = new GameObject("Dialog");
        DetachIdentityDialog dialog = dialogObject.AddComponent<DetachIdentityDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.GooglePlay;

        GoogleIdentity.RefreshGoogleIdentity(identity =>
        {
            App.Bc.Client.IdentityService.DetachGoogleIdentity(identity.GoogleId, contiuneAsAnonymous,
                dialog.OnSuccess_DetachIdentity, dialog.OnError_DetachIdentity);
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

    public void OnSuccess_DetachIdentity(string responseData, object cbObject)
    {
        ErrorHandlingApp.getInstance().m_user.OnIdentitiesChangedResponse(responseData);

        m_state = ResponseState.Success;
        m_response = responseData;

        Debug.Log("OnSuccess_DetachIdentity: " + responseData);
    }

    public void OnError_DetachIdentity(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        m_state = ResponseState.Error;
        m_response = reasonCode + ":" + statusMessage;

        Debug.LogError("OnError_DetachIdentity: " + statusMessage);

        if (ErrorHandling.SharedErrorHandling(statusCode, reasonCode, statusMessage, cbObject, gameObject))
        {
            return;
        }

        switch (reasonCode)
        {
            case ReasonCodes.DOWNGRADING_TO_ANONYMOUS_ERROR:
            {
                // Display to the user that removing this identity would make there account 
                // anonymous. Ask them if they are sure they want to perform this action
                Destroy(gameObject);
                AnonymousDowngradeDialog.CreateDialog(m_exampleAccountType);

                break;
            }

            case ReasonCodes.MISSING_IDENTITY_ERROR:
            {
                Destroy(gameObject);
                ErrorDialog.DisplayErrorDialog(
                    string.Format("You can't detach an {0} identity when you don't have one.",
                        UtilExampleAccountType.getTypeName(m_exampleAccountType)), reasonCode + ":" + statusMessage);

                break;
            }

            case ReasonCodes.SECURITY_ERROR:
            {
                Destroy(gameObject);
                ErrorDialog.DisplayErrorDialog(
                    string.Format("You can't detach an {0} identity that doesn't belong to you.",
                        UtilExampleAccountType.getTypeName(m_exampleAccountType)), reasonCode + ":" + statusMessage);

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
}