using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoModal : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private TextMeshProUGUI _actionButtonText;
    [SerializeField] private Button _cancelButton, _closeButton;
    [SerializeField] private Button _actionButton;
    [SerializeField] private Button _backgroundButton;

    private Animator _anim;
    private Action _onAction;
    private Action _onClosed;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _cancelButton.onClick.AddListener(CloseModal);
        _closeButton.onClick.AddListener(CloseModal);
        _actionButton.onClick.AddListener(OnActionButtonClicked);
        _backgroundButton.onClick.AddListener(CloseModal);
    }

    private void OnDisable()
    {
        _cancelButton.onClick.RemoveAllListeners();
        _closeButton.onClick.RemoveAllListeners();
        _actionButton.onClick.RemoveAllListeners();
        _backgroundButton.onClick.RemoveAllListeners();
    }

    public void SetData(string title, string message, string actionButtonLabel, Action onAction, Action onClosed = null)
    {
        _titleText.text = title;
        _messageText.text = message;
        _actionButtonText.text = actionButtonLabel;
        _onAction = onAction;
        _onClosed = onClosed;
    }

    private void CloseModal()
    {
        _anim.SetBool("fadeOut", true);
    }

    private void OnActionButtonClicked()
    {
        _onAction?.Invoke();
        CloseModal();
    }

    public void OnFadeOutComplete()
    {
        _onClosed?.Invoke();
        Destroy(gameObject);
    }
}
