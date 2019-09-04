#region

using BrainCloud.LitJson;
using TMPro;
using UnityEngine;
using BrainCloud;
#endregion

public class Login : GameScene
{
    private int MIN_CHARACTERS = 3;
    private int MAX_CHARACTERS = 23;
    private bool _isConnecting;
    public Texture BrainCloudLogo;
    public Spinner Spinner;
    [SerializeField] public TMP_InputField UniversalId;
    [SerializeField] public TMP_InputField Password;
    [SerializeField] public TextMeshProUGUI InfoBox;
    [SerializeField] public TextMeshProUGUI ErrorMessage;
    [SerializeField] public GameObject ErrorMessageBox;

    // Use this for initialization
    private void Start()
    {
        gameObject.transform.parent.gameObject.GetComponentInChildren<Camera>().rect = App.ViewportRect;
        ErrorMessage.text = "";
        ErrorMessageBox.SetActive(false);
        UniversalId.characterLimit = MAX_CHARACTERS;
        Password.characterLimit = MAX_CHARACTERS;
        LoginUI();
    }

    private void Update()
    {
        if (UniversalId.isFocused && Input.GetKeyDown(KeyCode.Tab))
        {
            SelectOtherInputField(UniversalId, Password);
        }
        else if (Password.isFocused && (Input.GetKeyDown(KeyCode.Tab)))
        {
            SelectOtherInputField(Password, UniversalId);
        }
    }

    public void OnConnect()
    {
        if (ValidateUserName() && !_isConnecting)
        {
            _isConnecting = true;
            Spinner.gameObject.SetActive(true);
            ErrorMessage.text = "";
            ErrorMessageBox.SetActive(false);
            // This Authentication is using a UniversalId
            App.Bc.AuthenticateUniversal(UniversalId.text, Password.text, true, OnAuthentication, FailureCallback);
        }
    }

    public void OnEndEditUserID(string str)
    {
        SelectOtherInputField(UniversalId, Password);
    }

    private void SelectOtherInputField(TMP_InputField current, TMP_InputField other)
    {
        current.DeactivateInputField();
        other.ActivateInputField();
        other.Select();
    }

    private bool ValidateUserName()
    {
        UniversalId.text = UniversalId.text.Trim();
        Password.text = Password.text.Trim();
        if (UniversalId.text.Length < MIN_CHARACTERS || Password.text.Length < MIN_CHARACTERS)
        {
            InfoBox.text = "The user ID and password must be at least " + MIN_CHARACTERS + " characters long.";
            return false;
        }
        return true;
    }

    // Authenticating Users into brainCloud
    private void LoginUI()
    {
        #region Reconnect
        // Use Reconnect for re-authentication. It uses an GUID (anonymousId) to authenticate the user
        // Don't save the Username and Password locally for re-authentication! This is bad practice!
        if (!_isConnecting && PlayerPrefs.GetString(App.WrapperName + "_hasAuthenticated", "false").Equals("true"))
        {
            _isConnecting = true;
            Spinner.gameObject.SetActive(true);

            App.Bc.Reconnect(OnAuthentication,
                (status, code, error, cbObject) =>
                {
                    // An error occured on reconnecting. Perhaps the User and Tester Data was cleared on the brainCloud dashboard,
                    // or perhaps there is no internet connection.
                    // Lets handle this error, by disabling this reconnect state
                    _isConnecting = false;
                    PlayerPrefs.SetString(App.WrapperName + "_hasAuthenticated", "false");
                    Spinner.gameObject.SetActive(false);
                });

        }
        #endregion
    }

    private void OnAuthentication(string response, object cbObject)
    {
        Spinner.gameObject.SetActive(false);
        var data = JsonMapper.ToObject(response)["data"];
        App.ProfileId = data["profileId"].ToString();
        App.Name = data["playerName"].ToString();

        PlayerPrefs.SetString(App.WrapperName + "_hasAuthenticated", "true");

        GetPlayerRating();

        // brainCloud gives us a newUser value to indicate if this account was just created.
        bool isNewUser = data["newUser"].ToString().Equals("true");

        if (isNewUser)
        {
            SetupNewPlayer();
        }
        else if (gameObject != null)
        {
            App.GotoMatchSelectScene(gameObject);
        }
    }

    public void FailureCallback(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Debug.Log("An Error Occured in Login");
        switch (reasonCode)
        {
            case ReasonCodes.MISSING_IDENTITY_ERROR:
                {  // Identity does not exist (and client has orphaned profileId)

                    // Reset profileId and re-authenticate
                    App.Bc.ResetStoredProfileId();
                    App.Bc.AuthenticateUniversal(UniversalId.text, Password.text, true);
                    break;
                }
            case ReasonCodes.SWITCHING_PROFILES:
                {  // Identity belongs to a different profile

                    // [Optional] Prompt user to confirm that they wish to switch accounts?

                    // Reset profileId and re-authenticate
                    App.Bc.ResetStoredProfileId();
                    App.Bc.AuthenticateUniversal(UniversalId.text, Password.text, true);
                    break;
                }
            case ReasonCodes.MISSING_PROFILE_ERROR:
                {  // Identity does not exist

                    // The account doesn't exist - create it now.
                    App.Bc.AuthenticateUniversal(UniversalId.text, Password.text, true);
                    break;
                }
            case ReasonCodes.TOKEN_DOES_NOT_MATCH_USER:
                {  // Wrong password

                    // Display a dialog telling the user that the password provided was invalid,
                    // and invite them to re-enter the password.
                    // ...
                    break;
                }
            default:
                { // Uncaught reasonCode

                    // Log the error for debugging later
                    // ...
                    break;
                }
        }
    }
    private void GetPlayerRating()
    {
        // We are Going to Read the Match Making to get the Current Player Rating.
        App.Bc.MatchMakingService.Read((jsonResponse, o) =>
            {
                var matchMakingData = JsonMapper.ToObject(jsonResponse)["data"];
                App.PlayerRating = matchMakingData["playerRating"].ToString();
            },
            (status, code, error, o) =>
            {
                ErrorMessageBox.SetActive(true);
                ErrorMessage.text = "Failed to Get MatchMaking Data";
                Debug.Log(ErrorMessage.text);
            });
    }

    private void SetupNewPlayer()
    {
        // If this is a new user, let's set their Name to their universalId
        App.Name = UniversalId.text;

        // and also update their name on brainCloud
        App.Bc.PlayerStateService.UpdateUserName(UniversalId.text,
            (jsonResponse, o) => { App.GotoMatchSelectScene(gameObject); });
    }
}
