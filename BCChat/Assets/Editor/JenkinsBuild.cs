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
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        var args = new Args();
        args.GetEnviroVariables();
        string fullPathAndName = args.targetDir + "/" + args.GetBuildFolderName();
        BuildProject(EnabledScenes, fullPathAndName, BuildTargetGroup.WebGL, BuildTarget.WebGL, BuildOptions.None);
    }
    
    // called from Jenkins
    public static void BuildWindowStandalone()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows);
        var args = new Args();
        args.GetEnviroVariables();
        string fullPathAndName = args.targetDir + "/" + args.GetBuildFolderName() + ".exe";
        BuildProject(EnabledScenes, fullPathAndName, BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows, BuildOptions.None);
    }
    
    // called from Jenkins
    public static void BuildMacOS()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
        var args = new Args();
        args.GetEnviroVariables();
        string fullPathAndName = args.targetDir + "/" + args.GetBuildFolderName() + ".app";
        BuildProject(EnabledScenes, fullPathAndName, BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX, BuildOptions.None);
    }

    public static void BuildIOS()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
        var args = new Args();
        args.GetEnviroVariables();
        string fullPathAndName = args.targetDir + "/" + args.GetBuildFolderName();
        string teamID = GetArg("-iosTeamID");

        if (!string.IsNullOrEmpty(teamID))
        {
            PlayerSettings.iOS.appleDeveloperTeamID = teamID;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        }

        BuildProject(EnabledScenes, fullPathAndName, BuildTargetGroup.iOS, BuildTarget.iOS, BuildOptions.None);
    }

    // called from Jenkins
    public static void BuildAndroid()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        var args = new Args();
        args.GetEnviroVariables();
        string fullPathAndName = args.targetDir + "/" + args.GetBuildFolderName() + ".apk";
        BuildProject(EnabledScenes, fullPathAndName, BuildTargetGroup.Android, BuildTarget.Android, BuildOptions.None);
    }
    
    // called from Jenkins
    public static void BuildDedicatedServer()
    {
        var args = new Args();
        args.GetEnviroVariables();
        string fullPathAndName = args.targetDir + "/" + args.GetBuildFolderName();
        BuildServer(fullPathAndName);
    }

    public static void WriteBuildDetails()
    {
        var args = new Args();
        args.GetEnviroVariables();

        string jsonString = args.ToJsonString();
        string filePath = Path.Combine(Application.dataPath, "../BuildDetails.json");

        Debug.Log($"Writing Json string {jsonString} to {filePath}");
        // Write the JSON string to a file
        File.WriteAllText(filePath, jsonString);
    }

    public static void OutputBCVersion()
    {
        Debug.Log(BrainCloud.Version.GetVersion());
    }

    public static void OutputProjectName()
    {
        Debug.Log(PlayerSettings.productName);
    }

    public static void OutputProjectVersion()
    {
        Debug.Log(PlayerSettings.bundleVersion);
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
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.options = buildOptions;
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.target = buildTarget;
        buildPlayerOptions.locationPathName = targetDir;

        BuildReport buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
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

    private static void BuildServer(string targetDir)
    {
        var buildPlayerOptions = new BuildPlayerOptions()
        {
            subtarget = (int) StandaloneBuildSubtarget.Server,
            scenes = EnabledScenes,
            target = BuildTarget.LinuxHeadlessSimulation,
            options = BuildOptions.Development
        };
        buildPlayerOptions.locationPathName = targetDir;
        // https://docs.unity3d.com/ScriptReference/BuildPipeline.BuildPlayer.html
        //BuildReport buildReport = BuildPipeline.BuildPlayer(scenes, targetDir, buildTarget, buildOptions);
        BuildReport buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary buildSummary = buildReport.summary;
        if (buildSummary.result == BuildResult.Succeeded)
        {
            System.Console.WriteLine("[JenkinsBuild] Build Success: Time:" + buildSummary.totalTime + " Size:" + buildSummary.totalSize + " bytes");
        }
        else
        {
            System.Console.WriteLine("[JenkinsBuild] Build Failed: Time:" + buildSummary.totalTime + " Total Errors:" + buildSummary.totalErrors);
        }
        
        if (buildReport.summary.totalErrors > 0)
            EditorApplication.Exit(1);
    }
 
    private class Args
    {
        public string appId;
        public string codeName;
        public string projectName;
        public string targetDir;
        public string buildNumber;
        public string environment;
        public string version;
        public string bcVersion;

        public string GetBuildFolderName()
        { 
            if(string.IsNullOrEmpty(buildNumber))
                GetEnviroVariables();

            return $"{codeName}_{environment}_{version}";
        }

        public string GetProjectName()
        {
            string[] s = Application.dataPath.Split('/');
            string projectName = s[s.Length - 2];
            return projectName;
        }
        
        public void GetEnviroVariables()
        {
            targetDir = System.Environment.GetEnvironmentVariable("TARGET_DIRECTORY");
            buildNumber = GetArg("-buildNumber");
            codeName = GetProjectName();
            projectName = PlayerSettings.productName;
            environment = BrainCloud.Plugin.Interface.DispatcherURL.Contains("internal") ? "Internal" : "Prod";
            appId = BrainCloud.Plugin.Interface.AppId;
            version = Application.version[0] + "." + Application.version[2] + "." + buildNumber;
            bcVersion = BrainCloud.Version.GetVersion();
            PlayerSettings.bundleVersion = version;
            Debug.Log("Build number: " + buildNumber);
            Debug.Log("Version set to " + version);
        }

        public string ToJsonString()
        {
            string json = string.Empty;
            json += "{ \n";
            json += $"\"appId\":\"{appId}\",";
            json += $"\"projectName\":\"{projectName}\",";
            json += $"\"codeName\":\"{codeName}\",";
            json += $"\"bcVersion\":\"{bcVersion}\",";
            json += $"\"projectVersion\":\"{version}\"";
            json += "}";

            return json;
        }
    }
}
