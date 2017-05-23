using UnityEngine;

public class DetachIdenititySection
{
    public void Display()
    {
        GUILayout.BeginVertical();

        GUILayout.Label("Detach Identity");

        GUILayout.BeginHorizontal();


        if (Util.Button("Universal #1"))
        {
            DetachIdentityDialog.DetachIdentityUniversal_1();
        }
        if (Util.Button("Universal #2"))
        {
            DetachIdentityDialog.DetachIdentityUniversal_2();
        }

        if (Util.Button("Email"))
        {
            DetachIdentityDialog.DetachIdentityEmail();
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }
}