using UnityEngine;

public class LoginSection
{
    public void Display()
    {
        GUILayout.BeginVertical();

        GUILayout.Label("Login As");

        GUILayout.BeginHorizontal();


        if (Util.Button("Anonymous"))
        {
            AuthenticateDialog.AuthenticateAsAnonymous();
        }
        if (Util.Button("Universal #1"))
        {
            AuthenticateDialog.AuthenticateAsUniversal_1();
        }
        if (Util.Button("Universal #2"))
        {
            AuthenticateDialog.AuthenticateAsUniversal_2();
        }
        if (Util.Button("Email"))
        {
            AuthenticateDialog.AuthenticateAsEmail();
        }
        if (Util.Button("GooglePlay (Android Only)"))
        {
            AuthenticateDialog.AuthenticateAsGooglePlay();
        }


        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }
}