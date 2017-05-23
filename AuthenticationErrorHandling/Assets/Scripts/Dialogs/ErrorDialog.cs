using UnityEngine;

public class ErrorDialog : Dialog
{
    string m_message = "Error";

    public void OnDestroy()
    {
        Detach();
    }

    public void OnGUI()
    {
        GUILayout.Window(0, SIZE.Dialog(), DoAuthWindow, "ErrorDialog");
    }


    public static void DisplayErrorDialog(string message, string response)
    {
        GameObject dialogObject = new GameObject("Dialog");
        ErrorDialog dialog = dialogObject.AddComponent<ErrorDialog>();
        dialog.m_message = message;
        dialog.m_response = response;
    }

    private void DoAuthWindow(int windowId)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(m_message);
        CloseButton();
        GUILayout.EndHorizontal();

        DisplayResponse();
    }
}