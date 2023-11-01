using System;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

/// <summary>
/// Simple button that holds our <see cref="BCProduct"/> data.
/// </summary>
[RequireComponent(typeof(Image), typeof(Button))]
public class IAPButton : MonoBehaviour
{
    private static readonly Color GAME_CURRENCY_TEXT_COLOR       = new Color32(030, 000, 080, 255);
    private static readonly Color GAME_CURRENCY_BACKGROUND_COLOR = new Color32(200, 170, 255, 255);
    private static readonly Color REAL_CURRENCY_TEXT_COLOR       = new Color32(050, 035, 000, 255);
    private static readonly Color REAL_CURRENCY_BACKGROUND_COLOR = new Color32(255, 255, 150, 255);
    private static readonly Color SUBSCRIPTION_TEXT_COLOR        = new Color32(000, 060, 015, 255);
    private static readonly Color SUBSCRIPTION_BACKGROUND_COLOR  = new Color32(150, 255, 150, 255);

    [SerializeField] private TMP_Text ItemLabel = default;
    [SerializeField] private TMP_Text DescriptionLabel = default;
    [SerializeField] private TMP_Text CostLabel = default;
    [SerializeField] private Image SeparatorLine = default;

    private Image BGImage = default;
    private Button SelfButton = default;

    public Action<BCProduct> OnButtonAction { get; set; }

    public BCProduct ProductData { get; private set; }

    public bool IsInteractable
    {
        get => SelfButton.interactable;
        set => SelfButton.interactable = value;
    }

#region Unity Messages

    private void Awake()
    {
        BGImage = GetComponent<Image>();
        SelfButton = GetComponent<Button>();
        ItemLabel.text = string.Empty;
        DescriptionLabel.text = string.Empty;
        CostLabel.text = string.Empty;
    }

    private void OnEnable()
    {
        SelfButton.onClick.AddListener(OnPurchaseButton);
    }

    private void OnDisable()
    {
        SelfButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        BGImage = null;
        SelfButton = null;
        ProductData = null;
    }

    #endregion

    public void SetProductDetails(BCProduct data, string gameCurrencyName = "")
    {
        bool usesRealCurrency = string.IsNullOrWhiteSpace(gameCurrencyName);

        ItemLabel.text = data.title;
        DescriptionLabel.text = data.description;
        CostLabel.text = usesRealCurrency ? $"{data.GetLocalizedPriceString()}" : $"{data.GetCurrencyAmount(gameCurrencyName)}\n{gameCurrencyName}!";

        switch (data.IAPProductType)
        {
            case ProductType.Consumable:
            case ProductType.NonConsumable:
                ItemLabel.color = usesRealCurrency ? REAL_CURRENCY_TEXT_COLOR : GAME_CURRENCY_TEXT_COLOR;
                DescriptionLabel.color = usesRealCurrency ? REAL_CURRENCY_TEXT_COLOR : GAME_CURRENCY_TEXT_COLOR;
                CostLabel.color = usesRealCurrency ? REAL_CURRENCY_TEXT_COLOR : GAME_CURRENCY_TEXT_COLOR;
                SeparatorLine.color = usesRealCurrency ? REAL_CURRENCY_TEXT_COLOR : REAL_CURRENCY_TEXT_COLOR;
                BGImage.color = usesRealCurrency ? REAL_CURRENCY_BACKGROUND_COLOR : GAME_CURRENCY_BACKGROUND_COLOR;
                break;
            case ProductType.Subscription:
                ItemLabel.color = SUBSCRIPTION_TEXT_COLOR;
                DescriptionLabel.color = SUBSCRIPTION_TEXT_COLOR;
                CostLabel.color = SUBSCRIPTION_TEXT_COLOR;
                SeparatorLine.color = SUBSCRIPTION_TEXT_COLOR;
                BGImage.color = SUBSCRIPTION_BACKGROUND_COLOR;
                CostLabel.text += "\nmonthly";
                break;
            default:
                usesRealCurrency = false;
                goto case ProductType.Consumable;
        }

        ProductData = data;
    }

    private void OnPurchaseButton() => OnButtonAction?.Invoke(ProductData);
}
