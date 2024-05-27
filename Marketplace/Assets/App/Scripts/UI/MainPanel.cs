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

    private ExampleApp App = null;
    private BrainCloudWrapper BC = null;

    #region Unity Messages

    private void OnEnable()
    {
        LogoutButton.onClick.AddListener(OnLogoutButton);
        SendPushButton.onClick.AddListener(OnSendPushButton);
        OpenStoreButton.onClick.AddListener(OnOpenStoreButton);
    }

    private void Start()
    {
        App = FindObjectOfType<ExampleApp>();

        StartCoroutine(GetBrainCloudWrapper());
    }

    private void OnDisable()
    {
        LogoutButton.onClick.RemoveAllListeners();
        SendPushButton.onClick.RemoveAllListeners();
        OpenStoreButton.onClick.RemoveAllListeners();
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

        App.IsInteractable = true;
    }

    private void OnLogoutButton()
    {
        App.IsInteractable = false;

        void onSuccess(string jsonResponse, object cbObject)
        {
            Debug.Log($"Logout success!");

            App.ChangePanelState(PanelState.Login);
            App.GetStoredUserIDs();
            App.IsInteractable = true;
        };

        void onFailure(int status, int reason, string jsonError, object cbObject)
        {
            App.OnBrainCloudError(status, reason, jsonError, cbObject);

            Debug.LogError($"Logout failed!");
            Debug.LogError($"Try restarting the app...");

            App.GetStoredUserIDs();
        }

        BC.Logout(true, onSuccess, onFailure, this);
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
}
