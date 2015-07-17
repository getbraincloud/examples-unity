using System;
using UnityEngine;
using System.Collections;
using LitJson;


namespace BrainCloudSlots.Connection
{
    public class Connect : MonoBehaviour
    {

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

        void Start()
        {
            m_skin = (GUISkin)Resources.Load("skin");
            Application.runInBackground = true;

            ///////////////////////////////////////////////////////////////////
            // brainCloud game configuration
            ///////////////////////////////////////////////////////////////////

            BrainCloudWrapper.Initialize();

            ///////////////////////////////////////////////////////////////////
            m_username = PlayerPrefs.GetString("username");

            // Stores the password in plain text directly in the unity store.
            // This is obviously not secure but speeds up debugging/testing.
            m_password = PlayerPrefs.GetString("password");
        }

        void OnGUI()
        {
            GUI.skin = m_skin;
            int width = 500;
            int height = 400;

            m_windowRect = new Rect(Screen.width / 2 - (width / 2), Screen.height / 2 - (height / 2), width, height);
            GUI.Window(30, m_windowRect, OnWindow, "brainCloud Login");
        }

        void OnWindow(int windowID)
        {
            switch (m_authMode)
            {
                case eAuthMode.AUTH_MODE_UNIVERSAL:

                    GUI.Label(new Rect(m_windowRect.width / 2 - 100, 45, 100, 20), "Username \n(If it doesn't exist, it will be created)");
                    m_username = GUI.TextField(new Rect(m_windowRect.width / 2 - 100, 80, 200, 20), m_username);

                    GUI.Label(new Rect(m_windowRect.width / 2 - 100, 110, 100, 20), "Password");
                    m_password = GUI.PasswordField(new Rect(m_windowRect.width / 2 - 100, 140, 200, 20), m_password, '*');

                    if (GUI.Button(new Rect(m_windowRect.width / 2 - 50, 180, 100, 30), "Login"))
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
                            //BrainCloudWrapper.GetInstance().AuthenticateAnonymous(OnSuccess_Authenticate, OnError_Authenticate);
                            ///////////////////////////////////////////////////////////////////
                        }
                    }
                    break;
            }

            GUI.TextArea(new Rect(m_windowRect.width / 2 - 140, 240, 280, 90), m_authStatus);

            if (GUI.Button(new Rect(m_windowRect.width / 2 + 100, 360, 100, 30), "Clear Log"))
            {
                m_authStatus = "";
            }

        }

        private void AppendLog(string log)
        {
            string oldStatus = m_authStatus;
            m_authStatus = "\n" + log + "\n" + oldStatus;
            Debug.Log(log);
        }

        public void OnSuccess_Authenticate(string responseData, object cbObject)
        {
            JsonData response = JsonMapper.ToObject(responseData);
            response = response["data"];
            if (response["newUser"].ToString() == "true")
            {
                //this is player's first login, create their user entity
                string userData = "{\"purchaseHistory\" : [], \"firstName\" : \"\", \"lastName\" : \"\", \"email\" : \"\", \"lifetimeWins\" : 0, \"biggestWin\" : {\"amount\" : 0, \"date\" : \"" 
                    + new DateTime().ToShortDateString() 
                    + "\", \"time\" : \"" + new DateTime().ToShortTimeString() 
                    + "\"}, \"joinDateTime\" : {\"date\" : \"" 
                    + DateTime.Now.ToShortDateString() + "\", \"time\" : \"" + DateTime.Now.ToShortTimeString() + "\"}}";

                BrainCloudWrapper.GetBC().EntityService.CreateEntity("userData", userData, null, CreateEntitySuccess, EntityFailure, null);
            }
            else
            {
                //this is a returning user, store their information
                BrainCloudWrapper.GetBC().EntityService.GetEntitiesByType("userData", GetEntitySuccess, EntityFailure, null);
            }
            AppendLog("Authenticate successful!");
            BrainCloudWrapper.GetBC().PlayerStateService.UpdatePlayerName(m_username);
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_userName = m_username;
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().ReadSlotsData();
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().ReadStatistics();
            BrainCloudWrapper.GetBC().GlobalEntityService.GetList("{ \"entityType\" : \"termsAndConditions\" }", "", 1, GetTermsEntitySuccess, EntityFailure, null);
            Application.LoadLevel("Lobby");
        }

        public void OnError_Authenticate(int a, int b, string errorData, object cbObject)
        {
            AppendLog("Authenticate failed: " + errorData);
        }

        public void CreateEntitySuccess(string responseData, object cbObject)
        {
            BrainCloudWrapper.GetBC().EntityService.GetEntitiesByType("userData", GetEntitySuccess, EntityFailure, null);
        }

        public void GetEntitySuccess(string responseData, object cbObject)
        {
            JsonData response = JsonMapper.ToObject(responseData);
            response = response["data"];

            if (response["entities"].Count == 0)
            {

                string userData = "{\"purchaseHistory\" : [], \"firstName\" : \"\", \"lastName\" : \"\", \"email\" : \"\", \"lifetimeWins\" : 0, \"biggestWin\" : {\"amount\" : 0, \"date\" : \""
                    + new DateTime().ToShortDateString()
                    + "\", \"time\" : \"" + new DateTime().ToShortTimeString()
                    + "\"}, \"joinDateTime\" : {\"date\" : \""
                    + DateTime.Now.ToShortDateString() + "\", \"time\" : \"" + DateTime.Now.ToShortTimeString() + "\"}}";

                BrainCloudWrapper.GetBC().EntityService.CreateEntity("userData", userData, null, CreateEntitySuccess, EntityFailure, null);
                return;
            }
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_userData = response["entities"][0];
        }

        public void GetTermsEntitySuccess(string responseData, object cbObject)
        {
            JsonData response = JsonMapper.ToObject(responseData);
            response = response["data"];
            GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_termsConditionsString = response["entityList"][0]["data"]["text"].ToString();
        }

        public void EntityFailure(int a, int b, string errorData, object cbObject)
        {
            Debug.Log(a);
            Debug.Log(b);
            Debug.Log(errorData);
        }
    }
}