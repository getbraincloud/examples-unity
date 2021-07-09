#region

using BrainCloud.LitJson;
using TMPro;
using UnityEngine;
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
            App.Bc.AuthenticateUniversal(UniversalId.text, Password.text, true, OnAuthentication,
                (status, code, error, cbObject) =>
                {
                    _isConnecting = false;
                    ErrorMessageBox.SetActive(true);
                    ErrorMessage.text = "Connection error. Please wait a bit and try again.";
                    Debug.Log(ErrorMessage.text);
                    Spinner.gameObject.SetActive(false);
                });
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

    private void SetupNewPlayer()
    {
        // If this is a new user, let's set their Name to their universalId
        App.Name = UniversalId.text;

        // and also update their name on brainCloud
        App.Bc.PlayerStateService.UpdateUserName(UniversalId.text,
            (jsonResponse, o) => { App.GotoMatchSelectScene(gameObject); });
    }
}