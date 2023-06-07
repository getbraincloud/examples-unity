using UnityEngine;

public class MergeIdenititySection
{
    public void Display()
    {
        GUILayout.BeginVertical();

        GUILayout.Label("Merge Identity");

        GUILayout.BeginHorizontal();

        if (Util.Button("Universal #1"))
        {
            MergeIdentityDialog.MergeIdentityUniversal_1();
        }
        if (Util.Button("Universal #2"))
        {
            MergeIdentityDialog.MergeIdentityUniversal_2();
        }

        if (Util.Button("Email"))
        {
            MergeIdentityDialog.MergeIdentityEmail();
        }
        if (Util.Button("GooglePlay (Android Only)"))
        {
            MergeIdentityDialog.MergeIdentityGooglePlay();
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }
}