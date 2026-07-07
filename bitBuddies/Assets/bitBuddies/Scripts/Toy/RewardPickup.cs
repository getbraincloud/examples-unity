using Gameframework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RewardPickup : MonoBehaviour
{
    [SerializeField] private Sprite[] RewardSprites;
    [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Image _pickUpImage;
    private Collider2D _collider;
    private int _rewardAmount;
    private CurrencyTypes _currencyType;

    public int RewardAmount { get { return _rewardAmount; } }
    public CurrencyTypes CurrencyType { get { return _currencyType; } }
    private Action<Vector2> OnPickUp;
    private float _totalDuration;
    private float _timeBeforeBlinkDuration;
    private float _blinkDuration;
    private float _blinkThresholdPercent = 0.25f;
    private ToyBench _toyBench;
    private bool _isCollected;

    //Blinking related
    public float _fadeDuration = 0.1f;
    private float _blinkIntervalStart = 1f;
    private float _blinkIntervalEnd = 0.5f;
    private bool _isBlinking = false;
    private float _startTime;
    private float _blinkStartTime;
    private Vector3 _targetPosition;
    private CanvasGroup _canvasGroup;
    private Coroutine _timerCoroutine;
    private RectTransform _rewardRectTransform;
    private string _entityId;
    private BuddysRoom _buddysRoom;
    private float _arcHeight = 100f;

    public string EntityId
    {
        get => _entityId;
        set => _entityId = value;
    }

    public float moveDuration = 0.2f;

    private void Awake()
    {
        _pickUpImage = GetComponent<Image>();
        _collider = GetComponent<Collider2D>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        _collider.enabled = false;
        StartCoroutine(PerformBehavior());
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    public void SetUpPickup(CurrencyTypes in_currencyType, int in_rewardAmount, ToyBench in_toyBench, Vector3 in_targetPosition, string in_entityId, BuddysRoom in_buddysRoom)
    {
        _totalDuration = GameManager.Instance.RewardPickupDuration;
        _timeBeforeBlinkDuration = _totalDuration * (1f - _blinkThresholdPercent);
        _blinkDuration = _totalDuration * _blinkThresholdPercent;
        _currencyType = in_currencyType;
        _pickUpImage.sprite = RewardSprites[(int)in_currencyType];
        _rewardAmount = in_rewardAmount;
        _toyBench = in_toyBench;
        _targetPosition = in_targetPosition;
        _rewardRectTransform = GetComponent<RectTransform>();
        _entityId = in_entityId;
        _buddysRoom = in_buddysRoom;
    }

    public void PickUpCollected()
    {
        RectTransform target = _buddysRoom.GetCurrencyTextRectTransform(_currencyType);
        StateManager.Instance.PlayCurrencyAnimationLocal
        (
            _rewardRectTransform,
            target,
            _currencyType,
            _buddysRoom.CanvasRectTransform,
            BitBuddiesConsts.OFFSET_CURRENCY_BUDDYS_ROOM
        );
        ToyManager.Instance.DecrementRewardSpawnCount();
        ToyManager.Instance.AddRewardPickup(this);
        StopAllCoroutines();
        Destroy(gameObject);
    }

    private IEnumerator PerformBehavior()
    {
        //Go to target location to land
        yield return StartCoroutine(MoveToLocation());

        //Wait to start blinking
        yield return new WaitForSeconds(_timeBeforeBlinkDuration);

        _isBlinking = true;
        yield return StartCoroutine(BlinkSequence());

        Destroy(gameObject);
    }

    private IEnumerator BlinkSequence()
    {
        float endTime = Time.time + _blinkDuration;
        while (Time.time < endTime)
        {
            // Calculate progress (0 to 1) through the blink duration
            float progress = (Time.time - _startTime) / _blinkDuration;

            // Lerp from slow interval to fast interval based on progress
            float currentInterval = Mathf.Lerp(_blinkIntervalStart, _blinkIntervalEnd, progress);
            float currentFadeDuration = Mathf.Min(_fadeDuration, currentInterval / 4f);
            yield return StartCoroutine(FadeOut(currentFadeDuration));
            yield return StartCoroutine(FadeIn(currentFadeDuration));
        }
    }

    private IEnumerator MoveToLocation()
    {
        var duration = moveDuration;
        float elapsed = 0f;
        Vector2 from = _rewardRectTransform.anchoredPosition;
        Vector2 to = _targetPosition;

        // A perpendicular arc point between the two positions
        Vector2 mid = (from + to) / 2f + Vector2.up * _arcHeight;

        while (elapsed < duration)
        {
            float t = _moveCurve.Evaluate(elapsed / duration);

            // Quadratic bezier: from -> mid -> to
            Vector2 pos = Mathf.Pow(1 - t, 2) * from
                          + 2 * (1 - t) * t * mid
                          + Mathf.Pow(t, 2) * to;

            _rewardRectTransform.anchoredPosition = pos;
            elapsed += Time.deltaTime;
            yield return null;
        }

        _rewardRectTransform.anchoredPosition = to;

        // Sit for half a second
        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        _collider.enabled = true;
    }

    IEnumerator FadeOut(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(0f);
    }

    IEnumerator FadeIn(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(1f);
    }

    void SetAlpha(float alpha)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = alpha;
        }
        else if (_pickUpImage != null)
        {
            Color c = _pickUpImage.color;
            c.a = alpha;
            _pickUpImage.color = c;
        }
    }

    // Get remaining time (useful for UI displays)
    public float GetRemainingTime()
    {
        if (_startTime == 0f) return 0f;
        float elapsed = Time.time - _startTime;
        return Mathf.Max(0f, _totalDuration - elapsed);
    }

    // Check if currently blinking
    public bool IsBlinking()
    {
        return _isBlinking;
    }
}
