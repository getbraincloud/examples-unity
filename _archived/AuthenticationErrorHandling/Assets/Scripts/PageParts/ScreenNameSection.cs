using BrainCloud.Internal;
using UnityEngine;

public class ScreenNameSection
{
    public string m_screenName = "";

    public void Display()
    {
        GUILayout.BeginVertical();

        GUILayout.Label("screenName:");

        GUILayout.BeginHorizontal();
        m_screenName = Util.TextField(m_screenName);

        if (Util.Button("Update"))
        {
            ChangeScreenNameDialog.ChangeScreenName(m_screenName);
        }


        GUILayout.EndHorizontal();

        //   BrainCloudComms.DEBUG_PACKET_NUMBER = Util.Toggle(BrainCloudComms.DEBUG_PACKET_NUMBER,
        //       "Toggle Debug Packets");

        GUILayout.EndVertical();
    }
}