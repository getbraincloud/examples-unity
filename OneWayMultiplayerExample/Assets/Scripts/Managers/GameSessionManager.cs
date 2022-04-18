using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameSessionManager : MonoBehaviour
{
    public float RoundDuration;
    public Image ClockFillImage;
    
    // Start is called before the first frame update
    void Start()
    {
        ClockFillImage.fillAmount = 1;
        StartCoroutine(Timer(RoundDuration));
    }

    public IEnumerator Timer(float duration)
    {
        float startTime = Time.time;
        float time = duration;
        float value = 1;

        while (Time.time - startTime < duration)
        {
            time -= Time.deltaTime;
            value = time / duration;
            ClockFillImage.fillAmount = value;
            yield return new WaitForFixedUpdate();
        }
    }
}
