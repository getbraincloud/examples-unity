using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InactivityTimer : MonoBehaviour
{
    [SerializeField] private TMP_Text countdownString;
    [SerializeField] private TMP_Text countdownStringParent;
    [SerializeField] private float inactivityDuration = 3f;
    private float timer = 0f;
    private float visibleCountdown = 57f;
    private Vector3 lastMousePosition;
    private bool isInactive = false;

    private void Start()
    {
        lastMousePosition = Input.mousePosition;
        timer = inactivityDuration;
        countdownStringParent = countdownString.transform.parent.GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (Input.mousePosition != lastMousePosition)
        {
            timer = inactivityDuration;
            lastMousePosition = Input.mousePosition;
            if (isInactive)
            {
                isInactive = false;
                visibleCountdown = 57f;
            }
        }
        else
        {
            timer -= Time.deltaTime;
            if (timer <= 0f && !isInactive)
            {
                isInactive = true;
                visibleCountdown = 57f;
            }
        }

        if (!isInactive)
        {
            if (countdownString != null)
                countdownString.text = "";
            if (countdownStringParent != null)
                countdownStringParent.text = "";
        }
        else
        {
            if (visibleCountdown > 0f)
            {
                visibleCountdown -= Time.deltaTime;
                if (visibleCountdown < 0f) visibleCountdown = 0f;
            }
            if (countdownString != null)
                countdownString.text = TimerUtils.FormatTime(visibleCountdown);
            if (countdownStringParent != null)
                countdownStringParent.text = "Disconnecting in";
        }
    }
}
