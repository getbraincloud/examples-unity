using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class FindMissingFonts : EditorWindow
{
    Font font = null;

    // Add menu named "My Window" to the Window menu
    [MenuItem("Tools/Replace Missing Fonts")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        FindMissingFonts window = (FindMissingFonts)GetWindow(typeof(FindMissingFonts));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Replace Missing Fonts", EditorStyles.boldLabel);
        font = EditorGUILayout.ObjectField("Font", font, typeof(Font), false) as Font;

        if (GUILayout.Button("Find and Replace")) FindAndReplace();
    }

    void FindAndReplace()
    {
        var text = FindObjectsOfType<Text>();

        int count = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i].font == null)
            {
                count++;
                text[i].font = font;
            }
        }

        Debug.Log("Replaced missing fonts in " + count + " Text objects");
    }
}