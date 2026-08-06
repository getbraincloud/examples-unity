using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using static UnityEngine.GUILayout;
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("PrimeTween.Internal")]

namespace PrimeTween {
    internal class PrimeTweenInstaller : ScriptableObject {
        [SerializeField] internal SceneAsset demoScene;
        [SerializeField] internal SceneAsset demoSceneUrp;
        [SerializeField] internal SceneAsset demoScenePro;
        [SerializeField] internal SceneAsset demoSceneProUrp;
        [SerializeField] internal Color uninstallButtonColor;

        [ContextMenu(nameof(ResetReviewRequest))] void ResetReviewRequest() => ReviewRequest.ResetReviewRequest();
        [ContextMenu(nameof(DebugReviewRequest))] void DebugReviewRequest() => ReviewRequest.DebugReviewRequest();
    }

    [CustomEditor(typeof(PrimeTweenInstaller), false)]
    internal class InstallerInspector : Editor {
        internal const string pluginName = "PrimeTween";
        internal const string pluginPackageId = "com.kyrylokuzyk.primetween";
        internal const string packagesPath = "Packages/com.kyrylokuzyk.primetween.tgz";
        const string assetsPath = "Assets/Plugins/PrimeTween/internal/com.kyrylokuzyk.primetween.tgz";
        internal const string newAssetsPath = "Assets/Plugins/PrimeTween/internal/com.kyrylokuzyk.primetween-" + version + ".tgz";
        const string documentationUrl = "https://github.com/KyryloKuzyk/PrimeTween";
        bool isInstalled;
        bool hasNewTgz;
        GUIStyle boldButtonStyle;
        GUIStyle uninstallButtonStyle;
        GUIStyle wordWrapLabelStyle;

        void OnEnable() {
            isInstalled = CheckPluginInstalled();
            hasNewTgz = File.Exists(newAssetsPath);
        }

        /// Use Package Manager because Unity 2018 doesn't support version defines
        static bool CheckPluginInstalled() {
            var listRequest = Client.List(true);
            while (!listRequest.IsCompleted) {
            }
            return listRequest.Result.Any(_ => _.name == pluginPackageId);
        }

        public override void OnInspectorGUI() {
            if (boldButtonStyle == null) {
                boldButtonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            }
            var installer = (PrimeTweenInstaller)target;
            if (uninstallButtonStyle == null) {
                uninstallButtonStyle = new GUIStyle(GUI.skin.button) { normal = { textColor = installer.uninstallButtonColor } };
            }
            if (wordWrapLabelStyle == null) {
                wordWrapLabelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = true, margin = new RectOffset(4, 4, 8, 8) };
            }
            EditorGUI.indentLevel = 5;
            Space(8);
            Label(pluginName, EditorStyles.boldLabel);
            Space(4);
            if (!isInstalled) {
                if (Button("Install " + pluginName)) {
                    installPlugin();
                }
                return;
            }
            if (hasNewTgz) {
                if (Button($"Update to {version}", boldButtonStyle)) {
                    ReviewRequest.OnPackageUpdate();
                    hasNewTgz = false;
                }
                Space(8);
            }
            if (Button("Documentation", boldButtonStyle)) {
                Application.OpenURL(documentationUrl);
            }

