using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LitJson;
using BrainCloudPhotonExample.Game;
using UnityEngine.UI;

namespace BrainCloudPhotonExample.Connection
{
    public class Connect : MonoBehaviour
    {
        private bool m_connectedToPhoton = false;

        private string m_username = "";
        private string m_password = "";

        private Vector2 m_scrollPosition;
        private string m_authStatus = "Welcome to brainCloud";

        private Rect m_windowRect;
        private bool m_isLoggingIn = false;

        private string m_versionNumber = "";

        public static GameObject s_fullScreenButtonInstance;
        public static GameObject s_versionInstance;
        void Awake()
        {
            if (s_fullScreenButtonInstance)
                DestroyImmediate(GameObject.Find("FullScreen"));
            else
                s_fullScreenButtonInstance = GameObject.Find("FullScreen");

            if (s_versionInstance)
                DestroyImmediate(GameObject.Find("Version Text"));
            else
                s_versionInstance = GameObject.Find("Version Text");

        }
        void Start()
        {

            m_versionNumber = ((TextAsset)Resources.Load("Version")).text.ToString();
            GameObject.Find("Version Text").GetComponent<Text>().text = m_versionNumber;
            DontDestroyOnLoad(GameObject.Find("Version Text"));
            DontDestroyOnLoad(GameObject.Find("FullScreen"));
            GameObject.Find("Version Text").transform.SetParent(GameObject.Find("Canvas").transform);
            GameObject.Find("FullScreen").transform.SetParent(GameObject.Find("Canvas").transform);
            Application.runInBackground = true;
            if (!PhotonNetwork.connectedAndReady) PhotonNetwork.ConnectUsingSettings("1.0");

            ///////////////////////////////////////////////////////////////////
            // brainCloud game configuration
            ///////////////////////////////////////////////////////////////////

            BrainCloudWrapper.Initialize();

            ///////////////////////////////////////////////////////////////////

            if (!PhotonNetwork.connectedAndReady) AppendLog("Connecting to Photon...");
            else
            {
                AppendLog("Connected to Photon");
                m_connectedToPhoton = true;
            }

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

        void OnGUI()
        {
            OnWindow();
            //m_windowRect = new Rect(Screen.width / 2 - (width / 2), Screen.height / 2 - (height / 2), width, height);
            //GUI.Window(30, m_windowRect, OnWindow, "brainCloud Login");
        }

        public void Login()
        {
            m_username = GameObject.Find("UsernameBox").GetComponent<InputField>().text;
            m_password = GameObject.Find("PasswordBox").GetComponent<InputField>().text;

            if (m_connectedToPhoton)
            {
                if (m_username.Length == 0 || m_password.Length == 0)
                {
                    AppendLog("Username/password can't be empty", true);
                }
                else if(!m_username.Contains("@"))
                {
                    AppendLog("Missing @ symbol in email field", true);
                }
                else
                {
                    AppendLog("Attempting to authenticate...");
                    PlayerPrefs.SetString("username", m_username);
                    if (GameObject.Find("Toggle").GetComponent<Toggle>().isOn)
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
                    
                    ///////////////////////////////////////////////////////////////////
                    // brainCloud authentication
                    ///////////////////////////////////////////////////////////////////
                    BrainCloudWrapper.GetInstance().AuthenticateEmailPassword(m_username, m_password, true, OnSuccess_Authenticate, OnError_Authenticate);
                    //BrainCloudWrapper.GetInstance().AuthenticateUniversal(m_username, m_password, true, OnSuccess_Authenticate, OnError_Authenticate);

                    ///////////////////////////////////////////////////////////////////
                }
            }
        }

        void OnWindow()
        {
            if (!m_isLoggingIn)
            {
                if (m_connectedToPhoton)
                {
                    GameObject.Find("Login Button").GetComponent<Button>().interactable = true;
                    GameObject.Find("Login Button").transform.GetChild(0).GetComponent<Text>().text = "Log In";
                }
                else
                {
                    GameObject.Find("Login Button").GetComponent<Button>().interactable = false;
                    GameObject.Find("Login Button").transform.GetChild(0).GetComponent<Text>().text = "Please Wait...";
                }
            }
            else
            {
                GameObject.Find("Login Button").transform.GetChild(0).GetComponent<Text>().text = "Logging in...";
                GameObject.Find("Login Button").GetComponent<Button>().interactable = false;
                GameObject.Find("Forgot Password").GetComponent<Button>().interactable = false;
            }
            /*
            switch (m_authMode)
            {
                case eAuthMode.AUTH_MODE_UNIVERSAL:

                    GUI.Label(new Rect(m_windowRect.width / 2 - 100, 45, 100, 20), "Username \n(If it doesn't exist, it will be created)");
                    m_username = GUI.TextField(new Rect(m_windowRect.width / 2 - 100, 80, 200, 20), m_username);

                    GUI.Label(new Rect(m_windowRect.width / 2 - 100, 110, 100, 20), "Password");
                    m_password = GUI.PasswordField(new Rect(m_windowRect.width / 2 - 100, 140, 200, 20), m_password, '*');

                    string buttonText = "";
                    if (!m_connectedToPhoton)
                    {
                        buttonText = "Please Wait...";
                    }
                    else
                    {
                        buttonText = "Login";
                    }

                    if (GUI.Button(new Rect(m_windowRect.width / 2 - 50, 180, 100, 30), buttonText))
                    {
                        if (m_connectedToPhoton)
                        {
                            if (m_username.Length == 0 || m_password.Length == 0)
                            {
                                AppendLog("Username/password can't be empty");
                            }
                            else
                            {
                                AppendLog("Attempting to authenticate...");
                                PlayerPrefs.SetString("username", m_username);
                                PlayerPrefs.SetString("password", m_password);

                                ///////////////////////////////////////////////////////////////////
                                // brainCloud authentication
                                ///////////////////////////////////////////////////////////////////
                                BrainCloudWrapper.GetInstance().AuthenticateUniversal(m_username, m_password, true, OnSuccess_Authenticate, OnError_Authenticate);

                                ///////////////////////////////////////////////////////////////////
                            }
                        }
                    }
                    break;
            }

            GUI.TextArea(new Rect(m_windowRect.width / 2 - 140, 240, 280, 90), m_authStatus);

            if (GUI.Button(new Rect(m_windowRect.width / 2 + 100, 360, 100, 30), "Clear Log"))
            {
                m_authStatus = "";
            }
            */
        }

        void OnConnectedToPhoton()
        {
            m_connectedToPhoton = PhotonNetwork.connectedAndReady;
            AppendLog("Connected to Photon");
            ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(BulletController.BulletInfo), (byte)'B', BulletController.BulletInfo.SerializeBulletInfo, BulletController.BulletInfo.DeserializeBulletInfo);
            ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(BombController.BombInfo), (byte)'b', BombController.BombInfo.SerializeBombInfo, BombController.BombInfo.DeserializeBombInfo);
            ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(ShipController.ShipTarget), (byte)'s', ShipController.ShipTarget.SerializeShipInfo, ShipController.ShipTarget.DeserializeShipInfo);
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
            m_username = GameObject.Find("UsernameBox").GetComponent<InputField>().text;

