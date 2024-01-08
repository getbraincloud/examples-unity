using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ExampleApp;

public class StorePanel : MonoBehaviour
{
    private const string OPENING_STORE_TEXT = "Getting products...";
    private const string NO_PRODUCTS_STORE_TEXT = "No products found.";
    private const string ERROR_STORE_TEXT = "Error trying to receive products.";

    [Header("UI Elements")]
    [SerializeField] private TMP_Text CurrencyInfoText = default;
    [SerializeField] private Button CloseStoreButton = default;
    [SerializeField] private Transform IAPContent = default;
    [SerializeField] private TMP_Text StoreInfoText = default;

    [Header("Templates")]
    [SerializeField] private IAPButton IAPButtonTemplate = default;

    private ExampleApp App = null;
    private BrainCloudWrapper BC = null;

    #region Unity Messages

    private void OnEnable()
    {
        CloseStoreButton.onClick.AddListener(OnCloseStoreButton);

        if (BrainCloudMarketplace.IsInitialized)
        {
            GetProducts();
        }
    }

    private void Start()
    {
        App = FindObjectOfType<ExampleApp>();

        StartCoroutine(GetBrainCloudWrapper());
    }

    private void OnDisable()
    {
        CloseStoreButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        BC = null;
        App = null;
    }

    #endregion

    private IEnumerator GetBrainCloudWrapper()
    {
        App.IsInteractable = false;

        yield return new WaitUntil(() =>
        {
            BC = BC ?? FindObjectOfType<BrainCloudWrapper>();

            return BC != null && BC.Client != null && BC.Client.IsInitialized();
        });

        yield return null;

        GetProducts();
    }

    #region UI

    private void GetProducts()
    {
        App.IsInteractable = false;

        CurrencyInfoText.text = "---";
        StoreInfoText.gameObject.SetActive(true);
        StoreInfoText.text = OPENING_STORE_TEXT;

        void onFetchProducts(BCProduct[] inventory)
        {
            App.IsInteractable = true;
            if (inventory == null || inventory.Length < 0 || BrainCloudMarketplace.HasErrorOccurred)
            {
                StoreInfoText.text = BrainCloudMarketplace.HasErrorOccurred ? ERROR_STORE_TEXT : NO_PRODUCTS_STORE_TEXT;
                return;
            }

            foreach (var product in inventory)
            {
                var iapButton = Instantiate(IAPButtonTemplate, IAPContent, false);
                iapButton.SetProductDetails(product);

                if (BrainCloudMarketplace.OwnsNonconsumable(product) ||
                    BrainCloudMarketplace.HasSubscription(product))
                {
                    iapButton.IsInteractable = false;
                }
                else
                {
                    iapButton.OnButtonAction += OnPurchaseBCProduct;
                }
            }

            UpdateUserData();
            StoreInfoText.gameObject.SetActive(false);
        }

        BrainCloudMarketplace.FetchProducts(onFetchProducts);
    }

    private void UpdateUserData()
    {
        CurrencyInfoText.text = "---";
    }

    private void OnCloseStoreButton()
    {
        var iapButtons = IAPContent.GetComponentsInChildren<IAPButton>();
        for (int i = 0; i < iapButtons.Length; i++)
        {
            Destroy(iapButtons[i].gameObject);
        }

        App.ChangePanelState(PanelState.Main);
    }

    private void OnPurchaseBCProduct(BCProduct product)
    {
        App.IsInteractable = false;

        void onPurchaseFinished(BCProduct[] purchasedProducts)
        {
            if (purchasedProducts != null && purchasedProducts.Length > 0)
            {
                IAPButton[] buttons = IAPContent.GetComponentsInChildren<IAPButton>();
                foreach (var item in purchasedProducts)
                {
                    Debug.Log($"Purchase Success: {item.title} (ID: {item.GetProductID()} | Price: {item.GetLocalizedPrice()} | Type: {item.IAPProductType})");

                    for (int i = 0; i < buttons.Length; i++)
                    {
                        if (buttons[i].ProductData == item &&
                            (BrainCloudMarketplace.OwnsNonconsumable(product) ||
                             BrainCloudMarketplace.HasSubscription(product)))
                        {
                            buttons[i].OnButtonAction -= OnPurchaseBCProduct;
                            buttons[i].IsInteractable = false;
                            break;
                        }
                    }
                }
            }

            App.IsInteractable = true;
        }

        BrainCloudMarketplace.PurchaseProduct(product, onPurchaseFinished);
    }

    private void OnRedeemBCItem(BCItem item)
    {
        //App.IsInteractable = false;

        // TODO: Be able to redeem currencies for items

        //Debug.Log($"Redeeming {item.defId} x{item.quantity}");
    }

    #endregion
}
