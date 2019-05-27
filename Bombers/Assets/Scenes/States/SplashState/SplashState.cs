//#define SMRJ_HACK
#if ENABLE_ASSET_BUNDLES
using AssetBundles;
#endif

using UnityEngine;
using System.Collections;
using Gameframework;

namespace BrainCloudUNETExample
{
    public class SplashState : BaseState
    {
        public static string STATE_NAME = "splashState";
        public static string ASSET_BUNDLE_URL = "http://apps.braincloudservers.com/BrainCloudUNETExample-dev/BrainCloudUNETExample_webgl/Current/StreamingAssets/";
        public static string ASSET_BUNDLE_URL_LIVE = "http://apps.braincloudservers.com/BrainCloudUNETExample-live/BrainCloudUNETExample_webgl/Current/StreamingAssets/";

        #region BaseState
        // Use this for initialization
        protected override void Start()
        {
            _stateInfo = new StateInfo(STATE_NAME, this);
            StartCoroutine(StartUpMgrs());
            base.Start();

            // This is set because there are issues pressing multiple buttons at once.
            // If multitouch is turned back on, we run into the issue of multiple buttons 
            // being pressed at once softlocking the game or other unexpected behaviour [NP]
            Input.multiTouchEnabled = false;
        }
        #endregion

        #region Private
        private IEnumerator StartUpMgrs()
        {
            // start up StateMgr
            yield return YieldFactory.GetWaitForEndOfFrame();
            GStateManager.Instance.ForceStateInfo(_stateInfo);

            // warmup shaders inside /Resources/Shaders/ (do this once)
            Shader.WarmupAllShaders();

            // ensure the rest are setup
            yield return YieldFactory.GetWaitForEndOfFrame();
            GCore.Instance.EnsureMgrsAreSetup();

            while (!GCore.Instance.IsInitialized)
            {
                yield return YieldFactory.GetWaitForEndOfFrame();
            }

            // play music
            //GSoundMgr.Instance.PlayMusic("commonMusic");

#if ENABLE_ASSET_BUNDLES

#if SMRJ_HACK
            AssetBundleManager.SetDevelopmentAssetBundleServer();
#else
            // LIVE URL
            if (GCore.Wrapper.Client.AppId == "30015")
            {
                AssetBundleManager.SetSourceAssetBundleURL(ASSET_BUNDLE_URL_LIVE);
            }
            // TEST URL
            else
            {
                AssetBundleManager.SetSourceAssetBundleURL(ASSET_BUNDLE_URL);
            }

#endif

#if UNITY_EDITOR
            if (!AssetBundleManager.SimulateAssetBundleInEditor)
#endif
            {
                AssetBundleLoadManifestOperation request = AssetBundles.AssetBundleManager.Initialize();
                if (request != null)
                    yield return StartCoroutine(request);

                while (AssetBundleManager.AssetBundleManifestObject == null)
                {
                    yield return YieldFactory.GetWaitForEndOfFrame();
                }
            }

            AssetBundleManager.LoadAssetBundle("eggiesslotstate");
            while (AssetBundleManager.IsDownloadingBundles())
            {
                GStateManager.Instance.ForcedUpdatedLoadingAssetBundle();
                yield return YieldFactory.GetWaitForEndOfFrame();
            }
            GStateManager.Instance.EnableLoadingScreen(false);
#endif

            yield return StartCoroutine(loadCommonSounds());

            yield return YieldFactory.GetWaitForEndOfFrame();
        }
        #endregion

        private IEnumerator loadCommonSounds()
        {
            GStateManager.Instance.ForcedUpdatedLoadingAssetBundle();
            yield return StartCoroutine(GSoundMgr.Instance.LoadSoundConfigRoutine("commonsounds", "SoundConfig"));
            GStateManager.Instance.EnableLoadingScreen(false);
            GStateManager.Instance.PushSubState(ConnectingSubState.STATE_NAME);
        }
    }
}