using UnityEngine;

public class LoginValuesSection
{
    public string m_password = "example_password";
    public string m_universal_1 = "example_username_1";
    public string m_universal_2 = "example_username_2";
    public string m_email = "example_email@example_email.com";

    public void Display()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Label("screenName: " + User.getScreeName());
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        GUILayout.Label("identities: " + User.getIdentities());
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();


        // Text values used for the authentication
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical();
        GUILayout.Label("Password");
        m_password = Util.TextField(m_password);
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        GUILayout.Label("Universal #1");
        m_universal_1 = Util.TextField(m_universal_1);
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        GUILayout.Label("Universal #2");
        m_universal_2 = Util.TextField(m_universal_2);
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        GUILayout.Label("Email");
        m_email = Util.TextField(m_email);
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }
}