using System;
using TMPro;
using UnityEngine;

public class CountdownTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI TimerLabel;

    private long _endEpochMs;
    private bool _isRunning = false;
    private ShopItem _shopItem;

    private void Awake()
    {
        _shopItem = GetComponent<ShopItem>();
        TimerLabel.enabled = false;
    }

    public void StartCountdown(long epochMS)
    {
        _endEpochMs = epochMS;
        _isRunning = true;
        UpdateDisplay();
        TimerLabel.enabled = true;
    }
    
    void Update()
    {
        if (!_isRunning) return;
        UpdateDisplay();
    }
    
    void UpdateDisplay()
    {
        TimeSpan remaining = GetRemainingTime(_endEpochMs);

        if (remaining.TotalSeconds <= 0)
        {
            _isRunning = false;
            TimerLabel.text = "";
            TimerLabel.enabled = false;
            OnCooldownComplete();
            return;
        }

        TimerLabel.text = FormatTime(remaining);
    }
    
    public static TimeSpan GetRemainingTime(long endEpochMs)
    {
        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        long remainingMs = endEpochMs - nowMs;

        if (remainingMs <= 0)
            return TimeSpan.Zero;

        return TimeSpan.FromMilliseconds(remainingMs);
    }
    
    private string FormatTime(TimeSpan t)
    {
        //Format dynamically if time should include days, hours or just minutes/seconds.
        if (t.TotalDays >= 1)
            return $"{(int)t.TotalDays}d {t.Hours:D2}h {t.Minutes:D2}m {t.Seconds:D2}s";

        if (t.TotalHours >= 1)
            return $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
        
        return $"{t.Minutes:D2}:{t.Seconds:D2}";
    }
    
    private void OnCooldownComplete()
    {
        if(_shopItem)
        {
            _shopItem.EnableBuyButton();
        }
        GameManager.Instance.FreebieItemCooldownUntil = 0;
    }
}
