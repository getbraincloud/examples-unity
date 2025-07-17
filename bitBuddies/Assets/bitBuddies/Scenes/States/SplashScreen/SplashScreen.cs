using System.Collections;
using Gameframework;
using UnityEngine;

public class SplashScreen : BaseState
{
    public static string STATE_NAME = "SplashScreen";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        _stateInfo = new StateInfo(STATE_NAME, this);
        StartCoroutine(StartUpMgrs());
        base.Start();
    }

    private IEnumerator StartUpMgrs()
    {
        // start up StateMgr
        yield return YieldFactory.GetWaitForEndOfFrame();
        GStateManager.Instance.ForceStateInfo(_stateInfo);

        // warmup shaders inside /Resources/Shaders/ (do this once)
        //Shader.WarmupAllShaders();

        // ensure the rest are setup
        yield return YieldFactory.GetWaitForEndOfFrame();
        GCore.Instance.EnsureMgrsAreSetup();

        while (!GCore.Instance.IsInitialized)
        {
            yield return YieldFactory.GetWaitForEndOfFrame();
        }
        
        GStateManager.Instance.EnableLoadingScreen(false);
        GStateManager.Instance.PushSubState(ConnectingSubState.STATE_NAME);
    }
}
