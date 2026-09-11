using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using System;
using TMPro;
using UnityEngine;

public class CountdownTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI TimerLabel;
    [SerializeField] private GameObject ParentObject;

    private long _endEpochMs;
    private bool _isRunning = false;
    private ShopItem _shopItem;

    private void Awake()
    {
        _shopItem = GetComponent<ShopItem>();
        TimerLabel.gameObject.SetActive(false);
        if (ParentObject)
        {
            ParentObject.SetActive(false);
        }

    }

    public void StartCountdown(long epochMS)
    {
        if (ParentObject)
        {
            ParentObject.SetActive(true);
        }

        _endEpochMs = epochMS;
        UpdateDisplay();
        _isRunning = true;
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
            TimerLabel.gameObject.SetActive(false);
            OnCooldownComplete();
            return;
        }

        if (TimerLabel && TimerLabel.gameObject && !TimerLabel.gameObject.activeSelf)
        {
            TimerLabel.gameObject.SetActive(true);
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
        //Format dynamically if time should include hours or just minutes/seconds.

        if (t.TotalHours >= 1)
            return $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";

        return $"{t.Minutes:D2}:{t.Seconds:D2}";
    }

    private void OnCooldownComplete()
    {
        if (_shopItem)
        {
            _shopItem.EnableBuyButton();
            if (!_shopItem.ItemInfo.ShopId.IsNullOrEmpty() && _shopItem.ItemInfo.ShopId == "freebie")
            {
                GameManager.Instance.FreebieItemCooldownUntil = 0;
            }
        }
        if (ParentObject)
        {
            ParentObject.SetActive(false);
        }

    }
}
