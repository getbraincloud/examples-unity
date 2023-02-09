using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text MenuLabel;
    [SerializeField] private Button MenuButton;

    public string Label
    {
        get { return MenuLabel.text; }
        set { MenuLabel.text = value; }
    }

    public Action ButtonAction;

    #region Unity Messages

    private void OnEnable()
    {
        MenuButton.onClick.AddListener(OnMenuButton);
    }

    private void OnDisable()
    {
        MenuButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        ButtonAction = null;
    }

    #endregion

    #region UI Functionality

    private void OnMenuButton()
    {
        ButtonAction?.Invoke();
    }

    #endregion
}
