using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class BuildVersionProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Version newVersion = FindCurrentVersion();
        newVersion.IncrementPatch();
        PlayerSettings.bundleVersion = newVersion.ConvertToString();
    }

    private Version FindCurrentVersion()
    {
        Version currentVersion = new Version(PlayerSettings.bundleVersion);
        return currentVersion;
    }
}

public struct Version
{
    public int major;
    public int minor;
    public int patch;
    public string extra;

    public Version(string newExtra, int newMajor, int newMinor = 0, int newPatch = 0)
    {
        major = newMajor;
        minor = newMinor;
        patch = newPatch;
        extra = newExtra;
    }

    public Version(string versionString)
    {
        string[] dividedVersion = PlayerSettings.bundleVersion.Split('.', ':');
        major = int.Parse(dividedVersion[0]);
        minor = int.Parse(dividedVersion[1]);
        patch = int.Parse(dividedVersion[2]);
        extra = (dividedVersion.Length > 3) ? dividedVersion[3] : "";
    }

    public string ConvertToString()
    {
        string output = string.Concat(major, ".", minor, ".", patch);
        if (extra != "") output += ":" + extra;
        return output;
    }

    public void IncrementPatch()
    {
        patch += 1;
    }
}
