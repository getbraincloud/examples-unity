using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SplashScreen : MonoBehaviour
{
    [SerializeField]
    private LoadingBar _loadingBar;

    private Animator _anim;

    private bool _brainCloudLoaded = false;

    private bool _skipToMain = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    private void Start()
    {
        StartCoroutine(StartupProcess());
        _loadingBar.OnLoadingComplete.AddListener(OnLoadingComplete);
    }

    IEnumerator StartupProcess()
    {
        //Step 1: Ensure brainCloud is initialized
        yield return new WaitUntil(() => BCManager.Instance.isInitialized);

        //BrainCloud is initialized!
        _brainCloudLoaded = true;

        _loadingBar.SetLoadingValue(0.5f);

        yield return new WaitForSeconds(1f);

        //Step 2: Check if remember me was set to true and if we can reconnect
        if(PlayerPrefs.GetInt(Globals.PP_REMEMBER_ME) == 1 && BCManager.Instance.BCWrapper.CanReconnect())
        {
            BCManager.Instance.BCWrapper.Reconnect(
                (string responseJson, object cb) =>
                {
                    _skipToMain = true;
                    //reconnect successfull
                    AppManager.Instance.ProcessUserData(responseJson, () =>
                    {
                        _loadingBar.SetLoadingValue(1f);
                    });
                },
                (int status, int responseCode, string responseJson, object cb) =>
                {
                    _loadingBar.SetLoadingValue(1f);
                });
        }
        else
        {
            _loadingBar.SetLoadingValue(1f);
        }
    }

    private void OnLoadingComplete()
    {
        if (_brainCloudLoaded)
        {
            _anim.SetBool("fadeOut", true);
        }
    }

    public void OnFadeOutComplete()
    {
        if (_skipToMain)
        {
            AppManager.Instance.SwitchScenes("Main");
        }
        else
        {
            AppManager.Instance.SwitchScenes("Login");
        }
    }
}
