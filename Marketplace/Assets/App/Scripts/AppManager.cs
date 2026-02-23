using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;



public class AppManager : MonoBehaviour
{
    [SerializeField]
    private LoadingOverlay _loadingOverlayPrefab;
    [SerializeField]
    private DynamicCurrencyAnim _currencyAnimPrefab;

    private LoadingOverlay _currentLoadingOverlay;

    public static AppManager Instance;

    public UserData userData { get; private set; }

    public Action<int> OnCoinsUpdated;
    public Action<int> OnGemsUpdated;
    public Action<int,int,string> OnUserLevelUpdated;
    public Action<int> OnUserXPUpdated;
    public Action<string> OnUsernameUpdated;
    public Action<UserData> OnStatsUpdated;

    private Canvas _appCanvas;

    private RectTransform _coinIconRect, _gemIconRect, _starIconRect;

    private void Awake()
    {
        // Ensure there's only one instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ProcessUserData(string jsonResponse, Action OnComplete)
    {
        if(userData == null)
        {
            userData = new UserData();
        }

        if (BCManager.Instance.BCWrapper.GetStoredProfileId() is string id && !string.IsNullOrEmpty(id))
        {
            Debug.Log($"User Profile ID: {id}");
        }

        //update user data
        var data = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)["data"] as Dictionary<string, object>;

        var currencyInfo = data["currency"] as Dictionary<string, object>;
        bool isNewUser = Convert.ToBoolean(data["newUser"]);

        if (isNewUser)
        {
            //if it's a new user let's set up their account
            BCManager.Instance.BCWrapper.ScriptService.RunScript("SetupUserAccount", "{}",
                (string responseJson, object cb) =>
                {
                    Debug.Log("Setup User account: " + responseJson);
                });
        }
        

        AuthResponse authResponse = new AuthResponse()
        {
            ExperienceLevel = Convert.ToInt32(data["experienceLevel"]),
            PlayerName = data["playerName"] as string,
            PictureUrl = data["pictureUrl"] as string,

            Currency = new CurrencyData
            {
                Coins = Convert.ToInt32(
                    (currencyInfo["Coins"] as Dictionary<string, object>)["balance"]
                ),
                Gems = Convert.ToInt32(
                    (currencyInfo["Gems"] as Dictionary<string, object>)["balance"]
                )
            }
        };

        userData.UpdateFromAuth(authResponse);

        //fetch other player XP data
        BCManager.Instance.BCWrapper.GamificationService.ReadAllGamification(true,
        (string xpJsonResponse, object cbObj) =>
        {
            Debug.Log("[Gamification] " + xpJsonResponse);
            var xpData = (JsonReader.Deserialize<Dictionary<string, object>>(xpJsonResponse)["data"] as Dictionary<string, object>)["xp"] as Dictionary<string, object>;
            var xpLevelData = xpData["xpLevel"] as Dictionary<string, object>;

            GamificationResponse gamificationResponse = new GamificationResponse()
            {
                XPCapped = Convert.ToBoolean(xpData["xpCapped"]),
                CurrentXP = Convert.ToInt32(xpData["experiencePoints"]),
                LevelStatusName = xpLevelData["statusTitle"] as string
            };

            userData.UpdateFromGamification(gamificationResponse);

            //TODO: In the future, XPToNextLevel will be added as a field in the response to the ReadAllGamification call,
            //Therefore it will not be necessary at that point to get that info from ReadXpLevelsMetaData

            if (!userData.XPCapped)
            {
                //we are not at the max level so we want to know how much to reach the next level
                //Get the XPToNextLevel value
                BCManager.Instance.BCWrapper.GamificationService.ReadXpLevelsMetaData(
                    (string xpMetaDataResponse, object cbObj_xp) =>
                    {
                        var xpMetaData = (JsonReader.Deserialize<Dictionary<string, object>>(xpMetaDataResponse)["data"] as Dictionary<string, object>)["xp_levels"] as object[];

                        int nextLevel = userData.Level + 1;
                        int xpRequired = 0;
                        foreach (Dictionary<string, object> level in xpMetaData)
                        {
                            if (Convert.ToInt32(level["level"]) == nextLevel)
                            {
                                xpRequired = Convert.ToInt32(level["experience"]);
                                break;
                            }
                        }
                        userData.UpdateXPToNextLevel(xpRequired);
                        OnUserDataCollected();

                    },
                    (int status, int responseCode, string jsonErrorData, object _cb) =>
                    {
                        Debug.LogError("ReadXpLevelsMetaData failed: " + jsonErrorData);
                        OnUserDataCollected();
                    });
            }
            else
            {
                //we are at the max level so we don't need that data
                OnUserDataCollected();
            }
        },
        (int gamificationStatus, int gamificationRCode, string gamificationJsonResponse, object gamificationCb) =>
        {
            Debug.LogError("ReadAllGamification failed: " + gamificationJsonResponse);
            OnUserDataCollected();
        });

        void OnUserDataCollected()
        {
            OnStatsUpdated?.Invoke(userData);
            OnComplete?.Invoke();
        }
    }

