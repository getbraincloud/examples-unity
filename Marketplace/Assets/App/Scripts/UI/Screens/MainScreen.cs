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
    private Button _avatarButton, _collectCoinsButton;
    //GameObjects
    [SerializeField]
    private GameObject _collectCoinsBaseMessageDisplay,
                        _goldAvatarFrameDisplay;
    //UI Text
    [SerializeField]
    private TextMeshProUGUI _coinsAmountText,
                            _gemsAmountText,
                            _levelText,
                            _collectCoinsBaseAmountText,
                            _currentMarketplaceText;
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
    [SerializeField]
    private LevelUpStarAnim _animatedLevelUpStarPrefab;
    [SerializeField]
    private Sprite defaultAvatarImage;
    


    private Animator _anim;
    private Coroutine _xpBarUpdateCoroutine;
    private CoinMultiplierStatus _coinMultipierStatus;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _avatarButton.onClick.AddListener(OnAvatarButtonClicked);
        _collectCoinsButton.onClick.AddListener(OnCollectCoinsButtonClicked);
        AppManager.Instance.OnCoinsUpdated += OnCoinsUpdated;
        AppManager.Instance.OnGemsUpdated += OnGemsUpdated;
        AppManager.Instance.OnUserXPUpdated += OnUserXPUpdated;
        AppManager.Instance.OnUserLevelUpdated += OnUserLevelUpdated;
        AppManager.Instance.OnMultiplierActivated += OnMultiplierActivated;

        InventoryService.Instance.OnItemEquipChange += OnItemEquipChange;

        UpdateAllUserStatUI(AppManager.Instance.userData);
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
        LevelUpStarAnim starAnim = Instantiate(_animatedLevelUpStarPrefab, transform);
        starAnim.OnAnimComplete += () =>
        {
            _levelText.text = level.ToString();
        };

        _collectCoinsBaseAmountText.text = level.ToString();
    }

    private void OnUserXPUpdated(int newXp)
    {
        float fillAmount = (float)newXp / (float)AppManager.Instance.userData.XPToNextLevel;
        fillAmount = Mathf.Clamp(fillAmount, 0f, 1f);
        UpdateXPBarUI(fillAmount, 0.75f);
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
        AppManager.Instance.OnCoinsUpdated -= OnCoinsUpdated;
        AppManager.Instance.OnGemsUpdated -= OnGemsUpdated;
        AppManager.Instance.OnUserXPUpdated -= OnUserXPUpdated;
        AppManager.Instance.OnUserLevelUpdated -= OnUserLevelUpdated;
        AppManager.Instance.OnMultiplierActivated -= OnMultiplierActivated;

        InventoryService.Instance.OnItemEquipChange -= OnItemEquipChange;
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
            ProfileModal pModal = _profileModalAnim.GetComponent<ProfileModal>();
            //this could be any color shirt
            if (itemData.isEquipped)
            {
                Sprite newProfileImage = await ImageCacheService.Instance.GetImageAsync(itemData.imageUrl);
                _avatarImage.sprite = newProfileImage;
                _userProfileImage.sprite = newProfileImage;

                //update profile image in profile modal
                pModal.UpdateProfileImage(newProfileImage);
            }
            else
            {
                _avatarImage.sprite = defaultAvatarImage;
                _userProfileImage.sprite = defaultAvatarImage;
                pModal.UpdateProfileImage(defaultAvatarImage);
            }
        }
    }

    private void UpdateAllUserStatUI(UserData userData)
    {
        _coinsAmountText.text = userData.Coins.ToString();
        _gemsAmountText.text = userData.Gems.ToString();
        _levelText.text = userData.Level.ToString();
        float fillAmount = (float)userData.CurrentXP / (float)userData.XPToNextLevel;
        fillAmount = Mathf.Clamp(fillAmount, 0f, 1f);

        _collectCoinsBaseAmountText.text = userData.Level.ToString();

        Debug.Log("Initial xp bar fill amount " + fillAmount + " currentXp: " + userData.CurrentXP + " XPToNextLevel: "  + userData.XPToNextLevel) ;
        UpdateXPBarUI(fillAmount, 0.25f);

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

    private void UpdateXPBarUI(float fillAmount, float seconds, bool fromLevelUp = false)
    {
        if (_xpBarUpdateCoroutine != null)
        {
            StopCoroutine(_xpBarUpdateCoroutine);
        }

        _xpBarUpdateCoroutine = StartCoroutine(UpdateXPBarUI_CR(fillAmount, seconds, fromLevelUp));
    }

    private IEnumerator UpdateXPBarUI_CR(float fillAmount, float seconds, bool fromLevelUp = false)
    {
        float t = 0;
        RectTransform barRect = _xpBar.gameObject.GetComponent<RectTransform>();
        float originalFill = barRect.offsetMax.x;
        float fill = Globals.XP_BAR_FILL * -fillAmount;
        float targetFill = -(Globals.XP_BAR_FILL + fill);
        Debug.Log($"Original Fill: {originalFill} FillAmount: {fill} Target: {targetFill}");
        bool leveledUp = false;

        if (fromLevelUp)
        {
            //reset xp bar fill
            originalFill = -Globals.XP_BAR_FILL;
        }

        if(targetFill < originalFill && !fromLevelUp)
        {
            //This is a level up, fill bar completely before resetting it
            targetFill = 0;
            leveledUp = true;
        }

        while (t <= seconds)
        {
            float lerpedAmount = Mathf.Lerp(originalFill, targetFill, t / seconds);
            barRect.offsetMax = new Vector2(lerpedAmount, 0);

            t += Time.deltaTime;
            yield return null;
        }

        barRect.offsetMax = new Vector2(targetFill, 0);

        if (leveledUp)
        {
            UpdateXPBarUI(fillAmount, seconds, true);
        }
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

            AnimatedCoinCollect coinCollectAnim = Instantiate(_animatedCoinCollectPrefab, transform.parent);
            coinCollectAnim.newCoinsBalance = newCoinsBalance;
            coinCollectAnim.OnCoinAnimComplete += OnCoinCollectAnimComplete;

            
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
