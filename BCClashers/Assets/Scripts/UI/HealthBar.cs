using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image FillImage;
    public Image BorderImage;
    public Image HeartImage;
    private Slider _slider;
    public Image TeamColorImage;
    private void Awake()
    {
        _slider = GetComponent<Slider>();
        
        AdjustImageBeingActive(false);
    }

    public void AssignTeamColor(Color in_teamColor)
    {
        TeamColorImage.color = in_teamColor;
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

        //Show the bar from the moment the unit spawns. It used to stay hidden until the first
        //hit (only SetHealth re-enabled it), so healthy troops had no bar at all and you couldn't
        //read their health until something damaged them.
        AdjustImageBeingActive(true);
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
