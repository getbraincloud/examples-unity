using TMPro;
using UnityEngine;

/// <summary>
/// Used for app navigation on the App screen.
/// It is able to Instantiate the various ServiceUI prefab through <see cref="MainMenuUI"/>.
/// </summary>
public class AppContentUI : MonoBehaviour, IContentUI
{
    [Header("Main")]
    [SerializeField] private CanvasGroup UICanvasGroup = default;
    [SerializeField] private TMP_Text TitleLabel = default;
    [SerializeField] private OpenLinkButton APILink = default;
    [SerializeField] private Transform ServiceContent = default;
    [SerializeField] private LoggerContentUI Logger = default;

    [Header("Information Box")]
    [SerializeField] private TMP_Text InfoBoxBodyText = default;

    [Header("Text Defaults")]
    [SerializeField] private string DefaultHeaderText = string.Empty;
    [SerializeField, TextArea(6, 6)] private string DefaultInformationText = string.Empty;

    private IContentUI currentServiceUI;

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
        TitleLabel.text = string.Empty;
        InfoBoxBodyText.text = string.Empty;
    }

    private void OnEnable()
    {
        APILink.enabled = true;

        if (currentServiceUI != null)
        {
            currentServiceUI.IsInteractable = true;
        }
    }

    private void Start()
    {
        TitleLabel.text = DefaultHeaderText;
        InfoBoxBodyText.text = DefaultInformationText;

        APILink.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        APILink.enabled = false;

        if (currentServiceUI != null)
        {
            currentServiceUI.IsInteractable = false;
        }
    }

    private void OnDestroy()
    {
        APILink.URLToOpen = string.Empty;
        currentServiceUI = null;
    }

    #endregion

    #region UI

    public void LoadServiceItemContent(ServiceItem serviceItem)
    {
        if (currentServiceUI != null &&
            currentServiceUI.GameObject != null)
        {
            Destroy(currentServiceUI.GameObject);
            currentServiceUI = null;
        }

        TitleLabel.text = serviceItem.Name;
        InfoBoxBodyText.text = serviceItem.Description;
        APILink.URLToOpen = serviceItem.APILink;
        APILink.gameObject.SetActive(!serviceItem.APILink.IsNullOrEmpty());

        currentServiceUI = Instantiate(serviceItem.Prefab, ServiceContent).GetComponent(typeof(IContentUI)) as IContentUI;
        currentServiceUI.GameObject.SetActive(true);
        currentServiceUI.GameObject.SetName(serviceItem.Name, "{0}ContentUI");
    }

    #endregion
}
