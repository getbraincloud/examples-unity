using System;
using System.Collections;
using System.Collections.Generic;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class LoadingScreen : MonoBehaviour
{
    public static Stack<IEnumerator> Tasks = new Stack<IEnumerator>(); 

    [SerializeField] private CanvasGroup _canvasGroup;
    
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private TMP_Text _infoText;

    [FormerlySerializedAs("fadeDuration")] [SerializeField] private float _fadeDuration = 0.2f;

    private void Awake()
    {
        UpdateInfo("");
        UpdateProgress(-1);
        StartCoroutine(LoadingScreenFadeIn());
    }
    
    private void CheckForNextTask()
    {
        if(Tasks.TryPop(out IEnumerator nextTask))
        {
            StartCoroutine(RunTask(nextTask));
        }
        else
        {
            StartCoroutine(LoadingScreenFadeOut());
        }
    }

    public void UpdateProgress(float in_progress)
    {
        if(in_progress < 0)
        {
            in_progress = -10;
        }
        UpdateProgress(Mathf.CeilToInt(in_progress * 100));
    }
    
    public void UpdateProgress(int in_progress)
    {
        if (_progressText == null)
            return;
        if(in_progress >= 0)
        {
            _progressText.text = $"({in_progress:D2}%) Loading...";
        }
        else
        {
            _progressText.text = "Loading...";
        }
    }
    
    public void UpdateInfo(string in_info)
    {
        if(in_info.IsNullOrEmpty())
        {
            return;
        }

        _infoText.text = in_info;
    }
    
    private IEnumerator RunTask(IEnumerator in_task)
    {
        while(in_task.MoveNext())
        {
            object current = in_task.Current;
            yield return current;
        }
        
        CheckForNextTask();
    }
    
    private IEnumerator LoadingScreenFadeIn()
    {
        yield return StartCoroutine(LoadingScreenFade(0, 1));
        CheckForNextTask();
    }
    
    private IEnumerator LoadingScreenFadeOut()
    {
        yield return StartCoroutine(LoadingScreenFade(1, 0));
        SceneLoader.RemoveLoadingScreen();
    }
    
    private IEnumerator LoadingScreenFade(float startValue, float targetValue)
    {
        if(_canvasGroup != null)
        {
            float time = 0;
            while(time < _fadeDuration)
            {
                time += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startValue, targetValue, time/_fadeDuration);
                yield return null;
            }            
        }
    }
}
