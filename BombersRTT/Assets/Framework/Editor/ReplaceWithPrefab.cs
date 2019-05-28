using UnityEngine;
using UnityEditor;
namespace Gameframework
{
    public class ReplaceWithPrefab : EditorWindow
    {
        [SerializeField]
        private GameObject prefab;

        [MenuItem("PBTools/Replace With Prefab")]
        static void CreateReplaceWithPrefab()
        {
            EditorWindow.GetWindow<ReplaceWithPrefab>();
        }

        private void OnGUI()
        {
            prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

            if (GUILayout.Button("Replace"))
            {
                var selection = Selection.gameObjects;

                for (var i = selection.Length - 1; i >= 0; --i)
                {
                    var selected = selection[i];
#if UNITY_2018_3_OR_NEWER
                    var prefabType = PrefabUtility.GetPrefabAssetType(prefab);
#else
                    var prefabType = PrefabUtility.GetPrefabType(prefab);
#endif
                    GameObject newObject;
#if UNITY_2018_3_OR_NEWER
                    if (prefabType == PrefabAssetType.Regular)
#else
                    if (prefabType == PrefabType.Prefab)
#endif
                    {
                        newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    }
                    else
                    {
                        newObject = Instantiate(prefab);
                        newObject.name = prefab.name;
                    }

                    if (newObject == null)
                    {
                        Debug.LogError("Error instantiating prefab");
                        break;
                    }

                    Undo.RegisterCreatedObjectUndo(newObject, "Replace With Prefabs");
                    newObject.transform.parent = selected.transform.parent;
                    newObject.transform.localPosition = selected.transform.localPosition;
                    newObject.transform.localRotation = selected.transform.localRotation;
                    newObject.transform.localScale = selected.transform.localScale;
                    newObject.transform.SetSiblingIndex(selected.transform.GetSiblingIndex());
                    Undo.DestroyObjectImmediate(selected);
                }
            }

            GUI.enabled = false;
            EditorGUILayout.LabelField("Selection count: " + Selection.objects.Length);
        }
    }
}