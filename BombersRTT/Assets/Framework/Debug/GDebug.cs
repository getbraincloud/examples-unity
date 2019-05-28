#define CONSOLE_WRITE
using UnityEngine;

namespace Gameframework
{
    public static class GDebug
    {
        //
        // Static Methods
        [System.Diagnostics.Conditional("DEBUG_LOG_ENABLED")]
        public static void Log(object message)
        {
#if !CONSOLE_WRITE
            Debug.Log(message);
#else
            System.Console.WriteLine(message);
#endif
        }

        [System.Diagnostics.Conditional("DEBUG_LOG_ENABLED")]
        public static void Log(object message, Object context)
        {
            Debug.Log(message, context);
        }

        [System.Diagnostics.Conditional("DEBUG_LOG_ENABLED")]
        public static void LogError(object message)
        {
#if !CONSOLE_WRITE
            Debug.LogError(message);
#else
            System.Console.WriteLine(message);
#endif
        }

        [System.Diagnostics.Conditional("DEBUG_LOG_ENABLED")]
        public static void LogError(object message, Object context)
        {
            Debug.LogError(message, context);
        }

        [System.Diagnostics.Conditional("DEBUG_LOG_ENABLED")]
        public static void LogErrorFormat(string format, params object[] args)
        {
            Debug.LogErrorFormat(format, args);
        }

        [System.Diagnostics.Conditional("DEBUG_LOG_ENABLED")]
        public static void LogErrorFormat(Object context, string format, params object[] args)
        {
            Debug.LogErrorFormat(context, format, args);
        }

        [System.Diagnostics.Conditional("DEBUG_LOG_ENABLED")]
        public static void LogException(System.Exception exception) 
        {
#if !CONSOLE_WRITE
            Debug.LogException(exception);
#else
            System.Console.WriteLine(exception);
#endif
        }

        [System.Diagnostics.Conditional("DEBUG_LOG_ENABLED")]
        public static void LogException(System.Exception exception, Object context) 
        {
            Debug.LogException(exception, context);
        }

        [System.Diagnostics.Conditional("DEBUG_LOG_ENABLED")]
        public static void LogFormat(string format, params object[] args) 
        {
            Debug.LogFormat(format, args);
        }

        [System.Diagnostics.Conditional("DEBUG_LOG_ENABLED")]
        public static void LogFormat(Object context, string format, params object[] args) 
        {
            Debug.LogFormat(context, format, args);
        }

        [System.Diagnostics.Conditional("DEBUG_LOG_ENABLED")]
        public static void LogWarning(object message) 
        {
#if !CONSOLE_WRITE
            Debug.LogWarning(message);
#else
            System.Console.WriteLine(message);
#endif
        }

        [System.Diagnostics.Conditional("DEBUG_LOG_ENABLED")]
        public static void LogWarning(object message, Object context) 
        {
            Debug.LogWarning(message, context);
        }

        [System.Diagnostics.Conditional("DEBUG_LOG_ENABLED")]
        public static void LogWarningFormat(string format, params object[] args)
        {
            Debug.LogWarningFormat(format, args);
        }

        [System.Diagnostics.Conditional("DEBUG_LOG_ENABLED")]
        public static void LogWarningFormat(Object context, string format, params object[] args)
        {
            Debug.LogWarningFormat(context, format, args);
        }
    }
}
