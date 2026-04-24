using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;



public class AppManager : MonoBehaviour
{
    [SerializeField]
    private LoadingOverlay loadingOverlayPrefab;
    [SerializeField]
    private DynamicCurrencyAnim currencyAnimPrefab;
    [SerializeField]
    private ViewItemModal viewItemModalPrefab;

    private LoadingOverlay _currentLoadingOverlay;

    public static AppManager Instance;

    public UserData userData { get; private set; }

    public Action<int> OnCoinsUpdated;
    public Action<int> OnGemsUpdated;
    public Action<int,int,string> OnUserLevelUpdated;
    public Action<int> OnUserXPUpdated;
    public Action<string> OnUsernameUpdated;
    public Action<UserData> OnStatsUpdated;
    public Action<CoinMultiplierStatus> OnMultiplierActivated;

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

    public void ProcessUserData(string jsonResponse, Action OnComplete, string loginUsername = null)
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

            if (!string.IsNullOrEmpty(loginUsername))
            {
                UpdatePlayerNameOnServer(loginUsername);
            }
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

        BCManager.Instance.BCWrapper.ScriptService.RunScript("GetUserXPData", "{}",
        (string xpJsonResponse, object cbObj) =>
        {
            //script call was successful check if operation was successful
            var responseData = (JsonReader.Deserialize<Dictionary<string, object>>(xpJsonResponse)["data"] as Dictionary<string, object>)["response"] as Dictionary<string, object>;

            userData.XPCapped = Convert.ToBoolean(responseData["xpCapped"]);
            userData.CurrentXP = Convert.ToInt32(responseData["adjustedXp"]);
            userData.TotalXP = Convert.ToInt32(responseData["totalExperiencePoints"]);
            userData.LevelStatusName = responseData["statusTitle"] as string;
            userData.Level = Convert.ToInt32(responseData["experienceLevel"]);
            userData.XPToNextLevel = Convert.ToInt32(responseData["xpToNextLevel"]);

            OnUserDataCollected();
        },
        (int statusCode, int responseCode, string errorJson, object errorObj) =>
        {
            //still continue without the data we failed to collect
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
                _currentLoadingOverlay = Instantiate(loadingOverlayPrefab, canvas.transform, false);
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

    public void UpdatePlayerNameOnServer(string newPlayerName, Action onSuccess = null, Action<string> onFailure = null)
    {
        BCManager.Instance.BCWrapper.PlayerStateService.UpdateName(
            newPlayerName,
            (string responseJson, object cb) =>
            {
                UpdatePlayerName(newPlayerName);
                onSuccess?.Invoke();
            },
            (int statusCode, int reasonCode, string errorJson, object cb) =>
            {
                var error = JsonReader.Deserialize<Dictionary<string, object>>(errorJson);
                var message = error["status_message"] as string;
                Debug.LogError($"Failed to update player name: {message}");
                onFailure?.Invoke(message);
            });
    }

    public void UpdateCoinsAmount(int amount)
    {
        if(userData.Coins != amount)
        {
            userData.Coins = amount;
            OnCoinsUpdated?.Invoke(userData.Coins);
        }
    }

    public void ConsumeCoins(int amount)
    {
        int newAmount = userData.Coins - amount;
        if (newAmount < 0)
        {
            newAmount = 0;
        }

        UpdateCoinsAmount(newAmount);
    }

    public void AddCoins(int amount)
    {
        int newAmount = userData.Coins + amount;

        UpdateCoinsAmount(newAmount);
    }

    public void UpdateGemsAmount(int amount)
    {
        if(userData.Gems != amount)
        {
            userData.Gems = amount;
            OnGemsUpdated?.Invoke(userData.Gems);
        }
    }

    public void ConsumeGems(int amount)
    {
        int newAmount = userData.Gems - amount;
        if(newAmount < 0)
        {
            newAmount = 0;
        }

        UpdateGemsAmount(newAmount);
    }

    public void AddGems(int amount)
    {
        int newAmount = userData.Gems + amount;
        UpdateGemsAmount(newAmount);
    }

    public void UpdateUserLevel(int level, string levelName, int XPToNextLevel)
    {
        if(userData.Level != level)
        {
            userData.Level = level;

            if (!string.IsNullOrEmpty(levelName))
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

    public void ToggleCoinMultiplier(bool active, long activeUntil)
    {
        CoinMultiplierStatus multiplierStatus = new CoinMultiplierStatus
        {
            isActive = active,
            ActiveUntil = activeUntil,
            multiplierAmount = 2
        };

        OnMultiplierActivated?.Invoke(multiplierStatus);
    }

    public void AnimateDynamicAward(RectTransform sourceRect, CurrencyType currencyType, Action onComplete)
    {
        FetchReferences();

        DynamicCurrencyAnim awardAnim = Instantiate(currencyAnimPrefab, _appCanvas.transform);
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

    public void SpawnViewItemModal(UserItemData data, UserItemCard card, Action onClosed)
    {
        if(_appCanvas == null)
        {
            FetchReferences();
        }

        ViewItemModal viewItemModal = Instantiate(viewItemModalPrefab, _appCanvas.transform);
        viewItemModal.transform.localScale = Vector3.one;

        viewItemModal.SetData(data, card, onClosed);
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
