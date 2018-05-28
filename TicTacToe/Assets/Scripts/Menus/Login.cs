#region

using LitJson;
using UnityEngine;

#endregion

public class Login : GameScene
{
    private bool _isConnecting;
    public Texture BrainCloudLogo;
    public string Password;


    public Spinner Spinner;

    public string Username;

    // Use this for initialization
    private void Start()
    {
        gameObject.transform.parent.gameObject.GetComponentInChildren<Camera>().rect = App.ViewportRect;

        Username = PlayerPrefs.GetString(App.WrapperName + "_username");
        Password = PlayerPrefs.GetString(App.WrapperName + "_password");
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
        GUILayout.Label("Username");
        Username = GUILayout.TextField(Username, GUILayout.MinWidth(200));

        GUILayout.Label("Password");
        Password = GUILayout.PasswordField(Password, '*', GUILayout.MinWidth(100));

        #region Reconnect
        // Use Reconnect for re-authentication. It uses an GUID (anonymousId) to authenticate the user
        // Don't save the Username and Password locally for re-authentication! This is bad practice!
        if (false && !_isConnecting && PlayerPrefs.GetString(App.WrapperName + "_hasAuthenticated", "false").Equals("true"))
        {
            _isConnecting = true;
            Spinner.gameObject.SetActive(true);
            
            App.Bc.Reconnect(OnAuthentication,
                (status, code, error, cbObject) => { });


        }
        #endregion
        
        if (GUILayout.Button("Connect as Universal", GUILayout.MinHeight(50), GUILayout.MinWidth(100)))
        {
            _isConnecting = true;
            Spinner.gameObject.SetActive(true);

            // This Authentication is using a UniversalId
            App.Bc.AuthenticateUniversal(Username, Password, true, OnAuthentication, (status, code, error, cbObject) => { Debug.Log(error); });
        }
    }

    private void OnAuthentication(string response, object cbObject)
    {
        var data = JsonMapper.ToObject(response)["data"];
        App.ProfileId = data["profileId"].ToString();
        App.PlayerName = data["playerName"].ToString();


        PlayerPrefs.SetString(App.WrapperName + "_username", Username);
        PlayerPrefs.SetString(App.WrapperName + "_password", Password);


        //PlayerPrefs.SetString(App.WrapperName + "_hasAuthenticated", "true");


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

    private void SetupNewPlayer()
    {
        // If this is a new user, let's set their playerName to their universalId
        App.PlayerName = Username;

        // and also update their name on brainCloud
        App.Bc.PlayerStateService.UpdateUserName(Username,
            (jsonResponse, o) => { App.GotoMatchSelectScene(gameObject); });
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
}