using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoggerUI : MonoBehaviour
{
    internal struct LoggerMessage
    {
        private static readonly Color LOG_COLOR = Color.white;
        private static readonly Color WARNING_COLOR = new Color32(0xEE, 0xD2, 0x02, 255); // Yellow
        private static readonly Color ERROR_COLOR = new Color32(0xFF, 0x1E, 0x1E, 255); // Red

        private TMP_Text LogText;
        private Button LogButton;
        private LoggerUI Logger;

        public GameObject GameObject
        {
            get => LogText.gameObject;
        }

        public Transform Transform
        {
            get => LogText.transform;
        }

        public LoggerMessage(TMP_Text logText, LoggerUI logger)
        {
            LogText = logText;
            LogButton = logText.GetComponent<Button>();
            Logger = logger;
            DisableButton();
        }

        public void EnableButton()
        {
            DisableButton();
            LogButton.onClick.AddListener(OnLogMessageButton);
            LogButton.interactable = true;
        }

        public void DisableButton()
        {
            LogButton.onClick.RemoveAllListeners();
            LogButton.interactable = false;
        }

        public void Dispose()
        {
            if (LogButton != null) DisableButton();

            LogText = null;
            LogButton = null;
            Logger = null;
        }

        public void ConfigureLogObject(LogType type, string message, bool canCopy)
        {
            LogText.text = message;

            LogText.color = type switch
            {
                LogType.Assert or LogType.Error or LogType.Exception => ERROR_COLOR,
                LogType.Warning => WARNING_COLOR,
                _ => LOG_COLOR
            };

            if (canCopy)
            {
                EnableButton();
            }
            else
            {
                DisableButton();
            }
        }

        private void OnLogMessageButton()
        {
            const string APP_HEADER = "#APP";

            string text = LogText.text;
            if (!string.IsNullOrWhiteSpace(text) && GUIUtility.systemCopyBuffer != text)
            {
                GUIUtility.systemCopyBuffer = text;

                if (text.Contains("{") && text.EndsWith("}"))
                {
                    Logger.LogMessageNoCopy($"{APP_HEADER} - Copied JSON to clipboard.");
                }
                else
                {
                    Logger.LogMessageNoCopy($"{APP_HEADER} - Copied Log to clipboard.");
                }
            }
        }
    }

    private const int MAX_LOG_MESSAGES = 50;
    private const string LOG_APP_HEADER = "#APP";
    private const string LOG_BCC_HEADER = "#BCC";
    private const string LOG_INITIAL_TEXT = LOG_APP_HEADER + " - Logs, JSON, and Error messages will appear here. You can " +
#if UNITY_IOS || UNITY_ANDROID
        "Tap" +
#else
        "Click" +
#endif
        " on most logs to copy it to the clipboard.";

    [SerializeField] private ScrollRect LogScroll = default;
    [SerializeField] private Transform LogContent = default;
    [SerializeField] private TMP_Text LogTemplate = default;

    private int logIndex = 0;
    private List<LoggerMessage> logObjects = default;

    #region Unity Messages

    private void Awake()
    {
        logObjects = new()
        {
            Capacity = MAX_LOG_MESSAGES
        };
    }

    private void OnEnable()
    {
        Application.logMessageReceived += OnLogMessageReceived;

        if (logObjects != null && logObjects.Count > 0)
        {
            for (int i = 0; i < logObjects.Count; i++)
            {
                logObjects[i].EnableButton();
            }
        }
    }

    private void Start()
    {
        CreateLogObjects();
        LogMessageNoCopy(LOG_INITIAL_TEXT);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        Application.logMessageReceived -= OnLogMessageReceived;

        if (logObjects != null && logObjects.Count > 0)
        {
            for (int i = 0; i < logObjects.Count; i++)
            {
                logObjects[i].DisableButton();
            }
        }
    }

    private void OnDestroy()
    {
        if (logObjects != null)
        {
            for (int i = 0; i < logObjects.Count; i++)
            {
                logObjects[i].Dispose();
            }

            logObjects.Clear();
            logObjects = null;
        }
    }

    #endregion

    #region UI

    private void LogMessage(string message) => DisplayLogObject(LogType.Log, message, true);

    private void LogMessageNoCopy(string message) => DisplayLogObject(LogType.Log, message, false);

    private void CreateLogObjects()
    {
        logIndex = logObjects.Count;

        LogTemplate.text = string.Empty;
        LogTemplate.color = Color.white;

        for (int i = logIndex; i < MAX_LOG_MESSAGES; i++)
        {
            LoggerMessage log = new(Instantiate(LogTemplate, LogContent), this);
            log.GameObject.name = "UnusedLogObject";
            log.GameObject.SetActive(false);

            logObjects.Add(log);
        }

        Destroy(LogTemplate.gameObject);
    }

    private void DisplayLogObject(LogType type, string message, bool canCopy)
    {
        if (logObjects == null)
        {
            return;
        }
        else if (++logIndex >= logObjects.Count)
        {
            logIndex = 0;
        }

        LoggerMessage log = logObjects[logIndex];
        log.ConfigureLogObject(type, message, canCopy);
        log.Transform.SetAsLastSibling();
        log.GameObject.name = $"{type}{(type == LogType.Log ? "Message" : "Log")}Object_{logIndex:00}";
        log.GameObject.SetActive(true);

        if (isActiveAndEnabled)
        {
            StopCoroutine(ScrollAfterLogCreated());
            StartCoroutine(ScrollAfterLogCreated());
        }
    }

    private IEnumerator ScrollAfterLogCreated()
    {
        yield return null;

        LogScroll.verticalNormalizedPosition = 0.0f;
    }

    private void OnLogMessageReceived(string log, string _, LogType type)
    {
        if (log.Contains(LOG_BCC_HEADER))
        {
            OnBCCMessageReceived(log);
            return;
        }

        DisplayLogObject(type, $"{LOG_APP_HEADER} - {log}", true);
    }

    #endregion

    #region brainCloud

    private void OnBCCMessageReceived(string log)
    {
        if (!log.Contains("\n"))
        {
            LogMessageNoCopy(log);
            return;
        }

        string serverMessage = log[..log.IndexOf("\n")];

        string json = log[(log.LastIndexOf("\n") + 1)..]; // Build JSON Response
        if (json.StartsWith("{") && json.EndsWith("}"))
        {
            json = serverMessage + '\n' + json.FormatJSON();
            LogMessage(json);
        }
        else
        {
            LogMessageNoCopy(serverMessage);
        }
    }

    #endregion
}

