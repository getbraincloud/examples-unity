using UnityEngine;

public class InformationSection
{
    public void Display()
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label("anonId: " + App.Bc.Client.AuthenticationService.AnonymousId);
        GUILayout.Label("profileId: " + App.Bc.Client.AuthenticationService.ProfileId);
        
        GUILayout.EndHorizontal();
        
#if UNITY_ANDROID
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("googleToken: " + GoogleIdentity.GetGoogleToken());
        GUILayout.EndHorizontal();
#endif
    }
}