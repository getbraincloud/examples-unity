using System;
using UnityEngine;

public class WrapperCreateAccountDialog : Dialog
{
    public void OnDestroy()
    {
        Detach();
    }

    public ExampleAccountType m_exampleAccountType;

    public void OnGUI()
    {
        GUILayout.Window(0, SIZE.Dialog(), DoAuthWindow, "WrapperCreateAccountDialog: " + m_exampleAccountType);
    }

    public static void CreateDialog(ExampleAccountType exampleAccountType)
    {
        GameObject dialogObject = new GameObject("Dialog");
        WrapperCreateAccountDialog dialog = dialogObject.AddComponent<WrapperCreateAccountDialog>();
        dialog.m_exampleAccountType = exampleAccountType;
    }

    private void DoAuthWindow(int windowId)
    {
        GUILayout.BeginVertical();

        GUILayout.Label(
            string.Format(
                "The account your trying to log in to, {0}, doesn't exist. Do you wish to create the account?",
                UtilValues.getAccountLogin(m_exampleAccountType)));

        GUILayout.BeginHorizontal();


        if (Util.Button("Yes"))
        {
            switch (m_exampleAccountType)
            {
                case ExampleAccountType.Universal_1:
                {
                    WrapperAuthenticateDialog.AuthenticateAsUniversal_1(true);

                    break;
                }
                case ExampleAccountType.Universal_2:
                {
                    WrapperAuthenticateDialog.AuthenticateAsUniversal_2(true);

                    break;
                }
                case ExampleAccountType.Email:
                {
                    WrapperAuthenticateDialog.AuthenticateAsEmail(true);

                    break;
                }
                default:
                {
                    throw new Exception();
                }
            }

            Destroy(gameObject);
            return;
        }

        if (Util.Button("No"))
        {
            Destroy(gameObject);
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }
}