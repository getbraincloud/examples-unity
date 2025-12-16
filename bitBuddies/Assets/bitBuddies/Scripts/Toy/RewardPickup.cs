using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RewardPickup : MonoBehaviour
{
    [SerializeField] private Sprite[] RewardSprites;

    private Button _pickUpButton;
    private Image _pickUpImage;
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
    private RectTransform _rectTransform;
    private RectTransform _rewardRectTransform;
    public float moveDuration = 0.2f;
    
    private void Awake()
    {
        _pickUpImage = GetComponent<Image>();
        _pickUpButton = GetComponent<Button>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        StartCoroutine(PerformBehavior());
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    public void SetUpPickup(CurrencyTypes in_currencyType, int in_rewardAmount, ToyBench in_toyBench, Vector3 in_targetPosition)
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
    }
    
    public void PickUpCollected()
    {
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
        var startPosition = _rewardRectTransform.anchoredPosition;
        var duration = moveDuration;
        
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            _rewardRectTransform.anchoredPosition = Vector2.Lerp(startPosition, _targetPosition, t);
            yield return null;
        }

        _rewardRectTransform.anchoredPosition = _targetPosition;

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
