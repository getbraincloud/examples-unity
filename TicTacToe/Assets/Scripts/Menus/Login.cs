using LitJson;
using UnityEngine;

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

        GUILayout.Label("Username");
        Username = GUILayout.TextField(Username, GUILayout.MinWidth(200));

        GUILayout.Label("Password");
        Password = GUILayout.PasswordField(Password, '*', GUILayout.MinWidth(100));

        if (GUILayout.Button("Connect as Universal", GUILayout.MinHeight(50), GUILayout.MinWidth(100)))
        {
            _isConnecting = true;
            Spinner.gameObject.SetActive(true);

            App.Bc.AuthenticateUniversal(Username, Password, true, (response, cbObject) =>
            {
                var data = JsonMapper.ToObject(response)["data"];
                App.ProfileId = data["profileId"].ToString();
                App.PlayerName = Username;


                PlayerPrefs.SetString(App.WrapperName + "_username", Username);
                PlayerPrefs.SetString(App.WrapperName + "_password", Password);

                // If this is a new user, let's set their playerName
                if (data["newUser"].ToString().Equals("true"))
                    App.Bc.PlayerStateService.UpdatePlayerName(Username,
                        (jsonResponse, o) => { App.GotoMatchSelectScene(gameObject); });
                else
                    App.GotoMatchSelectScene(gameObject);
            }, (status, code, error, cbObject) => { Debug.Log(error); });
        }

        GUI.enabled = true;

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
    }
}