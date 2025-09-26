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

    private int baseContentCount = 0;
    private ExampleApp App = null;
    private BrainCloudWrapper BC = null;

    #region Unity Messages

    private void OnEnable()
    {
        BackButton.onClick.AddListener(OnBackButton);

        if (App != null)
        {
            GetUserItemHistory();
        }
    }

    private void Start()
    {
        App = FindFirstObjectByType<ExampleApp>();

        baseContentCount = HistoryContent.childCount;

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
            BC = BC == null ? FindFirstObjectByType<BrainCloudWrapper>() : BC;

            return BC != null && BC.Client != null && BC.Client.IsInitialized();
        });

        yield return null;

        GetUserItemHistory();
    }

    #region UI

    private void GetUserItemHistory()
    {
        App.IsInteractable = false;

        int pageNum = 1;
        HistoryContent.gameObject.SetActive(false);
        HistoryInfoText.gameObject.SetActive(true);
        HistoryInfoText.text = GET_HISTORY_TEXT;

        void onGetTransactionHistory(BCTransactionPage page)
        {
            bool isSuccess = !BrainCloudMarketplace.HasErrorOccurred;

            if (page != null && page.count > 0)
            {
                foreach (var item in page.items)
                {
                    var lineItem = Instantiate(HistoryItemTextTemplate, HistoryContent);
                    lineItem.text = $"<b>{item.title}</b> ({item.itemId} on {item.type}) {(item.pending ? " (Purchase Pending)" : string.Empty)}";
                    lineItem.gameObject.SetActive(true);
                }
            }
            else if (HistoryContent.childCount <= baseContentCount)
            {
                HistoryInfoText.text = isSuccess ? NO_TRANSACTION_HISTORY_TEXT : ERROR_HISTORY_TEXT;
            }

            if (isSuccess && page.moreAfter)
            {
                BrainCloudMarketplace.GetTransactionHistory(onGetTransactionHistory, ++pageNum);
            }
            else
            {
                HistoryContent.gameObject.SetActive(HistoryContent.childCount > baseContentCount);
                HistoryInfoText.gameObject.SetActive(!HistoryContent.gameObject.activeSelf);
                HistoryScroll.verticalNormalizedPosition = 1.0f;
                App.IsInteractable = true;
            }
        }

        if (!BrainCloudMarketplace.IsInitialized)
        {
            BrainCloudMarketplace.FetchProducts((_) =>
            {
                BrainCloudMarketplace.GetTransactionHistory(onGetTransactionHistory);
            });

            return;
        }

        BrainCloudMarketplace.GetTransactionHistory(onGetTransactionHistory);
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
