// iOS ONLY
// This will run after an iOS build is made if APPLE_SDK is also defined.

#if UNITY_IOS && APPLE_SDK

using AppleAuth.Editor;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class IOSPostBuild
{
    [PostProcessBuild(100)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        switch (target)
        {
            case BuildTarget.iOS:
                try
                {
                    var projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);

#if UNITY_2019_3_OR_NEWER // Adds entitlement depending on the Unity version used
                    var project = new PBXProject();
                    project.ReadFromString(File.ReadAllText(projectPath));
                    var manager = new ProjectCapabilityManager(projectPath, "Entitlements.entitlements", null, project.GetUnityMainTargetGuid());
                    manager.AddSignInWithAppleWithCompatibility(project.GetUnityFrameworkTargetGuid());
                    manager.WriteToFile();
#else
                    var manager = new ProjectCapabilityManager(projectPath, "Entitlements.entitlements", PBXProject.GetUnityTargetName());
                    manager.AddSignInWithAppleWithCompatibility();
                    manager.WriteToFile();
#endif
                    Debug.Log("Added ProjectCapabilityManager to Xcode Project.");
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
                return;
            default:
                Debug.LogError("IOSPostBuild should only be able to be run on iOS!");
                return;
        }
    }
}

#endif
