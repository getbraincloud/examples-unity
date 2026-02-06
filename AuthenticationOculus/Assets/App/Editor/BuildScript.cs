#if UNITY_EDITOR

using UnityEditor;

public class BuildScript
{
    public static void AndroidBuild()
    {
        string[] scenes =
        {
            "Assets/App/Main.unity",
        };

        BuildPipeline.BuildPlayer(scenes,
                                  "App.apk",
                                  BuildTarget.Android,
                                  BuildOptions.None);
    }
}

#endif
