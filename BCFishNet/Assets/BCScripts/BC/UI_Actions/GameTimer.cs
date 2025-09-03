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
    [SerializeField] private GameObject joinObject;


    private FishNet.Managing.NetworkManager _networkManager;


    void Start()
    {
        _networkManager = FishNet.InstanceFinder.NetworkManager;
        joinObject.SetActive(false);
    }

    void Update()
    {
        float serverUptime = 0f;
        double serverStartTime = -1;
        if (_networkManager != null && _networkManager.TimeManager != null)
        {
            // Use authoritative server start time if available
            serverStartTime = PlayerListItemManager.Instance != null ? PlayerListItemManager.Instance.ServerStartTime : -1;
            double now = TimeUtils.GetCurrentTime();
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

        float timeLeft = TimeUtils.MAX_UP_TIME - serverUptime;
        if (countdownString != null)
        {
            if (timeLeft > TimeUtils.ENDING_SOON_TIME)
            {
                countdownString.color = Color.white;
                countdownString.text = TimerUtils.FormatTime(Mathf.Max(0, timeLeft));
            }
            else if (timeLeft > 0)
            {
                countdownString.color = Color.yellow;
                countdownString.text = $"Game Ending Soon: {TimerUtils.FormatTime(timeLeft)}";
            }
            else
            {
                countdownString.color = Color.red;
                float overtime = -timeLeft;
                countdownString.text = $"Server is shutting down: {TimerUtils.FormatTime(overtime)}";

                //joinObject.SetActive(true);

                if (!_shutdownTriggered)
                {
                    _shutdownTriggered = true;
                    StartCoroutine(ShutdownSequence());
                }

            }
        }
    }

    private bool _shutdownTriggered = false;
    private IEnumerator ShutdownSequence()
    {
        yield return new WaitForSeconds(TimeUtils.SHUT_DOWN_TIME);

        BackBehavior back = FindObjectOfType<BackBehavior>();
        if (back != null)
        {
            Debug.Log("[GameTimer] Triggering BackBehavior.OnMainMenu(false) after shutdown delay.");
            back.OnMainMenu(false);
        }
        else
        {
            Debug.LogWarning("[GameTimer] No BackBehavior found in scene!");
        }
    }
}
