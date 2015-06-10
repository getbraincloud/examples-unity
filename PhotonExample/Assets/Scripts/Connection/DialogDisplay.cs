using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BrainCloudPhotonExample.Connection
{
    public class DialogDisplay : MonoBehaviour
    {

        public static DialogDisplay s_instance;

        List<DialogBox> m_dialogsToDisplay;
        List<FadeLabel> m_labelsToDisplay;

        private GUISkin m_skin;

        void Awake()
        {
            if (s_instance)
                DestroyImmediate(gameObject);
            else
                s_instance = this;
        }

        class DialogBox
        {
            public string m_message;
            public string m_title;

            public DialogBox(string aMessage, string aTitle)
            {
                m_message = aMessage;
                m_title = aTitle;
            }
        }

        class FadeLabel
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

        void OnGUI()
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

        void OnWindow(int windowID)
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

        void Start()
        {
            m_skin = (GUISkin)Resources.Load("skin");
            DontDestroyOnLoad(gameObject);
            m_dialogsToDisplay = new List<DialogBox>();
            m_labelsToDisplay = new List<FadeLabel>();
        }

        public void DisplayDialog(string aMessage, string aTitle)
        {
            m_dialogsToDisplay.Add(new DialogBox(aMessage, aTitle));
        }

        public void DisplayLabel(Rect aPosition, GUIContent aContent, float aLifeTime, float aFadeTime, Color aColor, int aFontSize)
        {
            m_labelsToDisplay.Add(new FadeLabel(aPosition, aContent, aLifeTime, aFadeTime, aColor, aFontSize));
        }

        void Update()
        {
            for (int i = 0; i < m_labelsToDisplay.Count; i++)
            {
                m_labelsToDisplay[i].Update();

                if (m_labelsToDisplay[i].m_isDone)
                {
                    m_labelsToDisplay.RemoveAt(i);
                }
            }
        }
    }
}