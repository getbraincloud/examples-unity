using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Gameframework
{
    public class FindReferences : EditorWindow, ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        List<string> displayedRefs = new List<string>();

        Dictionary<string, List<string>> allDeps = new Dictionary<string, List<string>>();

        // Convert dependencies dictionary to and from a Unity serializable form
        [SerializeField]
        List<string> _keys = new List<string>();
        [SerializeField]
        List<StringList> _values = new List<StringList>();
        [System.Serializable]
        struct StringList
        {
            public List<string> list;
            public StringList(List<string> list)
            {
                this.list = list;
            }
        }

        public void OnBeforeSerialize()
        {
            if (allDeps == null)
                return;
            _keys.Clear();
            _values.Clear();
            foreach (var kvp in allDeps)
            {
                _keys.Add(kvp.Key);
                _values.Add(new StringList(kvp.Value));
            }
            EditorApplication.update -= SelectionChanged;
        }

        public void OnAfterDeserialize()
        {
            if (_keys == null || _values == null)
            {
                return;
            }
            allDeps = new Dictionary<string, List<string>>();
            for (int i = 0; i < _keys.Count; i++)
            {
                allDeps.Add(_keys[i], _values[i].list);
            }
            EditorApplication.update += SelectionChanged;
        }

        [MenuItem("PBTools/Find References")]
        static void Init()
        {
            GetWindow<FindReferences>("Find References");
        }

        Object lastSelection;

        bool isScene;
        bool sceneNotInBuild;

        void OnEnable()
        {
            if (allDeps == null)
            {
                allDeps = new Dictionary<string, List<string>>();
            }
        }

        void SelectionChanged()
        {
            if (Selection.activeObject != lastSelection)
            {
                Repaint();
                lastSelection = Selection.activeObject;
                // Check that scenes are included in the build
                var p = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (p.EndsWith(".unity"))
                {
                    isScene = true;
                    sceneNotInBuild = !EditorBuildSettings.scenes.Where(s => s.path == p).Any();
                }
                else
                {
                    isScene = false;
                    sceneNotInBuild = false;
                }

                if (allDeps != null)
                {
                    DoFind(lastSelection);
                }
            }
        }

        Vector2 scroll;

        void OnGUI()
        {
            var sel = Selection.activeObject;
            if (sel != null)
            {
                GUILayout.Label("Selected:", EditorStyles.boldLabel);
                GUILayout.Label(sel.name);
            }

            var c = GUI.color;
            if (!HaveDeps())
            {
                GUI.color = Color.yellow;
            }
            string title = (!HaveDeps()) ? "Rebuild Database (Required)" : "Rebuild Database";
            if (GUILayout.Button(title))
            {
                RebuildDatabase();
            }
            GUI.color = c;

            GUILayout.Space(10);

            if (isScene)
            {
                if (sceneNotInBuild)
                {
                    c = GUI.color;
                    GUI.color = Color.yellow;
                    GUILayout.Label("Scene is <b>not</b> included in build");
                    GUI.color = c;
                }
                else
                {
                    c = GUI.color;
                    GUI.color = Color.cyan;
                    GUILayout.Label("Scene is included in build");
                    GUI.color = c;
                }
            }

            scroll = GUILayout.BeginScrollView(scroll);
            if (displayedRefs != null)
            {
                foreach (var r in displayedRefs)
                {
                    GUILayout.BeginHorizontal();
                    var fileName = System.IO.Path.GetFileName(r);
                    GUILayout.Box(GetTextureForFile(r), GUIStyle.none, GUILayout.Width(16), GUILayout.Height(16));
                    GUILayout.Label(fileName);
                    if (GUILayout.Button("Select", GUILayout.Width(50)))
                    {
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath(r, typeof(object));
                    }
                    if (GUILayout.Button("Open", GUILayout.Width(50)))
                    {
                        EditorUtility.OpenWithDefaultApp(r);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
        }

        Texture2D GetTextureForFile(string fileName)
        {
            //		var asset = AssetDatabase.LoadMainAssetAtPath (fileName);
            //		var image = AssetPreview.GetMiniThumbnail (asset);
            //		return image;

            int num = fileName.LastIndexOf('.');
            string text = (num != -1) ? fileName.Substring(num + 1).ToLower() : string.Empty;
            string text2 = text;
            switch (text2)
            {
                case "boo":
                    return EditorGUIUtility.FindTexture("boo Script Icon");
                case "cginc":
                    return EditorGUIUtility.FindTexture("CGProgram Icon");
                case "cs":
                    return EditorGUIUtility.FindTexture("cs Script Icon");
                case "guiskin":
                    return EditorGUIUtility.FindTexture("GUISkin Icon");
                case "js":
                    return EditorGUIUtility.FindTexture("Js Script Icon");
                case "mat":
                    return EditorGUIUtility.FindTexture("Material Icon");
                case "prefab":
                    return EditorGUIUtility.FindTexture("PrefabNormal Icon");
                case "shader":
                    return EditorGUIUtility.FindTexture("Shader Icon");
                case "txt":
                    return EditorGUIUtility.FindTexture("TextAsset Icon");
                case "unity":
                    return EditorGUIUtility.FindTexture("SceneAsset Icon");
                case "asset":
                case "prefs":
                    return EditorGUIUtility.FindTexture("GameManager Icon");
                case "anim":
                    return EditorGUIUtility.FindTexture("Animation Icon");
                case "meta":
                    return EditorGUIUtility.FindTexture("MetaFile Icon");
                case "ttf":
                case "otf":
                case "fon":
                case "fnt":
                    return EditorGUIUtility.FindTexture("Font Icon");
                case "aac":
                case "aif":
                case "aiff":
                case "au":
                case "mid":
                case "midi":
                case "mp3":
                case "mpa":
                case "ra":
                case "ram":
                case "wma":
                case "wav":
                case "wave":
                case "ogg":
                    return EditorGUIUtility.FindTexture("AudioClip Icon");
                case "ai":
                case "apng":
                case "png":
                case "bmp":
                case "cdr":
                case "dib":
                case "eps":
                case "exif":
                case "gif":
                case "ico":
                case "icon":
                case "j":
                case "j2c":
                case "j2k":
                case "jas":
                case "jiff":
                case "jng":
                case "jp2":
                case "jpc":
                case "jpe":
                case "jpeg":
                case "jpf":
                case "jpg":
                case "jpw":
                case "jpx":
                case "jtf":
                case "mac":
                case "omf":
                case "qif":
                case "qti":
                case "qtif":
                case "tex":
                case "tfw":
                case "tga":
                case "tif":
                case "tiff":
                case "wmf":
                case "psd":
                case "exr":
                    return EditorGUIUtility.FindTexture("Texture Icon");
                case "3df":
                case "3dm":
                case "3dmf":
                case "3ds":
                case "3dv":
                case "3dx":
                case "blend":
                case "c4d":
                case "lwo":
                case "lws":
                case "ma":
                case "max":
                case "mb":
                case "mesh":
                case "obj":
                case "vrl":
                case "wrl":
                case "wrz":
                case "fbx":
                    return EditorGUIUtility.FindTexture("Mesh Icon");
                case "asf":
                case "asx":
                case "avi":
                case "dat":
                case "divx":
                case "dvx":
                case "mlv":
                case "m2l":
                case "m2t":
                case "m2ts":
                case "m2v":
                case "m4e":
                case "m4v":
                case "mjp":
                case "mov":
                case "movie":
                case "mp21":
                case "mp4":
                case "mpe":
                case "mpeg":
                case "mpg":
                case "mpv2":
                case "ogm":
                case "qt":
                case "rm":
                case "rmvb":
                case "wmw":
                case "xvid":
                    return EditorGUIUtility.FindTexture("MovieTexture Icon");
                case "colors":
                case "gradients":
                case "curves":
                case "curvesnormalized":
                case "particlecurves":
                case "particlecurvessigned":
                case "particledoublecurves":
                case "particledoublecurvessigned":
                    return EditorGUIUtility.FindTexture("ScriptableObject Icon");
            }
            return EditorGUIUtility.FindTexture("DefaultAsset Icon");
        }

        bool HaveDeps()
        {
            if (allDeps == null)
            {
                return false;
            }
            if (allDeps.Count == 0)
            {
                return false;
            }
            return true;
        }

        void RebuildDatabase()
        {
            var allAssetPaths = AssetDatabase.GetAllAssetPaths();

            // Map of Dependency -> Assets with the dependency
            allDeps = new Dictionary<string, List<string>>();

            // Build dependency mapping for all assets in the project
            for (int p = 0; p < allAssetPaths.Length; p++)
            {

                if (p % 20 == 0)
                {
                    var cancel = EditorUtility.DisplayCancelableProgressBar("Building Dependency Database", allAssetPaths[p], (float)p / allAssetPaths.Length);
                    if (cancel)
                    {
                        allDeps = null;
                        break;
                    }
                }

                var currentAsset = allAssetPaths[p];

                // Get dependencies for the current asset we're looping over
                var depsForCurrentAsset = AssetDatabase.GetDependencies(new string[] { currentAsset });

                for (int d = 0; d < depsForCurrentAsset.Length; d++)
                {

                    var currentDependency = depsForCurrentAsset[d];

                    // Flip it around, instead of storing asset -> list of dependencies for asset,
                    // Store dependency -> list of assets relying upon the dependency

                    List<string> assetsUsingDependency;

                    // Get a list of all assets that rely on the dependency
                    var entryExists = allDeps.TryGetValue(currentDependency, out assetsUsingDependency);
                    if (!entryExists)
                    {
                        allDeps[currentDependency] = new List<string>();
                    }
                    // Don't self-reference
                    if (currentDependency != currentAsset)
                    {
                        allDeps[currentDependency].Add(currentAsset);
                    }
                }
            }

            EditorUtility.ClearProgressBar();
        }

        void DoFind(Object obj)
        {
            if (!HaveDeps())
            {
                return;
            }

            var searchedAssetPath = AssetDatabase.GetAssetPath(obj);

            // Look up the assets that depend upon the searched asset
            List<string> found;
            allDeps.TryGetValue(searchedAssetPath, out found);
            if (found != null)
            {
                displayedRefs = found;
            }
            else
            {
                displayedRefs.Clear();
            }
        }
    }
#endif

}
