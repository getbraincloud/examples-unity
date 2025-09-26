using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static ExampleApp;

public class MainPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button LogoutButton = default;
    [SerializeField] private Button SendPushButton = default;
    [SerializeField] private Button OpenStoreButton = default;
    [SerializeField] private Button HistoryButton = default;

    private ExampleApp App = null;
    private BrainCloudWrapper BC = null;

    #region Unity Messages

    private void OnEnable()
    {
        LogoutButton.onClick.AddListener(OnLogoutButton);
        SendPushButton.onClick.AddListener(OnSendPushButton);
        OpenStoreButton.onClick.AddListener(OnOpenStoreButton);
        HistoryButton.onClick.AddListener(OnHistoryButton);
    }

    private void Start()
    {
        App = FindFirstObjectByType<ExampleApp>();

        StartCoroutine(GetBrainCloudWrapper());
    }

    private void OnDisable()
    {
        LogoutButton.onClick.RemoveAllListeners();
        SendPushButton.onClick.RemoveAllListeners();
        OpenStoreButton.onClick.RemoveAllListeners();
        HistoryButton.onClick.RemoveAllListeners();
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

        App.IsInteractable = true;
    }

    private void OnLogoutButton()
    {
        App.ChangePanelState(PanelState.Logout);
    }

    private void OnSendPushButton()
    {
        App.SendPushNotification(() =>
        {
            SendPushButton.interactable = false;

            Debug.Log($"Push notification request sent!");
            Debug.Log($"Push notifications are expensive to send so the button will remain disabled for this login.");
        });
    }

    private void OnOpenStoreButton()
    {
        App.ChangePanelState(PanelState.Store);
    }

    private void OnHistoryButton()
    {
        App.ChangePanelState(PanelState.History);
    }
}
