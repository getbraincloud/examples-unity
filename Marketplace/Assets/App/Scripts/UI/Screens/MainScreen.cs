using BrainCloud.JsonFx;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainScreen : MonoBehaviour
{
    [SerializeField]
    private Button _avatarButton, _collectCoinsButton;

    [SerializeField]
    private TextMeshProUGUI _coinsAmountText, _gemsAmountText, _levelText;

    [SerializeField]
    private Image _xpBar, _userProfileImage, _avatarImage, _avatarFrameImage;

    [SerializeField]
    private Animator _profileModalAnim;
    [SerializeField]
    private AnimatedNumberIncrement _animatedNumberIncrementPrefab;
    [SerializeField]
    private AnimatedCoinCollect _animatedCoinCollectPrefab;
    [SerializeField]
    private LevelUpStarAnim _animatedLevelUpStarPrefab;

    private Animator _anim;

    private Coroutine _xpBarUpdateCoroutine;

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

        UpdateAllUserStatUI(AppManager.Instance.userData);
    }

    private void OnUserLevelUpdated(int level, int xpToNextLevel, string statusName)
    {
        LevelUpStarAnim starAnim = Instantiate(_animatedLevelUpStarPrefab, transform);
        starAnim.OnAnimComplete += () =>
        {
            _levelText.text = level.ToString();
        };
    }

    private void OnUserXPUpdated(int newXp)
    {
        UpdateXPBarUI((float)newXp / (float)AppManager.Instance.userData.XPToNextLevel);
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
    }

    private void UpdateAllUserStatUI(UserData userData)
    {
        _coinsAmountText.text = userData.Coins.ToString();
        _gemsAmountText.text = userData.Gems.ToString();
        _levelText.text = userData.Level.ToString();
        float fillAmount = (float)userData.CurrentXP / (float)userData.XPToNextLevel;

        Debug.Log("Initial xp bar fill amount " + fillAmount + " currentXp: " + userData.CurrentXP + " XPToNextLevel: "  + userData.XPToNextLevel) ;
        UpdateXPBarUI(fillAmount);
    }

    private void UpdateXPBarUI(float fillAmount)
    {
        if(_xpBarUpdateCoroutine != null)
        {
            StopCoroutine(_xpBarUpdateCoroutine);
            _xpBarUpdateCoroutine = StartCoroutine(UpdateXPBarUI_CR(fillAmount));
        }
        else
        {
            _xpBarUpdateCoroutine = StartCoroutine(UpdateXPBarUI_CR(fillAmount));
        }
    }

    private IEnumerator UpdateXPBarUI_CR(float fillAmount)
    {
        float t = 0;
        float originalFill = _xpBar.fillAmount;
        while(_xpBar.fillAmount != fillAmount)
        {
            _xpBar.fillAmount = Mathf.Lerp(originalFill, fillAmount, t);
            t += 0.01f;
            yield return null;
        }
        //xp bar filled
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
        _profileModalAnim.SetBool("ShowModal", true);
    }

    private void UpdateUserStatsDisplay()
    {

    }
}
