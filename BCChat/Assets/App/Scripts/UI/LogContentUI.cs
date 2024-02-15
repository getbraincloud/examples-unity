using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>
/// An example of how a Logger can be used in your app. Can be useful to help debug your app on tester devices.
/// </para>
/// 
/// <para>
/// Makes use of <see cref="global::LogMessage"/> objects to be able to store data about the logs and copy them to the clipboard.
/// </para>
/// 
/// <br><seealso cref="BCManager"/></br>
/// <br><seealso cref="BrainCloud.LogCallback"/></br>
/// </summary>
public class LogContentUI : ContentUIBehaviour
{
    private const string LOG_APP_HEADER = "#APP";
    private const string LOG_INITIAL_TEXT = "Logs, JSON, and Error messages will appear here.";

    [Header("Main")]
    [SerializeField] private ScrollRect LogScroll = default;
    [SerializeField] private Transform LogContent = default;
    [SerializeField] private Button ClearLogButton = default;
    [SerializeField] private Button BackButton = default;

    [Header("Navigation")]
    [SerializeField] private MainContentUI MainContent = default;

    [Header("Log Messages")]
    [SerializeField] private LogMessage LogTemplate = default;
    [SerializeField, Range(10, 200)] private int MaxLogMessages = 32;

    private bool scrollAfterLog = false;
    private int logIndex = 0;
    private List<LogMessage> logObjects = default;

    #region Unity Messages

    protected override void Awake()
    {
        logObjects = new()
        {
            Capacity = MaxLogMessages
        };

        base.Awake();
    }

    private void OnEnable()
    {
        scrollAfterLog = false;

        ClearLogButton.onClick.AddListener(OnClearLogButton);
        BackButton.onClick.AddListener(OnBackButton);

        if (BCManager.Client != null)
        {
            BCManager.Client.EnableLogging(true);
            BCManager.Client.RegisterLogDelegate(OnLogDelegate);
        }
        else
        {
            IEnumerator WaitForBCClient()
            {
                yield return new WaitUntil(() => BCManager.Client != null);

                BCManager.Client.EnableLogging(true);
                BCManager.Client.RegisterLogDelegate(OnLogDelegate);
            }

            StartCoroutine(WaitForBCClient());
        }

        Application.logMessageReceived += OnLogMessageReceived;
    }

    protected override void Start()
    {
        InitializeUI();

        base.Start();

        HideLog();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ClearLogButton.onClick.RemoveAllListeners();
        BackButton.onClick.RemoveAllListeners();

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

    public void ShowLog()
    {
        LogScroll.verticalNormalizedPosition = 0.0f;
        IsInteractable = true;
        BlocksRaycasts = true;
        Opacity = 1.0f;
    }

    public void HideLog()
    {
        IsInteractable = false;
        BlocksRaycasts = false;
        Opacity = 0.0f;
    }

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
            logObjects[i].gameObject.SetName("UnusedLogObject");
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

        LogMessage log;
        for (int i = logIndex; i < MaxLogMessages; i++)
        {
            log = Instantiate(LogTemplate, LogContent);
            log.gameObject.SetName("UnusedLogObject");
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

        LogMessage log = logObjects[logIndex];
        log.ConfigureLogObject(type, message, wordWrap, canCopy);
        log.transform.SetAsLastSibling();
        log.gameObject.SetName("{0}{1}{2}Object{3}", string.Empty,
                                                     type == LogType.Log ? "Message" : "Log",
                                                     type.ToString(),
                                                     logIndex.ToString("000"));
        log.gameObject.SetActive(true);

        if (isActiveAndEnabled && !scrollAfterLog)
        {
            scrollAfterLog = true;
            StartCoroutine(ScrollAfterLogCreated());
        }
    }

    private IEnumerator ScrollAfterLogCreated()
    {
        yield return null;

        LogScroll.verticalNormalizedPosition = 0.0f;

        scrollAfterLog = false;
    }

    private void OnClearLogButton()
    {
        ClearLogs();
    }

    private void OnBackButton()
    {
        MainContent.IsInteractable = true;
        MainContent.gameObject.SetActive(true);
        HideLog();
    }

    private void OnLogMessageReceived(string log, string _, LogType type)
    {
        if (log.Contains("\nJSON Response:\n")) // Strip JSON from Success & Failure callbacks from BCManager
        {
            log = log[..log.IndexOf("\nJSON Response:\n")];
        }
        else if (log.Contains("#BCC"))
        {
            return;
        }
#if UNITY_EDITOR
        else if (Time.timeSinceLevelLoad < 1.0f && (log.Contains("Firebase") ||
                                                    log.Contains("Manifest") ||
                                                    log.Contains("manifest") ||
                                                    log.Contains("ProjectSettings")))
        {
            return;
        }
#endif

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
        Debug.Log(log);

        if (!log.Contains("\n"))
        {
            LogMessage(log);
            return;
        }

        LogMessage(log[..log.IndexOf("\n")]);// Server Message 

        string json = log[(log.LastIndexOf("\n") + 1)..]; // Build JSON Response
        if (json.StartsWith("{") && json.EndsWith("}") || json.StartsWith("[") && json.EndsWith("]"))
        {
            LogMessage(json.FormatJSON(), wordWrap:false, canCopy:true);
        }
    }

    #endregion
}