    public void SwitchScenes(string scene)
    {
        SceneManager.LoadScene(scene);
    }

    public void OnBrainCloudError(int status, int reason, string jsonError, object _)
    {
        // Deserialize jsonError
        var error = JsonReader.Deserialize<Dictionary<string, object>>(jsonError);
        var message = (string)error["status_message"];

        Debug.LogError($"Status: {status} | Reason: {reason} | Message:\n{message}");
    }

    public void ToggleLoadingOverlay(LoadingOverlay.LoadingOverlayType loadingType, bool enabled, Action OnLoadingComplete = null)
    {
        if (enabled)
        {
            if (_currentLoadingOverlay == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                _currentLoadingOverlay = Instantiate(_loadingOverlayPrefab, canvas.transform, false);
                _currentLoadingOverlay.SetLoadingType(loadingType);
                _currentLoadingOverlay.loadingBar.OnLoadingComplete.AddListener(() => { OnLoadingComplete?.Invoke(); });
            }
        }
        else
        {
            if(_currentLoadingOverlay != null)
            {
                Destroy(_currentLoadingOverlay);
                _currentLoadingOverlay = null;
            }
        }
    }

    public void SetLoadingValue(float progress)
    {
        if(_currentLoadingOverlay != null)
        {
            _currentLoadingOverlay.SetLoadingValue(progress);
        }
    }

    public void SetUserStats(UserData newData)
    {
        userData = newData;
        OnStatsUpdated?.Invoke(userData);
    }

    public void UpdatePlayerName(string newPlayerName)
    {
        userData.PlayerName = newPlayerName;
        OnUsernameUpdated?.Invoke(userData.PlayerName);
    }

    public void UpdateCoinsAmount(int amount)
    {
        if(userData.Coins != amount)
        {
            userData.Coins = amount;
            OnCoinsUpdated?.Invoke(userData.Coins);
        }
    }

    public void UpdateGemsAmount(int amount)
    {
        if(userData.Gems != amount)
        {
            userData.Gems = amount;
            OnGemsUpdated?.Invoke(userData.Gems);
        }
    }

    public void UpdateUserLevel(int level, string levelName, int XPToNextLevel)
    {
        if(userData.Level != level)
        {
            userData.Level = level;

            if (string.IsNullOrEmpty(levelName))
            {
                userData.LevelStatusName = levelName;
            }

            userData.XPToNextLevel = XPToNextLevel;

            OnUserLevelUpdated?.Invoke(userData.Level, userData.XPToNextLevel, userData.LevelStatusName);
        }
    }

    public void UpdateUserXP(int xp)
    {
        userData.CurrentXP = xp;
        OnUserXPUpdated?.Invoke(userData.CurrentXP);
    }

    public void AnimateDynamicAward(RectTransform sourceRect, CurrencyType currencyType, Action onComplete)
    {
        FetchReferences();

        DynamicCurrencyAnim awardAnim = Instantiate(_currencyAnimPrefab, _appCanvas.transform);
        awardAnim.UpdateIcon(currencyType);

        RectTransform awardAnimRect = awardAnim.GetComponent<RectTransform>();

        awardAnimRect.SetAsLastSibling();
        awardAnimRect.localScale = Vector3.one;
        
        RectTransform targetRect = null;

        awardAnimRect.position = sourceRect.position;
        switch (currencyType)
        {
            case CurrencyType.Coins:
                targetRect = _coinIconRect;
                break;
            case CurrencyType.Gems:
                targetRect = _gemIconRect;
                break;
            case CurrencyType.Stars:
                targetRect = _starIconRect;
                break;
        }

        StartCoroutine(MoveWorld(awardAnimRect, targetRect.position, 2f, onComplete));
    }

    private IEnumerator MoveWorld(RectTransform rect, Vector3 targetPos, float duration, Action onComplete)
    {
        Vector3 startPos = rect.position;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // Smooth easing
            t = Mathf.SmoothStep(0f, 1f, t);

            rect.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        rect.position = targetPos;

        // Destroy after arrival
        Destroy(rect.gameObject);
        onComplete?.Invoke();
    }

    private void FetchReferences()
    {
        if(_coinIconRect == null) _coinIconRect = GameObject.FindGameObjectWithTag("coinIcon").GetComponent<RectTransform>();

        if (_gemIconRect == null) _gemIconRect = GameObject.FindGameObjectWithTag("gemIcon").GetComponent<RectTransform>();

        if (_starIconRect == null) _starIconRect = GameObject.FindGameObjectWithTag("starIcon").GetComponent<RectTransform>();

        if (_appCanvas == null) _appCanvas = FindFirstObjectByType<Canvas>();
    }
}
