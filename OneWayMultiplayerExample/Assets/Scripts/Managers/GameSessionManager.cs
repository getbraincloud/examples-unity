using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// This class is specifically for the Game Scene to manage the game flow such as:
///     - Uses a timer to determine when the game is over
///     - Displays Game Over screen
///     - Keeps track of Frame Number for play back purposes
///     - Returns to Main Menu from a button click on Game Over screen. 
///
/// </summary>

public class GameSessionManager : MonoBehaviour
{
    public float RoundDuration;
    public Image ClockFillImage;
    public int CheckInterval = 60;
    public GameOverScreen GameOverScreen;
    public GameObject ConfirmPopUp;
    public GameObject StopStreamButton;
    public GameObject TroopView;
    
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
    private void Start()
    {
        SetupGameSession();
    }

    public void SetupGameSession()
    {
        if (!GameManager.Instance.IsInPlaybackMode)
        {
            GameManager.Instance.GameSetup();
            ClockFillImage.transform.parent.gameObject.SetActive(true);
            ClockFillImage.fillAmount = 1;
            TroopView.SetActive(true);
            StartCoroutine(Timer(RoundDuration)); 
        }
        else
        {
            ClockFillImage.transform.parent.gameObject.SetActive(false);
            StopStreamButton.SetActive(true);
            TroopView.SetActive(false);
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
