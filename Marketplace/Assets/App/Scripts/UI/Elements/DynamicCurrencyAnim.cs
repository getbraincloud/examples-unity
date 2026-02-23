using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DynamicCurrencyAnim : MonoBehaviour
{
    private RectTransform _rect;
    private Image _image;

    public Action<int> OnAnimationComplete;

    [SerializeField]
    private Sprite coinIcon, gemIcon, starIcon;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
    }

    public void UpdateIcon(CurrencyType currencyType)
    {
        switch (currencyType)
        {
            case CurrencyType.Coins:
                _image.sprite = coinIcon;
                break;
            case CurrencyType.Gems:
                _image.sprite = gemIcon;
                break;
            case CurrencyType.Stars:
                _image.sprite = starIcon;
                break;
        }
    }

    public void AnimateToLocation(RectTransform target, CurrencyType currencyType, int amount)
    {
        UpdateIcon(currencyType);
        StartCoroutine(MoveUI(target, 1f, () => { OnAnimationComplete?.Invoke(amount); }));
    }

    private IEnumerator MoveUI(RectTransform target, float duration, Action onComplete)
    {
        Vector2 startPos = _rect.anchoredPosition;

        // Convert target world position into local space of rect's parent
        RectTransform parent = _rect.parent as RectTransform;

        Vector2 targetLocalPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            RectTransformUtility.WorldToScreenPoint(null, target.position),
            null,
            out targetLocalPos
        );

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            _rect.anchoredPosition = Vector2.Lerp(startPos, targetLocalPos, t);

            yield return null;
        }

        _rect.anchoredPosition = targetLocalPos;

        onComplete?.Invoke();
    }
}
