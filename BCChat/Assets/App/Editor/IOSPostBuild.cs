#if UNITY_IOS && APPLE_SDK

using System;
using System.Diagnostics;
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
        if (target != BuildTarget.iOS)
        {
            Debug.LogError("IOSPostBuild should only be run on iOS builds.");
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
            manager.AddSignInWithAppleWithCompatibility(project.GetUnityFrameworkTargetGuid());
            manager.WriteToFile();

            Debug.Log("Added ProjectCapabilityManager to Xcode Project.");

            // Build and Export to .ipa
            CreateIpaFromXcodeProject(pathToBuiltProject);
        }
        catch (Exception e)
        {
            Debug.LogError("Error in iOS post-build process: " + e);
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
        RunShellCommand("xcodebuild", $"-project {xcodeProjectPath}/Unity-iPhone.xcodeproj -scheme Unity-iPhone -archivePath {archivePath} archive");

        // Run xcodebuild export to create .ipa
        RunShellCommand("xcodebuild", $"-exportArchive -archivePath {archivePath} -exportPath {exportPath} -exportOptionsPlist {exportOptionsPlist}");

        Debug.Log("iOS build and .ipa creation complete.");
    }

    private static void RunShellCommand(string command, string args)
    {
        Process process = new Process();
        process.StartInfo.FileName = command;
        process.StartInfo.Arguments = args;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.OutputDataReceived += (sender, e) => Debug.Log(e.Data);
        process.ErrorDataReceived += (sender, e) => Debug.LogError(e.Data);

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
        // Adjust settings as needed (e.g., "development" or "app-store" for method)
        return @"
<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>method</key>
    <string>development</string>
    <key>teamID</key>
    <string>YOUR_TEAM_ID</string>
    <key>signingStyle</key>
    <string>manual</string>
    <key>provisioningProfiles</key>
    <dict>
        <key>YOUR_BUNDLE_ID</key>
        <string>YOUR_PROVISIONING_PROFILE</string>
    </dict>
</dict>
</plist>";
    }
}

#endif
