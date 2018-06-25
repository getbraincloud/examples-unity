using UnityEngine;

public class WrapperIdValuesSection
{
    public string m_anonymousId = "";
    public string m_profileId = "";


    public void Display()
    {
        if (App.Bc.GetStoredAnonymousId() != null)
            m_anonymousId = App.Bc.GetStoredAnonymousId();
        if (App.Bc.GetStoredProfileId() != null)
            m_profileId = App.Bc.GetStoredProfileId();

        GUILayout.BeginVertical();

        GUILayout.Label("Used Values");

        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical();
        GUILayout.Label("Anonymous ID");
        m_anonymousId = Util.TextField(m_anonymousId);


        GUILayout.BeginHorizontal();
        if (Util.Button("Generate"))
        {
            m_anonymousId = App.Bc.Client.AuthenticationService.GenerateAnonymousId();
        }

        if (Util.Button("Clear"))
        {
            m_anonymousId = "";
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        GUILayout.Label("Profile ID");
        m_profileId = Util.TextField(m_profileId);

        GUILayout.BeginHorizontal();
        if (Util.Button("Generate"))
        {
            // Profile id should not be generated with GenerateAnonymousId().
            // This button is to purposely create bugs for the sake of debugging
            m_profileId = App.Bc.Client.AuthenticationService.GenerateAnonymousId();
        }

        if (Util.Button("Clear"))
        {
            m_profileId = "";
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();


        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        App.Bc.SetStoredAnonymousId(m_anonymousId);
        App.Bc.SetStoredProfileId(m_profileId);
    }
}