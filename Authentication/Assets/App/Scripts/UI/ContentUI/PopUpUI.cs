using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 
/// </summary>
public class PopUpUI : ContentUIBehaviour
{
    private static readonly int UI_IS_ACTIVE = Animator.StringToHash("IsActive");

    [Header("Main")]
    [SerializeField] private Animator PopUpAnim = default;
    [SerializeField] private Animator BlockerAnim = default;
    [SerializeField] private Button BlockerButton = default;
    [SerializeField] private Transform Content = default;

    [Header("Labels")]
    [SerializeField] private TMP_Text HeaderLabel = default;
    [SerializeField] private TMP_Text ErrorBodyText = default;
    [SerializeField] private TMP_Text NormalBodyText = default;

    [Header("Button Templates")]
    [SerializeField] private Button PlainButton = default;
    [SerializeField] private Button BlueButton = default;
    [SerializeField] private Button GreenButton = default;
    [SerializeField] private Button RedButton = default;

    public bool PopUpActive
    {
        get => PopUpAnim.GetBool(UI_IS_ACTIVE);
        set => SetPopUpActiveState(value);
    }

    private List<GameObject> buttons = default;

    #region Unity Messages

    protected override void Awake()
    {
        HeaderLabel.text = string.Empty;
        ErrorBodyText.text = string.Empty;
        NormalBodyText.text = string.Empty;

        base.Awake();
    }

    private void OnEnable()
    {
        //
    }

    protected override void Start()
    {
        PlainButton.gameObject.SetActive(false);
        BlueButton.gameObject.SetActive(false);
        GreenButton.gameObject.SetActive(false);
        RedButton.gameObject.SetActive(false);

        buttons = new List<GameObject>();

        base.Start();
    }

    private void OnDisable()
    {
        BlockerButton.onClick.RemoveListener(OnClosePopUpButton);

        ClearButtons();
    }

    protected override void OnDestroy()
    {
        buttons?.Clear();
        buttons = null;

        base.OnDestroy();
    }

    #endregion

    #region UI

    protected override void InitializeUI()
    {
        PopUpActive = false;
        ClearButtons();
    }

    private void CreateButtons()
    {
        //foreach (ServiceItem serviceItem in ServiceItemUIs)
        //{
        //    MenuItemUI menuItem = Instantiate(MenuItemTemplate, MenuContent);
        //    menuItem.gameObject.SetActive(true);
        //    menuItem.gameObject.SetName(serviceItem.Name, "{0}MenuItem");
        //    menuItem.Label = serviceItem.Name;
        //    menuItem.ButtonAction = () => OnMenuItemButton(serviceItem);
        //
        //    menuItems.Add(menuItem);
        //}
    }

    private void SetPopUpActiveState(bool isActive)
    {
        IsInteractable = !isActive;
        PopUpAnim.SetBool(UI_IS_ACTIVE, isActive);
        BlockerAnim.SetBool(UI_IS_ACTIVE, isActive);

        if (isActive)
        {
            BlockerButton.onClick.AddListener(OnClosePopUpButton);
        }
        else
        {
            BlockerButton.onClick.RemoveListener(OnClosePopUpButton);
        }
    }

    private void OnClosePopUpButton()
    {
        PopUpActive = false;
    }

    private void ClearButtons()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            Destroy(buttons[i]);
        }

        buttons.Clear();
    }

    #endregion

    #region brainCloud

    private void OnServiceFunction()
    {
        //
    }

    #endregion
}
