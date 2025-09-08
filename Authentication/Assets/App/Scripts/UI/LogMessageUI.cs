using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Contains the log text and ability to copy them which are displayed on the <see cref="LoggerContentUI"/>.
/// </summary>
public class LogMessageUI : MonoBehaviour
{
    private static readonly Color LOG_COLOR = Color.white;
    private static readonly Color WARNING_COLOR = new Color32(0xEE, 0xD2, 0x02, 255); // Yellow
    private static readonly Color ERROR_COLOR = new Color32(0xFF, 0x1E, 0x1E, 255); // Red

    [SerializeField] private TMP_Text LogText = default;
    [SerializeField] private Button LogButton = default;

    private string stackTrace = string.Empty;

    /// <summary>
    /// The log message.
    /// </summary>
    public string Text
    {
        get => LogText.text;
        set => LogText.text = value;
    }

    /// <summary>
    /// Store stack trace. Note: Currently unused.
    /// </summary>
    public string StackTrace
    {
        get => stackTrace;
        set => stackTrace = value;
    }

    /// <summary>
    /// The color of the log text.
    /// </summary>
    public Color TextColor
    {
        get => LogText.color;
        set => LogText.color = value;
    }

    /// <summary>
    /// If the log text should word wrap or not in its container.
    /// </summary>
    public bool WordWrapText
    {
        get => LogText.textWrappingMode == TextWrappingModes.Normal;
        set => LogText.textWrappingMode = value ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
    }

    /// <summary>
    /// If the user can click on the log message to copy it to the clipboard.
    /// </summary>
    public bool CanCopyText
    {
        get { return LogButton.interactable; }
        set
        {
            LogButton.interactable = value;
            if (value)
            {
                LogButton.onClick.AddListener(OnLogMessageButton);
            }
            else
            {
                LogButton.onClick.RemoveAllListeners();
            }
        }
    }

    #region Unity Messages

    private void OnEnable()
    {
        if (CanCopyText)
        {
            LogButton.onClick.AddListener(OnLogMessageButton);
        }
    }

    private void Awake()
    {
        ClearLogObject();
    }

    private void OnDisable()
    {
        LogButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        LogText.text = string.Empty;
    }

    #endregion

    #region UI Functionality

    public void ConfigureLogObject(LogType type, string message, bool wordWrap, bool canCopy)
    {
        Text = message;
        SetLogType(type);
        WordWrapText = wordWrap;
        CanCopyText = canCopy;
    }

    public void ClearLogObject()
    {
        ConfigureLogObject(LogType.Log, string.Empty, false, false);
        StackTrace = string.Empty;
    }

    public void SetLogType(LogType type)
    {
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                TextColor = ERROR_COLOR;
                break;
            case LogType.Warning:
                TextColor = WARNING_COLOR;
                break;
            case LogType.Log:
            default:
                TextColor = LOG_COLOR;
                break;
        }
    }

    private void OnLogMessageButton()
    {
        if (!Text.IsEmpty() && GUIUtility.systemCopyBuffer != Text)
        {
            GUIUtility.systemCopyBuffer = Text;

            if (Text.StartsWith("{") && Text.EndsWith("}"))
            {
                Debug.Log("Copied JSON data to clipboard.");
            }
            else
            {
                Debug.Log("Copied Log to clipboard.");
            }
        }
    }

    #endregion
}
