﻿using BrainCloudUnity;
using BrainCloudUnity.BrainCloudSettingsDLL;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BrainCloudSettingsManual))]
public class BrainCloudSettingsEditor : Editor
{
    // Menu Bar
    [MenuItem("brainCloud/Select Settings", false, 001)]
    public static void OpenSettings()
    {
        Selection.activeObject = BrainCloudSettings.Instance;
    }

    [MenuItem("brainCloud/Select Manual Settings", false, 201)]
    public static void SelectManualSettings()
    {
        Selection.activeObject = BrainCloudSettingsManual.Instance;
    }

    [MenuItem("brainCloud/Launch Portal...", false, 100)]
    public static void GoPortal()
    {
        Help.BrowseURL(BrainCloudSettingsManual.Instance.PortalURL);
    }

    [MenuItem("brainCloud/View API Documentation...", false, 101)]
    public static void GoApiReference()
    {
        Help.BrowseURL(BrainCloudSettingsManual.Instance.ApiDocsURL + "/apiref/index.html");
    }

    [MenuItem("brainCloud/View Tutorials...", false, 102)]
    public static void GoTutorials()
    {
        Help.BrowseURL(BrainCloudSettingsManual.Instance.ApiDocsURL + "/tutorials/unity-tutorials/");
    }

    [MenuItem("brainCloud/View Code Examples...", false, 103)]
    public static void GoCodeExamples()
    {
        Help.BrowseURL("https://github.com/getbraincloud/UnityExamples");
    }


    // Draw the content of the inspector GUI
#if UNITY_EDITOR
    
    
    public static void Show (SerializedProperty list) {
        EditorGUILayout.PropertyField(list);
        EditorGUI.indentLevel += 1;
        if (list.isExpanded) {
            EditorGUILayout.PropertyField(list.FindPropertyRelative("Array.size"));
            for (int i = 0; i < list.arraySize; i++) {
                EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), true);
            }
        }
        EditorGUI.indentLevel -= 1;
    }
    
    public override void OnInspectorGUI()
    {
        BrainCloudSettingsManual instance = (BrainCloudSettingsManual) target;

        if (BrainCloudSettings.IsManualSettingsEnabled())
        {
            // Game Config
            EditorGUILayout.HelpBox("The game configuration parameters can be found on the brainCloud portal.",
                MessageType.None);

            serializedObject.Update();
            Show(serializedObject.FindProperty("m_appIdSecrets"));
            serializedObject.ApplyModifiedProperties();
            
            
            
            instance.AppId = EditorGUILayout.TextField("App Id", instance.AppId);
            instance.SecretKey = EditorGUILayout.TextField("App Secret", instance.SecretKey);
            instance.GameVersion = EditorGUILayout.TextField("App Version", instance.GameVersion);
            
            

            EditorGUILayout.Space();

            GUILayout.Space(20);
            EditorGUILayout.HelpBox("The brainCloud server to use. Most users should not have to change this value.",
                MessageType.None);
            instance.ServerURL = EditorGUILayout.TextField("Server URL", instance.ServerURL);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset to Default Server URL", GUILayout.ExpandWidth(false)))
            {
                instance.ServerURL = BrainCloudSettingsManual.DEFAULT_BRAINCLOUD_URL;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            EditorGUILayout.HelpBox("Additional development options for the brainCloud library.", MessageType.None);
            instance.EnableLogging = EditorGUILayout.Toggle("Enable Logging", instance.EnableLogging);

            GUILayout.Space(20);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                padding =
                {
                    left = 20,
                    right = 20
                }
            };

            EditorGUILayout.HelpBox("Links to brainCloud webpages.", MessageType.None);
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Launch Portal", buttonStyle))
            {
                GoPortal();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("View API Docs", buttonStyle))
            {
                GoApiReference();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("View Tutorials", buttonStyle))
            {
                GoTutorials();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox(
                "These are the manual settings for brainCloud. Please use the automatic settings asset.",
                MessageType.None);

            if (GUILayout.Button("Select Settings", GUILayout.ExpandWidth(false)))
            {
                Selection.activeObject = BrainCloudSettings.Instance;
            }
        }
    }
#endif
}