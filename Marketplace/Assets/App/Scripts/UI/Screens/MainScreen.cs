using BrainCloud.JsonFx;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.LowLevelPhysics2D;
using UnityEngine.UI;

public class MainScreen : MonoBehaviour
{
    //Buttons
    [SerializeField]
    private Button _avatarButton, _collectCoinsButton, _closeButton;
    //GameObjects
    [SerializeField]
    private GameObject _collectCoinsBaseMessageDisplay,
                        _goldAvatarFrameDisplay,
                        _adsBanner,
                        _xpDoublerIcon,
                        _maxLevelText;

    [SerializeField]
    private RectTransform _coinIconRef;
    //UI Text
    [SerializeField]
    private TextMeshProUGUI _coinsAmountText,
                            _gemsAmountText,
                            _levelText,
                            _collectCoinsBaseAmountText,
                            _currentMarketplaceText,
                            _generatorTimerText;
    //UI Image
    [SerializeField]
    private Image _xpBar, _userProfileImage, _avatarImage, _avatarFrameImage;
    //Other
    [SerializeField]
    private Animator _profileModalAnim;
    [SerializeField]
    private MultiplierDisplay _multiplierDisplay;

    [Header("Prefab References")]
    [SerializeField]
    private AnimatedNumberIncrement _animatedNumberIncrementPrefab;
    [SerializeField]
    private AnimatedCoinCollect _animatedCoinCollectPrefab;


    private Animator _anim;
    private Queue<(bool isLevelUp, float targetFill)> _xpAnimQueue = new();
    private float _lastQueuedFill;
    private Coroutine _xpAnimCoroutine;
    private CoinMultiplierStatus _coinMultipierStatus;
    private Coroutine _generatorTimerCoroutine;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _avatarButton.onClick.AddListener(OnAvatarButtonClicked);
        _collectCoinsButton.onClick.AddListener(OnCollectCoinsButtonClicked);
        _closeButton.onClick.AddListener(OnCloseButtonClicked);
        _adsBanner.GetComponent<Button>().onClick.AddListener(OnAdsBannerClicked);
        AppManager.Instance.OnCoinsUpdated += OnCoinsUpdated;
        AppManager.Instance.OnGemsUpdated += OnGemsUpdated;
        AppManager.Instance.OnUserXPUpdated += OnUserXPUpdated;
        AppManager.Instance.OnUserLevelUpdated += OnUserLevelUpdated;
        AppManager.Instance.OnMultiplierActivated += OnMultiplierActivated;
        AppManager.Instance.OnProfileImageChanged += OnProfileImageChanged;

        InventoryService.Instance.OnItemEquipChange += OnItemEquipChange;
        InventoryService.Instance.OnNoAdsStatusKnown += OnNoAdsStatusKnown;
        InventoryService.Instance.OnSubscriptionExpired += OnSubscriptionExpired;
        InventoryService.Instance.OnXpGeneratorStatusChanged += OnXpGeneratorStatusChanged;

        _adsBanner.SetActive(!InventoryService.Instance.NoAdsSubscriptionActive);
        OnXpGeneratorStatusChanged(InventoryService.Instance.XpGeneratorActive, InventoryService.Instance.XpGeneratorActiveUntil);
        _maxLevelText.SetActive(AppManager.Instance.userData.XPCapped);
        _userProfileImage.sprite = AppManager.Instance.GetCurrentProfileImage();

        UpdateAllUserStatUI(AppManager.Instance.userData);

