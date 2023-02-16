using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ExtensionMethods
{
    private static readonly int ANIMATION_TRIGGER_ERROR = Animator.StringToHash("Error");
    private const string ALPHANUMERIC_CHARACTER_ARRAY = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    /// <summary>
    /// A simple sanitization method to ensure a string only contains alphanumeric characters.
    /// </summary>
    public static string Sanitize(this string str) => Sanitize(str, str.Length);

    /// <summary>
    /// A simple sanitization method to ensure a string only contains alphanumeric characters up to a specific length.
    /// </summary>
    /// <param name="maxLength">How many characters the sanitized string can contain. Should be greater than 0.</param>
    public static string Sanitize(this string str, int maxLength)
    {
        maxLength = maxLength > 0 && str.Length < maxLength ? str.Length : maxLength;

        HashSet<char> alphanumeric = new HashSet<char>(ALPHANUMERIC_CHARACTER_ARRAY);

        int count = 0;
        StringBuilder result = new StringBuilder(maxLength);
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
    public static bool IsNullOrEmpty(this string str) => string.IsNullOrWhiteSpace(str);

    /// <summary>
    /// Extension method for all collection types to see if it is null or empty.
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
    {
        return enumerable switch
        {
            null => true,
            ICollection<T> collection => collection.Count == 0,
            _ => !enumerable.Any()
        };
    }

    /// <summary>
    /// Sets a name to the GameObject. This method will sanitize the name in the process to ensure it displays properly in the Unity editor.
    /// </summary>
    /// <param name="name">What to set the GameObject's display name to for the hierarchy window.</param>
    /// <param name="format">Optional format. This will not be truncated during the sanitization process.</param>
    public static GameObject SetName(this GameObject go, string name, string format = "{0}")
    {
        name = name.Sanitize(32);
        go.name = string.Format(format, name);

        return go;
    }

    /// <summary>
    /// Meant to be used alongside Selectables using the <b>BaseAnimator</b> asset. Will set the Selectable's animation to the <b>Error</b> state.
    /// </summary>
    public static Selectable DisplayError(this Selectable selectable)
    {
        if (selectable.animator != null)
        {
            selectable.animator.SetTrigger(ANIMATION_TRIGGER_ERROR);
        }
        else
        {
            Debug.LogWarning($"Selectable {selectable.name} does not have an animator.");
        }

        return selectable;
    }
}
