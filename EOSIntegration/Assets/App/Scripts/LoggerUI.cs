using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoggerUI : MonoBehaviour
{
    private const string LOG_APP_HEADER = "#APP";
    private const string LOG_INITIAL_TEXT = "Logs, JSON, and Error messages will appear here.";
    private const int MAX_MESSAGES = 256;

    [Header("Main")]
    [SerializeField] private ScrollRect LogScroll = default;
    [SerializeField] private Transform LogContent = default;

    [Header("Log Messages")]
    [SerializeField] private LogMessageUI LogTemplate = default;

    private bool scrollAfterLog = false;
    private int logIndex = 0;
    private List<LogMessageUI> logObjects = default;

    #region Unity Messages

    private void Awake()
    {
        logObjects = new()
        {
            Capacity = MAX_MESSAGES
        };
    }

    private void OnEnable()
    {
        scrollAfterLog = false;

        IEnumerator AttachToBCClient()
        {
            bool isAttached = false;

            while (true)
            {
                if (FindAnyObjectByType<BrainCloudWrapper>() is var bc &&
                    bc != null && bc.Client.IsInitialized() && isAttached == false)
                {
                    isAttached = true;
                    bc.Client.EnableLogging(true);
                    bc.Client.RegisterLogDelegate(OnLogDelegate);
                }
                else if (bc == null || bc.Client == null)
                {
                    isAttached = false;
                }

                yield return null;
            }
        }

        StartCoroutine(AttachToBCClient());

        Application.logMessageReceived += OnLogMessageReceived;
    }

    private void Start()
    {
        if (logObjects.Count < MAX_MESSAGES)
        {
            CreateLogObjects();
        }
        else
        {
            ClearLogs();
        }

        LogMessage($"{LOG_APP_HEADER} - {LOG_INITIAL_TEXT}");
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        Application.logMessageReceived -= OnLogMessageReceived;
    }

    private void OnDestroy()
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
    }

    #endregion

    #region Logger UI

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

    private void CreateLogObjects()
    {
        logIndex = logObjects.Count;

        LogMessageUI log;
        for (int i = logIndex; i < MAX_MESSAGES; i++)
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

        LogMessageUI log = logObjects[logIndex];
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
        yield return new WaitForFixedUpdate();

        LogScroll.verticalNormalizedPosition = 0.0f;

        scrollAfterLog = false;
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
        else if (Time.timeSinceLevelLoad < 1.0f && log.ToLower() is string lower &&
                 (lower.Contains("externaldependencymanager") ||
                  lower.Contains("firebase") ||
                  lower.Contains("manifest") ||
                  lower.Contains("projectsettings")))
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

    public void OnLogDelegate(string log)
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
            LogMessage(json.FormatJSON(), wordWrap: false, canCopy: true);
        }
    }

    #endregion
}
