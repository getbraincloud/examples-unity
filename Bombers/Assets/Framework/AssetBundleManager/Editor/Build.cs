using UnityEditor;
using System.Collections;
using System.Linq;
using System;
using System.IO;
using AssetBundles;

public class Build
{
    static string ARTIFACTS_FOLDER = "../autobuild/artifacts";
    static string IOS_OUTPUT_FOLDER = ARTIFACTS_FOLDER + "/generated_build";
    static string ANDROID_OUTPUT_APK = ARTIFACTS_FOLDER + "/android_build.apk";
    static string WP8_OUTPUT_FOLDER = ARTIFACTS_FOLDER + "/wp8_build";
    static string DESKTOP_OUTPUT = ARTIFACTS_FOLDER + "/BrainCloudUNETExample";
    static string WEBGL_OUTPUT_FOLDER = ARTIFACTS_FOLDER + "/BrainCloudUNETExample_webgl";

    static string[] GetScenes()
    {
        string[] scenes = (from scene in EditorBuildSettings.scenes where scene.enabled select scene.path).ToArray();
        return scenes;
    }

    static void BuildAssetBundlesForPlatform()
    {
        AssetBundles.BuildScript.BuildAssetBundles(EditorUserBuildSettings.activeBuildTarget);
    }

    static void UpdateBrainCloudSettings()
    {
        string[] args = Environment.GetCommandLineArgs();
        foreach (string arg in args)
        {
            /** for the future...
			if (arg.StartsWith("-bcappid="))
			{
				BrainCloudSettings.Instance.GameId = arg.Substring(("-bcappid=").Length);
			}
			else if (arg.StartsWith ("-bcsecret="))
			{
				BrainCloudSettings.Instance.SecretKey = arg.Substring(("-bcsecret=").Length);
			}
			else if (arg.StartsWith ("-bcurl="))
			{
				BrainCloudSettings.Instance.ServerURL = arg.Substring (("-bcurl=").Length);
			}
			*/
        }
    }

