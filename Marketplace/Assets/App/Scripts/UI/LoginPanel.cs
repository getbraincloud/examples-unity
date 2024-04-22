using BrainCloud.Common;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static ExampleApp;

public class LoginPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button LoginButton = default;

    private ExampleApp App = null;
    private BrainCloudWrapper BC = null;

    #region Unity Messages

    private void OnEnable()
    {
        LoginButton.onClick.AddListener(OnLoginButton);
    }

    private void Start()
    {
        App = FindObjectOfType<ExampleApp>();

        StartCoroutine(GetBrainCloudWrapper());
        if(BC.CanReconnect())
        {
            HandleAutomaticLogin();
        }
    }

    private void OnDisable()
    {
        LoginButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        BC = null;
        App = null;
    }

    #endregion

    private IEnumerator GetBrainCloudWrapper()
    {
        yield return new WaitUntil(() =>
        {
            BC = BC ?? FindObjectOfType<BrainCloudWrapper>();

            return BC != null && BC.Client != null && BC.Client.IsInitialized();
        });

        yield return null;
    }

    private void HandleAutomaticLogin()
    {
        App.IsInteractable = false;

        Debug.Log($"Logging in with previous credentials...");

        BC.Reconnect(OnAuthenticationSuccess,
                     OnAuthenticationFailure,
                     this);
    }

    private void OnLoginButton()
    {
        BC.AuthenticateAnonymous(OnAuthenticationSuccess,
            OnAuthenticationFailure,
            this);
    }

    private void OnAuthenticationSuccess(string jsonResponse, object cbObject)
    {
        BC.SetStoredAuthenticationType(AuthenticationType.Anonymous.ToString());

        App.ChangePanelState(PanelState.Main);
        App.IsInteractable = true;
        if(!App.RememberMeIsOn)
        {
            BC.ResetStoredProfileId();
        }
        App.GetStoredUserIDs();
        Debug.Log($"User Profile ID: {BC.GetStoredProfileId()}");
        Debug.Log($"User Anonymous ID: {BC.GetStoredAnonymousId()}");

        Debug.Log("Authentication success! You are now logged into your app on brainCloud.");
    }

    private void OnAuthenticationFailure(int status, int reason, string jsonError, object cbObject)
    {
        BC.ResetStoredAuthenticationType();
        App.GetStoredUserIDs();

        App.OnBrainCloudError(status, reason, jsonError, cbObject);

        Debug.LogError($"Authentication failed! Please try again.");
    }
}
