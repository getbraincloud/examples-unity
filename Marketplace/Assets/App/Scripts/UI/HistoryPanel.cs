using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
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

        if (App != null)
        {
            GetUserItemHistory();
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

        GetUserItemHistory();
    }

    #region UI

    private void GetUserItemHistory()
    {
        const string APP_STORE =
#if UNITY_ANDROID
            "googlePlay";
#elif UNITY_IOS
            "itunes";
#else
            "";
#endif

    App.IsInteractable = false;

        HistoryContent.gameObject.SetActive(false);
        HistoryInfoText.gameObject.SetActive(true);
        HistoryInfoText.text = GET_HISTORY_TEXT;

        void onGetTransactionHistory(BCProduct[] inventory)
        {
            
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

            HistoryScroll.verticalNormalizedPosition = 1.0f;

            App.IsInteractable = true;
        }

        void onSuccess(string jsonResponse, object cbObject)
        {
            onGetTransactionHistory(null);
        }

        void onFailure(int status, int reasonCode, string jsonError, object cbObject)
        {
            onGetTransactionHistory(null);
        }

        BC.ScriptService.RunScript("GetTransactionHistory",
                                   JsonWriter.Serialize(new Dictionary<string, object>()
                                   {
                                       {
                                           "pagination", new Dictionary<string, object>()
                                            {
                                                { "rowsPerPage", 50 },
                                                { "pageNumber", 1 }
                                            }
                                       },
                                       {
                                            "searchCriteria", new Dictionary<string, object>()
                                            {
                                                { "profileId", BC.GetStoredProfileId() },
                                                { "type", APP_STORE },
                                                { "pending", true }
                                            }
                                       },
                                       {
                                            "sortCriteria", new Dictionary<string, object>()
                                            {
                                                { "createdAt", -1 }
                                            }
                                       }
                                   }),
                                   onSuccess,
                                   onFailure);

        onGetTransactionHistory(null);
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
