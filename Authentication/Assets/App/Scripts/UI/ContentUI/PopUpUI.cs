using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
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

        base.Start();

        InitializeUI();
    }

    private void OnDisable()
    {
        BlockerButton.onClick.RemoveAllListeners();

        ClearUIElements();
    }

    protected override void OnDestroy()
    {
        uiElements?.Clear();
        uiElements = null;

        base.OnDestroy();
    }

    #endregion

    #region UI

    protected override void InitializeUI()
    {
        PopupActive = false;
        ClearUIElements();
    }

    private void CreateUIElement(string elementName, GameObject template)
    {
        GameObject uiElement = Instantiate(template, BodyContent);
        uiElement.SetActive(true);
        uiElement.SetName(elementName);

        uiElements.Add(uiElement);
    }

    private void OnClosePopupButton()
    {
        PopupActive = false;
    }

    private void ClearUIElements()
    {
        for (int i = 0; i < uiElements.Count; i++)
        {
            Destroy(uiElements[i]);
        }

        uiElements.Clear();
    }

    #endregion

    #region brainCloud

    private void OnServiceFunction()
    {
        //
    }

    #endregion
}