            Space(8);
            var rpAsset = GraphicsSettings.
                #if UNITY_2019_3_OR_NEWER
                defaultRenderPipeline;
                #else
                renderPipelineAsset;
                #endif
            bool isUrp = rpAsset != null && rpAsset.GetType().Name.Contains("Universal");
            if (Button("Open Demo", boldButtonStyle)) {
                var demoScene = isUrp ? installer.demoSceneUrp : installer.demoScene;
                if (demoScene == null) {
                    Debug.LogError("Please re-import the plugin from Asset Store and import the 'Demo' folder.\n");
                    return;
                }
                string path = AssetDatabase.GetAssetPath(demoScene);
                EditorSceneManager.OpenScene(path);
            }
            #if PRIME_TWEEN_PRO
            if (Button("Open Demo (Pro)", boldButtonStyle)) {
                var demoScene = isUrp ? installer.demoSceneProUrp : installer.demoScenePro;
                if (demoScene == null) {
                    Debug.LogError("Please re-import the plugin from Asset Store and import the 'Demo' folder.\n");
                    return;
                }
                string path = AssetDatabase.GetAssetPath(demoScene);
                EditorSceneManager.OpenScene(path);
            }
            #endif
            #if UNITY_2019_4_OR_NEWER
            if (Button("Import Basic Examples")) {
                EditorUtility.DisplayDialog(pluginName, $"Please select the '{pluginName}' package in 'Package Manager', then press the 'Samples/Import' button at the bottom of the plugin's description.", "Ok");
                UnityEditor.PackageManager.UI.Window.Open(pluginPackageId);
            }
            #endif
            if (Button("Support")) {
                Application.OpenURL("https://github.com/KyryloKuzyk/PrimeTween#support");
            }

            Space(8);
            if (Button("Uninstall", uninstallButtonStyle)) {
                MoveToAssets();
                Client.Remove(pluginPackageId);
                isInstalled = false;
                string msg = $"Please remove the folder manually to uninstall {pluginName} completely: 'Assets/Plugins/{pluginName}'";
                EditorUtility.DisplayDialog(pluginName, msg, "Ok");
                Debug.Log(msg);
            }

            if (EditorPrefs.GetBool(InsertCallbackBug.showInsertCallbackBugUi, false)) {
                Space(24);
                Label("Updating from PrimeTween [1.1.10 - 1.1.22]", EditorStyles.boldLabel);
                Label("The behaviour of 'Sequence.ChainCallback()' and 'InsertCallback()' was fixed in PrimeTween 1.2.0 so the code written with older versions may work differently in some cases.", wordWrapLabelStyle);
                if (Button("Find potential issues")) {
                    InsertCallbackBug.Find();
                }
                BeginHorizontal();
                if (Button("More info")) {
                    Application.OpenURL(InsertCallbackBug.moreInfoUrl);
                }
                if (Button("Download version 1.1.22")) {
                    Application.OpenURL("https://github.com/KyryloKuzyk/PrimeTween/blob/545dcc52769d52841e282c772e98c8984bfeb243/Benchmarks/Packages/com.kyrylokuzyk.primetween.tgz");
                }
                EndHorizontal();
            }

            Space(24);
            Label("Enjoying PrimeTween?", EditorStyles.boldLabel);
            Label("Consider leaving an <b>honest review</b> and starring PrimeTween on GitHub!\n\n" +
                  "Honest reviews make PrimeTween better and help other developers discover it.", wordWrapLabelStyle);
            if (Button("Leave review!", GUI.skin.button)) {
                ReviewRequest.DisableReviewRequest();
                ReviewRequest.OpenReviewsURL();
            }
        }

        static void installPlugin() {
            if (!MoveToPackages()) {
                return;
            }
            ReviewRequest.OnBeforeInstall();
            var path = $"file:../{packagesPath}";
            var addRequest = Client.Add(path);
            while (!addRequest.IsCompleted) {
            }
            if (addRequest.Status == StatusCode.Success) {
                Debug.Log($"{pluginName} {version} installed successfully.\n" +
                          $"Offline documentation is located at Packages/{pluginName}/Documentation.md.\n" +
                          $"Online documentation: {documentationUrl}\n");
            } else {
                Debug.LogError($"Please re-import the plugin from the Asset Store and check that the file exists: [{path}].\n\n{addRequest.Error?.message}\n");
            }
        }

