using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Gradient Gradient;
    public Image FillImage;
    public Image BorderImage;
    public Image HeartImage;
    private Slider _slider;
    private void Awake()
    {
        _slider = GetComponent<Slider>();
        
        AdjustImageBeingActive(false);
    }

    private void AdjustImageBeingActive(bool isActive)
    {
        BorderImage.enabled = isActive;
        FillImage.enabled = isActive;
        HeartImage.enabled = isActive;
    }

    public void SetMaxHealth(int newMaxValue)
    {
        _slider.maxValue = newMaxValue; 
        _slider.value = newMaxValue;

        FillImage.color = Gradient.Evaluate(1f);
    }

    public void SetHealth(int newValue)
    {
        if (!BorderImage.enabled)
        {
            AdjustImageBeingActive(true);
        }
        
        _slider.value = newValue;
        FillImage.color = Gradient.Evaluate(_slider.normalizedValue);
    }

}
