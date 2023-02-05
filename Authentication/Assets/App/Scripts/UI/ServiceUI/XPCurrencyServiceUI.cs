using BrainCloud;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XPCurrencyServiceUI : MonoBehaviour, IServiceUI
{
    private const string DEFAULT_EMPTY_FIELD = "-";
    private const string DEFAULT_CURRENCY_TYPE = "Gems";
    private const string CURRENCY_TYPE_FORMAT = "Currency Type: {0}";

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

    private BrainCloudPlayerState playerStateService = default;

    #region Unity Messages

    private void Awake()
    {
        PlayerLevelField.text = DEFAULT_EMPTY_FIELD;
        XPAccruedField.text = DEFAULT_EMPTY_FIELD;
        IncrementXPField.text = string.Empty;
        CurrencyTypeLabel.text = string.Format(CURRENCY_TYPE_FORMAT, DEFAULT_CURRENCY_TYPE);
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

        playerStateService = BCManager.PlayerStateService;

        // TODO: Get Player XP & Currency
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
        playerStateService = null;
    }

    #endregion

    #region UI

    private void OnResetButton()
    {

    }

    private void OnIncrementXPButton()
    {

    }

    private void OnAwardGemsButton()
    {

    }

    private void OnConsumeGemsButton()
    {

    }

    #endregion

    #region Service

    private void HandleServiceFunction()
    {

    }

    #endregion
}