        internal static bool MoveToPackages() {
            string origPath;
            if (File.Exists(newAssetsPath)) {
                origPath = newAssetsPath;

                // delete the old file from before the Unity 6.5.5 fix
                File.Delete(assetsPath);
                File.Delete(assetsPath + ".meta");
            } else if (File.Exists(assetsPath)) {
                origPath = assetsPath;
            } else {
                Debug.LogError($"The installation archive is missing: '{newAssetsPath}'. Please re-import PrimeTween from Asset Store.");
                return false;
            }
            File.Delete(packagesPath);
            File.Move(origPath, packagesPath);
            File.Delete(origPath + ".meta");
            return true;
        }

        static void MoveToAssets() {
            if (File.Exists(packagesPath)) {
                File.Delete(assetsPath);
                File.Move(packagesPath, assetsPath);
                File.WriteAllText(assetsPath + ".meta", @"fileFormatVersion: 2
guid: cdd0c4b9889044d73bc958a922ada300
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
");
            }
        }

        [InitializeOnLoadMethod]
        static void InitOnLoad() {
            AssetDatabase.importPackageCompleted += name => {
                if (name.Contains(pluginName)) {
                    ReviewRequest.log($"importPackageCompleted {pluginName}");
                    if (CheckPluginInstalled()) {
                        ReviewRequest.OnPackageUpdate();
                    } else {
                        var installer = AssetDatabase.LoadAssetAtPath<PrimeTweenInstaller>("Assets/Plugins/PrimeTween/PrimeTweenInstaller.asset");
                        EditorUtility.FocusProjectWindow(); // this is important to show the installer object in the Project window
                        Selection.activeObject = installer;
                        EditorGUIUtility.PingObject(installer);
                        EditorApplication.update += InstallAndUnsubscribeFromUpdate;
                        void InstallAndUnsubscribeFromUpdate() {
                            EditorApplication.update -= InstallAndUnsubscribeFromUpdate;
                            installPlugin();
                        }
                    }
                }
            };
        }

        internal const string version = "1.4.12";
    }

    internal static class FixedUpdateParameterMigration {
        internal static string[] FindLocalScriptGuids() {
            var listRequest = Client.List(true);
            while (!listRequest.IsCompleted) {
            }
            Assert.AreEqual(StatusCode.Success, listRequest.Status);
            string[] folders = listRequest.Result
                .Where(x => x.source == PackageSource.Embedded || x.source == PackageSource.Local)
                .Where(x => x.name != InstallerInspector.pluginPackageId)
                .Select(x => x.assetPath)
                .Append("Assets")
                .ToArray();
            return AssetDatabase.FindAssets("t:Script", folders);
        }

