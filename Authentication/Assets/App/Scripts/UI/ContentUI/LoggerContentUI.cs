using BrainCloud.LitJson;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>
/// An example of how a Logger can be used in your app. Can be useful to help debug your app on tester devices.
/// </para>
///
/// <seealso cref="BCManager"/><br></br>
/// <seealso cref="BrainCloud.LogCallback"/>
/// </summary>
public class LoggerContentUI : ContentUIBehaviour
{
    private const int MAX_LOG_MESSAGES = 30;
    private const string LOG_APP_HEADER = "#APP";
    private const string LOG_INITIAL_TEXT = "Logs, JSON, and Error messages will appear here.";
    private const string LOG_COPY_TEXT = "Previous BCC Log copied to clipboard.";

    [Header("Main")]
    [SerializeField] private ScrollRect LogScroll = default;
    [SerializeField] private Transform LogContent = default;
    [SerializeField] private Button ClearLogButton = default;
    [SerializeField] private Button CopyLogButton = default;

#pragma warning disable CS0414
    [Space, SerializeField] private GameObject CopyLogContainer = default;
#pragma warning restore CS0414

    [Header("Templates")]
    [SerializeField] private TMP_Text LogTemplate = default;
    [SerializeField] private TMP_Text WarningTemplate = default;
    [SerializeField] private TMP_Text ErrorTemplate = default;

    private int logCount = 0;
    private int warningCount = 0;
    private int errorCount = 0;
    private string lastMessage = string.Empty;
    private List<GameObject> logGOs = default;

    #region Unity Messages

    protected override void Awake()
    {
        logGOs = new List<GameObject>();

        LogTemplate.text = string.Empty;
        WarningTemplate.text = string.Empty;
        ErrorTemplate.text = string.Empty;

        base.Awake();
    }

    private void OnEnable()
    {
        ClearLogButton.onClick.AddListener(OnClearLogButton);

#if UNITY_STANDALONE
        CopyLogButton.onClick.AddListener(OnCopyLogButton);
#endif

        BCManager.Client.EnableLogging(true);
        BCManager.Client.RegisterLogDelegate(OnLogDelegate);

        Application.logMessageReceived += OnLogMessageReceived;
    }

    protected override void Start()
    {
        LogTemplate.gameObject.SetActive(false);
        WarningTemplate.gameObject.SetActive(false);
        ErrorTemplate.gameObject.SetActive(false);

#if !UNITY_STANDALONE
        CopyLogContainer.SetActive(false);
#endif
        InitializeUI();

        base.Start();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ClearLogButton.onClick.RemoveAllListeners();
        CopyLogButton.onClick.RemoveAllListeners();
        BCManager.Client?.EnableLogging(false);
        Application.logMessageReceived -= OnLogMessageReceived;
    }

    protected override void OnDestroy()
    {
        logGOs?.Clear();
        logGOs = null;

        base.OnDestroy();
    }

    #endregion

    #region UI

    public void LogMessage(string message) =>
        CreateLogObject("Log", ++logCount, message, LogTemplate);

    public void LogWarning(string warning) =>
        CreateLogObject("Warning", ++warningCount, warning, WarningTemplate);

    public void LogError(string error) =>
        CreateLogObject("Error", ++errorCount, error, ErrorTemplate);

    public void ClearLogs()
    {
        for (int i = 0; i < logGOs.Count; i++)
        {
            Destroy(logGOs[i]);
        }

        logGOs.Clear();

        logCount = 0;
        warningCount = 0;
        errorCount = 0;
        lastMessage = string.Empty;

        LogScroll.verticalNormalizedPosition = 1.0f;
    }

    protected override void InitializeUI()
    {
        ClearLogs();
        LogMessage($"{LOG_APP_HEADER} - {LOG_INITIAL_TEXT}");
    }

    private void CreateLogObject(string type, int count, string message, TMP_Text textTemplate)
    {
        if (logGOs == null)
        {
            Debug.LogWarning("Logger is not initialized yet!");
            return;
        }
        else if (logGOs.Count >= MAX_LOG_MESSAGES)
        {
            Destroy(logGOs[0]);
            logGOs.RemoveAt(0);
        }

        TMP_Text text = Instantiate(textTemplate, LogContent);
        text.gameObject.SetActive(true);
        text.gameObject.SetName($"{type}{count}", "{0}LogText");
        text.text = message;

        logGOs.Add(text.gameObject);

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

    private void OnCopyLogButton()
    {
        if(!lastMessage.IsEmpty())
        {
            GUIUtility.systemCopyBuffer = lastMessage;
            LogMessage($"{LOG_APP_HEADER} - {LOG_COPY_TEXT}");
        }
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
            lastMessage = log;
            LogMessage(log);
            return;
        }

        string message = log[..log.IndexOf("\n")]; // Server Message 

        string json = log[(log.LastIndexOf("\n") + 1)..]; // Build JSON Response
        if (json.StartsWith("{") && json.EndsWith("}"))
        {
            StringBuilder sb = new StringBuilder();
            JsonWriter writer = new JsonWriter(sb)
            {
                PrettyPrint = true
            };

            JsonMapper.ToJson(JsonMapper.ToObject(json), writer);

            message += sb.ToString();
        }

        lastMessage = message;
        LogMessage(message);
    }

    #endregion
}
