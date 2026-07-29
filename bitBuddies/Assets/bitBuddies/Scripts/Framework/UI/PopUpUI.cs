using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopUpUI : ContentUIBehaviour
{
    private const int COUNT_OF_COPIABLE_ITEMS = 3;
    private const string BUTTON_CLOSE_TEXT = "Close";

    public enum ButtonColor { Red, Blue, Green }

    [Header("Background")]
    [SerializeField] private GameObject Background;
    [SerializeField] private GameObject Background_CloseButton;
    [SerializeField] private Button CloseButton;

    [Header("Content")]
    [SerializeField] private Transform Content;
    [SerializeField] private TMP_Text HeaderText;
    [SerializeField] private Image BaseBodyImage;
    [SerializeField] private TMP_Text BaseBodyText;

    [Header("Reward Content")]
    [SerializeField] private Transform RewardContainer;
    [SerializeField] private GameObject RewardItem;

    [Header("Buttons")]
    [SerializeField] private Transform ButtonGroup;
    [SerializeField] private GameObject RedButtonHolder;
    [SerializeField] private GameObject BlueButtonHolder;
    [SerializeField] private GameObject GreenButtonHolder;

    private void OnEnable()
    {
        if (BlueButtonHolder.activeSelf)
        {
            BlueButtonHolder.GetComponentInChildren<Button>().onClick.AddListener(OnCloseButton);
        }

        CloseButton.onClick.AddListener(OnCloseButton);
    }

    private void OnDisable()
    {
        if (ButtonGroup.GetComponentsInChildren<Button>(true) is var buttons &&
            buttons != null && buttons.Length > 0)
        {
            foreach (var button in buttons)
            {
                button.onClick.RemoveAllListeners();
            }
        }

        CloseButton.onClick.RemoveAllListeners();
    }

    protected override void InitializeUI() { }

    private int GetInactiveCopiableItems() => Content.childCount - (COUNT_OF_COPIABLE_ITEMS - (RewardContainer.gameObject.activeSelf ? 1 : 0));

    public static PopUpUI Show(string headerText, bool showClose = true)
    {
        var self = Instantiate(Resources.Load<PopUpUI>("PopUpCanvas"));

        self.Background.SetActive(!showClose);
        self.Background_CloseButton.SetActive(showClose);
        self.HeaderText.text = headerText;
        self.BaseBodyImage.gameObject.SetActive(false);
        self.BaseBodyText.gameObject.SetActive(false);
        self.RewardContainer.gameObject.SetActive(false);
        self.RewardItem.SetActive(false);
        self.RedButtonHolder.SetActive(false);
        self.GreenButtonHolder.SetActive(false);

        if (showClose)
        {
            self.BlueButtonHolder.GetComponentInChildren<Button>()
                                 .GetComponentInChildren<TMP_Text>().text = BUTTON_CLOSE_TEXT;
        }
        
        self.BlueButtonHolder.SetActive(showClose);

        DontDestroyOnLoad(self);

        return self;
    }

    public PopUpUI AddImage(Sprite image)
    {
        var bodyImage = Instantiate(BaseBodyImage, Content, false);

        bodyImage.sprite = image;
        bodyImage.transform.SetSiblingIndex(GetInactiveCopiableItems());
        bodyImage.gameObject.SetActive(true);

        return this;
    }

    public PopUpUI AddBodyText(string text)
    {
        var bodyText = Instantiate(BaseBodyText, Content, false);

        bodyText.text = text;
        bodyText.transform.SetSiblingIndex(GetInactiveCopiableItems());
        bodyText.gameObject.SetActive(true);

        return this;
    }

    public PopUpUI AddRewardItem(string name, Sprite image, int amount = 0, string header = "")
    {
        if (!string.IsNullOrEmpty(header))
        {
            AddBodyText(header);
        }

        if (!RewardContainer.gameObject.activeSelf)
        {
            RewardContainer.gameObject.SetActive(true);
            RewardContainer.SetSiblingIndex(GetInactiveCopiableItems());
        }

        var reward = Instantiate(RewardItem, RewardContainer, false);

        // This is a little hacky but didn't want to make a new component just for this one situation
        foreach (var tmText in reward.GetComponentsInChildren<TMP_Text>())
        {
            if (tmText.gameObject.name == "RewardTitle")
            {
                tmText.text = name;
                continue;
            }

            if (tmText.gameObject.name == "RewardAmount")
            {
                if (amount > 0)
                {
                    tmText.text = $"x{amount:N0}";
                }
                else
                {
                    tmText.gameObject.SetActive(false);
                }

                continue;
            }
        }

        var rewardImage = reward.GetComponentInChildren<Image>();
        rewardImage.sprite = image;

        reward.SetActive(true);

        return this;
    }

    public PopUpUI AddButton(string buttonText, ButtonColor color, Action buttonAction, bool isInteractable = true, bool closePopup = true)
    {
        GameObject buttonHolder;

        switch(color)
        {
            case ButtonColor.Red:
                buttonHolder = RedButtonHolder;
                break;
            case ButtonColor.Blue:
                buttonHolder = BlueButtonHolder;
                break;
            case ButtonColor.Green:
                buttonHolder = GreenButtonHolder;
                break;
            default:
                throw new ArgumentOutOfRangeException($"Not a possible button option: {color}");
        }

        buttonHolder = Instantiate(buttonHolder, ButtonGroup, false);

        Button button = buttonHolder.GetComponentInChildren<Button>();

        if (closePopup)
        {
            button.onClick.AddListener(() =>
            {
                buttonAction?.Invoke();
                OnCloseButton();
            });
        }
        else
        {
            button.onClick.AddListener(() => buttonAction?.Invoke());
        }

        button.GetComponentInChildren<TMP_Text>().text = buttonText;
        button.interactable = isInteractable;
        buttonHolder.SetActive(true);

        return this;
    }
}
