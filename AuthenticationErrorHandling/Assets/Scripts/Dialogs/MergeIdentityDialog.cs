using System;
using BrainCloud;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

public class MergeIdentityDialog : Dialog
{
    public void OnDestroy()
    {
        Detach();
    }

    ResponseState m_state = ResponseState.InProgress;
    ExampleAccountType m_exampleAccountType = ExampleAccountType.Anonymous;

    public void OnGUI()
    {
        GUILayout.Window(0, SIZE.Dialog(), DoAuthWindow, "MergeIdentityDialog: " + m_exampleAccountType);
    }

    public static void MergIdentityRequestDialog(ExampleAccountType exampleAccountType)
    {
        GameObject dialogObject = new GameObject("Dialog");
        MergeIdentityDialog dialog = dialogObject.AddComponent<MergeIdentityDialog>();
        dialog.m_exampleAccountType = exampleAccountType;
        dialog.m_state = ResponseState.Setup;
    }

    public static void MergeIdentityUniversal_1()
    {
        GameObject dialogObject = new GameObject("Dialog");
        MergeIdentityDialog dialog = dialogObject.AddComponent<MergeIdentityDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Universal_1;

        BrainCloudClient.Get()
            .IdentityService.MergeUniversalIdentity(UtilValues.getUniversal_1(), UtilValues.getPassword(),
                dialog.OnSuccess_MergeIdentity, dialog.OnError_MergeIdentity);
    }

    public static void MergeIdentityUniversal_2()
    {
        GameObject dialogObject = new GameObject("Dialog");
        MergeIdentityDialog dialog = dialogObject.AddComponent<MergeIdentityDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Universal_2;

        BrainCloudClient.Get()
            .IdentityService.MergeUniversalIdentity(UtilValues.getUniversal_2(), UtilValues.getPassword(),
                dialog.OnSuccess_MergeIdentity, dialog.OnError_MergeIdentity);
    }

    public static void MergeIdentityEmail()
    {
        GameObject dialogObject = new GameObject("Dialog");
        MergeIdentityDialog dialog = dialogObject.AddComponent<MergeIdentityDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.Email;

        BrainCloudClient.Get()
            .IdentityService.MergeEmailIdentity(UtilValues.getEmail(), UtilValues.getPassword(),
                dialog.OnSuccess_MergeIdentity, dialog.OnError_MergeIdentity);
    }

    public static void MergeIdentityGooglePlay()
    {
#if UNITY_ANDROID
        GameObject dialogObject = new GameObject("Dialog");
        MergeIdentityDialog dialog = dialogObject.AddComponent<MergeIdentityDialog>();
        dialog.m_exampleAccountType = ExampleAccountType.GooglePlay;

        GoogleIdentity.RefreshGoogleIdentity(identity =>
        {
            BrainCloudWrapper.Client.IdentityService.MergeGoogleIdentity(identity.GoogleId, identity.GoogleToken,
                dialog.OnSuccess_MergeIdentity, dialog.OnError_MergeIdentity);
        });

#else
        ErrorDialog.DisplayErrorDialog("AuthenticateAsGooglePlay", "You can only use GooglePlay auth on Android Devices");
#endif
    }

    private void DoAuthWindow(int windowId)
    {
        GUILayout.BeginHorizontal();

        if (m_state == ResponseState.Setup)
        {
            GUILayout.Label(
                "This account with this identity already exists. Would you like the merge the two accounts?");

            if (Util.Button("Yes"))
            {
                Destroy(gameObject);

                switch (m_exampleAccountType)
                {
                    case ExampleAccountType.Universal_1:
                    {
                        MergeIdentityUniversal_1();
                        break;
                    }
                    case ExampleAccountType.Universal_2:
                    {
                        MergeIdentityUniversal_2();
                        break;
                    }
                    case ExampleAccountType.Email:
                    {
                        MergeIdentityEmail();
                        break;
                    }
                    case ExampleAccountType.GooglePlay:
                    {
                        MergeIdentityGooglePlay();
                        break;
                    }
                }

                Destroy(gameObject);
            }

            if (Util.Button("No"))
            {
                Destroy(gameObject);
            }
        }
        else if (m_state == ResponseState.InProgress)
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

        if (m_state != ResponseState.InProgress && m_state != ResponseState.Setup)
        {
            CloseButton();
        }

        GUILayout.EndHorizontal();

        DisplayResponse();
    }

    public void OnSuccess_MergeIdentity(string responseData, object cbObject)
    {
        ErrorHandlingApp.getInstance().m_user.OnIdentitiesChangedResponse(responseData);

        m_state = ResponseState.Success;
        m_response = responseData;

        Debug.Log("OnSuccess_MergeIdentity: " + responseData);
    }

    public void OnError_MergeIdentity(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        m_state = ResponseState.Error;
        m_response = reasonCode + ":" + statusMessage;

        Debug.LogError("OnError_MergeIdentity: " + statusMessage);

        if (ErrorHandling.SharedErrorHandling(statusCode, reasonCode, statusMessage, cbObject, gameObject))
        {
            return;
        }

        switch (reasonCode)
        {
            case ReasonCodes.DUPLICATE_IDENTITY_TYPE:
            {
                // Users cannot attach an identity of a type that is already on there account
                // Inform user to detach identities that are of the same type, before the merge
                Destroy(gameObject);
                ErrorDialog.DisplayErrorDialog(
                    string.Format(
                        "Accounts has conflicting identities, and cannot be merged. Remove identity types {0} matching the merging account, then merge.",
                        User.getIdentities()), m_response);

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