using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static ExampleApp;

public class LogoutPanel : MonoBehaviour
{
    private const int LOGOUT_COUNT_NEEDED = 3;

    [Header("UI Elements")]
    [SerializeField] private Button BackButton = default;
    [SerializeField] private Button LogoutButton = default;

    private ExampleApp App = null;
    private BrainCloudWrapper BC = null;

    private int logoutCount = 0;

    #region Unity Messages

    private void OnEnable()
    {
        logoutCount = 0;
        BackButton.onClick.AddListener(OnBackButton);
        LogoutButton.onClick.AddListener(OnLogoutButton);
    }

    private void Start()
    {
        App = FindObjectOfType<ExampleApp>();

        StartCoroutine(GetBrainCloudWrapper());
    }

    private void OnDisable()
    {
        BackButton.onClick.RemoveAllListeners();
        LogoutButton.onClick.RemoveAllListeners();
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

    private void OnBackButton()
    {
        App.ChangePanelState(PanelState.Main);
    }

    private void OnLogoutButton()
    {
        if (++logoutCount < LOGOUT_COUNT_NEEDED)
        {
            Debug.Log($"Press the LOG OUT button {LOGOUT_COUNT_NEEDED - logoutCount} more time(s) to Confirm logout.");
            return;
        }

        App.IsInteractable = false;

        void onSuccess(string jsonResponse, object cbObject)
        {
            Debug.Log($"Logout success!");

            App.ChangePanelState(PanelState.Login);
            App.GetStoredUserIDs();
            App.IsInteractable = true;
        }
        ;

        void onFailure(int status, int reason, string jsonError, object cbObject)
        {
            App.OnBrainCloudError(status, reason, jsonError, cbObject);

            Debug.LogError($"Logout failed!");
            Debug.LogError($"Try restarting the app...");

            App.GetStoredUserIDs();
        }

        BC.Logout(true, onSuccess, onFailure, this);
    }

    #endregion
}
