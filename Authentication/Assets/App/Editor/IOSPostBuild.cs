// iOS ONLY

#if UNITY_IOS
#if APPLE_SDK
using AppleAuth.Editor;
#endif
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
#if APPLE_SDK
                    // Add entitlements for Apple Sign-in
                    var projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);

                    PBXProject project = new PBXProject();
                    project.ReadFromString(File.ReadAllText(projectPath));
                    var manager = new ProjectCapabilityManager(projectPath, "Entitlements.entitlements", null, project.GetUnityMainTargetGuid());
                    manager.AddSignInWithAppleWithCompatibility();
                    manager.WriteToFile();
#endif
                    // Update Plist
                    var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
                    var plist = new PlistDocument();
                    plist.ReadFromString(File.ReadAllText(plistPath));

                    PlistElementDict rootDict = plist.root;

                    // Add values here as needed
                    rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);
                    rootDict.SetString("NSUserTrackingUsageDescription", "Facebook Logins");

                    File.WriteAllText(plistPath, plist.WriteToString());

                    Debug.Log("Updated Xcode project.");
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
