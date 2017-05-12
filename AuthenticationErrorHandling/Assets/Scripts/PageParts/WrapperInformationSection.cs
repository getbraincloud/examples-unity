using UnityEngine;

public class WrapperInformationSection
{
    public void Display()
    {
        GUILayout.BeginHorizontal();
        
        GUILayout.Label("anonId: " + BrainCloudWrapper.GetInstance().GetStoredAnonymousId());
        GUILayout.Label("profileId: " + BrainCloudWrapper.GetInstance().GetStoredProfileId());

        GUILayout.EndHorizontal();
    }
}