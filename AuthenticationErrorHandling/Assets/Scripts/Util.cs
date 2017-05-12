/**
 * Util helpers
 */

using UnityEngine;

public abstract class Util
{
#if UNITY_IOS || UNITY_ANDROID || UNITY_WP_8_1
    private const int MIN_SIZE = 32;
#else
    private const int MIN_SIZE = 0;
#endif

    public static bool Toggle(bool withState, string withText)
    {
        return GUILayout.Toggle(withState, withText, GUILayout.MinHeight(MIN_SIZE), GUILayout.MinWidth(MIN_SIZE));
    }

    public static bool Button(string withText)
    {
        return GUILayout.Button(withText, GUILayout.MinHeight(MIN_SIZE), GUILayout.MinWidth(MIN_SIZE));
    }

    public static string TextField(string withText)
    {
        return GUILayout.TextField(withText, GUILayout.MinHeight(MIN_SIZE), GUILayout.MinWidth(MIN_SIZE));
    }
}