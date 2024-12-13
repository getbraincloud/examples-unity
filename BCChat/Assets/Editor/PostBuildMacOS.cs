
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System;

#if UNITY_STANDALONE_OSX
public class PostBuildMacOS
{
    [PostProcessBuild(100)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        switch (target)
        {
            case BuildTarget.StandaloneOSX:
                try
                {
                    string[] bundles = Directory.GetDirectories(Path.Combine(pathToBuiltProject, "Contents", "PlugIns"), "*.bundle");

                    foreach (string bundle in bundles)
                    {
                        UnityEditor.OSXStandalone.MacOSCodeSigning.CodeSignAppBundle(bundle);

                        Debug.Log($"Found Bundle: {bundle}");
                    }

                    UnityEditor.OSXStandalone.MacOSCodeSigning.CodeSignAppBundle(pathToBuiltProject);

                    Debug.Log("MacOS Bundles & App Signed.");
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
#elif UNITY_IOS

public class PostBuildMacOS
{
    [PostProcessBuild(100)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        switch (target)
        {
            case BuildTarget.iOS:
                try
                {
                    string[] bundles = Directory.GetDirectories(Path.Combine(pathToBuiltProject, "Contents", "PlugIns"), "*.bundle");

                    foreach (string bundle in bundles)
                    {
                        //UnityEditor.OSXStandalone.MacOSCodeSigning.CodeSignAppBundle(bundle);

                        Debug.Log($"Found Bundle: {bundle} but not doing anything with it yet");
                    }

                    //UnityEditor.OSXStandalone.MacOSCodeSigning.CodeSignAppBundle(pathToBuiltProject);

                    Debug.Log("MacOS Bundles & App Signed.");
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
                return;
            default:
                Debug.LogError("This post build script should only run on builds targetting iOS!");
                return;
        }
    }
}

#endif