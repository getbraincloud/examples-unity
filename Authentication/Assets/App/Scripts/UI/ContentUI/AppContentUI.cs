using TMPro;
using UnityEngine;

/// <summary>
/// Used for app navigation on the App screen.
/// It is able to Instantiate the various ServiceUI prefab through <see cref="MainMenuUI"/>.
/// </summary>
public class AppContentUI : ContentUIBehaviour
{
    [Header("Main")]
    [SerializeField] private TMP_Text TitleLabel = default;
    [SerializeField] private OpenLinkButton APILink = default;
    [SerializeField] private Transform ServiceContent = default;

    [Header("Information Box")]
    [SerializeField] private TMP_Text InfoBoxBodyText = default;

    [Header("Text Defaults")]
    [SerializeField] private string DefaultHeaderText = string.Empty;
    [SerializeField, TextArea(6, 6)] private string DefaultInformationText = string.Empty;

    private ContentUIBehaviour currentServiceUI = default;

    #region Unity Messages

    protected override void Awake()
    {
        TitleLabel.text = string.Empty;
        InfoBoxBodyText.text = string.Empty;

        base.Awake();
    }

    private void OnEnable()
    {
        APILink.enabled = true;

        if (currentServiceUI != null)
        {
            currentServiceUI.IsInteractable = true;
        }
    }

    protected override void Start()
    {
        InitializeUI();

        base.Start();
    }

    private void OnDisable()
    {
        APILink.enabled = false;

        if (currentServiceUI != null)
        {
            currentServiceUI.IsInteractable = false;
        }
    }

    protected override void OnDestroy()
    {
        APILink.URLToOpen = string.Empty;
        ClearCurrentServiceUI();

        base.OnDestroy();
    }

    #endregion

    #region UI

    public void LoadServiceItemContent(ServiceItem serviceItem)
    {
        ClearCurrentServiceUI();

        TitleLabel.text = serviceItem.Name;
        InfoBoxBodyText.text = serviceItem.Description;
        APILink.URLToOpen = serviceItem.APILink;
        APILink.gameObject.SetActive(!serviceItem.APILink.IsEmpty());

        currentServiceUI = Instantiate(serviceItem.Prefab, ServiceContent);
        currentServiceUI.gameObject.SetActive(true);
        currentServiceUI.gameObject.SetName(serviceItem.Name, "{0}ContentUI");
    }

    protected override void InitializeUI()
    {
        ClearCurrentServiceUI();

        TitleLabel.text = DefaultHeaderText;
        InfoBoxBodyText.text = DefaultInformationText;

        APILink.gameObject.SetActive(false);
    }

    private void ClearCurrentServiceUI()
    {
        if (currentServiceUI != null &&
            currentServiceUI.gameObject != null)
        {
            Destroy(currentServiceUI.gameObject);
            currentServiceUI = null;
        }
    }

    #endregion
}
