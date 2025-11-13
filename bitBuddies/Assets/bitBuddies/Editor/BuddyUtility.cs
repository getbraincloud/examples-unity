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
                    GUILayout.Label("++ child XP ");
                    if (GUILayout.Button("Increase XP for buddy", GUILayout.Width(100), GUILayout.Height(40)))
                    {
                        FindAnyObjectByType<BuddysRoom>().SpawnValueSubtractedAnimation(1000);
                    }
                }
            }
            //Button 2
            {
                GUILayout.Space(12); // Left Padding

                GUI.enabled = true;
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("++ Gems 100");
                    if (GUILayout.Button("Increase Gems", GUILayout.Width(100), GUILayout.Height(40)))
                    {
                        BrainCloudManager.Instance.RewardGemsToParent(100);
                    }
                }
            }
            //Button 3
            {
                GUILayout.Space(12); // Left Padding

                GUI.enabled = true;
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("++ BuddyBling");
                    if (GUILayout.Button("AwardBling", GUILayout.Width(100), GUILayout.Height(40)))
                    {
                        BrainCloudManager.Instance.AwardBlingToChild(100);
                    }
                }
            }
        }
    }
}
#endif