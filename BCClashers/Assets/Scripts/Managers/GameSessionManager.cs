using System.Collections;
using System.Collections.Generic;
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
    public TMPro.TMP_Text CountdownText; 
    public int CheckInterval = 60;
    public GameOverScreen GameOverScreen;
    public GameObject ConfirmPopUp;
    public GameObject StopStreamButton;
    public GameObject TroopView;
    public RectTransform ButtonBorder;
    public GameObject SurrenderButton;

    private readonly List<float> _selectionXPlacement = new List<float> {-149, 0.5f, 149};
    private float _startTime;
    private float _gameSessionTimer;
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

        float minutes = Mathf.FloorToInt(RoundDuration / 60);
        float seconds = Mathf.FloorToInt(RoundDuration % 60);
        CountdownText.text = $"{minutes:00}:{seconds:00}";
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
            SurrenderButton.SetActive(true);
            GameManager.Instance.GameSetup();
            TroopView.SetActive(true);
        }
        else
        {
            SurrenderButton.SetActive(false);
            StopStreamButton.SetActive(true);
            TroopView.SetActive(false);
        }

        StartCoroutine(Timer(RoundDuration)); 
    }

    public void UpdateBorderPosition(int index)
    {
        Vector2 pos = ButtonBorder.anchoredPosition;
        pos.y = 12;
        pos.x = _selectionXPlacement[index];
        ButtonBorder.anchoredPosition = pos;
    }
    
    private void FixedUpdate()
    {
        if (!_replayMode)
        {
            _frameId++;
        }
    }

    public void Surrender()
    {
        GameManager.Instance.GameOver(false);
    }

    public void OpenConfirmPopUp()
    {
        ConfirmPopUp.SetActive(true);
    }

    public void StopTimer()
    {
        if (_gameSessionTimer > 0.0f)
        {
            float minutes = Mathf.FloorToInt(_gameSessionTimer / 60);
            float seconds = Mathf.FloorToInt(_gameSessionTimer % 60);
            GameOverScreen.TimerText.text = $"Time Remaining: {minutes:00}:{seconds:00}";
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

        while (Time.time - _startTime < duration)
        {
            _gameSessionTimer -= Time.deltaTime;
            if (_gameSessionTimer <= 0)
            {
                _gameSessionTimer = 0;
                CountdownText.text = $"0:00";
            }
            else
            {
                float minutes = Mathf.FloorToInt(_gameSessionTimer / 60);
                float seconds = Mathf.FloorToInt(_gameSessionTimer % 60);
                CountdownText.text = $"{minutes:00}:{seconds:00}";
            }
            
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
