using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoggerContentUI : MonoBehaviour, IContentUI
{
    private const string INITIAL_TEXT = "##APP LOG -\nLogs, Json, and Error messages will appear here.";

    [Header("Main")]
    [SerializeField] private CanvasGroup UICanvasGroup = default;
    [SerializeField] private ScrollRect LogScroll = default;
    [SerializeField] private Transform LogContent = default;
    [SerializeField] private Button ClearLogButton = default;
    [SerializeField] private Button CopyLogButton = default;
    [SerializeField] private TMP_Text LogTemplate = default;
    [SerializeField] private TMP_Text ErrorTemplate = default;

    private int logCount = 0;
    private int errorCount = 0;
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
    }

    private void Start()
    {
        logGOs = new List<GameObject>();

        LogTemplate.gameObject.SetActive(false);
        ErrorTemplate.gameObject.SetActive(false);

#if UNITY_STANDALONE
        CopyLogButton.gameObject.SetActive(false);
#endif

        LogMessage(INITIAL_TEXT);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ClearLogButton.onClick.RemoveAllListeners();
        CopyLogButton.onClick.RemoveAllListeners();
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

        logCount = 0;
        errorCount = 0;
        logGOs.Clear();
    }

    private void OnCopyLogButton()
    {
        // TODO: Do copy
    }

#endregion
}