        internal static bool Process(string[] scripts, bool? fixAutomatically = null) {
            var logSb = new StringBuilder();
            var fileSb = new StringBuilder();
            foreach (string guid in scripts)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                string text = textAsset.text;
                if (!Regex.IsMatch(text, @"using PrimeTween\s*;")) {
                    continue;
                }
                var parameterMatches = Regex.Matches(text, @"useFixedUpdate\s*:");
                if (parameterMatches.Count > 0) {
                    if (!fixAutomatically.HasValue) {
                        return true;
                    }
                    logSb.Clear();
                    if (fixAutomatically.Value) {
                        fileSb.Clear();
                        fileSb.Append(text);
                        for (int i = parameterMatches.Count - 1; i >= 0; i--) {
                            var paramMatch = parameterMatches[i];
                            fileSb.Remove(paramMatch.Index, paramMatch.Length);
                            fileSb.Insert(paramMatch.Index, "updateType:");
                        }
                        File.WriteAllText(path, fileSb.ToString());
                        logSb.Append($"PrimeTween automatically renamed ({parameterMatches.Count}) occurrences of 'useFixedUpdate' to 'updateType' in file '{textAsset.name}.cs':\n");
                    } else {
                        logSb.Append($"PrimeTween: please rename ({parameterMatches.Count}) occurrences of 'useFixedUpdate' to 'updateType' in file '{textAsset.name}.cs':\n");
                    }
                    var lineMatches = Regex.Matches(text, @"(?m)^.*useFixedUpdate\s*:.*$", RegexOptions.Multiline);
                    foreach (Match match in lineMatches) {
                        logSb.Append($"{match.Value.Trim().Replace("useFixedUpdate", "<b>useFixedUpdate</b>")}\n");
                    }
                    Debug.unityLogger.Log(fixAutomatically.Value ? LogType.Warning : LogType.Error, logSb.ToString());
                }
            }
            return false;
        }
    }

    internal static class ReviewRequest {
        const string version = InstallerInspector.version;
        const string canAskKey = "PrimeTween.canAskForReview";
        const string versionKey = "PrimeTween.version";

        internal static void OnPackageUpdate() {
            log("OnPackageUpdate");
            if (!File.Exists(InstallerInspector.newAssetsPath)) {
                Debug.LogError($"The installation archive is missing: '{InstallerInspector.newAssetsPath}'. Please re-import PrimeTween from Asset Store.");
                return;
            }
            bool shouldAskForReview = true;
            string[] scriptGuids = FixedUpdateParameterMigration.FindLocalScriptGuids();
            if (FixedUpdateParameterMigration.Process(scriptGuids)) {
                shouldAskForReview = false;
                const string msg = "'bool useFixedUpdate' parameter was changed to 'UpdateType updateType' in version 1.3.0, which will cause breaking changes in your current project.\n" +
                                   "PrimeTween can fix the breaking changes automatically, or you can fix them manually after the update.\n";
                Debug.LogWarning($"PrimeTween: the {msg}");
                int response = EditorUtility.DisplayDialogComplex($"{InstallerInspector.pluginName} {version}",
                    $"The {msg}",
                    "Fix automatically",
                    "Cancel",
                    "Fix manually");
                string cancelMessage = $"PrimeTween: update to {version} was cancelled. You can trigger update manually from 'Assets/Plugins/PrimeTween/PrimeTweenInstaller'.";
                if (response == 1) {
                    Debug.LogWarning(cancelMessage);
                    return;
                }
                if (!EditorUtility.DisplayDialog($"{InstallerInspector.pluginName} {version}", "Please back up your project before proceeding.", "OK", "Cancel")) {
                    Debug.LogWarning(cancelMessage);
                    return;
                }
                bool fixAutomatically = response == 0;
                FixedUpdateParameterMigration.Process(scriptGuids, fixAutomatically);
            }
            if (!InstallerInspector.MoveToPackages()) {
                return;
            }
            if (UNITY_2018) {
                var removeRequest = Client.Remove(InstallerInspector.pluginPackageId);
                while (!removeRequest.IsCompleted) {
                }
                string path = $"file:../{InstallerInspector.packagesPath}";
                Client.Add(path);
            } else {
                Client.Add($"file:../{InstallerInspector.packagesPath}");
                EditorApplication.ExecuteMenuItem("Assets/Refresh"); // AssetDatabase.Refresh() refreshes the project only partially
            }

            string prevVersion = savedVersion;
            if (savedVersion == version) {
                log($"same version {version}");
                return;
            }
            savedVersion = version;

            if (InsertCallbackBug.IsUpdatingFromVersionWithBug(prevVersion)) {
                InsertCallbackBug.Find();
                EditorPrefs.SetBool(InsertCallbackBug.showInsertCallbackBugUi, true);
                shouldAskForReview = false;
            } else {
                EditorPrefs.SetBool(InsertCallbackBug.showInsertCallbackBugUi, false);
            }
            log($"updated from version {prevVersion} to {version}, {nameof(shouldAskForReview)}: {shouldAskForReview}");
            if (shouldAskForReview) {
                TryAskForReview();
            }
        }

        static bool UNITY_2018 {
            get {
                #if UNITY_2018
                    return true;
                #else
                    return false;
                #endif
            }
        }

        static void TryAskForReview() {
            if (!EditorPrefs.GetBool(canAskKey, true)) {
                log("can't ask");
                return;
            }
            DisableReviewRequest();
            var response = EditorUtility.DisplayDialogComplex("Enjoying PrimeTween?",
                "Would you mind to leave an honest review on Asset store? Honest reviews make PrimeTween better and help other developers discover it.",
                "Sure, leave a review!",
                "Never ask again",
                "");
            if (response == 0) {
                OpenReviewsURL();
            }
        }

        internal static void OnBeforeInstall() {
            log($"OnBeforeInstall {version}");
            if (string.IsNullOrEmpty(savedVersion)) {
                savedVersion = version;
            }
        }

        static string savedVersion {
            get => EditorPrefs.GetString(versionKey);
            set => EditorPrefs.SetString(versionKey, value);
        }

        internal static void DisableReviewRequest() => EditorPrefs.SetBool(canAskKey, false);
        internal static void OpenReviewsURL() => Application.OpenURL("https://assetstore.unity.com/packages/slug/252960#reviews");

        internal static void ResetReviewRequest() {
            Debug.Log(nameof(ResetReviewRequest));
            EditorPrefs.DeleteKey(versionKey);
            EditorPrefs.DeleteKey(canAskKey);
        }

        internal static void DebugReviewRequest() {
            Debug.Log(nameof(DebugReviewRequest));
            savedVersion = "1.1.22";
            EditorPrefs.SetBool(canAskKey, false);
            // TryAskForReview();
        }

        [System.Diagnostics.Conditional("_")]
        internal static void log(string msg) {
            Debug.Log($"ReviewRequest: {msg}");
        }
    }

    internal static class InsertCallbackBug {
        internal const string moreInfoUrl = "https://github.com/KyryloKuzyk/PrimeTween/discussions/112";
        internal const string showInsertCallbackBugUi = "PrimeTween.showInsertCallbackBugUi";
        static Dictionary<short, OpCode> OpCodeDict;
        static MethodInfo[] methodsWithBug;
        static MethodInfo[] groupMethods;

        internal static bool IsUpdatingFromVersionWithBug(string prevVersionString) {
            if (Version.TryParse(prevVersionString, out var prevVersion)
                && new Version(1, 1, 10) <= prevVersion
                && prevVersion <= new Version(1, 1, 22)
                ) {
                return true;
            }
            return false;
        }

        internal static void Find() {
            OpCodeDict = typeof(OpCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(x => (OpCode)x.GetValue(null))
                .ToDictionary(x => x.Value, x => x);
            #if PRIME_TWEEN_INSTALLED
            methodsWithBug = typeof(Sequence).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(methodInfo => methodInfo.Name == nameof(Sequence.ChainCallback) || methodInfo.Name == nameof(Sequence.InsertCallback))
                .Select(methodInfo => methodInfo.IsGenericMethod ? methodInfo.GetGenericMethodDefinition() : methodInfo)
                .ToArray();
            Assert.AreEqual(4, methodsWithBug.Length);
            groupMethods = typeof(Sequence).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(methodInfo => methodInfo.Name == nameof(Sequence.Group))
                .ToArray();
            #endif
            Assert.AreEqual(2, groupMethods.Length);

            string methodAssemblyName = methodsWithBug[0].Module.Assembly.FullName;
            const BindingFlags findAll = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            int numPotentialIssues =
                #if UNITY_6000_7_OR_NEWER
                UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies()
                #else
                AppDomain.CurrentDomain.GetAssemblies()
                #endif
                .Where(assembly => assembly.GetReferencedAssemblies().Any(dependency => dependency.FullName == methodAssemblyName))
                .Where(assembly => !assembly.GetName().Name.StartsWith("PrimeTween.", StringComparison.Ordinal))
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(findAll).Cast<MethodBase>().Union(type.GetConstructors(findAll)))
                .Count(method => FindInMethod(method));
            if (numPotentialIssues == 0) {
                Debug.Log($"PrimeTween updated to version {InstallerInspector.version}: no potential issues found in ChainCallback() and InsertCallback() usages.\n" +
                          $"More info: {moreInfoUrl}\n");
            }

            int updateResponse = EditorUtility.DisplayDialogComplex($"{InstallerInspector.pluginName} {InstallerInspector.version}",
                "PrimeTween 1.2.0 fixed a bug in ChainCallback() and InsertCallback() methods.\n" +
                "This fix may introduce breaking changes in the existing projects. Please see the Console output for more details.",
                "More info",
                "Close",
                "");
            if (updateResponse == 0) {
                Application.OpenURL(moreInfoUrl);
            }
        }

        /// https://stackoverflow.com/a/33034906/1951038
        static bool FindInMethod(MethodBase method) {
            byte[] il = method.GetMethodBody()?.GetILAsByteArray();
            if (il == null) {
                return false;
            }
            bool bugFound = false;
            using (var br = new BinaryReader(new MemoryStream(il))) {
                while (br.BaseStream.Position < br.BaseStream.Length) {
                    byte firstByte = br.ReadByte();
                    short opCodeValue = firstByte == 0xFE ? BitConverter.ToInt16(new[] { br.ReadByte(), firstByte }, 0) : firstByte;
                    OpCode opCode = OpCodeDict[opCodeValue];
                    switch (opCode.OperandType) {
                        case OperandType.ShortInlineBrTarget:
                        case OperandType.ShortInlineVar:
                        case OperandType.ShortInlineI:
                            br.ReadByte();
                            break;
                        case OperandType.InlineVar:
                            br.ReadInt16();
                            break;
                        case OperandType.InlineField:
                        case OperandType.InlineType:
                        case OperandType.ShortInlineR:
                        case OperandType.InlineString:
                        case OperandType.InlineSig:
                        case OperandType.InlineI:
                        case OperandType.InlineBrTarget:
                            br.ReadInt32();
                            break;
                        case OperandType.InlineI8:
                        case OperandType.InlineR:
                            br.ReadInt64();
                            break;
                        case OperandType.InlineSwitch:
                            var size = (int)br.ReadUInt32();
                            br.ReadBytes(size * 4);
                            break;
                        case OperandType.InlineTok:
                            br.ReadUInt32();
                            break;
                        case OperandType.InlineMethod:
                            int token = (int)br.ReadUInt32();
                            if (method.Module.ResolveMethod(token) is MethodInfo resolvedMethod) {
                                if (bugFound) {
                                    if (groupMethods.Contains(resolvedMethod)) {
                                        Debug.LogError($"PrimeTween updated to version {InstallerInspector.version}: potential breaking change found in the '{method.DeclaringType}.{method.Name}()' method.\n" +
                                                       "Please double-check the behavior if Group() is called immediately after the ChainCallback() or InsertCallback() and apply the fix manually if necessary.\n" +
                                                       "Or use ChainCallbackObsolete/InsertCallbackObsolete() instead to preserve the old incorrect behavior.\n" +
                                                       $"More info: {moreInfoUrl}\n");
                                        return true;
                                    }
                                } else {
                                    bugFound = isMethodWithBug(resolvedMethod);
                                }
                            }
                            break;
                        case OperandType.InlineNone:
                            break;
                        default:
                            throw new Exception();
                    }
                }
            }
            return false;
        }

        static bool isMethodWithBug(MethodInfo method) {
            foreach (var methodWithBug in methodsWithBug) {
                if (methodWithBug.IsGenericMethodDefinition && method.IsGenericMethod) {
                    if (methodWithBug == method.GetGenericMethodDefinition()) {
                        return true;
                    }
                } else if (methodWithBug == method) {
                    return true;
                }
            }
            return false;
        }
    }
}
