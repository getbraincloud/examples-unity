using LitJson;
using UnityEngine;

public class Login : GameScene
{
    public Texture brainCloudLogo;

    private bool isConnecting;

    public string password = "";

    public Spinner spinner;
    public string username = "";

    // Use this for initialization
    private void Start()
    {
        gameObject.transform.parent.gameObject.GetComponentInChildren<Camera>().rect = app.viewportRect;

        username = PlayerPrefs.GetString(app.wrapperName + "_username");
        password = PlayerPrefs.GetString(app.wrapperName + "_password");
    }

    private void OnGUI()
    {
        GUILayout.Window(app.windowId, new Rect(Screen.width / 2 - 125 + app.offset, Screen.height / 2 - 100, 250, 200),
            OnWindow,
            "brainCloud Login");
    }

    private void OnWindow(int windowId)
    {
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();

        GUILayout.Box(brainCloudLogo);
        GUILayout.Space(30);

        GUI.enabled = !isConnecting;

        GUILayout.Label("Username");
        username = GUILayout.TextField(username, GUILayout.MinWidth(200));

        GUILayout.Label("Password");
        password = GUILayout.PasswordField(password, '*', GUILayout.MinWidth(100));

        if (GUILayout.Button("Connect as Universal", GUILayout.MinHeight(50), GUILayout.MinWidth(100)))
        {
            isConnecting = true;
            spinner.gameObject.SetActive(true);

            app.bc.AuthenticateUniversal(username, password, true, (response, cbObject) =>
            {
                var data = JsonMapper.ToObject(response)["data"];
                app.ProfileId = data["profileId"].ToString();
                app.PlayerName = username;


                PlayerPrefs.SetString(app.wrapperName + "_username", username);
                PlayerPrefs.SetString(app.wrapperName + "_password", password);

                // If this is a new user, let's set their playerName
                if (data["newUser"].ToString().Equals("true"))
                    app.bc.PlayerStateService.UpdatePlayerName(username,
                        (jsonResponse, o) => { app.GotoMatchSelectScene(gameObject); });
                else
                    app.GotoMatchSelectScene(gameObject);
            }, (status, code, error, cbObject) => { Debug.Log(error); });
        }

        GUI.enabled = true;

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
    }
}