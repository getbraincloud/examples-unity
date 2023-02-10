using BrainCloud.LitJson;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>
/// An example of how a Logger can be used in your app.
/// Can be useful to help debug your app on tester devices.
/// </para>
///
/// <seealso cref="BCManager"/><br></br>
/// <seealso cref="BrainCloud.LogCallback"/>
/// </summary>
public class LoggerContentUI : MonoBehaviour, IContentUI
{
    private const string LOG_INITIAL_TEXT = "#APP - Logs, Json, and Error messages will appear here.";
    private const string LOG_COPY_TEXT = "#APP - Previous BCC Log copied to clipboard.";

    [Header("Main")]
    [SerializeField] private CanvasGroup UICanvasGroup = default;
    [SerializeField] private ScrollRect LogScroll = default;
    [SerializeField] private Transform LogContent = default;
    [SerializeField] private Button ClearLogButton = default;
    [SerializeField] private Button CopyLogButton = default;
    [SerializeField] private GameObject CopyLogContainer = default;

    [Header("Templates")]
    [SerializeField] private TMP_Text LogTemplate = default;
    [SerializeField] private TMP_Text ErrorTemplate = default;

    private int logCount = 0;
    private int errorCount = 0;
    private string lastMessage = string.Empty;
    private List<GameObject> logGOs = default;

    #region IContentUI

    public bool IsInteractable
    {
        get { return UICanvasGroup.interactable; }
        set { UICanvasGroup.interactable = value; }
    }

    public float Opacity
    {
        get { return UICanvasGroup.alpha; }
        set { UICanvasGroup.alpha = value < 0.0f ? 0.0f : value > 1.0f ? 1.0f : value; }
    }

    public GameObject GameObject => gameObject;

    public Transform Transform => transform;

    #endregion

    #region Unity Messages

    private void Awake()
    {
        LogTemplate.text = string.Empty;
        ErrorTemplate.text = string.Empty;
    }

    private void OnEnable()
    {
        ClearLogButton.onClick.AddListener(OnClearLogButton);

#if UNITY_STANDALONE
        CopyLogButton.onClick.AddListener(OnCopyLogButton);
#endif

        BCManager.Client.EnableLogging(true);
        BCManager.Client.RegisterLogDelegate(OnLogDelegate);
    }

    private void Start()
    {
        logGOs = new List<GameObject>();

        LogTemplate.gameObject.SetActive(false);
        ErrorTemplate.gameObject.SetActive(false);

#if UNITY_STANDALONE
        CopyLogContainer.SetActive(false);
#endif

        LogMessage(LOG_INITIAL_TEXT);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ClearLogButton.onClick.RemoveAllListeners();
        CopyLogButton.onClick.RemoveAllListeners();
        BCManager.Client?.EnableLogging(false);
    }

    private void OnDestroy()
    {
        logGOs.Clear();
        logGOs = null;
    }

    #endregion

    #region UI

    public void LogMessage(string message) =>
        CreateLogObject("Log", ++logCount, message, LogTemplate);

    public void LogError(string error) =>
        CreateLogObject("Error", ++errorCount, error, ErrorTemplate);

    private void CreateLogObject(string type, int count, string message, TMP_Text textTemplate)
    {
        TMP_Text text = Instantiate(textTemplate, LogContent);
        text.gameObject.SetActive(true);
        text.gameObject.SetName($"{type}{count}", "{0}LogText");
        text.text = message;

        logGOs.Add(text.gameObject);

        StopCoroutine(ScrollAfterLogCreated());
        StartCoroutine(ScrollAfterLogCreated());
    }

    private IEnumerator ScrollAfterLogCreated()
    {
        yield return null;

        LogScroll.verticalNormalizedPosition = 0.0f;
    }

    private void OnClearLogButton()
    {
        for (int i = 0; i < logGOs.Count; i++)
        {
            Destroy(logGOs[i]);
        }

        logGOs.Clear();

        logCount = 0;
        errorCount = 0;
        lastMessage = string.Empty;
    }

    private void OnCopyLogButton()
    {
        if(!lastMessage.IsNullOrEmpty())
        {
            GUIUtility.systemCopyBuffer = lastMessage;
            LogMessage(LOG_COPY_TEXT);
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

        string json = log[(log.LastIndexOf("\n") + 1)..]; // Build Json Response
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
