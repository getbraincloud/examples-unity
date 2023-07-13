using BrainCloud;
using BrainCloud.JSONHelper;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>
/// Example of how user currency can be handled via brainCloud's VirtualCurrency and PlayerState services.
/// </para>
///
/// <br><seealso cref="BrainCloudVirtualCurrency"/></br>
/// <br><seealso cref="BrainCloudPlayerState"/></br>
/// <br><seealso cref="BrainCloudPlayerStatistics"/></br>
/// <br><seealso cref="BrainCloudScript"/></br>
/// </summary>
/// VirtualCurrency API: https://getbraincloud.com/apidocs/apiref/?csharp#capi-virtualcurrency
/// PlayerState API: https://getbraincloud.com/apidocs/apiref/?csharp#capi-playerstate
public class CurrencyServiceUI : ContentUIBehaviour
{
    private const int MINIMUM_AWARD_AMOUNT = 0;
    private const int MAXIMUM_AWARD_AMOUNT = 99999;
    private const string DEFAULT_EMPTY_FIELD = "-";
    private const string DEFAULT_CURRENCY_TYPE = "gems";
    private const string CURRENCY_TYPE_FORMAT = "Currency Type: <b>{0}</b>";

    [Header("Main")]
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

    private BrainCloudScript scriptService = default;
    private BrainCloudPlayerState userStateService = default;
    private BrainCloudPlayerStatistics statsService = default;
    private BrainCloudVirtualCurrency currencyService = default;

    #region Unity Messages

    protected override void Awake()
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

