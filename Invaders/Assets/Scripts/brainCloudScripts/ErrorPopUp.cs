using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ErrorPopUp : MonoBehaviour
{
    public TMP_Text ErrorMessage;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void SetupPopupPanel(string errorMessage)
    {
        ErrorMessage.text = errorMessage;
        gameObject.SetActive(true);
    }
}
