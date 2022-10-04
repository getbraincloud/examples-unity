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
    
    private float _startTime;
    private float _time;
    private float _value;
    private int _frameId;
    private bool _replayMode;
    
    
    public int FrameID
    {
        get => _frameId - 10;
    }
    // Start is called before the first frame update
    void Start()
    {
        if (!GameManager.Instance.IsInPlaybackMode)
        {
            GameOverScreen.gameObject.SetActive(false);
            ClockFillImage.fillAmount = 1;
            StartCoroutine(Timer(RoundDuration)); 
        }
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
        GameManager.Instance.IsInPlaybackMode = false;
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
}
