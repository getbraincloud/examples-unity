using BrainCloud;
using LitJson;
using UnityEngine;

public class BrainCloudLogin : MonoBehaviour
{
    public static string ProfileId;

    //static public string PlayerPicUrl;
    public static string PlayerName;
    public Texture brainCloudLogo;

    private bool isConnecting;
    public string password = "";
    public Spinner spinner;


    public string username = "";

    // Use this for initialization
    private void Start()
    {
        BrainCloudClient.EnableSingletonMode = true;
        BrainCloudWrapper.Initialize();
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

        if (GUILayout.Button("Connect as User", GUILayout.MinHeight(50), GUILayout.MinWidth(100)))
        {
            isConnecting = true;
            spinner.gameObject.SetActive(true);

            BrainCloudWrapper.Instance.AuthenticateUniversal(username, password, true, (response, cbObject) =>
            {
                var data = JsonMapper.ToObject(response)["data"];
                ProfileId = data["profileId"].ToString();
                PlayerName = username;

                if (data["newUser"].ToString().Equals("true"))
                {
                    BrainCloudWrapper.Instance.PlayerStateService.UpdatePlayerName(username, (jsonResponse, o) =>
                    {
                        Debug.Log(jsonResponse);
                        
                        Application.LoadLevel("GamePicker"); // Load our game
                        
                    });
                }
                else
                {

                    Application.LoadLevel("GamePicker"); // Load our game                    
                }

                Debug.Log(response);

            }, (status, code, error, cbObject) => { Debug.Log(error); });
        }

        GUI.enabled = true;

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
    }
}