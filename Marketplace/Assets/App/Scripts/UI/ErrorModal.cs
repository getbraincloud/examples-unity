using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ErrorModal : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI messageText;

    [SerializeField]
    private Button dismissButton;

    private void OnEnable()
    {
        dismissButton.onClick.AddListener(OnDismissClicked);
    }

    private void OnDisable()
    {
        dismissButton.onClick.RemoveAllListeners();
    }
    private void OnDismissClicked()
    {

    }
}
