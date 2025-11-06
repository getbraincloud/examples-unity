using BrainCloud.JsonFx.Json;
using Oculus.Platform;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginHandler : MonoBehaviour
{
    public static string MetaUserID { get; private set; }

    [SerializeField] private CanvasGroup MainCanvas = null;
    [SerializeField] private TMP_Text ErrorMessage = null;

    [Header("Log In Content")]
    [SerializeField] private GameObject LogInContent = null;
    [SerializeField] private TMP_Text VersionLabel = null;
    [SerializeField] private Button LogInButton = null;

    [Header("Log In Content")]
    [SerializeField] private GameObject InfoContent = null;
    [SerializeField] private TMP_Text AppIDLabel = null;
    [SerializeField] private TMP_Text ProfileIDLabel = null;
    [SerializeField] private TMP_Text AnonymousIDLabel = null;

    private BrainCloudWrapper BC = null;

    private void Awake()
    {
        MainCanvas.interactable = false;

        ErrorMessage.text = string.Empty;
        ErrorMessage.gameObject.SetActive(false);

        LogInContent.SetActive(false);
        InfoContent.SetActive(false);
    }

    private void OnEnable()
    {
        LogInButton.onClick.AddListener(OnLogInButton);
    }

    private void Start()
    {
#if UNITY_EDITOR
        DisplayError("This will not run properly in the Unity Editor.");
        DisplayError("Please build as an Android app or Windows executable.");
        return;
#endif
#pragma warning disable CS0162 // Unreachable code detected
        VersionLabel.text = BrainCloud.Version.GetVersion();

        Core.AsyncInitialize().OnComplete((msg) =>
        {
            if (msg.IsError)
            {
                DisplayError("Core.AsyncInitialize Error:");
                DisplayError(msg.GetError().Message);
            }
            else
            {
                Entitlements.IsUserEntitledToApplication().OnComplete((msg) =>
                {
                    if (msg.IsError)
                    {
                        DisplayError("Entitlements.IsUserEntitledToApplication Error:");
                        DisplayError(msg.GetError().Message);
                    }
                    else
                    {
                        LogInContent.SetActive(true);
                        MainCanvas.interactable = true;
                    }
                });
            }
        });
#pragma warning restore CS0162 // Unreachable code detected
    }

    private void OnDisable()
    {
        LogInButton.onClick.RemoveAllListeners();
    }

    private void OnLogInButton()
    {
        StopAllCoroutines();
        MainCanvas.interactable = false;
        StartCoroutine(HandleLogInFlow());
    }

    private IEnumerator HandleLogInFlow()
    {
        // First let's initialize BC
        BC = gameObject.AddComponent<BrainCloudWrapper>();

        yield return new WaitForFixedUpdate();

        BC.Init();

        yield return new WaitUntil(() => BC.Client != null &&
                                         BC.Client.Initialized);

        yield return new WaitForFixedUpdate();
        BC.Client.EnableLogging(true);
        yield return new WaitForFixedUpdate();

        // Next we get the user's ID...
        bool isSuccess = false;
        string userID = string.Empty;

        Users.GetLoggedInUser().OnComplete((msg) =>
        {
            if (msg.IsError)
            {
                DisplayError("Users.GetLoggedInUser Error:");
                DisplayError(msg.GetError().Message);
            }
            else if (msg.Data.ID <= 0 || msg.Data.ID.ToString().Length <= 1)
            {
                DisplayError($"User ID is not valid! Has the app been set up properly on Meta Horizon Developer?\nUser ID: {msg.Data.ID}");
            }
            else
            {
                Debug.Log($"User ID: {msg.Data.ID}");

                userID = msg.Data.ID.ToString();
                isSuccess = true;
            }
        });

        yield return new WaitUntil(() => isSuccess);

        // And nonce from the user proof
        isSuccess = false;
        string nonce = string.Empty;

        Users.GetUserProof().OnComplete((msg) =>
        {
            if (msg.IsError)
            {
                DisplayError("Users.GetUserProof Error:");
                DisplayError(msg.GetError().Message);
            }
            else
            {
                Debug.Log($"User ID: {msg.Data.Value}");

                nonce = msg.Data.Value;
                isSuccess = true;
            }
        });

        yield return new WaitUntil(() => isSuccess);

        // Then we authenticate with the ID and nonce
        BC.AuthenticateOculus(userID,
                              nonce,
                              true,
                              OnAuthenticateSuccess,
                              OnAuthenticateFailure,
                              null);

        yield return new WaitUntil(() => BC.Client != null &&
                                         BC.Client.Authenticated &&
                                         BC.GetStoredProfileId() != string.Empty &&
                                         BC.GetStoredAnonymousId() != string.Empty);
        yield return new WaitForFixedUpdate();

        // We'll store the User ID here for verifying purchases
        MetaUserID = userID;

        // Finally let's update the info after authentication
        AppIDLabel.text = $"BC App: {BC.Client.AppId}";
        ProfileIDLabel.text = $"Profile ID:\n{BC.GetStoredProfileId()}";
        AnonymousIDLabel.text = $"Anonymous ID:\n{BC.GetStoredAnonymousId()}";

        LogInContent.SetActive(false);
        InfoContent.SetActive(true);

        // Enable purchasing canvas
        if (FindFirstObjectByType<PurchaseHandler>() is var ph && ph != null)
        {
            ph.enabled = true;
        }
    }

    private void OnAuthenticateSuccess(string jsonResponse, object cbObject)
    {
        // Normally we would want to do something with the authentication info but since we're just doing a quick demo we'll keep this empty
    }

    private void OnAuthenticateFailure(int status, int reasonCode, string jsonError, object cbObject)
    {
        var error = JsonReader.Deserialize(jsonError) as Dictionary<string, object>;

        DisplayError($"Error Received - Status: {status} || Reason {reasonCode} || Message:\n{error["status_message"]}");
    }

    private void DisplayError(string msg)
    {
        StopAllCoroutines();

        Debug.LogError(msg);

        LogInContent.SetActive(false);
        InfoContent.SetActive(false);
        ErrorMessage.gameObject.SetActive(true);

        ErrorMessage.text = string.IsNullOrWhiteSpace(ErrorMessage.text) ? msg
                          : ErrorMessage.text + $"\n{msg}";
    }
}