public static class JSONExtension
{
    public static string FormatJSON(this string json)
    {
        // Consts
        const string tab = "    ";

        // Setup
        char current;
        bool insideProperty = false;
        string indents = string.Empty;
        StringBuilder sb = new();
        for (int i = 0; i < json.Length; i++)
        {
            current = json[i];
            if (current == '\"' && json[i - 1] != '\\')
            {
                insideProperty = !insideProperty;
            }

            if (insideProperty)
            {
                sb.Append(current);
            }
            else if (!char.IsWhiteSpace(current))
            {
                if (current == '{' || current == '[')
                {
                    sb.Append(current);
                    if ((current == '{' && json[i + 1] == '}') ||
                        (current == '[' && json[i + 1] == ']'))
                    {
                        sb.Append(json[i + 1]);
                        i++;
                    }
                    else
                    {
                        indents += tab;
                        sb.Append(Environment.NewLine);
                        sb.Append(indents);
                    }
                }
                else if (current == '}' || current == ']')
                {
                    sb.Append(Environment.NewLine);

                    if (indents.Length >= tab.Length)
                    {
                        int removeTab = indents.Length - tab.Length;
                        indents = indents[..removeTab];
                    }

                    sb.Append(indents);
                    sb.Append(current);
                }
                else if (current == ',')
                {
                    sb.Append(current);
                    sb.Append(Environment.NewLine);
                    sb.Append(indents);
                }
                else
                {
                    sb.Append(current);
                }
            }
        }

        return sb.ToString();
    }
}
