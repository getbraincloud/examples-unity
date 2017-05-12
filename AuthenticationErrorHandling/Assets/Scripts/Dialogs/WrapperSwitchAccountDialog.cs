using UnityEngine;

public class WrapperSwitchAccountDialog : Dialog
{
    public void OnDestroy()
    {
        Detach();
    }

    public ExampleAccountType m_exampleAccountType;

    public void OnGUI()
    {
        GUILayout.Window(0, SIZE.Dialog(), DoAuthWindow, "WrapperSwitchAccountDialog: " + m_exampleAccountType);
    }

    public static void CreateDialog(ExampleAccountType exampleAccountType)
    {
        GameObject dialogObject = new GameObject("Dialog");
        WrapperSwitchAccountDialog dialog = dialogObject.AddComponent<WrapperSwitchAccountDialog>();
        dialog.m_exampleAccountType = exampleAccountType;
    }

    private void DoAuthWindow(int windowId)
    {
        GUILayout.BeginVertical();

        GUILayout.Label(string.Format("Your already logged in as {0}. Do you want to switch accounts?",
            ErrorHandlingApp.getInstance().m_user.m_userData.m_screenName));

        GUILayout.BeginHorizontal();


        if (Util.Button("Yes"))
        {
            BrainCloudWrapper.GetInstance().ResetStoredAnonymousId();
            BrainCloudWrapper.GetInstance().ResetStoredAuthenticationType();
            BrainCloudWrapper.GetInstance().ResetStoredProfileId();

            switch (m_exampleAccountType)
            {
                case ExampleAccountType.Universal_1:
                {
                    WrapperAuthenticateDialog.AuthenticateAsUniversal_1();

                    break;
                }
                case ExampleAccountType.Universal_2:
                {
                    WrapperAuthenticateDialog.AuthenticateAsUniversal_2();

                    break;
                }
                case ExampleAccountType.Email:
                {
                    WrapperAuthenticateDialog.AuthenticateAsEmail();

                    break;
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