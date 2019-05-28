using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using AssetBundles;

namespace Gameframework
{
    public class GEntityFactory : SingletonBehaviour<GEntityFactory>
    {

        public override void Awake()
        {
            base.Awake();
        }
        #region Public
        public AudioClip CreateAudioResourceAtPath(string in_path, Transform in_parent = null, bool in_overridePos = true)
        {
            Object tempToReturn = GetResourceAtPath(in_path);
            AudioClip toReturn = null;
            if (tempToReturn != null)
            {
                toReturn = Instantiate(tempToReturn, in_parent) as AudioClip;
            }

            return toReturn;
        }

        public GameObject CreateResourceAtPath(string in_path, Transform in_parent = null, bool in_overridePos = true)
        {
            Object tempToReturn = GetResourceAtPath(in_path);
            GameObject toReturn = null;
            if (tempToReturn != null)
            {
                toReturn = Instantiate(tempToReturn, in_parent) as GameObject;
                if (toReturn != null && in_overridePos) toReturn.transform.localPosition = Vector3.zero;
            }

            return toReturn;
        }

        public GameObject CreateObject(GameObject template, Vector3 position, Quaternion rotation, Transform parent)
        {
            GameObject obj;

            if (parent)
                obj = Instantiate(template, position, rotation, parent);
            else
                obj = Instantiate(template, position, rotation);

            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = rotation;

            return obj;
        }

        public IEnumerator LoadObjectFromAssetBundle<T>(string assetBundleName, string assetName) where T : UnityEngine.Object
        {
            if (GetObjectFromAssetBundle<T>(assetBundleName, assetName) == default(T))
            {
                // Load asset from assetBundle.
                AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(assetBundleName, assetName, typeof(T));

                if (request == null)
                    yield break;

                yield return StartCoroutine(request);

                // get and store the asset
                string key = assetBundleName + assetName;
                T tempTemplate = request.GetAsset<T>();
                if (!m_loadedAssetBundleResourceMap.ContainsKey(key))
                {
                    m_loadedAssetBundleResourceMap.Add(key, tempTemplate);
                }
                else
                {
                    // overwrite it!
                    m_loadedAssetBundleResourceMap[key] = tempTemplate;
                }
            }

            yield return null;
        }

        public T GetObjectFromAssetBundle<T>(string assetBundleName, string assetName) where T : UnityEngine.Object
        {
            string key = assetBundleName + assetName;
            if (m_loadedAssetBundleResourceMap.ContainsKey(key))
            {
                try
                {
                    return (T)m_loadedAssetBundleResourceMap[key];
                }
                catch (System.InvalidCastException)
                {
                    return default(T);
                }
            }
            else
            {
                return default(T);
            }
        }

        public IEnumerator LoadSpriteFromAssetBundle(string assetBundleName, string assetName)
        {
            if (GetObjectFromAssetBundle<Sprite>(assetBundleName, assetName.Replace("*", "")) == default(Sprite) &&
                GetObjectFromAssetBundle<Texture2D>(assetBundleName, assetName.Replace("*", "")) == default(Texture2D))
            {
                // Load asset from assetBundle.
                AssetBundleLoadAssetOperation request;
#if UNITY_EDITOR
                if (!AssetBundleManager.SimulateAssetBundleInEditor)
#endif
                {
                    request = AssetBundleManager.LoadAssetAsync(assetBundleName, assetName.Replace("*", ""), typeof(Sprite));
                }
#if UNITY_EDITOR
                else
                {
                    request = AssetBundleManager.LoadAssetAsync(assetBundleName, assetName.Replace("*", ""), typeof(Texture2D));
                }
#endif

                if (request == null)
                    yield break;

                yield return StartCoroutine(request);

                // get and store the asset
                string key = assetBundleName + assetName;

                // for some reason when simulating them locally they come out in their raw form
                if (!m_loadedAssetBundleResourceMap.ContainsKey(key))
                {
#if UNITY_EDITOR
                    if (!AssetBundleManager.SimulateAssetBundleInEditor)
#endif
                    {
                        Sprite tempTexture = request.GetAsset<Sprite>();
                        m_loadedAssetBundleResourceMap.Add(key, tempTexture);
                    }
#if UNITY_EDITOR
                    else
                    {
                        Texture2D tempTexture = request.GetAsset<Texture2D>();
                        m_loadedAssetBundleResourceMap.Add(key, tempTexture);
                    }
#endif
                }
                // we need to OVERWRITE IT
                else
                {
#if UNITY_EDITOR
                    if (!AssetBundleManager.SimulateAssetBundleInEditor)
#endif
                    {
                        Sprite tempTexture = request.GetAsset<Sprite>();
                        m_loadedAssetBundleResourceMap[key] = tempTexture;
                    }
#if UNITY_EDITOR
                    else
                    {
                        Texture2D tempTexture = request.GetAsset<Texture2D>();
                        m_loadedAssetBundleResourceMap[key] = tempTexture;
                    }
#endif
                }
            }

            yield return null;
        }

