using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using FishNet;
using FishNet.Connection;
using TMPro;

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
        if (_networkManager != null && _networkManager.TimeManager != null)
        {
            clientUptime = _networkManager.TimeManager.ClientUptime;
            serverUptime = _networkManager.TimeManager.ServerUptime;
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

    private double GetCurrentNetworkTime()
    {
        // Use FishNet's TimeManager if available
        var nm = FishNet.InstanceFinder.NetworkManager;
        if (nm != null && nm.TimeManager != null)
        {
            return nm.TimeManager.ServerUptime;
        }
        // Fallback to local time if not available
        return Time.timeAsDouble;
    }
}
