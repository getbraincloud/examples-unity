using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image FillImage;
    public Image BorderImage;
    public Image HeartImage;
    private Slider _slider;
    private void Awake()
    {
        _slider = GetComponent<Slider>();
        
        AdjustImageBeingActive(false);
    }

    public void AssignTeamColor(Color in_teamColor)
    {
        FillImage.color = in_teamColor;
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
    }

    public void SetHealth(int newValue)
    {
        if (!BorderImage.enabled)
        {
            AdjustImageBeingActive(true);
        }
        
        _slider.value = newValue;
    }
}
