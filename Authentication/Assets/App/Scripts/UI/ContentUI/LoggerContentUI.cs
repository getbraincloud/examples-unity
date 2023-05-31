using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>
/// An example of how a Logger can be used in your app. Can be useful to help debug your app on tester devices.
/// </para>
/// <para>
/// Makes use of <see cref="LogMessageUI"/> objects to be able to store data about the logs and copy them to the clipboard.
/// </para>
/// <seealso cref="BCManager"/><br></br>
/// <seealso cref="BrainCloud.LogCallback"/>
/// </summary>
public class LoggerContentUI : ContentUIBehaviour
{
    private const string LOG_APP_HEADER = "#APP";
    private const string LOG_INITIAL_TEXT = "Logs, JSON, and Error messages will appear here.";

    [Header("Main")]
    [SerializeField] private ScrollRect LogScroll = default;
    [SerializeField] private Transform LogContent = default;
    [SerializeField] private Button ClearLogButton = default;

    [Header("Log Messages")]
    [SerializeField] private LogMessageUI LogTemplate = default;
    [SerializeField, Range(10, 200)] private int MaxLogMessages = 30;

    private int logIndex = 0;
    private List<LogMessageUI> logObjects = default;

    #region Unity Messages

    protected override void Awake()
    {
        logObjects = new List<LogMessageUI>();
        logObjects.Capacity = MaxLogMessages;

        base.Awake();
    }

    private void OnEnable()
    {
        ClearLogButton.onClick.AddListener(OnClearLogButton);

        BCManager.Client.EnableLogging(true);
        BCManager.Client.RegisterLogDelegate(OnLogDelegate);

        Application.logMessageReceived += OnLogMessageReceived;
    }

    protected override void Start()
    {
        InitializeUI();

        base.Start();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ClearLogButton.onClick.RemoveAllListeners();
        BCManager.Client?.EnableLogging(false);
        Application.logMessageReceived -= OnLogMessageReceived;
    }

    protected override void OnDestroy()
    {
        if (!logObjects.IsNullOrEmpty())
        {
            for (int i = 0; i < logObjects.Count; i++)
            {
                Destroy(logObjects[i]);
            }

            logObjects.Clear();
            logObjects = null;
        }

        base.OnDestroy();
    }

    #endregion

    #region UI

    public void LogMessage(string message, bool wordWrap = true, bool canCopy = false) =>
        DisplayLogObject(LogType.Log, message, wordWrap, canCopy);

    public void LogWarning(string warning, bool wordWrap = true, bool canCopy = false) =>
        DisplayLogObject(LogType.Warning, warning, wordWrap, canCopy);

    public void LogError(string error, bool wordWrap = true, bool canCopy = false) =>
        DisplayLogObject(LogType.Error, error, wordWrap, canCopy);

    public void ClearLogs()
    {
        for (int i = 0; i < logObjects.Count; i++)
        {
            logObjects[i].ClearLogObject();
            logObjects[i].gameObject.SetName($"UnusedLogObject");
            logObjects[i].gameObject.SetActive(false);
        }

        logIndex = 0;
        LogScroll.verticalNormalizedPosition = 1.0f;
    }

    protected override void InitializeUI()
    {
        if (logObjects.Count < MaxLogMessages)
        {
            CreateLogObjects();
        }
        else
        {
            ClearLogs();
        }

        LogMessage($"{LOG_APP_HEADER} - {LOG_INITIAL_TEXT}");
    }

    private void CreateLogObjects()
    {
        logIndex = logObjects.Count;

        LogMessageUI log;
        for (int i = logIndex; i < MaxLogMessages; i++)
        {
            log = Instantiate(LogTemplate, LogContent);
            log.gameObject.SetName($"UnusedLogObject");
            log.gameObject.SetActive(false);

            logObjects.Add(log);
        }
    }

    private void DisplayLogObject(LogType type, string message, bool wordWrap, bool canCopy)
    {
        if (logObjects.IsNullOrEmpty())
        {
            return;
        }
        else if (++logIndex >= logObjects.Count)
        {
            logIndex = 0;
        }

        LogMessageUI log = logObjects[logIndex];
        log.ConfigureLogObject(type, message, wordWrap, canCopy);
        log.transform.SetAsLastSibling();
        log.gameObject.SetName($"{type}{(type == LogType.Log ? "Message" : "Log")}Object{logIndex:00}");
        log.gameObject.SetActive(true);

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

    private void OnClearLogButton()
    {
        ClearLogs();
    }

    private void OnLogMessageReceived(string log, string _, LogType type)
    {
        if (log.Contains("\nJSON Response:\n")) // Strip JSON from Success & Failure callbacks from BCManager
        {
            log = log[..log.IndexOf("\nJSON Response:\n")];
        }

        log = $"{LOG_APP_HEADER} - {log}";
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                LogError(log);
                break;
            case LogType.Warning:
                LogWarning(log);
                break;
            case LogType.Log:
            default:
                LogMessage(log);
                break;
        }
    }

    #endregion

    #region brainCloud

    private void OnLogDelegate(string log)
    {
        if (!log.Contains("\n"))
        {
            LogMessage(log);
            return;
        }

        LogMessage(log[..log.IndexOf("\n")]);// Server Message 

        string json = log[(log.LastIndexOf("\n") + 1)..]; // Build JSON Response
        if (json.StartsWith("{") && json.EndsWith("}"))
        {
            LogMessage(FormatJSON(json), wordWrap:false, canCopy:true);
        }
    }

    private string FormatJSON(string json)
    {
        // Consts
        const string tab = "    ";

        // Setup
        char current;
        bool insideProperty = false;
        string indents = string.Empty;
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < json.Length; i++)
        {
            current = json[i];
            if (current == '\"')
            {
                insideProperty = !insideProperty;
            }

            if (insideProperty)
            {
                if (current == '\\' && json[i + 1] == 'n')
                {
                    if (json[i + 2] != '\"')
                    {
                        sb.Append(Environment.NewLine);
                    }
                    i++;
                }
                else if (current == '\\' && json[i + 1] == 't')
                {
                    sb.Append(indents + tab);
                    i++;
                }
                else
                {
                    sb.Append(current);
                }
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

    #endregion
}
