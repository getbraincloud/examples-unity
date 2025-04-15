using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ExampleApp;

public class HistoryPanel : MonoBehaviour
{
    private const string GET_HISTORY_TEXT = "Getting history...";
    private const string GEMS_ENERGY_TEXT = "Gems: {0}   ||   Energy {1}";
    private const string NO_TRANSACTION_HISTORY_TEXT = "No transaction history.";
    private const string ERROR_HISTORY_TEXT = "Error trying to get history.";

    [Header("UI Elements")]
    [SerializeField] private Button BackButton = default;
    [SerializeField] private ScrollRect HistoryScroll = default;
    [SerializeField] private Transform HistoryContent = default;
    [SerializeField] private TMP_Text HistoryInfoText = default;
    [SerializeField] private TMP_Text CurrencyInfoText = default;
    [SerializeField] private TMP_Text ItemInfoText = default;

    [Header("Templates")]
    [SerializeField] private TMP_Text HistoryItemTextTemplate = default;

    private ExampleApp App = null;
    private BrainCloudWrapper BC = null;

    #region Unity Messages

    private void OnEnable()
    {
        BackButton.onClick.AddListener(OnBackButton);

        if (BrainCloudMarketplace.IsInitialized)
        {
            GetTransactionHistory();
        }
    }

    private void Start()
    {
        App = FindObjectOfType<ExampleApp>();

        StartCoroutine(GetBrainCloudWrapper());
    }

    private void OnDisable()
    {
        BackButton.onClick.RemoveAllListeners();
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
            BC = BC == null ? FindObjectOfType<BrainCloudWrapper>() : BC;

            return BC != null && BC.Client != null && BC.Client.IsInitialized();
        });

        yield return null;

        App.IsInteractable = true;
    }

    #region UI

    private void GetTransactionHistory()
    {
        //App.IsInteractable = false;

        HistoryContent.gameObject.SetActive(false);
        HistoryInfoText.gameObject.SetActive(true);
        HistoryInfoText.text = GET_HISTORY_TEXT;

        void onGetTransactionHistory(BCProduct[] inventory)
        {
            App.IsInteractable = true;
            //if (inventory == null || inventory.Length < 0 || BrainCloudMarketplace.HasErrorOccurred)
            //{
            //    HistoryInfoText.text = BrainCloudMarketplace.HasErrorOccurred ? ERROR_HISTORY_TEXT : NO_TRANSACTION_HISTORY_TEXT;
            //    return;
            //}
            //
            //foreach (var product in inventory)
            //{
            //    var iapButton = Instantiate(IAPButtonTemplate, IAPContent, false);
            //    iapButton.SetProductDetails(product);
            //
            //    if (BrainCloudMarketplace.OwnsNonconsumable(product) ||
            //        BrainCloudMarketplace.HasSubscription(product))
            //    {
            //        iapButton.IsInteractable = false;
            //    }
            //    else
            //    {
            //        iapButton.OnButtonAction += OnPurchaseBCProduct;
            //    }
            //}
            //
            //UpdateUserData();
            //HistoryInfoText.gameObject.SetActive(false);
        }

        //BrainCloudMarketplace.FetchProducts(onFetchProducts);

        HistoryScroll.verticalNormalizedPosition = 1.0f;
    }

    private void UpdateUserData()
    {
        CurrencyInfoText.text = GEMS_ENERGY_TEXT;
    }

    private void OnBackButton()
    {
        var texts = HistoryContent.GetComponentsInChildren<TMP_Text>();
        for (int i = 0; i < texts.Length; i++)
        {
            var text = texts[i];
            if (text != CurrencyInfoText &&
                text != ItemInfoText &&
                text != HistoryItemTextTemplate)
            {
                Destroy(text.gameObject);
            }
        }

        App.ChangePanelState(PanelState.Main);
    }

    #endregion
}
