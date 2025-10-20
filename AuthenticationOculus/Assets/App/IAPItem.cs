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
        BuyButtonAction?.Invoke(Product.ItemSku);
    }

    public void InitializeIAPItem(BCProduct product, Action<string> onBuyButton)
    {
        Product = product;
        TitleLabel.text = product.title;
        DescriptionLabel.text = product.description;
        ButtonLabel.text = product.MetaProduct.FormattedPrice;
        BuyButtonAction = onBuyButton;
    }

    public void SetToPurchased()
    {
        BuyButtonAction = null;
        BuyButton.onClick.RemoveAllListeners();
        BuyButton.interactable = false;
        ButtonLabel.text = "Got!";
    }
}