    /* From the forums: http://forum.unity3d.com/threads/4-6-ios-64-bit-beta.290551/page-9#post-1948394
     * For Unity 5:
PlayerSettings.SetPropertyInt("ScriptingBackend",
(int)ScriptingImplementation.IL2CPP, BuildTargetGroup.iOS);
PlayerSettings.SetPropertyInt("Architecture",
architectureValue, BuildTargetGroup.iOS);

For Unity 4.6:
PlayerSettings.SetPropertyInt("ScriptingBackend",
(int)ScriptingImplementation.IL2CPP, BuildTargetGroup.iPhone);
PlayerSettings.SetPropertyInt("Architecture",
architectureValue, BuildTargetGroup.iPhone);

Where 'architectureValue' is as follows (the enum for architecture seems to be internal currently):
0 - ARMv7
1 - ARM64
2 - Universal
*/
    static void PlayerSettingsIl2cpp(bool enableIl2cpp)
    {
        int architectureValue = 2; // Universal
#if UNITY_4_6
		PlayerSettings.SetPropertyInt("ScriptingBackend", scriptingImplementation, BuildTargetGroup.iPhone);
		PlayerSettings.SetPropertyInt("Architecture", architectureValue, BuildTargetGroup.iPhone);
#else
        PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, architectureValue);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
#endif
    }

    static void PerformBuildIOS_mono()
    {
        Build.PlayerSettingsIl2cpp(false);
        BuildTarget target;
#if UNITY_4_6
		target = BuildTarget.iPhone;
#else
        target = BuildTarget.iOS;
#endif

        UpdateBrainCloudSettings();
        buildPlayer(target, System.IO.Path.GetFullPath(IOS_OUTPUT_FOLDER));
    }

    static void PerformBuildIOS_il2cpp()
    {
        //Build.PlayerSettingsIl2cpp(true);
        BuildTarget target;
#if UNITY_4_6
		target = BuildTarget.iPhone;
#else
        target = BuildTarget.iOS;
#endif

        UpdateBrainCloudSettings();
        buildPlayer(target, System.IO.Path.GetFullPath(IOS_OUTPUT_FOLDER));

        string projectFile = System.IO.Path.GetFullPath(IOS_OUTPUT_FOLDER) + "/Unity-iPhone.xcodeproj/project.pbxproj";
        string contents = File.ReadAllText(projectFile);

        // MessageUI.framework
        // GOOD!
        contents = contents.Replace("00000000008063A1000160D3 /* libiPhone-lib.a in Frameworks */ = {isa = PBXBuildFile; fileRef = D8A1C72A0E8063A1000160D3 /* libiPhone-lib.a */; };",
                                    "00000000008063A1000160D3 /* libiPhone-lib.a in Frameworks */ = {isa = PBXBuildFile; fileRef = D8A1C72A0E8063A1000160D3 /* libiPhone-lib.a */; }; 49E116951EA695D400256812 /* MessageUI.framework in Frameworks */ = {isa = PBXBuildFile; fileRef = 49E116941EA695D400256812 /* MessageUI.framework */; };");

        // GOOD!
        contents = contents.Replace("path = Classes/Native/Bulk_System_2.cpp; sourceTree = SOURCE_ROOT; };",
                                    "path = Classes/Native/Bulk_System_2.cpp; sourceTree = SOURCE_ROOT; }; 49E116941EA695D400256812 /* MessageUI.framework */ = {isa = PBXFileReference; lastKnownFileType = wrapper.framework; name = MessageUI.framework; path = System/Library/Frameworks/MessageUI.framework; sourceTree = SDKROOT; };");

        // GOOD! 
        contents = contents.Replace("00000000008063A1000160D3 /* libiPhone-lib.a in Frameworks */,",
                                    "00000000008063A1000160D3 /* libiPhone-lib.a in Frameworks */, 49E116951EA695D400256812 /* MessageUI.framework in Frameworks */,");

        // GOOD!
        contents = contents.Replace("AA5D99861AFAD3C800B27605 /* CoreText.framework */,",
                                    "49E116941EA695D400256812 /* MessageUI.framework */,AA5D99861AFAD3C800B27605 /* CoreText.framework */,");

        // good! -- push notifications
        contents = contents.Replace("SystemCapabilities = {\n\t\t\t\t\t\t\tcom.apple.GameControllers.appletvos = {\n\t\t\t\t\t\t\t\tenabled = 1;\n\t\t\t\t\t\t\t};\n\t\t\t\t\t\t};",
            "SystemCapabilities = {\n\t\t\t\t\t\t\tcom.apple.GameControllers.appletvos = {\n\t\t\t\t\t\t\t\tenabled = 1;\n\t\t\t\t\t\t\t};\n\t\t\t\t\t\t\tcom.apple.Push = {\n\t\t\t\t\t\t\t\tenabled = 1;\n\t\t\t\t\t\t\t};\n\t\t\t\t\t\t};");

        // attach the new entitlements
        contents = contents.Replace("CLANG_WARN_DEPRECATED_OBJC_IMPLEMENTATIONS = YES;",
            "CLANG_WARN_DEPRECATED_OBJC_IMPLEMENTATIONS = YES;\n\t\t\t\tCODE_SIGN_ENTITLEMENTS = \"Unity-iPhone/BrainCloudUNETExample.entitlements\";");

        // attach the references to the entitlements 
        contents = contents.Replace("Classes/Native/Il2CppMarshalingFunctionsTable.cpp; sourceTree = SOURCE_ROOT; };",
            "Classes/Native/Il2CppMarshalingFunctionsTable.cpp; sourceTree = SOURCE_ROOT; };\n\t\t490BB1CE1EF1A5880067B6B3 /* BrainCloudUNETExample.entitlements */ = {isa = PBXFileReference; lastKnownFileType = text.plist.entitlements; name = simpro3.entitlements; path = \"Unity-iPhone/simpro3.entitlements\"; sourceTree = \"<group>\"; };");
        // 
        contents = contents.Replace("AA31BF961B55660D0013FB1B /* Data */,",
            "490BB1CE1EF1A5880067B6B3 /* BrainCloudUNETExample.entitlements */,\n\t\t\t\tAA31BF961B55660D0013FB1B /* Data */,");

        File.WriteAllText(projectFile, contents);
    }

    static void PerformBuildAndroid()
    {
        BuildTarget target;
        target = BuildTarget.Android;

        UpdateBrainCloudSettings();
        buildPlayer(target, System.IO.Path.GetFullPath(ANDROID_OUTPUT_APK));
    }

    static void PerformBuildWP8()
    {
        BuildTarget target;
        target = BuildTarget.WSAPlayer;

        UpdateBrainCloudSettings();
        buildPlayer(target, System.IO.Path.GetFullPath(WP8_OUTPUT_FOLDER));
    }

    static void PerformBuildWin32()
    {
        BuildTarget target;
        target = BuildTarget.StandaloneWindows;

        UpdateBrainCloudSettings();
        buildPlayer(target, System.IO.Path.GetFullPath(DESKTOP_OUTPUT + ".exe"));
    }

    static void PerformBuildWin64()
    {
        BuildTarget target;
        target = BuildTarget.StandaloneWindows64;

        UpdateBrainCloudSettings();
        buildPlayer(target, System.IO.Path.GetFullPath(DESKTOP_OUTPUT + ".exe"));
    }

    static void PerformBuildOSX32()
    {
        BuildTarget target;
        target = BuildTarget.StandaloneOSX;

        UpdateBrainCloudSettings();
        buildPlayer(target, System.IO.Path.GetFullPath(DESKTOP_OUTPUT));
    }

    static void PerformBuildOSX64()
    {
        BuildTarget target;
        target = BuildTarget.StandaloneOSX;

        UpdateBrainCloudSettings();
        buildPlayer(target, System.IO.Path.GetFullPath(DESKTOP_OUTPUT));
    }

    static void PerformBuildWeb()
    {
        UpdateBrainCloudSettings();
        //string[] scenes = GetScenes();
        //BuildPipeline.BuildPlayer(scenes, System.IO.Path.GetFullPath(WEB_OUTPUT_FOLDER), BuildTarget.WebPlayer, BuildOptions.None);
    }

    static void PerformBuildWebGL()
    {
#if UNITY_4_6
		// no such luck
#else
        BuildTarget target;
        target = BuildTarget.WebGL;

        UpdateBrainCloudSettings();
        buildPlayer(target, System.IO.Path.GetFullPath(WEBGL_OUTPUT_FOLDER));
#endif
    }

    public static void BuildAllActiveAssetBundles()
    {
        BuildScript.RebuildBuildAssetBundles(BuildTarget.WebGL);
        BuildScript.RebuildBuildAssetBundles(BuildTarget.iOS);
    }

    private static void buildPlayer(BuildTarget target, string folderPath)
    {
        BuildPlayerOptions options = new BuildPlayerOptions();
        options.locationPathName = folderPath;
        options.scenes = GetScenes();
        options.target = target;
        options.options = BuildOptions.None;

        BuildPipeline.BuildPlayer(options);
    }
}