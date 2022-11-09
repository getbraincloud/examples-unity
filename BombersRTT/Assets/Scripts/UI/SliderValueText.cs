﻿using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderValueText : MonoBehaviour
{
    [SerializeField]
    private bool EvenOnly = false;

    private void Start()
    {
        m_sliderText = GetComponent<TextMeshProUGUI>();

        GetComponentInParent<Slider>().onValueChanged.AddListener(HandleValueChanged);

        HandleValueChanged(GetComponentInParent<Slider>().value);
    }

    private void HandleValueChanged(float value)
    {
        m_sliderText.text = string.Format(m_formatText, EvenOnly ? value * 2 : value);
    }

    private string m_formatText = "{0}";    //The text shown will be formatted using this string. {0} is replaced with the actual value
    private TextMeshProUGUI m_sliderText;
}
