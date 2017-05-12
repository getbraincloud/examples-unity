using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace BrainCloudUNETExample.Connection
{
    public class DialogDisplay : MonoBehaviour
    {
        public static DialogDisplay Instance { get { return s_instance; } }

        private bool m_isFullscreen = false;

        List<DialogBox> m_dialogsToDisplay;
        List<FadeLabel> m_labelsToDisplay;

        private GUISkin m_skin;

        public static DialogDisplay s_instance;

        private void Awake()
        {
            if (s_instance)
            {
                DestroyImmediate(gameObject);
                return;
            }

            s_instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private class DialogBox
        {
            public string m_message;
            public string m_title;

            public DialogBox(string aMessage, string aTitle)
            {
                m_message = aMessage;
                m_title = aTitle;
            }
        }

        private class FadeLabel
        {
            public Rect m_position;
            public GUIContent m_content;
            public float m_lifeTime;
            public float m_fadeTime;
            public Color m_color;
            public int m_fontSize;

            public FadeLabel(Rect aPosition, GUIContent aContent, float aLifeTime, float aFadeTime, Color aColor, int aFontSize)
            {
                m_position = aPosition;
                m_content = aContent;
                m_lifeTime = aLifeTime;
                m_fadeTime = aFadeTime;
                m_color = aColor;
                m_fontSize = aFontSize;
            }

            private float m_time = 0;
            private bool m_isFading = false;
            public bool m_isDone = false;

            public void Update()
            {
                m_time += Time.deltaTime;

                if (m_time >= m_lifeTime && !m_isFading)
                {
                    m_isFading = true;
                    m_time = 0;
                }

                if (m_time >= m_fadeTime && m_isFading && !m_isDone)
                {

                    m_isDone = true;
                }

                if (m_isFading)
                {
                    m_color = Color.Lerp(m_color, new Color(m_color.r, m_color.g, m_color.b, 0), 2 * Time.deltaTime);
                }
            }
        }

        private void OnGUI()
        {
            GUI.skin = m_skin;
            int width = 250;
            int height = 100;

            for (int i = 0; i < m_labelsToDisplay.Count; i++)
            {
                Color lastColor = GUI.skin.label.normal.textColor;
                int lastSize = GUI.skin.label.fontSize;
                GUI.skin.label.normal.textColor = m_labelsToDisplay[i].m_color;
                GUI.skin.label.fontSize = m_labelsToDisplay[i].m_fontSize;
                GUI.Label(m_labelsToDisplay[i].m_position, m_labelsToDisplay[i].m_content);
                GUI.skin.label.normal.textColor = lastColor;
                GUI.skin.label.fontSize = lastSize;
            }

            for (int i = 0; i < m_dialogsToDisplay.Count; i++)
            {
                GUILayout.Window(i, new Rect(Screen.width / 2 - (width / 2), Screen.height / 2 - (height / 2), width, height), OnWindow, m_dialogsToDisplay[i].m_title);
                GUI.BringWindowToFront(i);
            }
        }

        private void OnWindow(int windowID)
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();

            GUILayout.Label(m_dialogsToDisplay[windowID].m_message);
            if (GUILayout.Button("OK"))
            {
                m_dialogsToDisplay.RemoveAt(windowID);
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        private void Start()
        {
            m_skin = (GUISkin)Resources.Load("skin");
            DontDestroyOnLoad(gameObject);
            m_dialogsToDisplay = new List<DialogBox>();
            m_labelsToDisplay = new List<FadeLabel>();
        }

        public void DisplayDialog(string aMessage, bool aGoesAway = false)
        {
            if (!aGoesAway)
            {
                GameObject dialog = (GameObject)Instantiate(transform.GetChild(0).gameObject, transform.GetChild(0).transform.position, transform.GetChild(0).rotation);
                dialog.transform.SetParent(GameObject.Find("Canvas").transform);
                dialog.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => { Destroy(dialog); });
                dialog.transform.GetChild(1).GetComponent<Text>().text = aMessage;
            }
            else
            {
                GameObject dialog = (GameObject)Instantiate(transform.GetChild(3).gameObject, transform.GetChild(3).transform.position, transform.GetChild(3).rotation);
                dialog.transform.SetParent(GameObject.Find("Canvas").transform);
                dialog.transform.GetChild(0).GetComponent<Text>().text = aMessage;
                dialog.GetComponent<ParticlesDestroyer>().enabled = true;
            }
        }

        public void DisplayAchievement(int aID, string aTitle, string aDescription)
        {
            GameObject dialog = (GameObject)Instantiate(transform.GetChild(2).gameObject, transform.GetChild(2).transform.position, transform.GetChild(2).transform.rotation);
            switch (aID)
            {
                case 0:
                    dialog.transform.GetChild(0).gameObject.SetActive(true);
                    break;
                case 1:
                    dialog.transform.GetChild(1).gameObject.SetActive(true);
                    break;
                case 2:
                    dialog.transform.GetChild(2).gameObject.SetActive(true);
                    break;
            }
            dialog.transform.SetParent(GameObject.Find("Canvas").transform);
            dialog.GetComponent<ParticlesDestroyer>().enabled = true;
            dialog.GetComponent<ParticlesDestroyer>().m_lifeTime = 5;
            dialog.transform.GetChild(3).GetComponent<Text>().text = aTitle;
            dialog.transform.GetChild(4).GetComponent<Text>().text = aDescription;
        }

        public void DisplayRankUp(int newRank)
        {
            GameObject dialog = (GameObject)Instantiate(transform.GetChild(1).gameObject, transform.GetChild(1).transform.position, transform.GetChild(1).transform.rotation);
            dialog.transform.SetParent(GameObject.Find("Canvas").transform);
            dialog.GetComponent<ParticlesDestroyer>().enabled = true;
            dialog.GetComponent<ParticlesDestroyer>().m_lifeTime = 5;
            dialog.transform.GetChild(0).GetComponent<Text>().text = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_playerLevelTitles[newRank - 1] + " (" + newRank + ")";
        }

        private bool m_showHostLeft = false;
        private bool m_showHost = false;
        private bool m_showPlaying = false;
        private bool m_showPlay = false;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (m_showHostLeft)
            {
                m_showHost = true;
            }

            if (m_showPlaying)
            {
                m_showPlay = true;
            }
        }

        public void HostLeft()
        {
            m_showHostLeft = true;
        }

        public void IsPlaying()
        {
            m_showPlaying = true;
        }

        private void LateUpdate()
        {
            if (m_showHost)
            {
                m_showHost = false;
                m_showHostLeft = false;
                DisplayDialog("The host has left the game!");
            }

            if (m_showPlay)
            {
                m_showPlay = false;
                m_showPlaying = false;
                DisplayDialog("You can't join games in progress!");
            }
        }

        public void ToggleFullscreen()
        {
            m_isFullscreen = !m_isFullscreen;
            Screen.fullScreen = m_isFullscreen;
        }
    }
}