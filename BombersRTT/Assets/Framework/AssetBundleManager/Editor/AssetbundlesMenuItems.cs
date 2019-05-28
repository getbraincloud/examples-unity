using UnityEditor;
using UnityEngine;

namespace AssetBundles
{
    public class AssetBundlesMenuItems
    {
        const string kSimulationMode = "AssetBundles/Simulation Mode";

        [MenuItem(kSimulationMode)]
        public static void ToggleSimulationMode()
        {
            AssetBundleManager.SimulateAssetBundleInEditor = !AssetBundleManager.SimulateAssetBundleInEditor;
        }

        [MenuItem(kSimulationMode, true)]
        public static bool ToggleSimulationModeValidate()
        {
            Menu.SetChecked(kSimulationMode, AssetBundleManager.SimulateAssetBundleInEditor);
            return true;
        }

        [MenuItem("AssetBundles/Build AssetBundles")]
        static public void BuildAssetBundles()
        {
            BuildScript.BuildAssetBundles(EditorUserBuildSettings.activeBuildTarget);
        }

        [MenuItem("AssetBundles/Rebuild Build AssetBundles")]
        static public void RebuildBuildAssetBundles()
        {
            BuildScript.RebuildBuildAssetBundles(EditorUserBuildSettings.activeBuildTarget);
        }

        [MenuItem("AssetBundles/Clear Local Asset Bundles")]
        static public void ClearAssetBundles()
        {
            Caching.ClearCache();
        }

        [MenuItem("AssetBundles/Build Player (for use with engine code stripping)")]
        static public void BuildPlayer()
        {
            BuildScript.BuildPlayer();
        }

        [MenuItem("AssetBundles/Build All Asset Bundles")]
        static public void BuildPlayerAllAssetBundles()
        {
            Build.BuildAllActiveAssetBundles();
        }
    }
}
