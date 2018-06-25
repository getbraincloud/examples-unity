using UnityEngine;

public class WrapperInformationSection
{
    public void Display()
    {
        GUILayout.BeginHorizontal();
        
        GUILayout.Label("anonId: " + App.Bc.GetStoredAnonymousId());
        GUILayout.Label("profileId: " + App.Bc.GetStoredProfileId());

        GUILayout.EndHorizontal();
    }
}