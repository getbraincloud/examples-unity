using System.Collections.Generic;
using BrainCloud.JSONHelper;
using Gameframework;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class BuddyUtility : EditorWindow
{
    [MenuItem("bitBuddy/Buddy Utility")]
    private static void ShowWindow()
    {
        var window = GetWindow<BuddyUtility>();
        window.titleContent = new GUIContent("Utility Window");

        window.Show();
    }
    
        // Draw GUI components (buttons, textboxes, etc.)
    private void OnGUI()
    {
        GUI.skin.label.fontSize = 9;

        GUILayout.Space(20); // Top Padding

        // Buttons
        using (new GUILayout.HorizontalScope(GUILayout.MaxWidth(150)))
        {
            //Button 1
            {
                GUILayout.Space(12); // Left Padding

                GUI.enabled = true;
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("test button");
                    if (GUILayout.Button("test", GUILayout.Width(100), GUILayout.Height(40)))
                    {
                        Dictionary<string, object> scriptData = new Dictionary<string, object>();
                        scriptData.Add("childAppId", BitBuddiesConsts.APP_CHILD_ID);
                        scriptData.Add("profileId", "e068fdfb-f36e-4c9d-862a-d86f20d5e54b");
                        BrainCloudManager.Wrapper.ScriptService.RunScript
                        (
                            BitBuddiesConsts.GET_CHILD_ITEM_CATALOG_SCRIPT_NAME,
                            scriptData.Serialize()
                        );
                    }
                }
            }
        }
    }
}
#endif