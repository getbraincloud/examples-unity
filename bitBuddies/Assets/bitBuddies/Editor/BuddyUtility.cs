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
                    GUILayout.Label("Increase Coins");
                    if (GUILayout.Button("Increase Coins", GUILayout.Width(100), GUILayout.Height(40)))
                    {
                        BrainCloudManager.Instance.RewardCoinsToParent(100);
                    }
                }
            }
            //Button 2
            {
                GUILayout.Space(12); // Left Padding

                GUI.enabled = true;
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("Increase Gems");
                    if (GUILayout.Button("Increase Gems", GUILayout.Width(100), GUILayout.Height(40)))
                    {
                        BrainCloudManager.Instance.RewardGemsToParent(10);
                    }
                }
            }
            //Button 3
            {
                GUILayout.Space(12); // Left Padding

                GUI.enabled = true;
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("Level Up Parent");
                    if (GUILayout.Button("LevelUpParent", GUILayout.Width(100), GUILayout.Height(40)))
                    {
                        BrainCloudManager.Instance.LevelUpParent();
                    }
                }
            }
            //Button 4
            {
                GUILayout.Space(12); // Left Padding

                GUI.enabled = true;
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("NewBuddy");
                    if (GUILayout.Button("NewBuddy", GUILayout.Width(100), GUILayout.Height(40)))
                    {
                        BrainCloudManager.Instance.AddBuddy();
                    }
                }
            }
        }
    }
}
#endif