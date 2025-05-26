using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModalUI : ContentUIBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _modalTitle, _modalText, _yesText, _noText;

    [SerializeField]
    private Button _yesButton, _noButton;
    // Start is called before the first frame update

    private Action _onYesClicked, _onNoClicked;

    void Start()
    {
        _yesButton.onClick.AddListener(OnYesClicked);
        _noButton.onClick.AddListener(OnNoClicked);
    }

    private void OnDestroy()
    {
        _yesButton.onClick.RemoveListener(OnYesClicked);
        _noButton.onClick.RemoveListener(OnNoClicked);
    }

    //noText string can be empty to only show one button
    public void InitModal(string title, string text, string yesText, string noText = null, Action onYesClicked = null, Action onNoClicked = null)
    {
        _modalTitle.text = title;
        _modalText.text = text;
        _yesText.text = yesText;
        _noText.text = noText;
        _onYesClicked = onYesClicked;
        _onNoClicked = onNoClicked;

        if (string.IsNullOrEmpty(noText))
        {
            _noButton.gameObject.SetActive(false);
        }
    }

    private void OnYesClicked()
    {
        _onYesClicked?.Invoke();
        DismissModal();
    }

    private void OnNoClicked()
    {
        _onNoClicked?.Invoke();
        DismissModal();
    }

    private void DismissModal()
    {
        Destroy(this.gameObject);
    }

    protected override void InitializeUI()
    {
        InitModal(_modalTitle.text, _modalText.text, _yesText.text, _noText.text, _onYesClicked, _onNoClicked);
    }
}
