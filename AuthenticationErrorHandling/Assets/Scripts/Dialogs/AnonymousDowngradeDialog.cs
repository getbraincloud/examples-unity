using UnityEngine;

public class AnonymousDowngradeDialog : Dialog
{
    public void OnDestroy()
    {
        Detach();
    }

    public ExampleAccountType m_exampleAccountType;

    public void OnGUI()
    {
        GUILayout.Window(0, SIZE.Dialog(), DoAuthWindow, "AnonymousDowngradeDialog: " + m_exampleAccountType);
    }

    public static void CreateDialog(ExampleAccountType exampleAccountType)
    {
        GameObject dialogObject = new GameObject("Dialog");
        AnonymousDowngradeDialog dialog = dialogObject.AddComponent<AnonymousDowngradeDialog>();
        dialog.m_exampleAccountType = exampleAccountType;
    }

    private void DoAuthWindow(int windowId)
    {
        GUILayout.BeginVertical();

        GUILayout.Label(string.Format("Removing this the identity {0} will make you anonymous. Are you sure?",
            ErrorHandlingApp.getInstance().m_user.m_userData.m_screenName));

        GUILayout.BeginHorizontal();

        if (Util.Button("Yes"))
        {
            switch (m_exampleAccountType)
            {
                case ExampleAccountType.Universal_1:
                {
                    DetachIdentityDialog.DetachIdentityUniversal_1(true);

                    break;
                }

                case ExampleAccountType.Universal_2:
                {
                    DetachIdentityDialog.DetachIdentityUniversal_2(true);

                    break;
                }

                case ExampleAccountType.Email:
                {
                    DetachIdentityDialog.DetachIdentityEmail(true);

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