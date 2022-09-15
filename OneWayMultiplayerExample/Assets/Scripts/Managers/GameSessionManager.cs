using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using BrainCloud.JsonFx.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSessionManager : MonoBehaviour
{
    public float RoundDuration;
    public Image ClockFillImage;
    public int CheckInterval = 60;
    public GameOverScreen GameOverScreen;

    private Coroutine _replayCoroutine;
    private DefenderSpawner _defenderSpawner;
    private SpawnData _invaderSpawnData;
    
    private float _startTime;
    private float _time;
    private float _value;
    private int _frameId;
    private bool _replayMode;
    
    
    public int FrameID
    {
        get => _frameId;
    }
    // Start is called before the first frame update
    void Start()
    {
        GameOverScreen.gameObject.SetActive(false);
        ClockFillImage.fillAmount = 1;
        _defenderSpawner = FindObjectOfType<DefenderSpawner>();
        _invaderSpawnData = FindObjectOfType<SpawnController>().SpawnData;

        StartCoroutine(Timer(RoundDuration));  
    }

    private void FixedUpdate()
    {
        if (!_replayMode)
        {
            _frameId++;
        }
    }

    public void StopTimer()
    {
        if (_time > 0.0f)
        {
            GameOverScreen.TimerText.text = $"Time Remaining: {(int)_time} seconds";
        }
        else
        {
            GameOverScreen.TimerText.text = "Time Expired";
        }
        
        StopAllCoroutines();
    }

    //Called from Game over screen -> button
    public void LoadToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator Timer(float duration)
    {
        _startTime = Time.time;
        _time = duration;
        _value = 1;

        while (Time.time - _startTime < duration)
        {
            _time -= Time.deltaTime;
            _value = _time / duration;
            ClockFillImage.fillAmount = _value;

            //Check every x frames if game over conditions have been met
            if (Time.frameCount % CheckInterval == 0)
            {
                GameManager.Instance.CheckIfGameOver();
            }
            yield return new WaitForFixedUpdate();
        }
        GameManager.Instance.GameOver(false, true);
    }
    
    //
    public void StartStream()
    {
        GameManager.Instance.PrepareGameForPlayback();
        GameOverScreen.gameObject.SetActive(false);
        //Start Stream
        StartCoroutine(Timer(RoundDuration));
        _frameId = 0;
        _replayCoroutine = StartCoroutine(StartPlayBack());
    }

    IEnumerator StartPlayBack()
    {
        int replayIndex = 0;
        var _actionReplayRecords = GameManager.Instance.ReplayRecords;
        while (replayIndex < _actionReplayRecords.Count)
        {
            if (_frameId == _actionReplayRecords[replayIndex].frameId)
            {
                replayIndex++;
                switch (_actionReplayRecords[replayIndex].eventId)
                {
                    case EventId.Spawn:
                        TroopAI prefab = _invaderSpawnData.GetTroop(_actionReplayRecords[replayIndex].troopType);
                        TroopAI newSpawn = Instantiate(prefab, _actionReplayRecords[replayIndex].position, Quaternion.identity);
                        newSpawn.AssignToTeam(0);
                        Debug.Log("Spawning...");
                        break;
                }
            }

            yield return new WaitForFixedUpdate();
        }
        yield return null;
    }
}