        base.Awake();
    }

    private void OnEnable()
    {
        ResetButton.onClick.AddListener(OnResetButton);
        IncrementXPField.onEndEdit.AddListener((value) => ClampAwardAmount(IncrementXPField, value));
        IncrementXPButton.onClick.AddListener(OnIncrementXPButton);
        AwardGemsField.onEndEdit.AddListener((value) => ClampAwardAmount(AwardGemsField, value));
        AwardGemsButton.onClick.AddListener(OnAwardGemsButton);
        ConsumeGemsField.onEndEdit.AddListener((value) => ClampAwardAmount(ConsumeGemsField, value));
        ConsumeGemsButton.onClick.AddListener(OnConsumeGemsButton);
    }

    protected override void Start()
    {
        scriptService = BCManager.ScriptService;
        userStateService = BCManager.PlayerStateService;
        statsService = BCManager.PlayerStatisticsService;
        currencyService = BCManager.VirtualCurrencyService;

        InitializeUI();

        base.Start();
    }

    private void OnDisable()
    {
        ResetButton.onClick.RemoveAllListeners();
        IncrementXPField.onEndEdit.RemoveAllListeners();
        IncrementXPButton.onClick.RemoveAllListeners();
        AwardGemsField.onEndEdit.RemoveAllListeners();
        AwardGemsButton.onClick.RemoveAllListeners();
        ConsumeGemsField.onEndEdit.RemoveAllListeners();
        ConsumeGemsButton.onClick.RemoveAllListeners();
    }

    protected override void OnDestroy()
    {
        scriptService = null;
        userStateService = null;
        statsService = null;
        currencyService = null;

        base.OnDestroy();
    }

    #endregion

    #region UI

    protected override void InitializeUI()
    {
        IsInteractable = false;

        IncrementXPField.text = string.Empty;
        IncrementXPField.DisplayNormal();
        AwardGemsField.text = string.Empty;
        IncrementXPField.DisplayNormal();
        ConsumeGemsField.text = string.Empty;
        IncrementXPField.DisplayNormal();

        SuccessCallback initialSuccess = OnSuccess("User XP Updated", (response) =>
        {
            OnXPStateUpdate_Success(response);
            UpdateUserGems();
        });

        FailureCallback initialFailure = OnFailure("Cannot update User XP", () =>
        {
            OnXPStateUpdate_Failure();
            UpdateUserGems();
        });

        userStateService.ReadUserState(initialSuccess, initialFailure);
    }

    private void ClampAwardAmount(TMP_InputField field, string value)
    {
        field.text = value.Trim();
        if (!field.text.IsEmpty())
        {
            if (int.TryParse(field.text, out int result))
            {
                result = Mathf.Clamp(result, MINIMUM_AWARD_AMOUNT, MAXIMUM_AWARD_AMOUNT);
                field.text = result.ToString();
            }
            else
            {
                field.text = string.Empty;
            }
        }
    }

    private void OnIncrementXPButton()
    {
        if (int.TryParse(IncrementXPField.text, out int xpValue) && xpValue > 0)
        {
            IsInteractable = false;

            statsService.IncrementExperiencePoints(xpValue,
                                                   OnSuccess("User XP Updated", OnXPStateUpdate_Success),
                                                   OnFailure("Cannot update User XP", OnXPStateUpdate_Failure));
        }
        else
        {
            IncrementXPField.DisplayError();
            Debug.LogError("Please input a proper XP increment value.");
        }
    }

    private void OnAwardGemsButton()
    {
        if (int.TryParse(AwardGemsField.text, out int amount) && amount > 0)
        {
            IsInteractable = false;

            scriptService.RunScript("AwardCurrency",
                                    "{\"vcID\": \"gems\", \"vcAmount\": " + amount + "}",
                                    OnSuccess("AwardCurrency Script Ran Successfully", UpdateUserGems),
                                    OnFailure("AwardCurrency Script Failed", () => IsInteractable = true));
        }
        else
        {
            AwardGemsField.DisplayError();
            Debug.LogError("Please input a proper award value.");
        }
    }

    private void OnConsumeGemsButton()
    {
        if (ulong.TryParse(ConsumeGemsField.text, out ulong amount) && amount > 0)
        {
            IsInteractable = false;

            scriptService.RunScript("ConsumeCurrency",
                                    "{\"vcID\": \"gems\", \"vcAmount\": " + amount + "}",
                                    OnSuccess("ConsumeCurrency Script Ran Successfully", UpdateUserGems),
                                    OnFailure("ConsumeCurrency Script Failed", () => IsInteractable = true));
        }
        else
        {
            ConsumeGemsField.DisplayError();
            Debug.LogError("Please input a proper consume value.");
        }
    }

    private void UpdateUserGems()
    {
        currencyService.GetCurrency(DEFAULT_CURRENCY_TYPE,
                        OnSuccess($"User {DEFAULT_CURRENCY_TYPE} Updated", OnGemsStateUpdate_Success),
                        OnFailure($"Cannot update User {DEFAULT_CURRENCY_TYPE}", OnGemsStateUpdate_Failure));
    }

    private void OnResetButton()
    {
        IsInteractable = false;

        scriptService.RunScript("ResetCurrency", "{}",
                                OnSuccess("ResetCurrency Script Ran Successfully", UpdateUserGems),
                                OnFailure("ResetCurrency Script Failed", () => IsInteractable = true));
    }

    #endregion

    #region brainCloud

    private void OnXPStateUpdate_Success(string response)
    {
        var data = response.Deserialize("data");

        PlayerLevelField.text = data["experienceLevel"].ToString();
        XPAccruedField.text = data["experiencePoints"].ToString();
        IncrementXPField.text = string.Empty;
        IsInteractable = true;
    }

    private void OnXPStateUpdate_Failure()
    {
        IncrementXPField.text = string.Empty;
        IsInteractable = true;
    }

    private void OnGemsStateUpdate_Success(string response)
    {
        var data = response.Deserialize("data", "currencyMap");

        bool currencyFound = false;
        foreach (string key in data.Keys)
        {
            if (key == DEFAULT_CURRENCY_TYPE)
            {
                currencyFound = true;
                var gems = data[key] as Dictionary<string, object>;

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

    private void OnGemsStateUpdate_Failure()
    {
        AwardGemsField.text = string.Empty;
        ConsumeGemsField.text = string.Empty;

        IsInteractable = true;
    }

    #endregion
}
