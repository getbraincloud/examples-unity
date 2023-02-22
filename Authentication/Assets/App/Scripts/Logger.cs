using System;

public static class Logger
{
    public static Action<string> OnLogMessage = default;
    public static Action<string> OnLogWarning = default;
    public static Action<string> OnLogError = default;

    public static void ResetLogger()
    {
        OnLogMessage = null;
        OnLogWarning = null;
        OnLogError = null;
    }

    public static void Log(string message) => OnLogMessage?.Invoke(message);

    public static void Warning(string message) => OnLogWarning?.Invoke(message);

    public static void Error(string message) => OnLogError?.Invoke(message);
}