        public Object GetResourceAtPath(string in_path)
        {
            Object tempItem = (Object)LazyLoadResourceAtPath(in_path);
            if (tempItem != null)
            {
                return tempItem;
            }
            return null;
        }

        public bool RemoveReferencedResource(string in_path, bool in_flushAssets = false)
        {
            bool bToReturn = false;
            if (m_factoryLookup.ContainsKey(in_path))
            {
                m_factoryLookup[in_path] = null;
                if (in_flushAssets)
                {
                    Resources.UnloadUnusedAssets();
                }
                bToReturn = true;
            }

            return bToReturn;
        }

        public void UnloadAssetBundlesByName(string in_assetName)
        {
#if ENABLE_ASSET_BUNDLES
            AssetBundles.AssetBundleManager.UnloadAssetBundle(in_assetName);
#endif
        }

        public void UnloadAllReferencedAssets(bool in_clearAssets = true)
        {
            //GPoolManager.Instance.DestroyAllPools();

            //m_loadedAssetBundleResourceMap.Clear();

#if ENABLE_ASSET_BUNDLES
            AssetBundleManager.UnloadAssetBundle(LoadingScreen.GetAssetBundleForSceneName(GStateManager.Instance.CurrentStateId));
#endif
            if (in_clearAssets) m_factoryLookup.Clear();

            // remove sounds
            GSoundMgr.Instance.PurgeAllSounds();

            // drain assets
            Resources.UnloadUnusedAssets();
        }

        // This method loads an asset from an assetBundle and assigns it to a provided target
        public IEnumerator SetupFromAssetBundle(string in_assetBundle, string in_assetName, Image in_mainImage, RawImage in_rawImage)
        {
            in_rawImage.enabled = false;
            in_mainImage.enabled = false;

            yield return StartCoroutine(LoadSpriteFromAssetBundle(in_assetBundle, in_assetName));

#if UNITY_EDITOR
            if (!AssetBundleManager.SimulateAssetBundleInEditor)
#endif
            {
                Destroy(in_rawImage.gameObject); // DELETE THIS !

                in_mainImage.sprite = GetObjectFromAssetBundle<Sprite>(in_assetBundle, in_assetName);
                in_mainImage.enabled = true;
            }
#if UNITY_EDITOR
            else
            {
                in_rawImage.texture = GetObjectFromAssetBundle<Texture2D>(in_assetBundle, in_assetName);
                in_rawImage.enabled = true;
                in_mainImage.enabled = false;
            }
#endif
        }
        #endregion

        #region Private
        private Object LazyLoadResourceAtPath(string in_toLoad)
        {
            Object toReturn = null;
            bool bContainsKey = m_factoryLookup.ContainsKey(in_toLoad);
            if (!bContainsKey || m_factoryLookup[in_toLoad] == null)
            {
                m_factoryLookup[in_toLoad] = Resources.Load(in_toLoad);
                toReturn = m_factoryLookup[in_toLoad];
            }
            else if (bContainsKey)
            {
                toReturn = m_factoryLookup[in_toLoad];
            }
            return (Object)toReturn;
        }

        private Dictionary<string, Object> m_factoryLookup = new Dictionary<string, Object>();

        private Dictionary<string, object> m_loadedAssetBundleResourceMap = new Dictionary<string, object>();
        #endregion
    }
}
