using System;
using UnityEngine;
using System.Collections;
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

        private enum eAuthMode
        {
            AUTH_MODE_UNIVERSAL,
            AUTH_MODE_NONE
        };

        private eAuthMode m_authMode = eAuthMode.AUTH_MODE_UNIVERSAL;

        private GUISkin m_skin;

        private Rect m_windowRect;
        private bool m_isLoggingIn = false;

        void Start()
        {
            m_skin = (GUISkin)Resources.Load("skin");
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
            GUI.skin = m_skin;
            int width = 500;
            int height = 400;

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
                    AppendLog("Username/password can't be empty");
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
                    BrainCloudWrapper.GetInstance().AuthenticateUniversal(m_username, m_password, true, OnSuccess_Authenticate, OnError_Authenticate);

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

        private void AppendLog(string log)
        {
            string oldStatus = m_authStatus;
            m_authStatus = "\n" + log + "\n" + oldStatus;
            Debug.Log(log);
        }

        public void OnSuccess_Authenticate(string responseData, object cbObject)
        {
            AppendLog("Authenticate successful!");
            PhotonNetwork.player.name = m_username;
            BrainCloudWrapper.GetBC().PlayerStateService.UpdatePlayerName(m_username);
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().ReadStatistics();
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().ReadGlobalProperties();
            PhotonNetwork.sendRate = 20;
            Application.LoadLevel("Matchmaking");
        }

        public void OnError_Authenticate(int a, int b, string errorData, object cbObject)
        {
            AppendLog("Authenticate failed: " + errorData);
        }
    }
}