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
    [SerializeField] private Image Background = default;
    [SerializeField] private Image IconLeft = default;
    [SerializeField] private Image IconRight = default;

    private Button button;
    public Button Button
    {
        get
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            return button;
        }
    }

    private Animator animator;
    public Animator Animator
    {
        get
        {
            if (animator == null)
            {
                animator = Button.animator;
            }

            return animator;
        }
    }

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
        get => IconLeft != null ? IconLeft.sprite : null;
        set
        {
            if (IconLeft != null)
            {
                IconLeft.sprite = value;
                IconLeft.gameObject.SetActive(value != null);
            }
        }
    }

    public Sprite RightIcon
    {
        get => IconRight != null ? IconRight.sprite : null;
        set
        {
            if (IconRight != null)
            {
                IconRight.sprite = value;
                IconRight.gameObject.SetActive(value != null);
            }
        }
    }

    public Color LabelColor
    {
        get => ButtonLabel.color;
        set => ButtonLabel.color = value;
    }

    public Color BackgroundColor
    {
        get => Background.color;
        set => Background.color = value;
    }

    public Color LeftIconColor
    {
        get => IconLeft != null ? IconLeft.color : Color.white;
        set
        {
            if (IconLeft != null)
            {
                IconLeft.color = value;
            }
        }
    }

    public Color RightIconColor
    {
        get => IconRight != null ? IconRight.color : Color.white;
        set
        {
            if (IconRight != null)
            {
                IconRight.color = value;
            }
        }
    }

    #region Unity Messages

    private void Start()
    {
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
        if (IconLeft != null)
        {
            IconLeft.gameObject.SetActive(LeftIcon != null);
        }

        if (IconRight != null)
        {
            IconRight.gameObject.SetActive(RightIcon != null);
        }
    }

    public void HideIcons()
    {
        if (IconLeft != null)
        {
            IconLeft.gameObject.SetActive(false);
        }

        if (IconRight != null)
        {
            IconRight.gameObject.SetActive(false);
        }
    }

    #endregion
}
