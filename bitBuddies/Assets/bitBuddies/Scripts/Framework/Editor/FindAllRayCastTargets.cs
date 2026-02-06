using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;

namespace Gameframework
{
    public class FindAllRayCastTargets : EditorWindow
    {
        static List<GameObject> displayedRefs = new List<GameObject>();
        static List<GameObject> displayedPrefabs = new List<GameObject>();

        [MenuItem("PBTools/Find Ray Cast Targets")]
        static void Init()
        {
            GetWindow<FindAllRayCastTargets>("Find Ray Cast Targets");
            EditorApplication.hierarchyChanged += hierarchyWindowChanged;
        }

        Vector2 scroll;

        void OnGUI()
        {
            var c = GUI.color;
            string title = "Refresh Active Targets";
            if (GUILayout.Button(title))
            {
                RebuildDatabase();
            }
            GUI.color = c;

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label("In Scene");
            if (GUILayout.Button("All On"))
            {
                foreach (var r in displayedRefs)
                {
                    enableRayCastOnGameObject(r, true);
                }
            }
            GUILayout.Space(10);
            if (GUILayout.Button("All Off"))
            {
                foreach (var r in displayedRefs)
                {
                    enableRayCastOnGameObject(r, false);
                }
            }

            GUILayout.EndHorizontal();

            scroll = GUILayout.BeginScrollView(scroll);
            if (displayedRefs != null)
            {
                foreach (var r in displayedRefs)
                {
                    if (r != null)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Box(GetTextureForFile(r), GUIStyle.none, GUILayout.Width(16), GUILayout.Height(16));
                        GUILayout.Label(r.name);
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            Selection.activeObject = r;
                        }

                        if (GUILayout.Button("On", GUILayout.Width(50)))
                        {
                            enableRayCastOnGameObject(r, true);
                            Selection.activeObject = r;
                        }

                        if (GUILayout.Button("Off", GUILayout.Width(50)))
                        {
                            enableRayCastOnGameObject(r, false);
                            Selection.activeObject = r;
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Prefabs");
            if (GUILayout.Button("All On"))
            {
                foreach (var r in displayedPrefabs)
                {
                    enableRayCastOnGameObject(r, true);
                }
            }
            GUILayout.Space(10);
            if (GUILayout.Button("All Off"))
            {
                foreach (var r in displayedPrefabs)
                {
                    enableRayCastOnGameObject(r, false);
                }
            }

            GUILayout.EndHorizontal();

            // prefabs
            if (displayedPrefabs != null)
            {
                foreach (var r in displayedPrefabs)
                {
                    if (r != null)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Box(GetTextureForFile(r), GUIStyle.none, GUILayout.Width(16), GUILayout.Height(16));
                        GUILayout.Label(r.name);
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            Selection.activeObject = r;
                        }

                        if (GUILayout.Button("On", GUILayout.Width(50)))
                        {
                            enableRayCastOnGameObject(r, true);
                            Selection.activeObject = r;
                        }

                        if (GUILayout.Button("Off", GUILayout.Width(50)))
                        {
                            enableRayCastOnGameObject(r, false);
                            Selection.activeObject = r;
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.EndScrollView();
        }

        private static void hierarchyWindowChanged()
        {
            RebuildDatabase();
        }

        void enableRayCastOnGameObject(GameObject in_obj, bool in_value)
        {
            Image image = in_obj.GetComponent<Image>();
            if (image) image.raycastTarget = in_value;

            Text text = in_obj.GetComponent<Text>();
            if (text) text.raycastTarget = in_value;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.SetDirty(in_obj);
        }

        Texture GetTextureForFile(GameObject in_obj)
        {
            Image image = in_obj.GetComponent<Image>();
            
            if (image) return image.raycastTarget ? (Texture)AssetDatabase.LoadAssetAtPath("Assets/Framework/Icons/IconAffirmative.png", typeof(Texture)) : (Texture)AssetDatabase.LoadAssetAtPath("Assets/Framework/Icons/IconNegative.png", typeof(Texture));

            Text text = in_obj.GetComponent<Text>();
            if (text) return text.raycastTarget ? (Texture)AssetDatabase.LoadAssetAtPath("Assets/Framework/Icons/IconAffirmative.png", typeof(Texture)) : (Texture)AssetDatabase.LoadAssetAtPath("Assets/Framework/Icons/IconNegative.png", typeof(Texture));

            return (Texture)AssetDatabase.LoadAssetAtPath("Assets/Framework/Icons/IconAffirmative.png", typeof(Texture));
        }

        static void RebuildDatabase()
        {
            DoFindTargets();
        }

        public static List<GameObject> FindResourcesByType<T>(bool in_activeInScene) where T : UnityEngine.Object
        {
            List<GameObject> assets = new List<GameObject>();

            var scene = EditorSceneManager.GetActiveScene();
            var rootObj = scene.GetRootGameObjects();

            T[] resources = Resources.FindObjectsOfTypeAll<T>();

            for (int i = 0; i < resources.Length; i++)
            {
                MonoBehaviour monoB = resources[i] as MonoBehaviour;
                if (monoB != null)
                {
                    checkRootObj(rootObj, monoB, in_activeInScene, ref assets);
                }
            }
            return assets;
        }

        static void checkRootObj(GameObject[] rootObjs, MonoBehaviour in_obj, bool in_activeInScene, ref List<GameObject> ref_assets)
        {
            if (in_activeInScene && in_obj.hideFlags != HideFlags.HideInHierarchy)
                ref_assets.Add(in_obj.gameObject);
            else if (!in_activeInScene && in_obj.hideFlags == HideFlags.HideInHierarchy)
                ref_assets.Add(in_obj.gameObject);
        }

        static void DoFindTargets()
        {
#if ENABLE_ASSET_BUNDLES
            AssetBundles.AssetBundleManager.UnloadAssetBundle("eggiesslotstate");
            AssetBundles.AssetBundleManager.UnloadAssetBundle("rapanuislotstate");
            AssetBundles.AssetBundleManager.UnloadAssetBundle("menuicons");
#endif
            displayedRefs.Clear();
            displayedPrefabs.Clear();

            Resources.UnloadUnusedAssets();

            displayedRefs.AddRange(FindResourcesByType<UnityEngine.UI.Image>(true));
            displayedRefs.AddRange(FindResourcesByType<UnityEngine.UI.Text>(true));

            displayedPrefabs.AddRange(FindResourcesByType<UnityEngine.UI.Image>(false));
            displayedPrefabs.AddRange(FindResourcesByType<UnityEngine.UI.Text>(false));


        }
    }
}
