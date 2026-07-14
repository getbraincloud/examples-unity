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

    /// Binding lazily makes the bar work regardless of activation order.
    /// </summary>
    private Slider Slider => _slider != null ? _slider : (_slider = GetComponent<Slider>());

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
        Slider.maxValue = newMaxValue;
        Slider.value = newMaxValue;

        //Show the bar from the moment the unit spawns. It used to stay hidden until the first
        //hit (only SetHealth re-enabled it), so healthy troops had no bar at all.
        AdjustImageBeingActive(true);
    }

    public void SetHealth(int newValue)
    {
        if (!BorderImage.enabled)
        {
            AdjustImageBeingActive(true);
        }

        //Guard against a max of 0: if SetMaxHealth never landed, the slider would clamp every
        //value to 0/1 and the fill would look stuck.
        if (Slider.maxValue <= 0)
        {
            Slider.maxValue = Mathf.Max(newValue, 1);
        }

        Slider.value = newValue;
    }
}
