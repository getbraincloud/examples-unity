using UnityEngine;
using LitJson;
using UnityEngine.UI;
using UnityEngine.Networking;
using BrainCloud;
using UnityEngine.SceneManagement;

namespace BrainCloudUNETExample.Connection
{
    public class Connect : MonoBehaviour
    {
        private string m_username = "";
        private string m_password = "";

        private string m_authStatus = "Welcome to brainCloud";

        private bool m_isLoggingIn = false;

        private DialogDisplay m_dialogueDisplay;
        private InputField m_usernameField;
        private InputField m_passwordField;
        private Button m_loginBtn;
        private Button m_forgotPasswordBtn;
        private Toggle m_savePassToggle;
        
        void Awake()
        {
            BombersNetworkManager.RefreshBCVariable();

            m_dialogueDisplay = GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>();
            m_usernameField = GameObject.Find("UsernameBox").GetComponent<InputField>();
            m_passwordField = GameObject.Find("PasswordBox").GetComponent<InputField>();
            m_loginBtn = GameObject.Find("Login Button").GetComponent<Button>();
            m_forgotPasswordBtn = GameObject.Find("Forgot Password").GetComponent<Button>();
            m_savePassToggle = GameObject.Find("Toggle").GetComponent<Toggle>();
        }

        void Start()
        {
            Application.runInBackground = true;

            m_username = PlayerPrefs.GetString("username");
            if (PlayerPrefs.GetInt("remember") == 0)
            {
                GameObject.Find("Toggle").GetComponent<Toggle>().isOn = false;
            }
            else
            {
                GameObject.Find("Toggle").GetComponent<Toggle>().isOn = true;
            }
            // Stores the password in plain text directly in the unity store.
            // This is obviously not secure but speeds up debugging/testing.
            m_password = PlayerPrefs.GetString("password");
            GameObject.Find("UsernameBox").GetComponent<InputField>().text = m_username;
            GameObject.Find("PasswordBox").GetComponent<InputField>().text = m_password;
        }

        void Update()
        {
            OnWindow();
            if (Input.GetKeyDown(KeyCode.Return))
            {
                Login();
            }
        }

        public void Login()
        {
            m_username = m_usernameField.text;
            m_password = m_passwordField.text;

            if (m_username.Length == 0 || m_password.Length == 0)
            {
                m_dialogueDisplay.DisplayDialog("Username/password can't be empty!");
            }
            else if (!m_username.Contains("@"))
            {
                m_dialogueDisplay.DisplayDialog("Not a valid email address!");
            }
            else
            {
                AppendLog("Attempting to authenticate...");
                PlayerPrefs.SetString("username", m_username);
                if (m_savePassToggle.isOn)
                {
                    PlayerPrefs.SetString("password", m_password);
                    PlayerPrefs.SetInt("remember", 1);
                }
                else
                {
                    PlayerPrefs.SetString("password", "");
                    PlayerPrefs.SetInt("remember", 0);
                }

                m_isLoggingIn = true;

                // brainCloud authentication
                BombersNetworkManager._BC.AuthenticateEmailPassword(m_username, m_password, true, OnSuccess_Authenticate, OnError_Authenticate);
            }
        }

        void OnWindow()
        {
            if (!m_isLoggingIn)
            {
                m_loginBtn.interactable = true;
                m_loginBtn.transform.GetChild(0).GetComponent<Text>().text = "Log In";
            }
            else
            {
                m_loginBtn.transform.GetChild(0).GetComponent<Text>().text = "Logging in...";
                m_loginBtn.interactable = false;
                m_forgotPasswordBtn.interactable = false;
            }
        }

        private void AppendLog(string log, bool error = false)
        {
            string oldStatus = m_authStatus;
            m_authStatus = "\n" + log + "\n" + oldStatus;
            if (error)
            {
                Debug.LogError(log);
            }
            else
            {
                Debug.Log(log);
            }
        }

        public void ForgotPassword()
        {
            m_username = m_usernameField.text;

            if (m_username == "" || !m_username.Contains("@"))
            {
                m_dialogueDisplay.DisplayDialog("You need to enter an email first!");
                return;
            }
            BombersNetworkManager._BC.Client.AuthenticationService.ResetEmailPassword(m_username, OnSuccess_Reset, OnError_Reset);

        }

        public void OnSuccess_Reset(string responseData, object cbObject)
        {
            m_dialogueDisplay.DisplayDialog("Password reset instructions\nsent to registered email.");
        }

        public void OnError_Reset(int statusCode, int reasonCode, string responseData, object cbObject)
        {
            if (reasonCode == ReasonCodes.SECURITY_ERROR)
            {
                m_dialogueDisplay.DisplayDialog("Email not registered!");
            }
        }

        public void OnSuccess_Authenticate(string responseData, object cbObject)
        {
            AppendLog("Authenticate successful!");
            JsonData response = JsonMapper.ToObject(responseData);
            string username = "";
            if (response["data"]["playerName"].ToString() == "")
            {
                for (int i = 0; i < m_username.Length; i++)
                {
                    if (m_username[i] != '@')
                    {
                        username += m_username[i].ToString();
                    }
                    else
                    {
                        break;
                    }
                }
                BombersNetworkManager._BC.Client.PlayerStateService.UpdateUserName(username);
            }
            else
            {
                username = response["data"]["playerName"].ToString();
            }


            BrainCloudStats.Instance.ReadStatistics();
            BrainCloudStats.Instance.ReadGlobalProperties();
            BrainCloudStats.Instance.PlayerName = username;
            NetworkManager.singleton.StartMatchMaker();
            SceneManager.LoadScene("Matchmaking");
        }

        public void OnError_Authenticate(int statusCode, int reasonCode, string responseData, object cbObject)
        {
            if (reasonCode == ReasonCodes.TOKEN_DOES_NOT_MATCH_USER)
            {
                m_dialogueDisplay.DisplayDialog("Incorrect username/password combination!");
            }
            m_isLoggingIn = false;
            m_forgotPasswordBtn.interactable = true;
        }
    }
}