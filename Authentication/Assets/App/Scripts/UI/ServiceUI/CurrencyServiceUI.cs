using BrainCloud;
using BrainCloud.JsonFx;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyServiceUI : MonoBehaviour, IServiceUI
{
    private const string DEFAULT_EMPTY_FIELD = "-";
    private const string DEFAULT_CURRENCY_TYPE = "gems";
    private const string CURRENCY_TYPE_FORMAT = "Currency Type: <b>{0}</b>";

    [Header("Main")]
    [SerializeField] private CanvasGroup UICanvasGroup = default;
    [SerializeField] private Button ResetButton = default;

    [Header("XP Management")]
    [SerializeField] private TMP_Text PlayerLevelField = default;
    [SerializeField] private TMP_Text XPAccruedField = default;
    [SerializeField] private TMP_InputField IncrementXPField = default;
    [SerializeField] private Button IncrementXPButton = default;

    [Header("Currency Management")]
    [SerializeField] private TMP_Text CurrencyTypeLabel = default;
    [SerializeField] private TMP_Text BalanceField = default;
    [SerializeField] private TMP_Text ConsumedField = default;
    [SerializeField] private TMP_Text AwardedField = default;
    [SerializeField] private TMP_Text PurchasedField = default;
    [SerializeField] private TMP_InputField AwardGemsField = default;
    [SerializeField] private Button AwardGemsButton = default;
    [SerializeField] private TMP_InputField ConsumeGemsField = default;
    [SerializeField] private Button ConsumeGemsButton = default;

    public bool IsInteractable
    {
        get { return UICanvasGroup.interactable; }
        set { UICanvasGroup.interactable = value; }
    }

    private BrainCloudScript scriptService = default;
    private BrainCloudPlayerState userStateService = default;
    private BrainCloudPlayerStatistics statsService = default;
    private BrainCloudVirtualCurrency currencyService = default;

    #region Unity Messages

    private void Awake()
    {
        PlayerLevelField.text = DEFAULT_EMPTY_FIELD;
        XPAccruedField.text = DEFAULT_EMPTY_FIELD;
        IncrementXPField.text = string.Empty;
        CurrencyTypeLabel.text = string.Format(CURRENCY_TYPE_FORMAT, DEFAULT_CURRENCY_TYPE.ToUpper());
        BalanceField.text = DEFAULT_EMPTY_FIELD;
        ConsumedField.text = DEFAULT_EMPTY_FIELD;
        AwardedField.text = DEFAULT_EMPTY_FIELD;
        PurchasedField.text = DEFAULT_EMPTY_FIELD;
        AwardGemsField.text = string.Empty;
        ConsumeGemsField.text = string.Empty;
    }

    private void OnEnable()
    {
        ResetButton.onClick.AddListener(OnResetButton);
        IncrementXPButton.onClick.AddListener(OnIncrementXPButton);
        AwardGemsButton.onClick.AddListener(OnAwardGemsButton);
        ConsumeGemsButton.onClick.AddListener(OnConsumeGemsButton);
    }

    private void Start()
    {
        IsInteractable = false;

        scriptService = BCManager.ScriptService;
        userStateService = BCManager.PlayerStateService;
        statsService = BCManager.PlayerStatisticsService;
        currencyService = BCManager.VirtualCurrencyService;

        userStateService.ReadUserState(OnXPStateUpdate_Success,
                                       OnXPStateUpdate_Failure);
    }

    private void OnDisable()
    {
        ResetButton.onClick.RemoveAllListeners();
        IncrementXPButton.onClick.RemoveAllListeners();
        AwardGemsButton.onClick.RemoveAllListeners();
        ConsumeGemsButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        scriptService = null;
        userStateService = null;
        statsService = null;
        currencyService = null;
    }

    #endregion

    #region UI

    private void OnIncrementXPButton()
    {
        if (int.TryParse(IncrementXPField.text, out int xpValue) && xpValue > 0)
        {
            IsInteractable = false;

            statsService.IncrementExperiencePoints(xpValue,
                                                   OnXPStateUpdate_Success,
                                                   OnXPStateUpdate_Failure);
        }
    }

    private void OnAwardGemsButton()
    {
        if (int.TryParse(AwardGemsField.text, out int amount) && amount > 0)
        {
            IsInteractable = false;

            scriptService.RunScript("AwardCurrency",
                                    "{\"vcID\": \"gems\", \"vcAmount\": " + amount + "}",
                                    BCManager.CreateSuccessCallback("AwardCurrency Script Ran Successfully", UpdateUserGems),
                                    BCManager.CreateFailureCallback("AwardCurrency Script Failed", () => IsInteractable = true));
        }
    }

    private void OnConsumeGemsButton()
    {
        if (ulong.TryParse(ConsumeGemsField.text, out ulong amount) && amount > 0)
        {
            IsInteractable = false;

            scriptService.RunScript("ConsumeCurrency",
                                    "{\"vcID\": \"gems\", \"vcAmount\": " + amount + "}",
                                    BCManager.CreateSuccessCallback("ConsumeCurrency Script Ran Successfully", UpdateUserGems),
                                    BCManager.CreateFailureCallback("ConsumeCurrency Script Failed", () => IsInteractable = true));
        }
    }

    private void UpdateUserGems()
    {
        currencyService.GetCurrency(DEFAULT_CURRENCY_TYPE,
                        OnGemsStateUpdate_Success,
                        OnGemsStateUpdate_Failure);
    }

    private void OnResetButton()
    {
        IsInteractable = false;

        scriptService.RunScript("ResetCurrency", "{}",
                                BCManager.CreateSuccessCallback("ResetCurrency Script Ran Successfully", UpdateUserGems),
                                BCManager.CreateFailureCallback("ResetCurrency Script Failed", () => IsInteractable = true));
    }

    #endregion

    #region brainCloud

    private void OnXPStateUpdate_Success(string response, object _)
    {
        BCManager.LogMessage("User XP Updated", response);

        var responseObj = JsonReader.Deserialize(response) as Dictionary<string, object>;
        var data = responseObj["data"] as Dictionary<string, object>;

        PlayerLevelField.text = data["experienceLevel"].ToString();
        XPAccruedField.text = data["experiencePoints"].ToString();
        IncrementXPField.text = string.Empty;

        UpdateUserGems();
    }

    private void OnXPStateUpdate_Failure(int status, int code, string error, object _)
    {
        BCManager.LogError("Cannot update User XP", status, code, error);

        IncrementXPField.text = string.Empty;
        UpdateUserGems();
    }

    private void OnGemsStateUpdate_Success(string response, object _)
    {
        BCManager.LogMessage($"User {DEFAULT_CURRENCY_TYPE} Updated", response);

        var responseObj = JsonReader.Deserialize(response) as Dictionary<string, object>;
        var data = responseObj["data"] as Dictionary<string, object>;
        var currencyMap = data["currencyMap"] as Dictionary<string, object>;

        bool currencyFound = false;
        foreach (string key in currencyMap.Keys)
        {
            if (key == DEFAULT_CURRENCY_TYPE)
            {
                currencyFound = true;
                var gems = currencyMap[key] as Dictionary<string, object>;

                BalanceField.text = gems["balance"].ToString();
                ConsumedField.text = gems["consumed"].ToString();
                AwardedField.text = gems["awarded"].ToString();
                PurchasedField.text = gems["purchased"].ToString();

                break;
            }
        }

        if(!currencyFound)
        {
            Debug.LogError($"Could not find currency type: {DEFAULT_CURRENCY_TYPE}");
        }

        AwardGemsField.text = string.Empty;
        ConsumeGemsField.text = string.Empty;

        IsInteractable = true;
    }

    private void OnGemsStateUpdate_Failure(int status, int code, string error, object _)
    {
        BCManager.LogError($"Cannot update User {DEFAULT_CURRENCY_TYPE}", status, code, error);

        AwardGemsField.text = string.Empty;
        ConsumeGemsField.text = string.Empty;

        IsInteractable = true;
    }

    #endregion
}