            if (m_username == "" || !m_username.Contains("@"))
            {
                AppendLog("No email detected in email field!", true);
                return;
            }
            BrainCloudWrapper.GetBC().AuthenticationService.ResetEmailPassword(m_username, OnSuccess_Reset, OnError_Reset);
            
        }

        public void OnSuccess_Reset(string responseData, object cbObject)
        {
            AppendLog("Password reset instructions sent to registered email.");
        }

        public void OnError_Reset(int a, int b, string errorData, object cbObject)
        {
            AppendLog("Authenticate failed: " + errorData, true);
        }

        public void OnSuccess_Authenticate(string responseData, object cbObject)
        {
            AppendLog("Authenticate successful!");
            JsonData response = JsonMapper.ToObject(responseData);
            string username = "";
            if (response["data"]["playerName"].ToString() == "")
            {
                for (int i=0;i<m_username.Length;i++)
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
                BrainCloudWrapper.GetBC().PlayerStateService.UpdatePlayerName(username);
                PhotonNetwork.player.name = username;
            }
            else
            {
                PhotonNetwork.player.name = response["data"]["playerName"].ToString();
            }
            
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().ReadStatistics();
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().ReadGlobalProperties();
            GameObject.Find("Version Text").transform.SetParent(null);
            GameObject.Find("FullScreen").transform.SetParent(null);
            PhotonNetwork.sendRate = 20;
            Application.LoadLevel("Matchmaking");
        }

        public void OnError_Authenticate(int a, int b, string errorData, object cbObject)
        {
            AppendLog("Authenticate failed: " + errorData, true);
            m_isLoggingIn = false;
            
        }
    }
}