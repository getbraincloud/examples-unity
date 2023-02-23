using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Access the labels & icons in our custom buttons.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonContent : MonoBehaviour
{
    [SerializeField] private TMP_Text ButtonLabel = default;
    [SerializeField] private Image IconLeft = default;
    [SerializeField] private Image IconRight = default;

    [NonSerialized] public Button button = default;
    [NonSerialized] public Animator animator = default;

    public string Label
    {
        get => ButtonLabel.text;
        set
        {
            ButtonLabel.text = value.IsEmpty() ? string.Empty : value;
            ButtonLabel.gameObject.SetActive(!value.IsEmpty());
        }
    }

    public Sprite LeftIcon
    {
        get => IconLeft.sprite;
        set
        {
            IconLeft.sprite = value;
            IconLeft.gameObject.SetActive(value != null);
        }
    }

    public Sprite RightIcon
    {
        get => IconRight.sprite;
        set
        {
            IconRight.sprite = value;
            IconRight.gameObject.SetActive(value != null);
        }
    }

    #region Unity Messages

    private void Start()
    {
        button = GetComponent<Button>();
        animator = button.animator;

        Label = ButtonLabel.text;
        ShowIcons();
    }

    private void OnDestroy()
    {
        button = null;
        animator = null;
    }

    #endregion

    #region UI

    public void ShowIcons()
    {
        IconLeft.gameObject.SetActive(LeftIcon != null);
        IconRight.gameObject.SetActive(RightIcon != null);
    }

    public void HideIcons()
    {
        IconLeft.gameObject.SetActive(false);
        IconRight.gameObject.SetActive(false);
    }

    #endregion
}
