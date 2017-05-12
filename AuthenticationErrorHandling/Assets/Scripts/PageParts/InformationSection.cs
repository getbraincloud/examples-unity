using UnityEngine;

public class InformationSection
{
    public void Display()
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label("anonId: " + BrainCloud.BrainCloudClient.Get().AuthenticationService.AnonymousId);
        GUILayout.Label("profileId: " + BrainCloud.BrainCloudClient.Get().AuthenticationService.ProfileId);

        GUILayout.EndHorizontal();
    }
}