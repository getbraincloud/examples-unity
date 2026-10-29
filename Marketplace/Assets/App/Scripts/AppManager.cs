using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;



public class AppManager : MonoBehaviour
{
    /// <summary>
    /// When true, IAP purchases are simulated via brainCloud's mock store instead of real platform stores.
    /// Set to false to re-enable real purchases on Android/iOS.
    /// </summary>
    public static bool MockPurchasesEnabled = true;

    [SerializeField]
    private LoadingOverlay loadingOverlayPrefab;
    [SerializeField]
    private DynamicCurrencyAnim currencyAnimPrefab;
    [SerializeField]
    private ViewItemModal viewItemModalPrefab;
    [SerializeField]
    private ViewStoreItemModal viewStoreItemModalPrefab;
    [SerializeField]
    private InfoModal infoModalPrefab;
    [SerializeField]
    private LevelUpModal levelUpModalPrefab;
    [SerializeField]
    private ProfileImagePickerModal profileImagePickerModalPrefab;

    [SerializeField]
    private Sprite[] profileImages;

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
    public Action<Sprite> OnProfileImageChanged;

    public int ProfileImageIndex { get; private set; }

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

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var results = new List<RaycastResult>();
            var pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count == 0)
            {
                Debug.Log("[ClickDebug] Click hit no UI elements");
            }
            else
            {
                for (int i = 0; i < results.Count; i++)
                    Debug.Log($"[ClickDebug] Hit[{i}]: '{results[i].gameObject.name}' depth={results[i].depth} sortOrder={results[i].sortingOrder}");
            }
        }
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

        // We reuse brainCloud's native pictureUrl field to store the chosen profile image's
        // index (see SetProfileImageIndex) rather than an actual URL - it comes back fresh
        // with every login's auth response, so this is always this account's own selection
        // (defaulting to 0 if never set), never a stale value left over from another account.
        ProfileImageIndex = int.TryParse(userData.PictureUrl, out int savedProfileImageIndex)
            ? ClampProfileImageIndex(savedProfileImageIndex)
            : 0;

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

    private int ClampProfileImageIndex(int index)
    {
        if (profileImages == null || profileImages.Length == 0)
            return 0;

        return Mathf.Clamp(index, 0, profileImages.Length - 1);
    }

    /// <summary>
    /// Returns the sprite for the user's currently-selected profile image, or null if no
    /// profile images have been configured on this component.
    /// </summary>
    public Sprite GetCurrentProfileImage()
    {
        if (profileImages == null || profileImages.Length == 0)
            return null;

        return profileImages[ClampProfileImageIndex(ProfileImageIndex)];
    }

    /// <summary>
    /// Sets and persists the user's chosen profile image, then notifies listeners
    /// (MainScreen's HUD icon, ProfileModal) so they can update immediately.
    /// </summary>
    public void SetProfileImageIndex(int index, Action onComplete = null)
    {
        index = ClampProfileImageIndex(index);
        ProfileImageIndex = index;
        userData.PictureUrl = index.ToString();

        OnProfileImageChanged?.Invoke(GetCurrentProfileImage());

        // Reusing brainCloud's native pictureUrl field (storing the index as a string instead
        // of an actual URL) means this comes back for free in every future login's auth
        // response, tied to this specific account, with no extra attribute round trip needed.
        BCManager.Instance.BCWrapper.PlayerStateService.UpdateUserPictureUrl(
            index.ToString(),
            (string _, object __) => onComplete?.Invoke(),
            (int statusCode, int responseCode, string errorJson, object errorObj) =>
            {
                Debug.LogError("Failed to persist profile image selection: " + errorJson);
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// Checks the server for any XP accrued by the xp_generator status effect since it was
    /// last collected (covers time the user spent offline). If any was awarded, shows a modal
    /// with the level/XP the user had when they last logged out - the actual level/XP bar
    /// update (and any resulting level-up modal) is deferred until the user dismisses that
    /// modal, so they see the old values behind it and then watch the bar fill up afterward.
    /// If the status is still active (the offline window didn't use up the full 90s), resumes
    /// live in-session ticking for the remaining time.
    ///
    /// Must only be called once the Main scene (and its Canvas) is loaded — this relies on
    /// <see cref="SpawnInfoModal"/>, which is not safe to call during the splash/login flow.
    /// </summary>
    public void CollectOfflineXpGeneratorXP(Action onDone = null)
    {
        InventoryService.Instance.CollectXpGeneratorXP((result) =>
        {
            void ResumeTrackingIfStillActive()
            {
                // Only resume live ticking once we're done presenting the offline result -
                // otherwise a tick could sneak in and apply/animate XP while the "Welcome
                // Back" modal (showing the pre-collect level/XP) is still on screen.
                if (result.isActive)
                {
                    InventoryService.Instance.StartXpGeneratorTracking(result.activeUntil);
                }
            }

            if (result.xpAwarded <= 0)
            {
                ResumeTrackingIfStillActive();
                onDone?.Invoke();
                return;
            }

            SpawnInfoModal(
                title: "Welcome Back!",
                message: $"You gained {result.xpAwarded} XP while you were offline!",
                actionButtonLabel: "Collect",
                onAction: null,
                onClosed: () =>
                {
                    InventoryService.ApplyXpGeneratorResult(result);
                    ResumeTrackingIfStillActive();
                    onDone?.Invoke();
                });
        });
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
        BCManager.Instance.BCWrapper.PlayerStateService.UpdateUserName(
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

        StartCoroutine(MoveWorld(awardAnimRect, targetRect.position, 0.666f, onComplete));
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

    public void SpawnViewStoreItemModal(StoreItemData data, Action onActionButton, Action onClosed)
    {
        if (_appCanvas == null)
        {
            FetchReferences();
        }

        ViewStoreItemModal modal = Instantiate(viewStoreItemModalPrefab, _appCanvas.transform);
        modal.transform.localScale = Vector3.one;

        modal.SetData(data, onActionButton, onClosed);
    }

    public void SpawnInfoModal(string title, string message, string actionButtonLabel, Action onAction, Action onClosed = null)
    {
        if (_appCanvas == null)
        {
            FetchReferences();
        }

        InfoModal modal = Instantiate(infoModalPrefab, _appCanvas.transform);
        modal.transform.localScale = Vector3.one;

        modal.SetData(title, message, actionButtonLabel, onAction, onClosed);
    }

    public void SpawnLevelUpModal(int newLevel, Action onClosed = null)
    {
        if (_appCanvas == null)
        {
            FetchReferences();
        }

        LevelUpModal modal = Instantiate(levelUpModalPrefab, _appCanvas.transform);
        modal.transform.localScale = Vector3.one;

        modal.SetData(newLevel, onClosed);
    }

    public void SpawnProfileImagePickerModal(Action onClosed = null)
    {
        if (_appCanvas == null)
        {
            FetchReferences();
        }

        ProfileImagePickerModal modal = Instantiate(profileImagePickerModalPrefab, _appCanvas.transform);
        modal.transform.localScale = Vector3.one;

        modal.SetData(profileImages, (int selectedIndex) => SetProfileImageIndex(selectedIndex), onClosed);
    }

    private IEnumerator MoveWorld(RectTransform rect, Vector3 targetPos, float duration, Action onComplete)
    {
        Vector3 startPos = rect.position;
        Vector3 startScale = rect.localScale;
        float time = 0f;

        // Arc height scales with screen height so the hop looks proportionate on any
        // resolution, rather than using a fixed pixel value.
        float arcHeight = Screen.height * 0.12f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // Smooth easing
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
            pos.y += arcHeight * 4f * t * (1f - t); // parabola: 0 at t=0/1, peaks at t=0.5
            rect.position = pos;

            // Scale up to 3x at the midpoint, back to normal by the end
            float scale = 1f + 2.5f * Mathf.Sin(t * Mathf.PI);
            rect.localScale = startScale * scale;

            yield return null;
        }

        rect.position = targetPos;
        rect.localScale = startScale;

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
