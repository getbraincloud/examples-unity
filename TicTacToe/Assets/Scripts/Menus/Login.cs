#region

using BrainCloud.LitJson;
using UnityEngine;
using BrainCloud;

#endregion

public class Login : GameScene
{
    private bool _isConnecting;
    public Texture BrainCloudLogo;
    public string Password;


    public Spinner Spinner;

    public string UniversalId;

    // Use this for initialization
    private void Start()
    {
        gameObject.transform.parent.gameObject.GetComponentInChildren<Camera>().rect = App.ViewportRect;

    }

    private void OnGUI()
    {
        GUILayout.Window(App.WindowId, new Rect(Screen.width / 2 - 125 + App.Offset, Screen.height / 2 - 100, 250, 200),
            OnWindow,
            "brainCloud Login");
    }

    private void OnWindow(int windowId)
    {
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();

        GUILayout.Box(BrainCloudLogo);
        GUILayout.Space(30);

        GUI.enabled = !_isConnecting;

        LoginUI();

        GUI.enabled = true;

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
    }

    
    // Authenticating Users into brainCloud
    private void LoginUI()
    {
        GUILayout.Label("UserId");
        UniversalId = GUILayout.TextField(UniversalId, GUILayout.MinWidth(200));

        GUILayout.Label("Password");
        Password = GUILayout.PasswordField(Password, '*', GUILayout.MinWidth(100));

        #region Reconnect
        // Use Reconnect for re-authentication. It uses an GUID (anonymousId) to authenticate the user
        // Don't save the Username and Password locally for re-authentication! This is bad practice!
        if (true && !_isConnecting && PlayerPrefs.GetString(App.WrapperName + "_hasAuthenticated", "false").Equals("true"))
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
        
        if (GUILayout.Button("Connect as Universal", GUILayout.MinHeight(50), GUILayout.MinWidth(100)))
        {
            _isConnecting = true;
            Spinner.gameObject.SetActive(true);

            //TODO
            // This Authentication is using a UniversalId
            //App.Bc.AuthenticateUniversal(UniversalId, Password, true, OnAuthentication,
            //    (status, code, error, cbObject) =>
            //    {
            //        Debug.Log("An Error Occured in Login");   
            //    });

            App.Bc.AuthenticateUniversal(UniversalId, Password, true, OnAuthentication, FailureCallback);
   
        }
    }

    private void OnAuthentication(string response, object cbObject)
    {   
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
        else
        {
            App.GotoMatchSelectScene(gameObject);
        }
    }

    public void FailureCallback(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Debug.Log("An Error Occured in Login");
        //TODO
        switch (reasonCode)
        {
            case ReasonCodes.MISSING_IDENTITY_ERROR:
                {  // Identity does not exist (and client has orphaned profileId)

                    // Reset profileId and re-authenticate
                    App.Bc.ResetStoredProfileId();
                    App.Bc.AuthenticateUniversal(UniversalId, Password, true);
                    break;
                }
            case ReasonCodes.SWITCHING_PROFILES:
                {  // Identity belongs to a different profile

                    // [Optional] Prompt user to confirm that they wish to switch accounts?

                    // Reset profileId and re-authenticate
                    App.Bc.ResetStoredProfileId();
                    App.Bc.AuthenticateUniversal(UniversalId, Password, true);
                    //ask jon
                    break;
                }
            case ReasonCodes.MISSING_PROFILE_ERROR:
                {  // Identity does not exist

                    // The account doesn't exist - create it now.
                   App.Bc.AuthenticateUniversal(UniversalId, Password, true);
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
            (status, code, error, o) => { Debug.Log("Failed to Get MatchMaking Data"); });
    }

    private void SetupNewPlayer()
    {
        // If this is a new user, let's set their Name to their universalId
        App.Name = UniversalId;

        // and also update their name on brainCloud
        App.Bc.PlayerStateService.UpdateUserName(UniversalId,
            (jsonResponse, o) => { App.GotoMatchSelectScene(gameObject); });
    }    
}