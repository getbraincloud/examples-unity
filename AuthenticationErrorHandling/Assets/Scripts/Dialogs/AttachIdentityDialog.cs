using BrainCloud;
using UnityEngine;

public class AttachIdentityDialog : Dialog
{
    public void OnDestroy()
    {
        Detach();
    }

    ResponseState m_state = ResponseState.InProgress;
    ExampleAccountType m_exampleAccountType = ExampleAccountType.Anonymous;

    public void OnGUI()
    {
        GUILayout.Window(0, SIZE.Dialog(), DoAuthWindow, "AttachIdentityDialog: " + m_exampleAccountType);
    }

    public static void AttachIdentityUniversal_1()
    {
        GameObject dialogObject = new GameObject("Dialog");
        AttachIdentityDialog dialog = dialogObject.AddComponent<AttachIdentityDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Universal_1;

        BrainCloudClient.Get()
            .IdentityService.AttachUniversalIdentity(UtilValues.getUniversal_1(), UtilValues.getPassword(),
                dialog.OnSuccess_AttachIndentity, dialog.OnError_AttachIdentity);
    }

    public static void AttachIdentityUniversal_2()
    {
        GameObject dialogObject = new GameObject("Dialog");
        AttachIdentityDialog dialog = dialogObject.AddComponent<AttachIdentityDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Universal_2;

        BrainCloudClient.Get()
            .IdentityService.AttachUniversalIdentity(UtilValues.getUniversal_2(), UtilValues.getPassword(),
                dialog.OnSuccess_AttachIndentity, dialog.OnError_AttachIdentity);
    }

    public static void AttachIdentityEmail()
    {
        GameObject dialogObject = new GameObject("Dialog");
        AttachIdentityDialog dialog = dialogObject.AddComponent<AttachIdentityDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Email;

        BrainCloudClient.Get()
            .IdentityService.AttachEmailIdentity(UtilValues.getEmail(), UtilValues.getPassword(),
                dialog.OnSuccess_AttachIndentity, dialog.OnError_AttachIdentity);
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

    public void OnSuccess_AttachIndentity(string responseData, object cbObject)
    {
        ErrorHandlingApp.getInstance().m_user.OnIdentitiesChangedResponse(responseData);

        m_state = ResponseState.Success;
        m_response = responseData;

        Debug.Log("OnSuccess_AttachIndentity: " + responseData);
    }

    public void OnError_AttachIdentity(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        m_state = ResponseState.Error;
        m_response = reasonCode + ":" + statusMessage;

        Debug.LogError("OnError_AttachIdentity: " + statusMessage);

        if (ErrorHandling.SharedErrorHandling(statusCode, reasonCode, statusMessage, cbObject, gameObject))
        {
            return;
        }

        switch (reasonCode)
        {
            case ReasonCodes.DUPLICATE_IDENTITY_TYPE:
            {
                //Users cannot attach an identity of a type that is already on there account
                // decide how this will be handled, such as prompting the user remove the current
                // identity before attaching one of the same type
                Destroy(gameObject);
                ErrorDialog.DisplayErrorDialog(
                    string.Format("Account already has an identity of this {0} type, please detach first.",
                        UtilExampleAccountType.getTypeName(m_exampleAccountType)), m_response);

                break;
            }

            case ReasonCodes.MERGE_PROFILES:
            case ReasonCodes.SWITCHING_PROFILES:
            {
                //User cannot attach an identity that is already in use by another user
                // decide how this will be handled, such as prompting the user to merge the
                //  two user accounts
                Destroy(gameObject);
                MergeIdentityDialog.MergIdentityRequestDialog(m_exampleAccountType);

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