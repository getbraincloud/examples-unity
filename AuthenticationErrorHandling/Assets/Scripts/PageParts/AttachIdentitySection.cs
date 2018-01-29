using UnityEngine;

public class AttachIdenititySection
{
    public void Display()
    {
        GUILayout.BeginVertical();

        GUILayout.Label("Attach Identity");

        GUILayout.BeginHorizontal();

        if (Util.Button("Universal #1"))
        {
            AttachIdentityDialog.AttachIdentityUniversal_1();
        }
        if (Util.Button("Universal #2"))
        {
            AttachIdentityDialog.AttachIdentityUniversal_2();
        }
        if (Util.Button("Email"))
        {
            AttachIdentityDialog.AttachIdentityEmail();
        }
        if (Util.Button("GooglePlay (Android Only)"))
        {
            AttachIdentityDialog.AttachIdentityGooglePlay();
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }
}