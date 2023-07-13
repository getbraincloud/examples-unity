using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Used to display popups and present options to the user.
/// </summary>
public class PopupUI : ContentUIBehaviour
{
    private static readonly int UI_IS_ACTIVE = Animator.StringToHash("IsActive");

    [Header("Main")]
    [SerializeField] private Animator PopupAnim = default;
    [SerializeField] private Button BlockerButton = default;
    [SerializeField] private Transform BodyContent = default;

    [Header("Labels")]
    [SerializeField] private TMP_Text HeaderLabel = default;

    [Header("Text Templates")]
    [SerializeField] private TMP_Text CenteredBodyText = default;
    [SerializeField] private TMP_Text JustifiedBodyText = default;
    [SerializeField] private TMP_Text ErrorBodyText = default;

    [Header("Button Templates")]
    [SerializeField] private ButtonContent PlainButton = default;
    [SerializeField] private ButtonContent BlueButton = default;
    [SerializeField] private ButtonContent GreenButton = default;
    [SerializeField] private ButtonContent RedButton = default;

    public bool PopupActive
    {
        get => PopupAnim.GetBool(UI_IS_ACTIVE);
        set => PopupAnim.SetBool(UI_IS_ACTIVE, value);
    }

    private List<GameObject> uiElements = default;

    #region Unity Messages

    protected override void Awake()
    {
        HeaderLabel.text = string.Empty;
        CenteredBodyText.text = string.Empty;
        JustifiedBodyText.text = string.Empty;
        ErrorBodyText.text = string.Empty;

        base.Awake();
    }

    private void OnEnable()
    {
        BlockerButton.onClick.AddListener(OnClosePopupButton);
    }

    protected override void Start()
    {
        CenteredBodyText.gameObject.SetActive(false);
        JustifiedBodyText.gameObject.SetActive(false);
        ErrorBodyText.gameObject.SetActive(false);

        uiElements = new List<GameObject>();

        InitializeUI();

        base.Start();
    }

    private void OnDisable()
    {
        BlockerButton.onClick.RemoveAllListeners();

        ClearPopupBody();
    }

    protected override void OnDestroy()
    {
        ClearPopupBody();
        uiElements = null;

        base.OnDestroy();
    }

    #endregion

    #region UI

    public void DisplayPopup(PopupInfo popupInfo) => StartCoroutine(DisplayPopupWhenReady(popupInfo));

    public void DismissPopup() => OnClosePopupButton();

    protected override void InitializeUI()
    {
        PopupActive = false;
        ClearPopupBody();
    }

    private IEnumerator DisplayPopupWhenReady(PopupInfo popupInfo)
    {
        yield return new WaitUntil(() => Opacity <= 0.0f);

        ClearPopupBody();

        HeaderLabel.text = popupInfo.Title;
        HeaderLabel.gameObject.SetActive(popupInfo.HasTitle);

        if (!popupInfo.BodyTexts.IsNullOrEmpty())
        {
            foreach (PopupInfoBody bodyInfo in popupInfo.BodyTexts)
            {
                AddBodyText(bodyInfo);
            }
        }

        if (!popupInfo.Buttons.IsNullOrEmpty())
        {
            foreach (PopupInfoButton buttonInfo in popupInfo.Buttons)
            {
                AddButton(buttonInfo);
            }
        }

        BlockerButton.enabled = popupInfo.CanDismiss;
        if (popupInfo.CanDismiss)
        {
            AddButton(new PopupInfoButton(popupInfo.DismissButtonText, PopupInfoButton.Color.Red, null));
        }

        PopupActive = true;
    }

    private void AddBodyText(PopupInfoBody bodyInfo)
    {
        TMP_Text template = bodyInfo.BodyType == PopupInfoBody.Type.Error ? ErrorBodyText :
                            bodyInfo.BodyType == PopupInfoBody.Type.Justified ? JustifiedBodyText : CenteredBodyText;

        TMP_Text bodyText = Instantiate(template, BodyContent);
        bodyText.text = bodyInfo.Text;
        bodyText.gameObject.SetActive(true);
        bodyText.gameObject.SetName("Element{0}BodyText", (uiElements.Count + 1).ToString("00"));

        uiElements.Add(bodyText.gameObject);
    }

    private void AddButton(PopupInfoButton buttonInfo)
    {
        ButtonContent template = buttonInfo.ButtonColor == PopupInfoButton.Color.Red ? RedButton :
                                 buttonInfo.ButtonColor == PopupInfoButton.Color.Green ? GreenButton :
                                 buttonInfo.ButtonColor == PopupInfoButton.Color.Blue ? BlueButton: PlainButton;

        ButtonContent bodyButton = Instantiate(template, BodyContent);
        bodyButton.Label = buttonInfo.Label;
        bodyButton.HideIcons();

        if (buttonInfo.OnButtonAction != null)
        {
            bodyButton.Button.onClick.AddListener(() => buttonInfo.OnButtonAction());
        }

        bodyButton.Button.onClick.AddListener(OnClosePopupButton);
        bodyButton.gameObject.SetActive(true);
        bodyButton.gameObject.SetName("Element{0}Button", (uiElements.Count + 1).ToString("00"));

        uiElements.Add(bodyButton.gameObject);
    }

    private void OnClosePopupButton()
    {
        PopupActive = false;
    }

    private void ClearPopupBody()
    {
        HeaderLabel.text = string.Empty;

        for (int i = 0; i < uiElements.Count; i++)
        {
            if(uiElements[i].GetComponent<ButtonContent>() != null)
            {
                uiElements[i].GetComponent<ButtonContent>().Button.onClick.RemoveAllListeners();
            }

            Destroy(uiElements[i]);
        }

        uiElements.Clear();
    }

    #endregion
}
