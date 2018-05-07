using BrainCloud;
using LitJson;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Login : MonoBehaviour
{
    public static string ProfileId;
    public static string PlayerName;
    
    public Texture brainCloudLogo;

    private bool isConnecting;
    
    public Spinner spinner;

    public string password = "";
    public string username = "";

    // Use this for initialization
    private void Start()
    {   
        username = PlayerPrefs.GetString("username");
        password = PlayerPrefs.GetString("password");
    }

    private void OnGUI()
    {
        GUILayout.Window(0, new Rect(Screen.width / 2 - 125, Screen.height / 2 - 100, 250, 200), OnWindow,
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

            App.BC.AuthenticateUniversal(username, password, true, (response, cbObject) =>
            {
                var data = JsonMapper.ToObject(response)["data"];
                ProfileId = data["profileId"].ToString();
                PlayerName = username;

                
                PlayerPrefs.SetString("username", username);
                PlayerPrefs.SetString("password", password);
                
                // If this is a new user, let's set their playerName
                if (data["newUser"].ToString().Equals("true"))
                {
                    App.BC.PlayerStateService.UpdatePlayerName(username, (jsonResponse, o) =>
                    {
                        SceneManager.LoadScene("MatchSelect"); // Load our game
                    });
                }
                else
                {
                    SceneManager.LoadScene("MatchSelect"); // Load our game                    
                }

            }, (status, code, error, cbObject) => { Debug.Log(error); });
        }

        GUI.enabled = true;

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
    }
}