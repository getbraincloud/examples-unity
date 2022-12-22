using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSessionManager : MonoBehaviour
{
    public float RoundDuration;
    public Image ClockFillImage;
    public int CheckInterval = 60;
    public GameOverScreen GameOverScreen;
    public GameObject ConfirmPopUp;
    public GameObject StopStreamButton;
    
    private float _startTime;
    private float _gameSessionTimer;
    private float _value;
    private int _frameId;
    private bool _replayMode;

    public float GameSessionTimer
    {
        get => _gameSessionTimer;
        set => _gameSessionTimer = value;
    }
    
    public int FrameID
    {
        get => _frameId;
    }

    private void Awake()
    {
        ConfirmPopUp.SetActive(false);
        StopStreamButton.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!GameManager.Instance.IsInPlaybackMode)
        {
            GameManager.Instance.GameSetup();
            ClockFillImage.fillAmount = 1;
            StartCoroutine(Timer(RoundDuration)); 
        }
        else
        {
            StopStreamButton.SetActive(true);
        }
    }

    private void FixedUpdate()
    {
        if (!_replayMode)
        {
            _frameId++;
        }
    }

    public void OpenConfirmPopUp()
    {
        ConfirmPopUp.SetActive(true);
    }

    public void StopTimer()
    {
        if (_gameSessionTimer > 0.0f)
        {
            GameOverScreen.TimerText.text = $"Time Remaining: {(int)_gameSessionTimer} seconds";
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
        _gameSessionTimer = duration;
        _value = 1;

        while (Time.time - _startTime < duration)
        {
            _gameSessionTimer -= Time.deltaTime;
            _value = _gameSessionTimer / duration;
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
