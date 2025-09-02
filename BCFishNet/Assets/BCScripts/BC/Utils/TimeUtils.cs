using System;

public static class TimeUtils
{
    static public float MAX_UP_TIME = 60.0f;// Should we get this from the 
    static public float SHUT_DOWN_TIME = 3.0f;// Should we get this from the 
    static public float DELAY = 0.15f;
    static public float SHORT_DELAY = 0.05f;
    static public float ECHO_INTERVAL = DELAY * 5; // secondss
    /// <summary>
    /// Returns the current epoch time in seconds (with millisecond precision).
    /// </summary>
    public static double GetCurrentTime()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
    }
}
