using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    //Main menu palette: cyan accent at full health, falling through amber to the same warm red
    //FloatingDamageNumber uses for self-hits.
    public static readonly Color HealthyColor = new Color(0.18f, 0.93f, 0.89f);
    public static readonly Color WarningColor = new Color(1.00f, 0.78f, 0.25f);
    public static readonly Color CriticalColor = new Color(1.00f, 0.35f, 0.30f);

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

    private Slider Slider => _slider != null ? _slider : (_slider = GetComponent<Slider>());

    public void AssignTeamColor(Color in_teamColor)
    {
        //Structures leave TeamColorImage unassigned; only troops carry a team swatch.
        if (TeamColorImage == null)
        {
            return;
        }

        TeamColorImage.color = in_teamColor;
    }

    private void RefreshFillColor()
    {
        if (FillImage == null || Slider.maxValue <= 0)
        {
            return;
        }

        float percent = Mathf.Clamp01(Slider.value / Slider.maxValue);

        FillImage.color = percent >= 0.5f
            ? Color.Lerp(WarningColor, HealthyColor, (percent - 0.5f) / 0.5f)
            : Color.Lerp(CriticalColor, WarningColor, percent / 0.5f);
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

        RefreshFillColor();
    }

    public void SetHealth(int newValue)
    {
        if (Slider.value < Slider.maxValue)
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

        RefreshFillColor();
    }
}