        // Safe to check for offline xp_generator gains here (unlike during the splash/login
        // flow) since the Main scene's Canvas now exists for the resulting modal to spawn in.
        AppManager.Instance.CollectOfflineXpGeneratorXP();
    }

    private void OnXpGeneratorStatusChanged(bool isActive, long activeUntil)
    {
        _xpDoublerIcon.SetActive(isActive);

        if (_generatorTimerCoroutine != null)
        {
            StopCoroutine(_generatorTimerCoroutine);
            _generatorTimerCoroutine = null;
        }

        _generatorTimerText.gameObject.SetActive(isActive);
        if (isActive)
        {
            // Restarting here (rather than just updating a target) also covers extension:
            // activating another xp_generator item while already active pushes activeUntil
            // further out and re-fires this event with the new value.
            _generatorTimerCoroutine = StartCoroutine(GeneratorTimerRoutine(activeUntil));
        }
    }

    private IEnumerator GeneratorTimerRoutine(long activeUntilMs)
    {
        while (true)
        {
            long remainingMs = activeUntilMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (remainingMs <= 0)
            {
                _generatorTimerText.text = "0:00";
                yield break;
            }

            int totalSeconds = Mathf.CeilToInt(remainingMs / 1000f);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _generatorTimerText.text = $"{minutes}:{seconds:00}";

            yield return new WaitForSecondsRealtime(1f);
        }
    }

    private void OnAdsBannerClicked()
    {
        AppManager.Instance.SpawnInfoModal(
            title: "Go to Samples Page?",
            message: "Collaborative Canvas is a free, cooperative painting experience.\n\nExplore this and all our sample game projects, complete with full source code.",
            actionButtonLabel: "View All Samples",
            onAction: () => Application.OpenURL("https://getbraincloud.com/samples/")
        );
    }

    private void OnCloseButtonClicked()
    {
        // Cancel/close/background on InfoModal all just dismiss it, so this only needs
        // to wire up the confirm action - no separate cancel handler required.
        AppManager.Instance.SpawnInfoModal(
            title: "Quit Game?",
            message: "Are you sure you want to quit?",
            actionButtonLabel: "Quit",
            onAction: () => Application.Quit()
        );
    }

    private void OnMultiplierActivated(CoinMultiplierStatus status)
    {
        //update gameplay button to show that multiplier is active and set the cooldown
        if (status.isActive)
        {
            //show multiplier status and update coin amount (which is user level) by multiplier
            int newCoinAmountsToCollect = AppManager.Instance.userData.Level * status.multiplierAmount;
            _collectCoinsBaseAmountText.text = newCoinAmountsToCollect.ToString();

            //hide base collect message and show multiplier message and timer
            _collectCoinsBaseMessageDisplay.SetActive(false);
            _multiplierDisplay.gameObject.SetActive(true);
            _multiplierDisplay.SetCountdownTimer(status.ActiveUntil, () =>
            {
                //on multiplier period ended toggle off multiplier (which leads back to this function with status.isActive being false)
                AppManager.Instance.ToggleCoinMultiplier(false, -1);
            });
        }
        else
        {
            _collectCoinsBaseMessageDisplay.SetActive(true);
            _multiplierDisplay.gameObject.SetActive(false);

            _collectCoinsBaseAmountText.text = AppManager.Instance.userData.Level.ToString();
        }
    }

    private void OnUserLevelUpdated(int level, int xpToNextLevel, string statusName)
    {
        AppManager.Instance.SpawnLevelUpModal(level);
        _levelText.text = level.ToString();
        _collectCoinsBaseAmountText.text = level.ToString();
        _maxLevelText.SetActive(AppManager.Instance.userData.XPCapped);
    }

    private void OnUserXPUpdated(int newXp)
    {
        float newFill = Mathf.Clamp01((float)newXp / (float)AppManager.Instance.userData.XPToNextLevel);
        EnqueueXPFill(newFill);
    }

    private void OnGemsUpdated(int newGems)
    {
        int oldAmount = int.Parse(_gemsAmountText.text);
        int difference = newGems - oldAmount;

        AnimatedNumberIncrement animatedNumberIncrement = Instantiate(_animatedNumberIncrementPrefab, _gemsAmountText.transform.parent);

        animatedNumberIncrement.SetCurrencyType(CurrencyType.Gems);
        animatedNumberIncrement.SetAmountUI(difference);

        _gemsAmountText.text = newGems.ToString();
    }

    private void OnDisable()
    {
        _avatarButton.onClick.RemoveListener(OnAvatarButtonClicked);
        _collectCoinsButton.onClick.RemoveListener(OnCollectCoinsButtonClicked);
        _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        _adsBanner.GetComponent<Button>().onClick.RemoveListener(OnAdsBannerClicked);
        AppManager.Instance.OnCoinsUpdated -= OnCoinsUpdated;
        AppManager.Instance.OnGemsUpdated -= OnGemsUpdated;
        AppManager.Instance.OnUserXPUpdated -= OnUserXPUpdated;
        AppManager.Instance.OnUserLevelUpdated -= OnUserLevelUpdated;
        AppManager.Instance.OnMultiplierActivated -= OnMultiplierActivated;
        AppManager.Instance.OnProfileImageChanged -= OnProfileImageChanged;

        InventoryService.Instance.OnItemEquipChange -= OnItemEquipChange;
        InventoryService.Instance.OnNoAdsStatusKnown -= OnNoAdsStatusKnown;
        InventoryService.Instance.OnSubscriptionExpired -= OnSubscriptionExpired;
        InventoryService.Instance.OnXpGeneratorStatusChanged -= OnXpGeneratorStatusChanged;

        if (_xpAnimCoroutine != null)
        {
            StopCoroutine(_xpAnimCoroutine);
            _xpAnimCoroutine = null;
        }
        _xpAnimQueue.Clear();

        if (_generatorTimerCoroutine != null)
        {
            StopCoroutine(_generatorTimerCoroutine);
            _generatorTimerCoroutine = null;
        }
    }

    private void OnNoAdsStatusKnown(bool hasSubscription)
    {
        _adsBanner.SetActive(!hasSubscription);
    }

    private void OnSubscriptionExpired()
    {
        _adsBanner.SetActive(true);
    }

    private async void OnItemEquipChange(UserItemData itemData)
    {
        Debug.Log("OnItemEquipChange: " + itemData.defId + " " + itemData.isEquipped);
        if(itemData.defId == "gold_frame")
        {
            //equip gold frame
            _goldAvatarFrameDisplay.SetActive(itemData.isEquipped);
        }
        if(itemData.equippableSlot == "ShirtSlot")
        {
            //this could be any color shirt
            if (itemData.isEquipped)
            {
                Sprite newAvatarImage = await ImageCacheService.Instance.GetImageAsync(itemData.imageUrl);
                _avatarImage.sprite = newAvatarImage;
            }
            else
            {
                InventoryService.Instance.EquipDefaultItemForSlot("ShirtSlot", null);
            }
        }
    }

    private void OnProfileImageChanged(Sprite newImage)
    {
        _userProfileImage.sprite = newImage;
    }

    private void UpdateAllUserStatUI(UserData userData)
    {
        _coinsAmountText.text = userData.Coins.ToString();
        _gemsAmountText.text = userData.Gems.ToString();
        _levelText.text = userData.Level.ToString();
        float fillAmount = (float)userData.CurrentXP / (float)userData.XPToNextLevel;
        fillAmount = Mathf.Clamp(fillAmount, 0f, 1f);

        _collectCoinsBaseAmountText.text = userData.Level.ToString();

        // Reset any in-flight animation so the initial fill starts cleanly
        if (_xpAnimCoroutine != null)
        {
            StopCoroutine(_xpAnimCoroutine);
            _xpAnimCoroutine = null;
        }
        _xpAnimQueue.Clear();
        _xpBar.GetComponent<RectTransform>().offsetMax = new Vector2(-Globals.XP_BAR_FILL, 0);
        _lastQueuedFill = 0f;

        EnqueueXPFill(fillAmount);

        string currentStoreId = InventoryService.GetPlatformStoreId();
        switch (currentStoreId)
        {
            case "googlePlay":
                _currentMarketplaceText.text = "Google Play";
                break;
            case "itunes":
                _currentMarketplaceText.text = "Apple Store";
                break;
            case "windows":
                _currentMarketplaceText.text = "Windows Store";
                break;
        }
    }

    private void EnqueueXPFill(float newFill)
    {
        // A fill that is less than the last queued fill means a level-up occurred
        _xpAnimQueue.Enqueue((newFill < _lastQueuedFill, newFill));
        _lastQueuedFill = newFill;

        if (_xpAnimCoroutine == null)
            _xpAnimCoroutine = StartCoroutine(ProcessXPQueue());
    }

    private IEnumerator ProcessXPQueue()
    {
        RectTransform barRect = _xpBar.GetComponent<RectTransform>();

        while (_xpAnimQueue.Count > 0)
        {
            var (isLevelUp, targetFill) = _xpAnimQueue.Dequeue();

            if (!isLevelUp)
            {
                // Collapse consecutive non-level-up fills into one — skip to the last
                // non-level-up fill before the next level-up (or end of queue)
                while (_xpAnimQueue.Count > 0 && !_xpAnimQueue.Peek().isLevelUp)
                    (_, targetFill) = _xpAnimQueue.Dequeue();
            }

            if (isLevelUp)
            {
                // Fill to full, snap back to empty, then fill to the post-level amount
                yield return AnimateXPBar(barRect, 1f);
                barRect.offsetMax = new Vector2(-Globals.XP_BAR_FILL, 0);
                yield return AnimateXPBar(barRect, targetFill);
            }
            else
            {
                yield return AnimateXPBar(barRect, targetFill);
            }
        }

        _xpAnimCoroutine = null;
    }

    private IEnumerator AnimateXPBar(RectTransform barRect, float targetFill, float duration = 0.4f)
    {
        float startX = barRect.offsetMax.x;
        float endX = -Globals.XP_BAR_FILL * (1f - targetFill);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            barRect.offsetMax = new Vector2(Mathf.Lerp(startX, endX, t / duration), 0);
            yield return null;
        }

        barRect.offsetMax = new Vector2(endX, 0);
    }

    private void OnCoinsUpdated(int newAmount)
    {
        int oldAmount = int.Parse(_coinsAmountText.text);
        int difference = newAmount - oldAmount;

        AnimatedNumberIncrement animatedNumberIncrement = Instantiate(_animatedNumberIncrementPrefab, _coinsAmountText.transform.parent);

        animatedNumberIncrement.SetCurrencyType(CurrencyType.Coins);
        animatedNumberIncrement.SetAmountUI(difference);

        _coinsAmountText.text = newAmount.ToString();
    }

    private void OnCoinCollectAnimComplete(int newAmount)
    {
        AppManager.Instance.UpdateCoinsAmount(newAmount);
    }

    private void OnDestroy()
    {
        _avatarButton.onClick.RemoveAllListeners();
        _collectCoinsButton.onClick.RemoveAllListeners();
    }


    private void OnCollectCoinsButtonClicked()
    {
        _collectCoinsButton.interactable = false;
        BCManager.Instance.BCWrapper.ScriptService.RunScript("AwardUserCoins", null, (string jsonResponse, object cbObject) =>
        {
            Debug.Log("User award coins successfully! " + jsonResponse);
            var data = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)["data"] as Dictionary<string, object>;
            int newCoinsBalance = Convert.ToInt32(data["response"]);
            /*
            AnimatedCoinCollect coinCollectAnim = Instantiate(_animatedCoinCollectPrefab, transform.parent);
            coinCollectAnim.newCoinsBalance = newCoinsBalance;
            coinCollectAnim.OnCoinAnimComplete += OnCoinCollectAnimComplete;
            */

            AppManager.Instance.AnimateDynamicAward(_coinIconRef, CurrencyType.Coins, () =>
            {
                OnCoinCollectAnimComplete(newCoinsBalance);
            });

            _collectCoinsButton.interactable = true;
        }, (int status, int reason, string errorJson, object _) =>
        {
            Debug.LogError("Failed to run AwardUserCoins script: " + errorJson);
        });
    }

    private void OnAvatarButtonClicked()
    {
        ToggleProfileModal(true);
    }

    public void OnFadeOutComplete()
    {
        AppManager.Instance.SwitchScenes("Login");
    }

    public void ToggleProfileModal(bool enabled)
    {
        _profileModalAnim.SetBool("ShowModal", enabled);
    }

    private void ToggleGoldenFrame(bool enabled)
    {
        _goldAvatarFrameDisplay.SetActive(enabled);
    }

    private void UpdateUserStatsDisplay()
    {

    }
}
