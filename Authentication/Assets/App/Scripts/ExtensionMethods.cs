using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public static class ExtensionMethods
{
    private const int MAX_GAMEOBJECT_NAME = 32;
    private const string ALPHANUMERIC_CHARACTER_ARRAY = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const string GAMEOBJECT_NAME_CHARACTER_ARRAY = ALPHANUMERIC_CHARACTER_ARRAY + ".-+_()<>[]";
    private static readonly int ANIMATION_TRIGGER_NORMAL = Animator.StringToHash("Normal");
    private static readonly int ANIMATION_TRIGGER_ERROR = Animator.StringToHash("Error");

    /// <summary>
    /// A simple sanitization method to ensure a string only contains alphanumeric characters.
    /// </summary>
    public static string Sanitize(this string str)
        => Sanitize(str, str.Length);

    /// <summary>
    /// A simple sanitization method to ensure a string only contains alphanumeric characters up to a specific length.
    /// </summary>
    /// <param name="maxLength">How many characters the sanitized string can contain. Should be greater than 0.</param>
    public static string Sanitize(this string str, int maxLength)
    {
        maxLength = maxLength > 0 && str.Length < maxLength ? str.Length : maxLength;

        HashSet<char> alphanumeric = new(ALPHANUMERIC_CHARACTER_ARRAY);

        int count = 0;
        StringBuilder result = new(maxLength);
        foreach (char c in str)
        {
            if (alphanumeric.Contains(c))
            {
                result.Append(c);

                if (++count >= maxLength)
                {
                    break;
                }
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Extension method for <see cref="string"/> using <see cref="string.IsNullOrWhiteSpace(string)"/>.
    /// </summary>
    public static bool IsEmpty(this string str)
        => string.IsNullOrWhiteSpace(str);

    /// <summary>
    /// Extension method for all collection types to see if it is null or empty.
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
    {
        return enumerable switch
        {
            null => true,
            ICollection collection => collection.Count == 0,
            _ => !enumerable.Any()
        };
    }

    /// <summary>
    /// Sets a name to the GameObject. This method will sanitize the name in the process to ensure it displays properly in the Unity editor.
    /// </summary>
    /// <param name="name">What to set the GameObject's display name to for the hierarchy window.</param>
    public static void SetName(this GameObject go, string name)
        => go.SetName("{0}", name);

    /// <summary>
    /// Sets a name to the GameObject. Uses a format for the string as well as the arguments to make use of the format, such as count or type.
    /// <para>Note: When passing in the arguments, only the first string will be sanitized, the rest are extra arguments to denote count, type, etc.</para>
    /// </summary>
    /// <param name="format">The format the string should appear in.</param>
    /// <param name="args">The string arguments to use in the format. Note that only the first argument gets sanitized.</param>
    public static void SetName(this GameObject go, string format, params string[] args)
    {
        HashSet<char> alphanumeric = new(GAMEOBJECT_NAME_CHARACTER_ARRAY);

        if (!args[0].IsEmpty())
        {
            int count = 0;
            StringBuilder result = new(MAX_GAMEOBJECT_NAME);
            foreach (char c in args[0])
            {
                if (alphanumeric.Contains(c))
                {
                    result.Append(c);

                    if (++count >= MAX_GAMEOBJECT_NAME)
                    {
                        break;
                    }
                }
            }
            args[0] = result.ToString();
        }

        go.name = string.Format(format, args);
    }

    /// <summary>
    /// Meant to be used alongside Selectables using the <b>BaseAnimator</b> asset. Will set the Selectable's animation to the <b>Normal</b> state.
    /// </summary>
    public static void DisplayNormal(this Selectable selectable)
    {
        if (selectable.animator != null && selectable.interactable)
        {
            selectable.animator.SetTrigger(ANIMATION_TRIGGER_NORMAL);
        }
        else
        {
            Debug.LogWarning($"Selectable {selectable.name} does not have an animator.");
        }
    }

    /// <summary>
    /// Meant to be used alongside Selectables using the <b>BaseAnimator</b> asset. Will set the Selectable's animation to the <b>Error</b> state.
    /// </summary>
    public static void DisplayError(this Selectable selectable)
    {
        if (selectable.animator != null && selectable.interactable)
        {
            selectable.animator.SetTrigger(ANIMATION_TRIGGER_ERROR);
        }
        else
        {
            Debug.LogWarning($"Selectable {selectable.name} does not have an animator.");
        }
    }
}
