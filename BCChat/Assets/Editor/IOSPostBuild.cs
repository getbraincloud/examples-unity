#if UNITY_IOS
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class IOSPostBuild
{
    [PostProcessBuildAttribute(100)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        UnityEngine.Debug.Log("[On Post-Process Build()]");
        if (target != BuildTarget.iOS)
        {
            UnityEngine.Debug.LogError("IOSPostBuild should only be run on iOS builds.");
            return;
        }

        try
        {
            // Modify Xcode project settings
            var projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var project = new PBXProject();
            project.ReadFromString(File.ReadAllText(projectPath));
            
            var manager = new ProjectCapabilityManager(
                projectPath,
                "Entitlements.entitlements",
                null,
                project.GetUnityMainTargetGuid()
            );
            //manager.AddSignInWithApple();
            manager.WriteToFile();

            UnityEngine.Debug.Log("Added ProjectCapabilityManager to Xcode Project.");
            string bNum = PlayerSettings.bundleVersion.Split('.')[2];

            SetBundleVersionPlist(pathToBuiltProject, bNum);

            // Build and Export to .ipa
            CreateIpaFromXcodeProject(pathToBuiltProject);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Error in iOS post-build process: " + e);
        }
    }

    private static void CreateIpaFromXcodeProject(string pathToBuiltProject)
    {
        var xcodeProjectPath = pathToBuiltProject;
        var archivePath = Path.Combine(pathToBuiltProject, "BuildOutput.xcarchive");
        var exportPath = Path.Combine(pathToBuiltProject, "BuildOutput");

        // Prepare export options
        var exportOptionsPlist = Path.Combine(pathToBuiltProject, "ExportOptions.plist");
        File.WriteAllText(exportOptionsPlist, GetExportOptionsPlistContents());

        // Run xcodebuild archive
        //RunShellCommand("xcodebuild", $"-project {xcodeProjectPath}/Unity-iPhone.xcodeproj -scheme Unity-iPhone -archivePath {archivePath} archive");

        // Run xcodebuild export to create .ipa
        //RunShellCommand("xcodebuild", $"-exportArchive -archivePath {archivePath} -exportPath {exportPath} -exportOptionsPlist {exportOptionsPlist}");

        UnityEngine.Debug.Log("iOS build and .ipa process ready to begin");
    }

    private static void SetBundleVersionPlist(string pathToBuiltProject, string bundleVersion)
    {
        // Path to the Info.plist file in the built Xcode project
        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");

        // Load the Info.plist file
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        // Modify the CFBundleVersion key to set the bundle version
        plist.root.SetString("CFBundleVersion", bundleVersion);

        // Save the updated Info.plist file
        plist.WriteToFile(plistPath);

        UnityEngine.Debug.Log("Info.plist modified: CFBundleVersion set to " + bundleVersion);
    }

    private static void RunShellCommand(string command, string args)
    {
        Process process = new Process();
        process.StartInfo.FileName = command;
        process.StartInfo.Arguments = args;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.OutputDataReceived += (sender, e) => UnityEngine.Debug.Log(e.Data);
        process.ErrorDataReceived += (sender, e) => UnityEngine.Debug.LogError(e.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Command {command} {args} failed with exit code {process.ExitCode}");
        }
    }

    private static string GetExportOptionsPlistContents()
    {
        // Here we set `signingStyle` to `automatic` and remove teamID and provisioning profiles.
        return @"
<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>method</key>
    <string>development</string> <!-- Change to `app-store` for release builds -->
    <key>signingStyle</key>
    <string>automatic</string>
</dict>
</plist>";
    }
}

#endif
