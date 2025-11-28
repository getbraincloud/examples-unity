using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class MoveBuddyAnimation : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveDuration = 0.4f;

    [Header("Shake Settings")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 10f;

    private Vector2 startPosition;
    private bool isRunning = false;
    private Vector2 _targetPosition;
    private RectTransform _buddySpriteTransform;

    private Action OnFinishShaking;
    private void Awake()
    {
        if (_buddySpriteTransform == null)
            _buddySpriteTransform = GetComponent<RectTransform>();

        startPosition = _buddySpriteTransform.anchoredPosition;
    }

    private void OnDisable()
    {
        if(isRunning)
            StopAllCoroutines();
    }

    public void MoveBuddyToBench(Vector2 in_targetPosition, Action in_onFinishShaking)
    {
        if (isRunning) return;
        OnFinishShaking = in_onFinishShaking;
        _targetPosition = in_targetPosition;
        if (!isRunning)
            StartCoroutine(MoveShakeWaitForResponse());
    }
    
    public void MoveBuddyToPosition(Vector2 in_targetPosition)
    {
        if (isRunning) return;
        _targetPosition = in_targetPosition;
        
        if (!isRunning)
            StartCoroutine(MoveToLocation());
    }

    private System.Collections.IEnumerator MoveShakeWaitForResponse()
    {
        isRunning = true;
        startPosition = _buddySpriteTransform.anchoredPosition;
        
        yield return StartCoroutine(MoveToPosition(startPosition, _targetPosition, moveDuration));
        
        yield return StartCoroutine(Shake(shakeDuration, shakeMagnitude));
        
        //ToDo: Add a wait for response yield here..
        
        //yield return StartCoroutine(MoveToPosition(_buddySpriteTransform.anchoredPosition, startPosition, moveDuration));

        isRunning = false;
    }
    
    private System.Collections.IEnumerator MoveToLocation()
    {
        isRunning = true;
        
        startPosition = _buddySpriteTransform.anchoredPosition;
        
        yield return StartCoroutine(MoveToPosition(startPosition, _targetPosition, moveDuration));

        isRunning = false;
    }

    private System.Collections.IEnumerator MoveToPosition(Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            _buddySpriteTransform.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }

        _buddySpriteTransform.anchoredPosition = to;
    }

    private System.Collections.IEnumerator Shake(float duration, float magnitude)
    {
        float elapsed = 0f;
        Vector2 original = _buddySpriteTransform.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            _buddySpriteTransform.anchoredPosition = original + Random.insideUnitCircle * magnitude;

            yield return null;
        }
        
        if(OnFinishShaking != null)
        {
            OnFinishShaking();
        }

        _buddySpriteTransform.anchoredPosition = original;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var pickUpScript = other.GetComponent<RewardPickup>();
        if(pickUpScript)
        {
            pickUpScript.PickUpCollected();
        }
    }
}
