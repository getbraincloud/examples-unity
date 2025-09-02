using FishNet;
using FishNet.Connection;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    // display this as a timer counting UP
    [SerializeField] private TMP_Text countdownString;

    private FishNet.Managing.NetworkManager _networkManager;

    private float MAX_UP_TIME = 120.0f;// Should we get this from the 

    void Start()
    {
        _networkManager = FishNet.InstanceFinder.NetworkManager;
    }

    void Update()
    {
        float serverUptime = 0f;
        double serverStartTime = -1;
        if (_networkManager != null && _networkManager.TimeManager != null)
        {
            // Use authoritative server start time if available
            serverStartTime = PlayerListItemManager.Instance != null ? PlayerListItemManager.Instance.ServerStartTime : -1;
            double now = GetCurrentTime();
            if (serverStartTime >= 0)
            {
                serverUptime = (float)(now - serverStartTime);
            }
            else
            {
                serverUptime = _networkManager.TimeManager.ServerUptime;
            }
        }
        else
        {
            // Fallback to local time if TimeManager is not available
            serverUptime = (float)Time.timeAsDouble;
        }

        float timeLeft = MAX_UP_TIME - serverUptime;
        if (countdownString != null)
        {
            if (timeLeft > 0)
            {
                countdownString.color = Color.white;
                countdownString.text = TimerUtils.FormatTime(Mathf.Max(0, timeLeft));
            }
            else
            {
                countdownString.color = Color.red;
                float overtime = -timeLeft;
                countdownString.text = $"Server will shutdown soon: {TimerUtils.FormatTime(overtime)}";
            }
        }
    }

    private double GetCurrentTime()
    {
        // Use epoch time in seconds (with millisecond precision) to match PlayerListItem
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
    }
}
