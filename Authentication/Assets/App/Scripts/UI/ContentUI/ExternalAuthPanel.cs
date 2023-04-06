using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ExternalAuthPanel : ContentUIBehaviour
{
    [Header("Main")]
    [SerializeField] private Transform ButtonContent = default;
    [SerializeField] private ButtonContent ButtonTemplate = default;
    [SerializeField] private ExternalAuthItem[] AuthItems = default;

    private List<ButtonContent> authButtons = default;

    #region Unity Messages

    protected override void Awake()
    {
        //

        base.Awake();
    }

    private void OnEnable()
    {
        if (!authButtons.IsNullOrEmpty())
        {
            foreach (ButtonContent button in authButtons)
            {
                button.enabled = true;
            }
        }
    }

    protected override void Start()
    {
        authButtons = new List<ButtonContent>();

        foreach (ExternalAuthItem authItem in AuthItems)
        {
            ButtonContent button = Instantiate(ButtonTemplate, ButtonContent);
            button.gameObject.SetActive(true);
            button.gameObject.SetName(authItem.Name, "{0}MenuItem");
            button.Label = authItem.Name;
            button.LeftIcon = authItem.Icon;
            button.LabelColor = authItem.LabelColor;
            button.LeftIconColor = authItem.IconColor;
            button.BackgroundColor = authItem.BackgroundColor;
            //button.Button.onClick.AddListener(() => { });

            authButtons.Add(button);
        }

        base.Start();
    }

    private void OnDisable()
    {
        if (!authButtons.IsNullOrEmpty())
        {
            foreach (ButtonContent button in authButtons)
            {
                button.enabled = false;
            }
        }
    }

    protected override void OnDestroy()
    {
        authButtons.Clear();
        authButtons = null;

        base.OnDestroy();
    }

    #endregion

    #region UI

    protected override void InitializeUI()
    {
        //
    }

    private void OnInteractable()
    {
        //
    }

    #endregion
}
