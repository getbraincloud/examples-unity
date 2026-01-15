using Oculus.Platform.Models;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IAPItem : MonoBehaviour
{
    [SerializeField] private TMP_Text TitleLabel = null;
    [SerializeField] private TMP_Text DescriptionLabel = null;
    [SerializeField] private Button BuyButton = null;
    [SerializeField] private TMP_Text ButtonLabel = null;

    private Action<string> BuyButtonAction = null;

    public BCProduct Product { get; private set; }
    public string IAPSku => Product.IAPSku;

    private void OnEnable()
    {
        BuyButton.onClick.AddListener(OnBuyButton);
    }

    private void OnDisable()
    {
        BuyButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        BuyButtonAction = null;
    }

    private void OnBuyButton()
    {
        BuyButtonAction?.Invoke(IAPSku);
    }

    public void InitializeIAPItem(BCProduct product, Action<string> onBuyButton)
    {
        Product = product;
        TitleLabel.text = product.title;
        DescriptionLabel.text = product.description;
        ButtonLabel.text = $"${product.priceData.referencePrice}";
        BuyButtonAction = onBuyButton;
    }

    public void UpdatePrice()
    {
        if (Product.MetaProduct != null)
        {
            ButtonLabel.text = Product.MetaProduct.FormattedPrice;
        }
        else
        {
            Debug.LogError($"Cannot set to Formatted price because MetaProduct is null. BCProduct: {Product.itemId}");
        }
    }

    public void SetToPurchased()
    {
        BuyButtonAction = null;
        BuyButton.onClick.RemoveAllListeners();
        BuyButton.interactable = false;
        ButtonLabel.text = "Got!";
    }
}
