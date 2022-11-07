using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;
  
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
        args.targetDir = "C:/Users/buildmaster/Documents/BombersRTT_Ultra_WebGL/";
        string fullPathAndName = args.targetDir + args.GetBuildFolderName();
        BuildProject(EnabledScenes, fullPathAndName, BuildTargetGroup.WebGL, BuildTarget.WebGL, BuildOptions.None);
    }
    
    // called from Jenkins
    public static void BuildWindowStandalone()
    {
        var args = FindArgs();
        args.GetEnviroVariables();
        args.targetDir = "C:/Users/buildmaster/Documents/BombersRTT_Ultra_WindowsStandalone/";
        string fullPathAndName = args.targetDir + args.GetBuildFolderName();
        BuildProject(EnabledScenes, fullPathAndName, BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows, BuildOptions.None);
    }
    
    // called from Jenkins
    public static void BuildMacOS()
    {
        var args = FindArgs();
        args.GetEnviroVariables();
        args.targetDir = "BombersRTT_Ultra_MacOS";
        string fullPathAndName = args.targetDir + args.GetBuildFolderName();
        BuildProject(EnabledScenes, fullPathAndName, BuildTargetGroup.iOS, BuildTarget.StandaloneOSX, BuildOptions.None);
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
            return $"BombersRTT_Internal_clientVersion.{BrainCloud.Version.GetVersion()}_buildNumber.{buildNumber}.exe";
        }
        
        public void GetEnviroVariables()
        {
            targetDir = System.Environment.GetEnvironmentVariable("targetDirectory");
            buildNumber = System.Environment.GetEnvironmentVariable("BUILD_NUMBER");
        }
    }
}