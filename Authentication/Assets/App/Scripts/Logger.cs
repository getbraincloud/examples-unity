using System;

public static class Logger
{
    private static Action<string> logMessageAction = default;
    private static Action<string> logErrorAction = default;

    public static void SetLoggerMethods(Action<string> logMessageMethod, Action<string> logErrorMethod)
    {
        logMessageAction = logMessageMethod;
        logErrorAction = logErrorMethod;
    }

    public static void ClearLoggerMethods()
    {
        logMessageAction = null;
        logErrorAction = null;
    }

    public static void LogMessage(string message) =>
        logMessageAction?.Invoke(message);

    public static void LogError(string message) =>
        logErrorAction?.Invoke(message);
}
