using BrainCloud;
using UnityEngine;

public class ChangeScreenNameDialog : Dialog
{
    public void OnDestroy()
    {
        Detach();
    }

    ResponseState m_state = ResponseState.InProgress;

    public void OnGUI()
    {
        GUILayout.Window(0, SIZE.Dialog(), DoAuthWindow, "ChangeScreenName");
    }

    public static void ChangeScreenName(string newName)
    {
        GameObject dialogObject = new GameObject("Dialog");
        ChangeScreenNameDialog dialog = dialogObject.AddComponent<ChangeScreenNameDialog>();

        BrainCloudClient.Get()
            .PlayerStateService.UpdatePlayerName(newName, dialog.OnSuccess_ChangeScreenName,
                dialog.OnError_ChangeScreenName);
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

    public void OnSuccess_ChangeScreenName(string responseData, object cbObject)
    {
        ErrorHandlingApp.getInstance().m_user.OnNameChangedResponse(responseData);

        m_state = ResponseState.Success;
        m_response = responseData;
        Debug.Log("OnSuccess_Authenticate: " + responseData);
    }

    public void OnError_ChangeScreenName(int statusCode, int reasonCode, string statusMessage, object cbObject)
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
            default:
            {
                // log the reasonCode to your own internal error checking
                ErrorHandling.UncaughtError(statusCode, reasonCode, statusMessage, cbObject, gameObject);

                break;
            }
        }
    }
}