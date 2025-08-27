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

    void Start()
    {
        _networkManager = FishNet.InstanceFinder.NetworkManager;
    }

    void Update()
    {
        float clientUptime = 0f;
        float serverUptime = 0f;
        double serverStartTime = -1;
        if (_networkManager != null && _networkManager.TimeManager != null)
        {
            clientUptime = _networkManager.TimeManager.ClientUptime;
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
            clientUptime = serverUptime = (float)Time.timeAsDouble;
        }

        if (countdownString != null)
        {
            countdownString.text = $"{TimerUtils.FormatTime(clientUptime)} / {TimerUtils.FormatTime(serverUptime)}";
        }
    }

    private double GetCurrentTime()
    {
        // Use epoch time in seconds (with millisecond precision) to match PlayerListItem
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
    }
}
