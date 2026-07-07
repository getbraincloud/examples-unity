using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AddCurrencyAnimation : MonoBehaviour
{
    [SerializeField] private float _travelDuration = 0.8f;
    [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float _arcHeight = 100f;


    [SerializeField] private float _spawnScaleDuration = 0.15f;
    [SerializeField] private float _despawnScaleDuration = 0.1f;

    private RectTransform _rectTransform;
    private Image _image;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
    }
    public void PlayLocal(Vector2 in_startPos, Vector2 in_endPos, CurrencyTypes in_currencyType)
    {
        _image.sprite = GameManager.Instance.GetCurrencySprite(in_currencyType);
        _rectTransform.anchoredPosition = in_startPos;
        StartCoroutine(AnimationRoutineLocal(in_startPos, in_endPos));
    }

    public void PlayWorld(Vector2 in_startPos, Vector2 in_endPos, CurrencyTypes in_currencyType)
    {
        _image.sprite = GameManager.Instance.GetCurrencySprite(in_currencyType);
        _rectTransform.position = in_startPos;
        StartCoroutine(AnimationRoutineWorld(in_startPos, in_endPos));
    }

    private IEnumerator AnimationRoutineLocal(Vector2 startPos, Vector2 endPos)
    {
        // Spawn pop-in
        yield return StartCoroutine(ScaleTo(Vector3.zero, Vector3.one, _spawnScaleDuration));

        // Travel to target
        yield return StartCoroutine(LocallyMoveTo(startPos, endPos, _travelDuration));

        // Despawn pop-out
        yield return StartCoroutine(ScaleTo(Vector3.one, Vector3.zero, _despawnScaleDuration));

        Destroy(gameObject);
    }

    private IEnumerator LocallyMoveTo(Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;

        // A perpendicular arc point between the two positions
        Vector2 mid = (from + to) / 2f + Vector2.up * _arcHeight;

        while (elapsed < duration)
        {
            float t = _moveCurve.Evaluate(elapsed / duration);

            // Quadratic bezier: from -> mid -> to
            Vector2 pos = Mathf.Pow(1 - t, 2) * from
                        + 2 * (1 - t) * t * mid
                        + Mathf.Pow(t, 2) * to;

            _rectTransform.anchoredPosition = pos;
            elapsed += Time.deltaTime;
            yield return null;
        }

        _rectTransform.anchoredPosition = to;
    }

    private IEnumerator AnimationRoutineWorld(Vector2 startPos, Vector2 endPos)
    {
        // Spawn pop-in
        yield return StartCoroutine(ScaleTo(Vector3.zero, Vector3.one, _spawnScaleDuration));

        // Travel to target
        yield return StartCoroutine(WorldMoveTo(startPos, endPos, _travelDuration));

        // Despawn pop-out
        yield return StartCoroutine(ScaleTo(Vector3.one, Vector3.zero, _despawnScaleDuration));

        Destroy(gameObject);
    }

    private IEnumerator WorldMoveTo(Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;

        // A perpendicular arc point between the two positions
        Vector2 mid = (from + to) / 2f + Vector2.up * _arcHeight;

        while (elapsed < duration)
        {
            float t = _moveCurve.Evaluate(elapsed / duration);

            // Quadratic bezier: from -> mid -> to
            Vector2 pos = Mathf.Pow(1 - t, 2) * from
                          + 2 * (1 - t) * t * mid
                          + Mathf.Pow(t, 2) * to;

            _rectTransform.position = pos;
            elapsed += Time.deltaTime;
            yield return null;
        }

        _rectTransform.position = to;
    }

    private IEnumerator ScaleTo(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = to;
    }
}
