using UnityEditor;
using System.Collections;
using System.Linq;

public class Build {
    static string OUTPUT_FOLDER = "../autobuild/artifacts/generated_build";
    
    static string[] GetScenes()
    {
        string[] scenes = (from scene in EditorBuildSettings.scenes where scene.enabled select scene.path).ToArray();
        return scenes;
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
        int scriptingImplementation = (int) (enableIl2cpp ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);
#if UNITY_4_6
        PlayerSettings.SetPropertyInt("ScriptingBackend", scriptingImplementation, BuildTargetGroup.iPhone);
        PlayerSettings.SetPropertyInt("Architecture", architectureValue, BuildTargetGroup.iPhone);
#else
        PlayerSettings.SetPropertyInt("ScriptingBackend", scriptingImplementation, BuildTargetGroup.iOS);
        PlayerSettings.SetPropertyInt("Architecture", architectureValue, BuildTargetGroup.iOS);
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

        string[] scenes = GetScenes();
        BuildPipeline.BuildPlayer(scenes, System.IO.Path.GetFullPath(OUTPUT_FOLDER), target, BuildOptions.None);
    }

    static void PerformBuildIOS_il2cpp()
    {
        Build.PlayerSettingsIl2cpp(true);
        BuildTarget target;
#if UNITY_4_6
        target = BuildTarget.iPhone;
#else
        target = BuildTarget.iOS;
#endif

        string[] scenes = GetScenes();
        BuildPipeline.BuildPlayer(scenes, System.IO.Path.GetFullPath(OUTPUT_FOLDER), target, BuildOptions.None);
    }

	static void PerformBuildAndroid()
	{
		BuildTarget target;
		target = BuildTarget.Android;
		
		string[] scenes = GetScenes();
		BuildPipeline.BuildPlayer(scenes, System.IO.Path.GetFullPath(OUTPUT_FOLDER), target, BuildOptions.None);
	}

	static void PerformBuildWP8()
	{
		BuildTarget target;
		target = BuildTarget.WP8Player;
		
		string[] scenes = GetScenes();
		BuildPipeline.BuildPlayer(scenes, System.IO.Path.GetFullPath(OUTPUT_FOLDER), target, BuildOptions.None);
	}

	static void PerformBuildWin32()
	{
		BuildTarget target;
		target = BuildTarget.StandaloneWindows;
		
		string[] scenes = GetScenes();
		BuildPipeline.BuildPlayer(scenes, System.IO.Path.GetFullPath(OUTPUT_FOLDER), target, BuildOptions.None);
	}

	static void PerformBuildWin64()
	{
		BuildTarget target;
		target = BuildTarget.StandaloneWindows64;
			
		string[] scenes = GetScenes();
		BuildPipeline.BuildPlayer(scenes, System.IO.Path.GetFullPath(OUTPUT_FOLDER), target, BuildOptions.None);
	}

	static void PerformBuildOSX32()
	{
		BuildTarget target;
		target = BuildTarget.StandaloneOSXIntel;
		
		string[] scenes = GetScenes();
		BuildPipeline.BuildPlayer(scenes, System.IO.Path.GetFullPath(OUTPUT_FOLDER), target, BuildOptions.None);
	}

	static void PerformBuildOSX64()
	{
		BuildTarget target;
		target = BuildTarget.StandaloneOSXIntel64;
		
		string[] scenes = GetScenes();
		BuildPipeline.BuildPlayer(scenes, System.IO.Path.GetFullPath(OUTPUT_FOLDER), target, BuildOptions.None);
	}
    
    static void PerformBuildWeb()
    {
        string[] scenes = GetScenes();
        BuildPipeline.BuildPlayer(scenes, System.IO.Path.GetFullPath(OUTPUT_FOLDER), BuildTarget.WebPlayer, BuildOptions.None);
    }

	static void PerformBuildWebGL()
	{
		#if UNITY_4_6
		// no such luck
		#else
		BuildTarget target;
		target = BuildTarget.WebGL;
		
		string[] scenes = GetScenes();
		BuildPipeline.BuildPlayer(scenes, System.IO.Path.GetFullPath(OUTPUT_FOLDER), target, BuildOptions.None);
		#endif
	}
}
