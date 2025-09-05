using UnityEngine;

public static class TimerUtils
{
    /// <summary>
    /// Formats a float time value into MM:SS.mmm format.
    /// </summary>
    public static string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60F);
        int seconds = Mathf.FloorToInt(time % 60F);
        //int milliseconds = Mathf.FloorToInt((time * 1000F) % 1000F);
        return string.Format("{0:00}:{1:00}", minutes, seconds);//, milliseconds);
    }
}
