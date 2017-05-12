using UnityEngine;

public class WrapperLoginSection
{
    public void Display()
    {
        GUILayout.BeginVertical();

        GUILayout.Label("Login As");

        GUILayout.BeginHorizontal();
        if (Util.Button("Anonymous"))
        {
            WrapperAuthenticateDialog.AuthenticateAsAnonymous();
        }

        if (Util.Button("Universal #1"))
        {
            WrapperAuthenticateDialog.AuthenticateAsUniversal_1();
        }
        if (Util.Button("Universal #2"))
        {
            WrapperAuthenticateDialog.AuthenticateAsUniversal_2();
        }

        if (Util.Button("Email"))
        {
            WrapperAuthenticateDialog.AuthenticateAsEmail();
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }
}