using System;

public static class TimeUtils
{
    static public float MAX_UP_TIME = 120.0f;// Should we get this from the 
    /// <summary>
    /// Returns the current epoch time in seconds (with millisecond precision).
    /// </summary>
    public static double GetCurrentTime()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
    }
}
