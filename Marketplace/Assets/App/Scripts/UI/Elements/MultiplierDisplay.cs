using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class MultiplierDisplay : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _timerText;

    private Coroutine _countdownRoutine;

    public void SetCountdownTimer(long endTimestampMs, Action onComplete)
    {
        if (_countdownRoutine != null)
            StopCoroutine(_countdownRoutine);

        _countdownRoutine = StartCoroutine(StartCountdownCR(endTimestampMs, onComplete));
    }

    private IEnumerator StartCountdownCR(long endTimestampMs, Action onComplete)
    {
        while (true)
        {
            double remainingMs = endTimestampMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (remainingMs <= 0)
            {
                _timerText.text = "0.0s";
                _countdownRoutine = null;
                onComplete?.Invoke();
                yield break;
            }

            _timerText.text = $"{remainingMs / 1000.0:0.0}s";

            yield return new WaitForSecondsRealtime(0.1f);
        }
    }
}
