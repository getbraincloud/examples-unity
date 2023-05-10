// MACOS STANDALONE ONLY
// This will run after a MacOS build is made and will sign the app & bundles within so it does not appear as "damaged" for users.

#if UNITY_STANDALONE_OSX

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.OSXStandalone;
using UnityEngine;

public static class MacOSPostBuild
{
    [PostProcessBuild(100)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        switch (target)
        {
            case BuildTarget.StandaloneOSX:
                try
                {
#if APPLE_SDK
                    AppleAuth.Editor.AppleAuthMacosPostprocessorHelper.FixManagerBundleIdentifier(target, pathToBuiltProject);
#else
                    string[] bundles = Directory.GetDirectories(Path.Combine(pathToBuiltProject, "Contents", "PlugIns"), "*.bundle");

                    foreach (string bundle in bundles)
                    {
                        MacOSCodeSigning.CodeSignAppBundle(bundle);

                        Debug.Log($"Found Bundle: {bundle}");
                    }

                    MacOSCodeSigning.CodeSignAppBundle(pathToBuiltProject);

                    Debug.Log("MacOS Bundles & App Signed.");
#endif
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
                return;
            default:
                Debug.LogError("MacOSPostBuild should only be able to be run on Standalone OSX!");
                return;
        }
    }
}

#endif
