using TMPro;
using UnityEngine;

public class AnimatedNumberIncrement : MonoBehaviour
{
    [SerializeField]
    private Color _coinColor, _gemColor, _starColor;

    [SerializeField]
    private TextMeshProUGUI _text;

    private CurrencyType _currencyType;

    public void SetCurrencyType(CurrencyType currencyType)
    {
        _currencyType = currencyType;
        switch (_currencyType)
        {
            case CurrencyType.Coins:
                _text.color = _coinColor;
                break;
            case CurrencyType.Gems:
                _text.color = _gemColor;
                break;
            case CurrencyType.Stars:
                _text.color = _starColor;
                break;

        }
    }

    public void SetAmountUI(int amount)
    {
        _text.text = "+" + amount.ToString();
    }

    public void OnAnimationComplete()
    {
        Destroy(gameObject);
    }

    public void AdjustUI()
    {
        RectTransform rt = GetComponent<RectTransform>();

        rt.anchorMin = Vector2.zero;        // (0,0)
        rt.anchorMax = Vector2.one;         // (1,1)

        rt.offsetMin = Vector2.zero;        // Left & Bottom = 0
        rt.offsetMax = Vector2.zero;

        rt.localScale = Vector2.one;
    }

    private void OnEnable()
    {
        AdjustUI();
    }
}
