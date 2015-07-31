using UnityEngine;
using UnityEngine.UI;

public class NavButton : MonoBehaviour
{
    public Color EnabledTextColor;
    public Color DisabledTextColor;
    public Sprite EnabledSprite;
    public Sprite DisabledSprite;

    public Image ButtonImage;
    public Text ButtonText;
    public GameObject[] EnableTargetObjects;

    private void OnEnable()
    {
        SetEnabled(false);
    }

    public void SetEnabled(bool isEnabled)
    {
        for (int i = 0; i < EnableTargetObjects.Length; i++)
        {
            EnableTargetObjects[i].SetActive(isEnabled);
        }

        ButtonText.color = isEnabled ? EnabledTextColor : DisabledTextColor;

        if (EnabledSprite && DisabledSprite && ButtonImage)
            ButtonImage.sprite = isEnabled ? EnabledSprite : DisabledSprite;
    }
}
