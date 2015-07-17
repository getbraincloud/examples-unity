using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LitJson;
using BrainCloudUNETExample.Game;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace BrainCloudUNETExample.Connection
{
    public class Connect : MonoBehaviour
    {
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

            ///////////////////////////////////////////////////////////////////
            // brainCloud game configuration
            ///////////////////////////////////////////////////////////////////

            BrainCloudWrapper.Initialize();

            ///////////////////////////////////////////////////////////////////

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
            m_username = GameObject.Find("UsernameBox").GetComponent<InputField>().text;
            m_password = GameObject.Find("PasswordBox").GetComponent<InputField>().text;

                if (m_username.Length == 0 || m_password.Length == 0)
                {
                    GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("Username/password can't be empty!");
                }
                else if (!m_username.Contains("@"))
                {
                    GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("Not a valid email address!");
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
                    ///////////////////////////////////////////////////////////////////
                }
            
        }

        void OnWindow()
        {
            if (!m_isLoggingIn)
            {
               GameObject.Find("Login Button").GetComponent<Button>().interactable = true;
               GameObject.Find("Login Button").transform.GetChild(0).GetComponent<Text>().text = "Log In";
            }
            else
            {
                GameObject.Find("Login Button").transform.GetChild(0).GetComponent<Text>().text = "Logging in...";
                GameObject.Find("Login Button").GetComponent<Button>().interactable = false;
                GameObject.Find("Forgot Password").GetComponent<Button>().interactable = false;
            }
        }

        /*void OnConnectedToPhoton()
        {
            m_connectedToPhoton = PhotonNetwork.connectedAndReady;
            AppendLog("Connected to Photon");
            ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(BulletController.BulletInfo), (byte)'B', BulletController.BulletInfo.SerializeBulletInfo, BulletController.BulletInfo.DeserializeBulletInfo);
            ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(BombController.BombInfo), (byte)'b', BombController.BombInfo.SerializeBombInfo, BombController.BombInfo.DeserializeBombInfo);
            ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(ShipController.ShipTarget), (byte)'s', ShipController.ShipTarget.SerializeShipInfo, ShipController.ShipTarget.DeserializeShipInfo);
        }*/

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
                GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("You need to enter an email first!");
                return;
            }
            BrainCloudWrapper.GetBC().AuthenticationService.ResetEmailPassword(m_username, OnSuccess_Reset, OnError_Reset);

        }

        public void OnSuccess_Reset(string responseData, object cbObject)
        {
            GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("Password reset instructions\nsent to registered email.");
        }

        public void OnError_Reset(int a, int b, string responseData, object cbObject)
        {
            if (b == 40209)
            {
                GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("Email not registered!");
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
                BrainCloudWrapper.GetBC().PlayerStateService.UpdatePlayerName(username);
            }
            else
            {
                username = response["data"]["playerName"].ToString();
            }

            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().ReadStatistics();
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().ReadGlobalProperties();
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_playerName = username;
            GameObject.Find("Version Text").transform.SetParent(null);
            GameObject.Find("FullScreen").transform.SetParent(null);
            NetworkManager.singleton.StartMatchMaker();
            Application.LoadLevel("Matchmaking");
        }

        public void OnError_Authenticate(int a, int b, string responseData, object cbObject)
        {
            if (b == 40307)
            {
                GameObject.Find("DialogDisplay").GetComponent<DialogDisplay>().DisplayDialog("Incorrect username/password combination!");
            }
            m_isLoggingIn = false;
            GameObject.Find("Forgot Password").GetComponent<Button>().interactable = true;
        }
    }
}