using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;
using System.IO;
  
// ------------------------------------------------------------------------
// https://docs.unity3d.com/Manual/CommandLineArguments.html
// ------------------------------------------------------------------------
public class JenkinsBuild {
  
    static string[] EnabledScenes = FindEnabledEditorScenes();
  
    // called from Jenkins
    public static void BuildWebGL()
    {
        var args = FindArgs();
        args.GetEnviroVariables();
        string fullPathAndName = args.targetDir + args.GetBuildFolderName();
        BuildProject(EnabledScenes, fullPathAndName, BuildTargetGroup.WebGL, BuildTarget.WebGL, BuildOptions.None);
    }
    
    // called from Jenkins
    public static void BuildWindowStandalone()
    {
        var args = FindArgs();
        args.GetEnviroVariables();
        string fullPathAndName = args.targetDir + args.GetBuildFolderName();
        BuildProject(EnabledScenes, fullPathAndName, BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows, BuildOptions.None);
    }
    
    // called from Jenkins
    public static void BuildMacOS()
    {
        var args = FindArgs();
        args.GetEnviroVariables();
        string fullPathAndName = args.targetDir + args.GetBuildFolderName();
        BuildProject(EnabledScenes, fullPathAndName, BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX, BuildOptions.None);
    }

    // called from Jenkins
    public static void BuildAndroid()
    {
        var args = FindArgs();
        args.GetEnviroVariables();
        string fullPathAndName = args.targetDir + args.GetBuildFolderName();
        BuildProject(EnabledScenes, fullPathAndName, BuildTargetGroup.Android, BuildTarget.Android, BuildOptions.None);
    }
    
    //WIP, doesn't work on Mac but works fine in Windows..
    private static void SetRemoteBuildSettings()
    {
        string appId = GetArg("-appId");
        string appSecret = GetArg("-appSecret");
        string appAuthUrl = GetArg("-url");

        if(!string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(appSecret))
        {
            string path = "Assets/Resources/BCSettings.txt";
            StreamWriter writer = new StreamWriter(path, true);

            writer.WriteLine(appAuthUrl);
            writer.WriteLine(appId);
            writer.WriteLine(appSecret);
            writer.Close();

            AssetDatabase.ImportAsset(path);

            TextAsset bcsettings = Resources.Load<TextAsset>("BCSettings");


            Debug.Log($"Successfully set the appID and appSecret to: {bcsettings}");
        }
    }
    
    private static Args FindArgs()
    {
        var returnValue = new Args();
 
        // find: -executeMethod
        //   +1: JenkinsBuild.BuildMacOS
        //   +2: FindTheGnome
        //   +3: D:\Jenkins\Builds\Find the Gnome\47\output
        string[] args = System.Environment.GetCommandLineArgs();
        var execMethodArgPos = -1;
        bool allArgsFound = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-executeMethod")
            {
                execMethodArgPos = i;
            }
            var realPos = execMethodArgPos == -1 ? -1 : i - execMethodArgPos - 2;
            if (realPos < 0)
                continue;
 
            if (realPos == 0)
                returnValue.appName = args[i];
            if (realPos == 1)
            {
                returnValue.targetDir = args[i];
                if (!returnValue.targetDir.EndsWith(System.IO.Path.DirectorySeparatorChar + ""))
                    returnValue.targetDir += System.IO.Path.DirectorySeparatorChar;
 
                allArgsFound = true;
            }
        }
 
        if (!allArgsFound)
            System.Console.WriteLine("[JenkinsBuild] Incorrect Parameters for -executeMethod Format: -executeMethod JenkinsBuild.BuildWindows64 <app name> <output dir>");
 
        return returnValue;
    }
    
    private static string GetArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && args.Length > i + 1)
            {
                return args[i + 1];
            }
        }
        return null;
    }
    
    private static string[] FindEnabledEditorScenes(){
  
        List<string> EditorScenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.enabled)
                EditorScenes.Add(scene.path);
 
        return EditorScenes.ToArray();
    }
  
    // ------------------------------------------------------------------------
    // e.g. BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX
    // ------------------------------------------------------------------------
    private static void BuildProject(string[] scenes, string targetDir, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, BuildOptions buildOptions)
    {
        System.Console.WriteLine("[JenkinsBuild] Building:" + targetDir + " buildTargetGroup:" + buildTargetGroup.ToString() + " buildTarget:" + buildTarget.ToString());
  
        // https://docs.unity3d.com/ScriptReference/EditorUserBuildSettings.SwitchActiveBuildTarget.html
        bool switchResult = EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
        if (switchResult)
        {
            System.Console.WriteLine("[JenkinsBuild] Successfully changed Build Target to: " + buildTarget.ToString());
        }
        else
        {
            System.Console.WriteLine("[JenkinsBuild] Unable to change Build Target to: " + buildTarget.ToString() + " Exiting...");
            return;
        }
  
        // https://docs.unity3d.com/ScriptReference/BuildPipeline.BuildPlayer.html
        BuildReport buildReport = BuildPipeline.BuildPlayer(scenes, targetDir, buildTarget, buildOptions);
        BuildSummary buildSummary = buildReport.summary;
        if (buildSummary.result == BuildResult.Succeeded)
        {
            System.Console.WriteLine("[JenkinsBuild] Build Success: Time:" + buildSummary.totalTime + " Size:" + buildSummary.totalSize + " bytes");
        }
        else
        {
            System.Console.WriteLine("[JenkinsBuild] Build Failed: Time:" + buildSummary.totalTime + " Total Errors:" + buildSummary.totalErrors);
        }
    }
 
    private class Args
    {
        public string appName;
        public string targetDir;
        public string buildNumber;

        public string GetBuildFolderName()
        {
            GetEnviroVariables();
#if UNITY_STANDALONE_WIN
            return $"RelayTestApp_Internal_clientVersion.{BrainCloud.Version.GetVersion()}.exe";
#elif UNITY_STANDALONE_OSX
            return $"RelayTestApp_Internal_clientVersion.{BrainCloud.Version.GetVersion()}.app";
#else
            return $"RelayTestApp_Internal_clientVersion.{BrainCloud.Version.GetVersion()}.exe";
#endif
        }
        
        public void GetEnviroVariables()
        {
            targetDir = System.Environment.GetEnvironmentVariable("targetDirectory");
        }
    }
}